using ExceptionAgent.Contracts;
using ExceptionAgent.Contracts.Email;
using ExceptionAgent.Data;
using ExceptionAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using EmailEntity = ExceptionAgent.Domain.Entities.Email;

namespace ExceptionAgent.Application.Email;

public class EmailMatchingService
{
    private readonly AppDbContext _context;

    public EmailMatchingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmailMatchResult?> FindPurchaseOrderAsync(
    EmailEntity email,
    SupplierEmailExtractionResult extraction)
    {
        var purchaseOrders = await _context.PurchaseOrders
            .Where(p => p.SupplierId == email.SupplierId)
            .ToListAsync();

        if (!purchaseOrders.Any())
        {
            return null;
        }

        var purchaseOrderIds = purchaseOrders
            .Select(p => p.Id)
            .ToList();

        var purchaseOrderLines = await _context.PurchaseOrderLines
            .Where(line => purchaseOrderIds.Contains(line.PurchaseOrderId))
            .ToListAsync();

        var linesByPurchaseOrder = purchaseOrderLines
            .GroupBy(line => line.PurchaseOrderId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        var candidates = new List<Candidate>();

        foreach (var purchaseOrder in purchaseOrders)
        {
            var lines = linesByPurchaseOrder.TryGetValue(
                purchaseOrder.Id,
                out var orderLines)
                ? orderLines
                : new List<PurchaseOrderLine>();

            var pendingQuantity = lines
                .Sum(line =>
                    Math.Max(
                        0,
                        line.OrderedQuantity - line.ReceivedQuantity));

            var score = 0.0;
            var reasons = new List<string>();

            if (extraction.AffectedQuantity.HasValue &&
                pendingQuantity == extraction.AffectedQuantity.Value)
            {
                score += 0.7;

                reasons.Add(
                    $"La cantidad afectada ({extraction.AffectedQuantity.Value}) " +
                    $"coincide con las unidades pendientes del pedido ({pendingQuantity}).");
            }

            if (extraction.NewExpectedDate.HasValue &&
                purchaseOrder.ExpectedDate < extraction.NewExpectedDate.Value)
            {
                score += 0.2;

                reasons.Add(
                    $"La fecha prevista original del pedido " +
                    $"({purchaseOrder.ExpectedDate:dd/MM/yyyy}) " +
                    $"es anterior a la nueva fecha comunicada " +
                    $"({extraction.NewExpectedDate.Value:dd/MM/yyyy}).");
            }

            const double minimumScore = 0.7;

            if (score >= minimumScore)
            {
                candidates.Add(new Candidate
                {
                    PurchaseOrder = purchaseOrder,
                    Score = score,
                    Reasons = reasons
                });
            }
        }

        var candidateResults = candidates
            .OrderByDescending(candidate => candidate.Score)
            .Select(candidate => new EmailMatchCandidateResult
            {
                PurchaseOrderId = candidate.PurchaseOrder.Id,
                PurchaseOrderReference = candidate.PurchaseOrder.Reference,
                Score = candidate.Score,
                Reasons = candidate.Reasons
            })
            .ToList();

        if (!candidates.Any())
        {
            return new EmailMatchResult
            {
                Matched = false,
                Reasons =
            {
                "No se encontraron pedidos candidatos."
            },
                Candidates = candidateResults
            };
        }

        var orderedCandidates = candidates
            .OrderByDescending(candidate => candidate.Score)
            .ToList();

        var bestCandidate = orderedCandidates[0];

        if (orderedCandidates.Count > 1 &&
            orderedCandidates[1].Score == bestCandidate.Score)
        {
            return new EmailMatchResult
            {
                Matched = false,
                Score = bestCandidate.Score,
                Reasons =
            {
                "Existe más de un pedido candidato con la misma puntuación."
            },
                Candidates = candidateResults
            };
        }

        return new EmailMatchResult
        {
            Matched = true,
            PurchaseOrderId = bestCandidate.PurchaseOrder.Id,
            PurchaseOrderReference = bestCandidate.PurchaseOrder.Reference,
            Score = bestCandidate.Score,
            Reasons = bestCandidate.Reasons,
            Candidates = candidateResults
        };
    }

    private class Candidate
    {
        public PurchaseOrder PurchaseOrder { get; set; } = null!;

        public double Score { get; set; }

        public List<string> Reasons { get; set; } = new();
    }
}