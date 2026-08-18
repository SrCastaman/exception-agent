using ExceptionAgent.Application.Allocation.Models;
using ExceptionAgent.Application.Allocation.Policies;

namespace ExceptionAgent.Tests.Allocation;

public class DatePriorityAllocationPolicyTests
{
    [Fact]
    public void Allocate_ShouldRespectRequiredDates()
    {
        var policy = new DatePriorityAllocationPolicy();

        var supplies = new List<Supply>
        {
            new()
            {
                Reference = "STOCK",
                ProductId = 1,
                Quantity = 5,
                AvailableDate = new DateTime(2026, 8, 17)
            },
            new()
            {
                Reference = "PO-1042",
                ProductId = 1,
                Quantity = 40,
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
                Quantity = 30,
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

        var result = policy.Allocate(supplies, demands);

        Assert.Single(result.Allocations);

        Assert.Equal("STOCK",
            result.Allocations[0].SupplyReference);

        Assert.Equal("CO-8823",
            result.Allocations[0].DemandReference);

        Assert.Equal(5,
            result.Allocations[0].Quantity);

        Assert.Equal(3, result.UncoveredDemands.Count);

        Assert.Equal(25,
            result.UncoveredDemands
                .Single(x => x.DemandReference == "CO-8823")
                .Quantity);

        Assert.Equal(25,
            result.UncoveredDemands
                .Single(x => x.DemandReference == "CO-8821")
                .Quantity);

        Assert.Equal(20,
            result.UncoveredDemands
                .Single(x => x.DemandReference == "CO-8824")
                .Quantity);
    }


    [Fact]
    public void Allocate_ShouldUseSupplyThatArrivesBeforeRequiredDate()
    {
        var policy = new DatePriorityAllocationPolicy();

        var supplies = new List<Supply>
    {
        new()
        {
            Reference = "STOCK",
            ProductId = 1,
            Quantity = 5,
            AvailableDate = new DateTime(2026, 8, 17)
        },
        new()
        {
            Reference = "PO-1050",
            ProductId = 1,
            Quantity = 40,
            AvailableDate = new DateTime(2026, 8, 18)
        },
        new()
        {
            Reference = "PO-1051",
            ProductId = 1,
            Quantity = 80,
            AvailableDate = new DateTime(2026, 8, 21)
        }
    };

        var demands = new List<Demand>
    {
        new()
        {
            Reference = "CO-9001",
            ProductId = 1,
            Quantity = 30,
            RequiredDate = new DateTime(2026, 8, 18)
        },
        new()
        {
            Reference = "CO-9002",
            ProductId = 1,
            Quantity = 10,
            RequiredDate = new DateTime(2026, 8, 19)
        }
    };

        var result = policy.Allocate(supplies, demands);

        Assert.Equal(3, result.Allocations.Count);
        Assert.Empty(result.UncoveredDemands);

        var co9001 = result.Allocations
            .Where(a => a.DemandReference == "CO-9001")
            .ToList();

        Assert.Equal(2, co9001.Count);
        Assert.Equal(5,
            co9001.Single(a => a.SupplyReference == "STOCK").Quantity);
        Assert.Equal(25,
            co9001.Single(a => a.SupplyReference == "PO-1050").Quantity);

        var co9002 = result.Allocations
            .Where(a => a.DemandReference == "CO-9002")
            .ToList();

        Assert.Single(co9002);

        Assert.Equal("PO-1050", co9002[0].SupplyReference);
        Assert.Equal(10, co9002[0].Quantity);
    }

    [Fact]
    public void Allocate_ShouldPrioritizeDemandWithEarlierRequiredDate()
    {
        var policy = new DatePriorityAllocationPolicy();

        var supplies = new List<Supply>
    {
        new()
        {
            Reference = "PO-1052",
            ProductId = 1,
            Quantity = 30,
            AvailableDate = new DateTime(2026, 8, 18)
        }
    };

        var demands = new List<Demand>
    {
        new()
        {
            Reference = "CO-9003",
            ProductId = 1,
            Quantity = 20,
            RequiredDate = new DateTime(2026, 8, 18)
        },
        new()
        {
            Reference = "CO-9004",
            ProductId = 1,
            Quantity = 20,
            RequiredDate = new DateTime(2026, 8, 19)
        }
    };

        var result = policy.Allocate(supplies, demands);

        Assert.Equal(2, result.Allocations.Count);
        Assert.Single(result.UncoveredDemands);

        var allocationFor9003 = result.Allocations
            .Single(a => a.DemandReference == "CO-9003");

        Assert.Equal(20, allocationFor9003.Quantity);
        Assert.Equal("PO-1052", allocationFor9003.SupplyReference);

        var allocationFor9004 = result.Allocations
            .Single(a => a.DemandReference == "CO-9004");

        Assert.Equal(10, allocationFor9004.Quantity);
        Assert.Equal("PO-1052", allocationFor9004.SupplyReference);

        var uncovered9004 = result.UncoveredDemands
            .Single(d => d.DemandReference == "CO-9004");

        Assert.Equal(10, uncovered9004.Quantity);
    }
}