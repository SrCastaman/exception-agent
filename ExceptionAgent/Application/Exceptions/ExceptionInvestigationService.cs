using ExceptionAgent.Contracts;
using ExceptionAgent.Data;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Application.Exceptions;

public class ExceptionInvestigationService
{
    private readonly AppDbContext _context;
    private readonly ExceptionRiskCalculationService _riskCalculationService;

    public ExceptionInvestigationService(
        AppDbContext context,
        ExceptionRiskCalculationService riskCalculationService)
    {
        _context = context;
        _riskCalculationService = riskCalculationService;
    }

    public async Task<ExceptionInvestigation?> InvestigateAsync(
        int exceptionId)
    {
        var exception = await _context.OperationalExceptions
            .FirstOrDefaultAsync(e => e.Id == exceptionId);

        if (exception == null)
        {
            return null;
        }

        var purchaseOrder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == exception.PurchaseOrderId);

        if (purchaseOrder == null)
        {
            return null;
        }

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == purchaseOrder.SupplierId);

        if (supplier == null)
        {
            return null;
        }

        var purchaseOrderLines = await _context.PurchaseOrderLines
            .Where(l => l.PurchaseOrderId == purchaseOrder.Id)
            .ToListAsync();

        var productIds = purchaseOrderLines
            .Select(l => l.ProductId)
            .Distinct()
            .ToList();

        var inventory = await _context.Inventories
            .Where(i => productIds.Contains(i.ProductId))
            .ToListAsync();

        var customerOrders = await _context.CustomerOrders
            .Where(c => productIds.Contains(c.ProductId))
            .ToListAsync();

        var supplierEmailEvents = await _context.SupplierEmailEvents
            .Where(e => e.PurchaseOrderId == purchaseOrder.Id)
            .OrderByDescending(e => e.Id)
            .ToListAsync();

        var supplierEmailIds = supplierEmailEvents
            .Select(e => e.EmailId)
            .Distinct()
            .ToList();

        var supplierEmails = await _context.Emails
            .Where(e => supplierEmailIds.Contains(e.Id))
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        return new ExceptionInvestigation
        {
            Exception = exception,
            PurchaseOrder = purchaseOrder,
            Supplier = supplier,
            PurchaseOrderLines = purchaseOrderLines,
            Inventory = inventory,
            CustomerOrders = customerOrders,
            SupplierEmails = supplierEmails,
            SupplierEmailEvents = supplierEmailEvents
        };
    }

    public async Task<InvestigationContext> BuildContextAsync(
        ExceptionInvestigation investigation)
    {
        var orderedQuantity = investigation.PurchaseOrderLines
            .Sum(line => line.OrderedQuantity);

        var receivedQuantity = investigation.PurchaseOrderLines
            .Sum(line => line.ReceivedQuantity);

        var pendingQuantity = orderedQuantity - receivedQuantity;

        var latestEmailEvent = investigation.SupplierEmailEvents
            .Where(e => e.NewExpectedDate.HasValue)
            .OrderByDescending(e => e.Id)
            .FirstOrDefault();

        var updatedExpectedDate =
            latestEmailEvent?.NewExpectedDate;

        var riskCalculation =
            await _riskCalculationService.CalculateAsync(
                investigation.PurchaseOrder.Id);

        return new InvestigationContext
        {
            Exception = new ExceptionContext
            {
                Type = investigation.Exception.Type,
                Status = investigation.Exception.Status
            },

            PurchaseOrder = new PurchaseOrderContext
            {
                Reference = investigation.PurchaseOrder.Reference,
                OrderDate = investigation.PurchaseOrder.OrderDate,
                ExpectedDate = investigation.PurchaseOrder.ExpectedDate,
                UpdatedExpectedDate = updatedExpectedDate,
                Status = investigation.PurchaseOrder.Status,
                OrderedQuantity = orderedQuantity,
                ReceivedQuantity = receivedQuantity,
                PendingQuantity = pendingQuantity
            },

            Supplier = new SupplierContext
            {
                Name = investigation.Supplier.Name
            },

            Inventory = investigation.Inventory
                .Select(i => new InventoryContext
                {
                    ProductId = i.ProductId,
                    AvailableQuantity = i.AvailableQuantity
                })
                .ToList(),

            CustomerOrders = riskCalculation.CustomerOrders,

            Emails = investigation.SupplierEmails
                .Select(e => new EmailContext
                {
                    Sender = e.Sender,
                    Subject = e.Subject,
                    Date = e.Date,
                    Body = e.Body
                })
                .ToList(),

            CalculatedSeverity =
                riskCalculation.CalculatedSeverity,

            TotalShortageQuantity =
                riskCalculation.TotalShortageQuantity,

            RiskDate =
                riskCalculation.RiskDate
        };
    }
}