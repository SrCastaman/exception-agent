using ExceptionAgent.Application.Allocation.Models;
using ExceptionAgent.Application.Allocation.Policies;

namespace ExceptionAgent.Application.Allocation;

public class AllocationEngine
{
    private readonly IAllocationPolicy _policy;

    public AllocationEngine(IAllocationPolicy policy)
    {
        _policy = policy;
    }

    public AllocationResult Allocate(
        List<Supply> supplies,
        List<Demand> demands)
    {
        return _policy.Allocate(
            supplies,
            demands);
    }
}