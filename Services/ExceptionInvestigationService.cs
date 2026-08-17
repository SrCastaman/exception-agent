using ExceptionAgent.Data;
using ExceptionAgent.Models;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Services;

public class ExceptionInvestigationService
{
    private readonly AppDbContext _context;

    public ExceptionInvestigationService(
        AppDbContext context)
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

        var supplierEmailEvents = await _context.SupplierEmailEvents
            .Where(e => e.PurchaseOrderId == purchaseOrder.Id)
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

    public InvestigationContext BuildContext(
        ExceptionInvestigation investigation)
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

        var latestEmailEvent = investigation.SupplierEmailEvents
            .Where(e => e.NewExpectedDate.HasValue)
            .OrderByDescending(e => e.Id)
            .FirstOrDefault();

        var supplierExpectedDate =
            latestEmailEvent?.NewExpectedDate
            ?? investigation.PurchaseOrder.ExpectedDate;

        var customerOrderContexts = new List<CustomerOrderContext>();

        var customerOrdersByProduct = relevantCustomerOrders
            .GroupBy(order => order.ProductId);

        foreach (var productGroup in customerOrdersByProduct)
        {
            var productId = productGroup.Key;

            var availableStock = investigation.Inventory
                .FirstOrDefault(i => i.ProductId == productId)?
                .AvailableQuantity ?? 0;

            var remainingStock = availableStock;

            var orderedCustomers = productGroup
                .OrderBy(order => order.RequiredDate)
                .ThenBy(order => order.Reference)
                .ToList();

            foreach (var customerOrder in orderedCustomers)
            {
                var allocatedStock = Math.Min(
                    remainingStock,
                    customerOrder.Quantity);

                var shortage = customerOrder.Quantity - allocatedStock;

                var supplierDeliveryAfterRequiredDate =
                    supplierExpectedDate > customerOrder.RequiredDate;

                var atRisk =
                    shortage > 0 &&
                    supplierDeliveryAfterRequiredDate;

                customerOrderContexts.Add(new CustomerOrderContext
                {
                    Reference = customerOrder.Reference,
                    ProductId = customerOrder.ProductId,
                    Quantity = customerOrder.Quantity,
                    RequiredDate = customerOrder.RequiredDate,
                    AvailableStock = availableStock,
                    AllocatedStock = allocatedStock,
                    ShortageQuantity = shortage,
                    SupplierExpectedDate = supplierExpectedDate,
                    SupplierDeliveryAfterRequiredDate =
                        supplierDeliveryAfterRequiredDate,
                    AtRisk = atRisk
                });

                remainingStock -= allocatedStock;
            }

            
        }

        var totalShortageQuantity = customerOrderContexts
                .Where(order => order.AtRisk)
                .Sum(order => order.ShortageQuantity);

        var calculatedSeverity =
            customerOrderContexts.Any(order => order.AtRisk)
                ? "HIGH"
                : "LOW";

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

            CustomerOrders = customerOrderContexts,

            Emails = investigation.SupplierEmails
                .Select(e => new EmailContext
                {
                    Sender = e.Sender,
                    Subject = e.Subject,
                    Date = e.Date,
                    Body = e.Body
                })
                .ToList(),

            CalculatedSeverity = calculatedSeverity,
            TotalShortageQuantity = totalShortageQuantity
        };
    }
}