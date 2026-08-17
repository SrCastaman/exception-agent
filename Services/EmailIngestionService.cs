using ExceptionAgent.Data;
using ExceptionAgent.Models;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Services;

public class EmailIngestionService
{
    private readonly AppDbContext _context;
    private readonly EmailExtractionService _emailExtractionService;

    public EmailIngestionService(
        AppDbContext context,
        EmailExtractionService emailExtractionService)
    {
        _context = context;
        _emailExtractionService = emailExtractionService;
    }

    public async Task ProcessEmailsAsync()
    {
        var emails = await _context.Emails
            .OrderBy(e => e.Date)
            .ToListAsync();

        foreach (var email in emails)
        {
            var alreadyProcessed = await _context.SupplierEmailEvents
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

            if (string.IsNullOrWhiteSpace(
                extraction.PurchaseOrderReference))
            {
                continue;
            }

            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(p =>
                    p.Reference == extraction.PurchaseOrderReference);

            if (purchaseOrder == null)
            {
                continue;
            }

            if (purchaseOrder.SupplierId != email.SupplierId)
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

            await _context.SaveChangesAsync();
        }
    }
}