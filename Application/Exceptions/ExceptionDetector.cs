using ExceptionAgent.Data;
using Microsoft.EntityFrameworkCore;
using ExceptionAgent.Domain.Entities;

namespace ExceptionAgent.Aplication.Exceptions;

public class ExceptionDetector
{
    private readonly AppDbContext _context;

    public ExceptionDetector(AppDbContext context)
    {
        _context = context;
    }

    public async Task DetectDelayedPurchaseOrdersAsync()
    {
        var today = DateTime.Today;

        var purchaseOrders = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .ToListAsync();

        foreach (var purchaseOrder in purchaseOrders)
        {
            var totalOrdered = purchaseOrder.Lines
                .Sum(line => line.OrderedQuantity);

            var totalReceived = purchaseOrder.Lines
                .Sum(line => line.ReceivedQuantity);

            var hasPendingQuantity = totalReceived < totalOrdered;
            var isOverdue = purchaseOrder.ExpectedDate < today;

            if (!isOverdue || !hasPendingQuantity)
            {
                continue;
            }

            var exceptionAlreadyExists = await _context.OperationalExceptions
                .AnyAsync(e =>
                    e.PurchaseOrderId == purchaseOrder.Id &&
                    e.Type == "PURCHASE_ORDER_DELAY" &&
                    e.Status == "OPEN");

            if (exceptionAlreadyExists)
            {
                continue;
            }

            var operationalException = new OperationalException
            {
                Type = "PURCHASE_ORDER_DELAY",
                PurchaseOrderId = purchaseOrder.Id,
                Status = "OPEN",
                CreatedAt = DateTime.Now
            };

            _context.OperationalExceptions.Add(operationalException);
        }

        await _context.SaveChangesAsync();
    }
}