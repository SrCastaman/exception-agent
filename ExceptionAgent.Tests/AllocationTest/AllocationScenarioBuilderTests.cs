using ExceptionAgent.Application.Allocation;
using ExceptionAgent.Domain.Entities;

namespace ExceptionAgent.Tests.Allocation;

public class AllocationScenarioBuilderTests
{
    [Fact]
    public void Build_ShouldCreateSuppliesAndDemandsCorrectly()
    {
        var builder = new AllocationScenarioBuilder();

        var purchaseOrders = new List<PurchaseOrder>
        {
            new()
            {
                Id = 1,
                Reference = "PO-1042",
                ExpectedDate = new DateTime(2026, 8, 20)
            },
            new()
            {
                Id = 2,
                Reference = "PO-1044",
                ExpectedDate = new DateTime(2026, 8, 21)
            }
        };

        var purchaseOrderLines = new List<PurchaseOrderLine>
        {
            new()
            {
                PurchaseOrderId = 1,
                ProductId = 1,
                OrderedQuantity = 100,
                ReceivedQuantity = 60
            },
            new()
            {
                PurchaseOrderId = 2,
                ProductId = 1,
                OrderedQuantity = 100,
                ReceivedQuantity = 20
            }
        };

        var inventories = new List<Inventory>
        {
            new()
            {
                ProductId = 1,
                AvailableQuantity = 5
            }
        };

        var customerOrders = new List<CustomerOrder>
        {
            new()
            {
                Reference = "CO-8823",
                ProductId = 1,
                Quantity = 30,
                RequiredDate = new DateTime(2026, 8, 18)
            },
            new()
            {
                Reference = "CO-8821",
                ProductId = 1,
                Quantity = 25,
                RequiredDate = new DateTime(2026, 8, 19)
            }
        };

        var result = builder.Build(
            purchaseOrders,
            purchaseOrderLines,
            inventories,
            customerOrders);

        Assert.Equal(3, result.Supplies.Count);
        Assert.Equal(2, result.Demands.Count);

        var stock = result.Supplies
            .Single(s => s.Reference == "STOCK");

        Assert.Equal(5, stock.Quantity);
        Assert.Equal(1, stock.ProductId);

        var po1042 = result.Supplies
            .Single(s => s.Reference == "PO-1042");

        Assert.Equal(40, po1042.Quantity);
        Assert.Equal(
            new DateTime(2026, 8, 20),
            po1042.AvailableDate);

        var po1044 = result.Supplies
            .Single(s => s.Reference == "PO-1044");

        Assert.Equal(80, po1044.Quantity);
        Assert.Equal(
            new DateTime(2026, 8, 21),
            po1044.AvailableDate);

        var co8823 = result.Demands
            .Single(d => d.Reference == "CO-8823");

        Assert.Equal(30, co8823.Quantity);
        Assert.Equal(
            new DateTime(2026, 8, 18),
            co8823.RequiredDate);
    }
}