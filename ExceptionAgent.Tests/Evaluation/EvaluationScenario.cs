public class EvaluationScenario
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int ExpectedShortage { get; set; }

    public List<string> ExpectedAffectedOrders { get; set; } = new();

    public DateTime? ExpectedRiskDate { get; set; }
}