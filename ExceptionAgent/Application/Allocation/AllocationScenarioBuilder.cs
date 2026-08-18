using ExceptionAgent.Application.Allocation.Models;
using ExceptionAgent.Domain.Entities;

namespace ExceptionAgent.Application.Allocation;

public class AllocationScenarioBuilder
{
    public AllocationScenario Build(
        IEnumerable<PurchaseOrder> purchaseOrders,
        IEnumerable<PurchaseOrderLine> purchaseOrderLines,
        IEnumerable<Inventory> inventories,
        IEnumerable<CustomerOrder> customerOrders)
    {
        var scenario = new AllocationScenario();

        foreach (var inventory in inventories)
        {
            scenario.Supplies.Add(new Supply
            {
                Reference = "STOCK",
                ProductId = inventory.ProductId,
                Quantity = inventory.AvailableQuantity,
                AvailableDate = DateTime.MinValue
            });
        }

        var purchaseOrderLinesByOrder = purchaseOrderLines
            .GroupBy(line => line.PurchaseOrderId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        foreach (var purchaseOrder in purchaseOrders)
        {
            if (!purchaseOrderLinesByOrder.TryGetValue(
                purchaseOrder.Id,
                out var lines))
            {
                continue;
            }

            foreach (var line in lines)
            {
                var pendingQuantity =
                    Math.Max(
                        0,
                        line.OrderedQuantity -
                        line.ReceivedQuantity);

                if (pendingQuantity <= 0)
                {
                    continue;
                }

                scenario.Supplies.Add(new Supply
                {
                    Reference = purchaseOrder.Reference,
                    ProductId = line.ProductId,
                    Quantity = pendingQuantity,
                    AvailableDate = purchaseOrder.ExpectedDate
                });
            }
        }

        foreach (var customerOrder in customerOrders)
        {
            scenario.Demands.Add(new Demand
            {
                Reference = customerOrder.Reference,
                ProductId = customerOrder.ProductId,
                Quantity = customerOrder.Quantity,
                RequiredDate = customerOrder.RequiredDate
            });
        }

        return scenario;
    }

    public AllocationScenario BuildFromSupplyScenarios(
        IEnumerable<SupplyScenario> supplyScenarios,
        IEnumerable<Demand> demands)
    {
        var scenario = new AllocationScenario
        {
            Demands = demands.ToList()
        };

        foreach (var supplyScenario in supplyScenarios)
        {
            if (supplyScenario.Quantity <= 0)
            {
                continue;
            }

            scenario.Supplies.Add(new Supply
            {
                Reference = supplyScenario.Reference,
                ProductId = supplyScenario.ProductId,
                Quantity = supplyScenario.Quantity,
                AvailableDate = supplyScenario.CurrentAvailableDate
            });
        }

        return scenario;
    }
}