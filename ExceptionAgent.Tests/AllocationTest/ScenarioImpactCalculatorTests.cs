using ExceptionAgent.Application.Allocation;
using ExceptionAgent.Application.Allocation.Models;

namespace ExceptionAgent.Tests.Allocation;

public class ScenarioImpactCalculatorTests
{
    [Fact]
    public void Compare_ShouldDetectNewRisk()
    {
        var calculator = new ScenarioImpactCalculator();

        var normalResult = new AllocationResult
        {
            UncoveredDemands = new List<UncoveredDemand>()
        };

        var currentResult = new AllocationResult
        {
            UncoveredDemands = new List<UncoveredDemand>
            {
                new()
                {
                    DemandReference = "CO-8821",
                    ProductId = 1,
                    Quantity = 10,
                    RequiredDate = new DateTime(2026, 8, 19)
                },
                new()
                {
                    DemandReference = "CO-8823",
                    ProductId = 1,
                    Quantity = 30,
                    RequiredDate = new DateTime(2026, 8, 18)
                }
            }
        };

        var currentScenario = new AllocationScenario
        {
            Supplies = new List<Supply>
            {
                new()
                {
                    Reference = "PO-TEST",
                    ProductId = 1,
                    Quantity = 50,
                    AvailableDate = new DateTime(2026, 8, 20)
                }
            },
            Demands = new List<Demand>()
        };

        var result = calculator.Compare(
            normalResult,
            currentResult,
            currentScenario,
            "PO-TEST");

        Assert.Equal(
            2,
            result.NewlyAtRiskDemands.Count);

        Assert.Contains(
            "CO-8821",
            result.NewlyAtRiskDemands);

        Assert.Contains(
            "CO-8823",
            result.NewlyAtRiskDemands);

        Assert.Equal(
            40,
            result.AdditionalShortageQuantity);

        Assert.Empty(
            result.NoLongerAtRiskDemands);

        Assert.Equal(
            0,
            result.RecoveredShortageQuantity);

        Assert.Equal(
            new DateTime(2026, 8, 18),
            result.RiskDate);
    }
}