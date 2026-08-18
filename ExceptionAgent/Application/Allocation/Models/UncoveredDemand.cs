namespace ExceptionAgent.Application.Allocation.Models;

public class UncoveredDemand
{
    public string DemandReference { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public DateTime RequiredDate { get; set; }
}