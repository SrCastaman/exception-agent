using ExceptionAgent.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ExceptionAgent.Domain.Entities;
using ExceptionAgent.Domain.Enums;

namespace ExceptionAgent.Pages.Emails;

public class PendingModel : PageModel
{
    private readonly AppDbContext _context;

    public PendingModel(AppDbContext context)
    {
        _context = context;
    }

    public List<EmailProcessingResult> PendingEmails { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadPendingEmailsAsync();
    }

    public async Task<IActionResult> OnPostAssignAsync(
    int processingResultId,
    int purchaseOrderId)
    {
        var processingResult = await _context.EmailProcessingResults
            .Include(result => result.Email)
            .Include(result => result.Candidates)
            .FirstOrDefaultAsync(result =>
                result.Id == processingResultId &&
                result.MatchingStatus == EmailMatchingStatus.PendingReview);

        if (processingResult == null)
        {
            return NotFound();
        }

        var candidate = processingResult.Candidates
            .FirstOrDefault(c =>
                c.PurchaseOrderId == purchaseOrderId);

        if (candidate == null)
        {
            return BadRequest();
        }

        var purchaseOrder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p =>
                p.Id == purchaseOrderId);

        if (purchaseOrder == null)
        {
            return NotFound();
        }

        var emailEvent = new SupplierEmailEvent
        {
            EmailId = processingResult.EmailId,
            PurchaseOrderId = purchaseOrder.Id,
            EventType = processingResult.EventType,
            NewExpectedDate = processingResult.NewExpectedDate,
            AffectedQuantity = processingResult.AffectedQuantity,
            Evidence = processingResult.Evidence
        };

        _context.SupplierEmailEvents.Add(emailEvent);

        processingResult.MatchingStatus =
            EmailMatchingStatus.Matched;

        processingResult.PurchaseOrderId =
            purchaseOrder.Id;

        processingResult.MatchScore =
            candidate.Score;

        processingResult.MatchingReason =
            "Asociado manualmente por un usuario.";

        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

    private async Task LoadPendingEmailsAsync()
    {
        PendingEmails = await _context.EmailProcessingResults
            .Include(result => result.Email)
            .Include(result => result.Candidates)
                .ThenInclude(candidate => candidate.PurchaseOrder)
            .Where(result =>
                result.MatchingStatus == EmailMatchingStatus.PendingReview)
            .OrderByDescending(result => result.ProcessedAt)
            .ToListAsync();
    }
}