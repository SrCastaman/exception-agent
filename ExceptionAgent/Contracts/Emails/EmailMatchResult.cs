namespace ExceptionAgent.Contracts.Email;

public class EmailMatchResult
{
    public bool Matched { get; set; }

    public int? PurchaseOrderId { get; set; }

    public string PurchaseOrderReference { get; set; } = string.Empty;

    public double Score { get; set; }

    public List<string> Reasons { get; set; } = new();

    public List<EmailMatchCandidateResult> Candidates { get; set; } = new();
}