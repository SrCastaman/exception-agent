using ExceptionAgent.Application.Allocation.Models;

namespace ExceptionAgent.Application.Allocation.Policies;

public interface IAllocationPolicy
{
    AllocationResult Allocate(
        List<Supply> supplies,
        List<Demand> demands);
}