namespace ExceptionAgent.Application.Allocation.Models;

public class SupplyAllocation
{
    public string SupplyReference { get; set; } = string.Empty;

    public string DemandReference { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public DateTime AvailableDate { get; set; }

    public DateTime RequiredDate { get; set; }
}