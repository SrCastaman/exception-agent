namespace ExceptionAgent.Contracts;

public class SupplierEmailExtractionResult
{
    public int SourceEmailId { get; set; }

    public string PurchaseOrderReference { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public DateTime? NewExpectedDate { get; set; }

    public int? AffectedQuantity { get; set; }

    public string Evidence { get; set; } = string.Empty;
}