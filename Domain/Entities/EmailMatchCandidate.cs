namespace ExceptionAgent.Domain.Entities;

public class EmailMatchCandidate
{
    public int Id { get; set; }

    public int EmailProcessingResultId { get; set; }

    public EmailProcessingResult EmailProcessingResult { get; set; } = null!;

    public int PurchaseOrderId { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public double Score { get; set; }

    public string Reasons { get; set; } = string.Empty;
}