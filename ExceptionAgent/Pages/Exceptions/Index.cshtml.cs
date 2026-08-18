using ExceptionAgent.Application.Exceptions;
using ExceptionAgent.Domain.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Pages.Exceptions;

public class IndexModel : PageModel
{
    private readonly ExceptionDetector _exceptionDetector;
    private readonly Data.AppDbContext _context;
    private readonly ExceptionRiskCalculationService _riskCalculationService;

    public List<OperationalException> Exceptions { get; set; } = new();

    public IndexModel(
        ExceptionDetector exceptionDetector,
        Data.AppDbContext context,
        ExceptionRiskCalculationService riskCalculationService)
    {
        _exceptionDetector = exceptionDetector;
        _context = context;
        _riskCalculationService = riskCalculationService;
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

        foreach (var exception in Exceptions)
        {
            if (exception.PurchaseOrder == null)
            {
                continue;
            }

            var purchaseOrderLines = await _context.PurchaseOrderLines
                .Where(line =>
                    line.PurchaseOrderId == exception.PurchaseOrderId)
                .ToListAsync();

            var productIds = purchaseOrderLines
                .Select(line => line.ProductId)
                .Distinct()
                .ToList();

            var inventory = await _context.Inventories
                .Where(i => productIds.Contains(i.ProductId))
                .ToListAsync();

            var customerOrders = await _context.CustomerOrders
                .Where(c => productIds.Contains(c.ProductId))
                .ToListAsync();

            var supplierEmailEvents = await _context.SupplierEmailEvents
                .Where(e =>
                    e.PurchaseOrderId == exception.PurchaseOrderId)
                .ToListAsync();

            var latestEmailEvent = supplierEmailEvents
                .Where(e => e.NewExpectedDate.HasValue)
                .OrderByDescending(e => e.Id)
                .FirstOrDefault();

            var supplierExpectedDate =
                latestEmailEvent?.NewExpectedDate
                ?? exception.PurchaseOrder.ExpectedDate;

            var riskCalculation =
                await _riskCalculationService.CalculateAsync(
                    exception.PurchaseOrderId);

            exception.Severity =
                riskCalculation.CalculatedSeverity;
        }
    }
}