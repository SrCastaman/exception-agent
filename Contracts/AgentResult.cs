namespace ExceptionAgent.Contracts;

public class AgentResult
{
    public string Severity { get; set; } = string.Empty;

    public string Cause { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public double Confidence { get; set; }

    public AgentImpact Impact { get; set; } = new();

    public List<AgentAction> ProposedActions { get; set; } = new();

    public List<AgentEvidence> Evidence { get; set; } = new();
}

public class AgentImpact
{
    public List<string> AffectedCustomerOrders { get; set; } = new();

    public int ShortageQuantity { get; set; }

    public DateTime? RiskDate { get; set; }
}

public class AgentAction
{
    public AgentActionType Type { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class AgentEvidence
{
    public string Description { get; set; } = string.Empty;
}