namespace ExceptionAgent.Domain.Entities;

public class SupplierEmailEvent
{
    public int Id { get; set; }

    public int EmailId { get; set; }

    public int PurchaseOrderId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public DateTime? NewExpectedDate { get; set; }

    public int? AffectedQuantity { get; set; }

    public string Evidence { get; set; } = string.Empty;
}