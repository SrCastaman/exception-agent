using ExceptionAgent.Contracts;
using ExceptionAgent.Domain.Entities;

namespace ExceptionAgent.Aplication.Exceptions;

public class ExceptionRiskCalculationService
{
    public RiskCalculationResult Calculate(
        List<PurchaseOrderLine> purchaseOrderLines,
        List<Inventory> inventory,
        List<CustomerOrder> customerOrders,
        DateTime supplierExpectedDate)
    {
        var relevantProductIds = purchaseOrderLines
            .Select(line => line.ProductId)
            .Distinct()
            .ToList();

        var relevantCustomerOrders = customerOrders
            .Where(order => relevantProductIds.Contains(order.ProductId))
            .ToList();

        var customerOrderContexts = new List<CustomerOrderContext>();

        var customerOrdersByProduct = relevantCustomerOrders
            .GroupBy(order => order.ProductId);

        foreach (var productGroup in customerOrdersByProduct)
        {
            var productId = productGroup.Key;

            var availableStock = inventory
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

        var riskDate = customerOrderContexts
            .Where(order => order.AtRisk)
            .OrderBy(order => order.RequiredDate)
            .Select(order => (DateTime?)order.RequiredDate)
            .FirstOrDefault();

        return new RiskCalculationResult
        {
            CustomerOrders = customerOrderContexts,
            TotalShortageQuantity = totalShortageQuantity,
            CalculatedSeverity = calculatedSeverity,
            RiskDate = riskDate
        };
    }
}