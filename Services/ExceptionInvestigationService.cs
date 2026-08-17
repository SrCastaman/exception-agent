using ExceptionAgent.Data;
using ExceptionAgent.Models;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Services;

public class ExceptionInvestigationService
{
    private readonly AppDbContext _context;

    public ExceptionInvestigationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ExceptionInvestigation?> InvestigateAsync(int exceptionId)
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

        var supplierEmails = await _context.Emails
            .Where(e => e.SupplierId == supplier.Id)
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
            SupplierEmails = supplierEmails
        };
    }

    public InvestigationContext BuildContext(ExceptionInvestigation investigation)
    {
        var orderedQuantity = investigation.PurchaseOrderLines
            .Sum(line => line.OrderedQuantity);

        var receivedQuantity = investigation.PurchaseOrderLines
            .Sum(line => line.ReceivedQuantity);

        var pendingQuantity = orderedQuantity - receivedQuantity;

        var relevantProductIds = investigation.PurchaseOrderLines
            .Select(line => line.ProductId)
            .Distinct()
            .ToList();

        var relevantCustomerOrders = investigation.CustomerOrders
            .Where(order => relevantProductIds.Contains(order.ProductId))
            .ToList();

        return new InvestigationContext
        {
            Exception = new ExceptionContext
            {
                Type = investigation.Exception.Type,
                Severity = investigation.Exception.Severity,
                Status = investigation.Exception.Status
            },

            PurchaseOrder = new PurchaseOrderContext
            {
                Reference = investigation.PurchaseOrder.Reference,
                OrderDate = investigation.PurchaseOrder.OrderDate,
                ExpectedDate = investigation.PurchaseOrder.ExpectedDate,
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

            CustomerOrders = relevantCustomerOrders
                .Select(c =>
                {
                    var stock = investigation.Inventory
                        .FirstOrDefault(i => i.ProductId == c.ProductId)?
                        .AvailableQuantity ?? 0;

                    var shortage = Math.Max(0, c.Quantity - stock);

                    return new CustomerOrderContext
                    {
                        Reference = c.Reference,
                        ProductId = c.ProductId,
                        Quantity = c.Quantity,
                        RequiredDate = c.RequiredDate,
                        AvailableStock = stock,
                        ShortageQuantity = shortage
                    };
                })
                .ToList(),

            Emails = investigation.SupplierEmails
                .Select(e => new EmailContext
                {
                    Sender = e.Sender,
                    Subject = e.Subject,
                    Date = e.Date,
                    Body = e.Body
                })
                .ToList()
        };
    }
}
