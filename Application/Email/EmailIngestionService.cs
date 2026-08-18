using ExceptionAgent.Contracts.Email;
using ExceptionAgent.Data;
using ExceptionAgent.Domain.Entities;
using ExceptionAgent.Domain.Enums;
using ExceptionAgent.Infraestructure.AI;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Aplication.Email;

public class EmailIngestionService
{
    private readonly AppDbContext _context;
    private readonly EmailExtractionService _emailExtractionService;
    private readonly EmailMatchingService _emailMatchingService;

    public EmailIngestionService(
        AppDbContext context,
        EmailExtractionService emailExtractionService,
        EmailMatchingService emailMatchingService)
    {
        _context = context;
        _emailExtractionService = emailExtractionService;
        _emailMatchingService = emailMatchingService;
    }

    public async Task ProcessEmailsAsync()
    {
        var emails = await _context.Emails
            .OrderBy(e => e.Date)
            .ToListAsync();

        foreach (var email in emails)
        {
            var alreadyProcessed = await _context.EmailProcessingResults
                .AnyAsync(e => e.EmailId == email.Id);

            if (alreadyProcessed)
            {
                continue;
            }

            var extraction =
                await _emailExtractionService.ExtractAsync(email);

            if (extraction == null)
            {
                continue;
            }

            PurchaseOrder? purchaseOrder = null;
            EmailMatchResult? match = null;

            if (!string.IsNullOrWhiteSpace(
                extraction.PurchaseOrderReference))
            {
                purchaseOrder = await _context.PurchaseOrders
                    .FirstOrDefaultAsync(p =>
                        p.Reference == extraction.PurchaseOrderReference &&
                        p.SupplierId == email.SupplierId);
            }
            else
            {
                match = await _emailMatchingService.FindPurchaseOrderAsync(
                    email,
                    extraction);

                if (match == null || !match.Matched)
                {
                    var processingResult = new EmailProcessingResult
                    {
                        EmailId = email.Id,
                        MatchingStatus = EmailMatchingStatus.PendingReview,
                        PurchaseOrderId = null,
                        MatchScore = match?.Score ?? 0,
                        MatchingReason = match != null
                            ? string.Join(" ", match.Reasons)
                            : "No se pudo determinar un pedido de compra.",

                        EventType = extraction.EventType,
                        NewExpectedDate = extraction.NewExpectedDate,
                        AffectedQuantity = extraction.AffectedQuantity,
                        Evidence = extraction.Evidence,

                        ProcessedAt = DateTime.UtcNow
                    };

                    foreach (var candidate in match?.Candidates
                        ?? new List<EmailMatchCandidateResult>())
                    {
                        processingResult.Candidates.Add(
                            new EmailMatchCandidate
                            {
                                PurchaseOrderId = candidate.PurchaseOrderId,
                                Score = candidate.Score,
                                Reasons = string.Join(" ", candidate.Reasons)
                            });
                    }

                    _context.EmailProcessingResults.Add(processingResult);

                    await _context.SaveChangesAsync();

                    continue;
                }

                purchaseOrder = await _context.PurchaseOrders
                    .FirstOrDefaultAsync(p =>
                        p.Id == match.PurchaseOrderId);
            }

            if (purchaseOrder == null)
            {
                continue;
            }

            var emailEvent = new SupplierEmailEvent
            {
                EmailId = email.Id,
                PurchaseOrderId = purchaseOrder.Id,
                EventType = extraction.EventType,
                NewExpectedDate = extraction.NewExpectedDate,
                AffectedQuantity = extraction.AffectedQuantity,
                Evidence = extraction.Evidence
            };

            _context.SupplierEmailEvents.Add(emailEvent);

            var processingResultMatched = new EmailProcessingResult
            {
                EmailId = email.Id,

                MatchingStatus =
                    string.IsNullOrWhiteSpace(
                        extraction.PurchaseOrderReference)
                        ? EmailMatchingStatus.Matched
                        : EmailMatchingStatus.NotRequired,

                PurchaseOrderId = purchaseOrder.Id,

                MatchScore =
                    match?.Score ?? 1.0,

                MatchingReason =
                    string.IsNullOrWhiteSpace(
                        extraction.PurchaseOrderReference)
                        ? string.Join(
                            " ",
                            match?.Reasons ??
                            new List<string>
                            {
                            "Pedido identificado mediante matching."
                            })
                        : "Pedido identificado mediante referencia explícita.",

                ProcessedAt = DateTime.UtcNow


            };

            _context.EmailProcessingResults.Add(
                processingResultMatched);

            await _context.SaveChangesAsync();
        }
    }
}