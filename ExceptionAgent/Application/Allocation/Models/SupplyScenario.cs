namespace ExceptionAgent.Application.Allocation.Models;

public class SupplyScenario
{
    public string Reference { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public DateTime NormalAvailableDate { get; set; }

    public DateTime CurrentAvailableDate { get; set; }
}