namespace ExceptionAgent.Application.Allocation.Models;

public class AffectedDemand
{
    public string Reference { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public int AvailableStock { get; set; }

    public int AllocatedStock { get; set; }

    public int ShortageQuantity { get; set; }

    public DateTime RequiredDate { get; set; }

    public DateTime SupplierExpectedDate { get; set; }

    public bool SupplierDeliveryAfterRequiredDate { get; set; }

    public bool AtRisk { get; set; }
}