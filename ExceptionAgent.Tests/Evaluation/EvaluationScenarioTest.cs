using ExceptionAgent.Application.Allocation;
using ExceptionAgent.Application.Allocation.Models;
using ExceptionAgent.Application.Allocation.Policies;

namespace ExceptionAgent.Tests.Evaluation;

public class EvaluationScenarioTests
{
    [Fact]
    public void Scenario_01_DelayedSupplyWithImpact()
    {
        var policy = new DatePriorityAllocationPolicy();
        var engine = new AllocationEngine(policy);

        var supplies = new List<Supply>
        {
            new()
            {
                Reference = "PO-001",
                ProductId = 1,
                Quantity = 50,
                AvailableDate = new DateTime(2026, 8, 20)
            }
        };

        var demands = new List<Demand>
        {
            new()
            {
                Reference = "CO-001",
                ProductId = 1,
                Quantity = 50,
                RequiredDate = new DateTime(2026, 8, 18)
            }
        };

        var result = engine.Allocate(
            supplies,
            demands);

        Assert.Equal(
            50,
            result.UncoveredDemands.Sum(d => d.Quantity));

        Assert.Contains(
            result.UncoveredDemands,
            d => d.DemandReference == "CO-001");
    }

    [Fact]
    public void Scenario_02_DelayedSupplyWithoutImpact()
    {
        var policy = new DatePriorityAllocationPolicy();
        var engine = new AllocationEngine(policy);

        var supplies = new List<Supply>
        {
            new()
            {
                Reference = "STOCK",
                ProductId = 1,
                Quantity = 50,
                AvailableDate = DateTime.MinValue
            },
            new()
            {
                Reference = "PO-001",
                ProductId = 1,
                Quantity = 50,
                AvailableDate = new DateTime(2026, 8, 20)
            }
        };

        var demands = new List<Demand>
        {
            new()
            {
                Reference = "CO-001",
                ProductId = 1,
                Quantity = 50,
                RequiredDate = new DateTime(2026, 8, 18)
            }
        };

        var result = engine.Allocate(
            supplies,
            demands);

        Assert.Empty(result.UncoveredDemands);

        var allocation = result.Allocations
            .Single(a => a.DemandReference == "CO-001");

        Assert.Equal(
            "STOCK",
            allocation.SupplyReference);

        Assert.Equal(
            50,
            allocation.Quantity);
    }

    [Fact]
    public void Scenario_03_TwoDelayedPurchaseOrders_ShouldLeaveExpectedShortage()
    {
        var policy = new DatePriorityAllocationPolicy();
        var engine = new AllocationEngine(policy);

        var supplies = new List<Supply>
        {
            new()
            {
                Reference = "STOCK",
                ProductId = 1,
                Quantity = 5,
                AvailableDate = DateTime.MinValue
            },
            new()
            {
                Reference = "PO-1042",
                ProductId = 1,
                Quantity = 50,
                AvailableDate = new DateTime(2026, 8, 20)
            },
            new()
            {
                Reference = "PO-1044",
                ProductId = 1,
                Quantity = 80,
                AvailableDate = new DateTime(2026, 8, 21)
            }
        };

        var demands = new List<Demand>
        {
            new()
            {
                Reference = "CO-8823",
                ProductId = 1,
                Quantity = 25,
                RequiredDate = new DateTime(2026, 8, 18)
            },
            new()
            {
                Reference = "CO-8821",
                ProductId = 1,
                Quantity = 25,
                RequiredDate = new DateTime(2026, 8, 19)
            },
            new()
            {
                Reference = "CO-8824",
                ProductId = 1,
                Quantity = 20,
                RequiredDate = new DateTime(2026, 8, 19)
            }
        };

        var result = engine.Allocate(
            supplies,
            demands);

        Assert.Equal(
            65,
            result.UncoveredDemands.Sum(d => d.Quantity));

        Assert.Equal(
            20,
            result.UncoveredDemands
                .Single(d => d.DemandReference == "CO-8823")
                .Quantity);

        Assert.Equal(
            25,
            result.UncoveredDemands
                .Single(d => d.DemandReference == "CO-8821")
                .Quantity);

        Assert.Equal(
            20,
            result.UncoveredDemands
                .Single(d => d.DemandReference == "CO-8824")
                .Quantity);
    }

    [Fact]
    public void Scenario_04_PartialDelivery_ShouldLeaveRemainingShortage()
    {
        var policy = new DatePriorityAllocationPolicy();
        var engine = new AllocationEngine(policy);

        var supplies = new List<Supply>
        {
            new()
            {
                Reference = "PO-001",
                ProductId = 1,
                Quantity = 20,
                AvailableDate = new DateTime(2026, 8, 18)
            }
        };

        var demands = new List<Demand>
        {
            new()
            {
                Reference = "CO-001",
                ProductId = 1,
                Quantity = 50,
                RequiredDate = new DateTime(2026, 8, 18)
            }
        };

        var result = engine.Allocate(
            supplies,
            demands);

        Assert.Equal(
            30,
            result.UncoveredDemands
                .Single(d => d.DemandReference == "CO-001")
                .Quantity);

        var allocation = result.Allocations
            .Single(a => a.DemandReference == "CO-001");

        Assert.Equal(
            20,
            allocation.Quantity);

        Assert.Equal(
            "PO-001",
            allocation.SupplyReference);
    }

    [Fact]
    public void Scenario_05_AlternativeSupply_ShouldAvoidImpact()
    {
        var policy = new DatePriorityAllocationPolicy();
        var engine = new AllocationEngine(policy);

        var supplies = new List<Supply>
        {
            new()
            {
                Reference = "PO-LATE",
                ProductId = 1,
                Quantity = 50,
                AvailableDate = new DateTime(2026, 8, 20)
            },
            new()
            {
                Reference = "PO-ALTERNATIVE",
                ProductId = 1,
                Quantity = 50,
                AvailableDate = new DateTime(2026, 8, 17)
            }
        };

        var demands = new List<Demand>
        {
            new()
            {
                Reference = "CO-001",
                ProductId = 1,
                Quantity = 50,
                RequiredDate = new DateTime(2026, 8, 18)
            }
        };

        var result = engine.Allocate(
            supplies,
            demands);

        Assert.Empty(result.UncoveredDemands);

        var allocation = result.Allocations
            .Single(a => a.DemandReference == "CO-001");

        Assert.Equal(
            "PO-ALTERNATIVE",
            allocation.SupplyReference);

        Assert.Equal(
            50,
            allocation.Quantity);
    }
}