namespace ExceptionAgent.Application.Allocation.Models;

public class AllocationResult
{
    public List<SupplyAllocation> Allocations { get; set; } = new();

    public List<UncoveredDemand> UncoveredDemands { get; set; } = new();
}