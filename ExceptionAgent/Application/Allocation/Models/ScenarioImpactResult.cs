namespace ExceptionAgent.Application.Allocation.Models;

public class ScenarioImpactResult
{
    public List<string> NewlyAtRiskDemands { get; set; } = new();

    public List<string> NoLongerAtRiskDemands { get; set; } = new();

    public int AdditionalShortageQuantity { get; set; }

    public int RecoveredShortageQuantity { get; set; }

    public List<AffectedDemand> AffectedCustomerOrders { get; set; } = new();

    public DateTime? RiskDate { get; set; }
}