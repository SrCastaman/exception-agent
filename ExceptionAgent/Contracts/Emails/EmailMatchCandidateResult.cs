namespace ExceptionAgent.Contracts.Email;

public class EmailMatchCandidateResult
{
    public int PurchaseOrderId { get; set; }

    public string PurchaseOrderReference { get; set; } = string.Empty;

    public double Score { get; set; }

    public List<string> Reasons { get; set; } = new();
}