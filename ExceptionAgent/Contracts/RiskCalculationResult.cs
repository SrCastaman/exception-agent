namespace ExceptionAgent.Contracts;

public class RiskCalculationResult
{
    public List<CustomerOrderContext> CustomerOrders { get; set; } = new();

    public int TotalShortageQuantity { get; set; }

    public string CalculatedSeverity { get; set; } = "LOW";

    public DateTime? RiskDate { get; set; }
}