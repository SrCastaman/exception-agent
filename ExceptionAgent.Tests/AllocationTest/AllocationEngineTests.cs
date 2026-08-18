using ExceptionAgent.Application.Allocation;
using ExceptionAgent.Application.Allocation.Models;
using ExceptionAgent.Application.Allocation.Policies;

namespace ExceptionAgent.Tests.Allocation;

public class AllocationEngineTests
{
    [Fact]
    public void Allocate_ShouldDelegateToConfiguredPolicy()
    {
        var policy = new TestAllocationPolicy();
        var engine = new AllocationEngine(policy);

        var supplies = new List<Supply>
        {
            new()
            {
                Reference = "PO-TEST",
                ProductId = 1,
                Quantity = 10,
                AvailableDate = new DateTime(2026, 8, 18)
            }
        };

        var demands = new List<Demand>
        {
            new()
            {
                Reference = "CO-TEST",
                ProductId = 1,
                Quantity = 10,
                RequiredDate = new DateTime(2026, 8, 18)
            }
        };

        var result = engine.Allocate(
            supplies,
            demands);

        Assert.True(policy.WasCalled);
        Assert.NotNull(result);
    }

    private class TestAllocationPolicy : IAllocationPolicy
    {
        public bool WasCalled { get; private set; }

        public AllocationResult Allocate(
            List<Supply> supplies,
            List<Demand> demands)
        {
            WasCalled = true;

            return new AllocationResult();
        }
    }
}