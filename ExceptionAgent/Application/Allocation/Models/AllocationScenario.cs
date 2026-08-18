namespace ExceptionAgent.Application.Allocation.Models;

public class AllocationScenario
{
    public List<Supply> Supplies { get; set; } = new();

    public List<Demand> Demands { get; set; } = new();
}