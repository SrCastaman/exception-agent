using ExceptionAgent.Models;
using ExceptionAgent.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Pages.Exceptions;

public class IndexModel : PageModel
{
    private readonly ExceptionDetector _exceptionDetector;
    private readonly Data.AppDbContext _context;

    public List<OperationalException> Exceptions { get; set; } = new();

    public IndexModel(
        ExceptionDetector exceptionDetector,
        Data.AppDbContext context)
    {
        _exceptionDetector = exceptionDetector;
        _context = context;
    }

    public async Task OnGetAsync()
    {
        await LoadExceptionsAsync();
    }

    public async Task OnPostAsync()
    {
        await _exceptionDetector.DetectDelayedPurchaseOrdersAsync();

        await LoadExceptionsAsync();
    }

    private async Task LoadExceptionsAsync()
    {
        Exceptions = await _context.OperationalExceptions
            .Include(e => e.PurchaseOrder)
            .ToListAsync();
    }
}