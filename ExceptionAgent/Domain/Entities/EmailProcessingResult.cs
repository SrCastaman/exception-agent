using ExceptionAgent.Domain.Enums;

namespace ExceptionAgent.Domain.Entities;

public class EmailProcessingResult
{
    public int Id { get; set; }

    public int EmailId { get; set; }

    public Email Email { get; set; } = null!;

    public EmailMatchingStatus MatchingStatus { get; set; }

    public int? PurchaseOrderId { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }

    public double MatchScore { get; set; }

    public string MatchingReason { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public DateTime? NewExpectedDate { get; set; }

    public int? AffectedQuantity { get; set; }

    public string Evidence { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; }

    public List<EmailMatchCandidate> Candidates { get; set; } = new();
}