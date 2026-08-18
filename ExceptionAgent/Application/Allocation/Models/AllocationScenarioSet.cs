namespace ExceptionAgent.Application.Allocation.Models;

public class AllocationScenarioSet
{
    public AllocationScenario Normal { get; set; } = new();

    public AllocationScenario Current { get; set; } = new();

    public AllocationScenario WithoutInvestigatedDelay { get; set; } = new();
}