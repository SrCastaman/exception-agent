namespace ExceptionAgent.Models
{
    public class OperationalException
    {
        public int Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public int PurchaseOrderId { get; set; }

        public string Severity { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? Cause { get; set; }

        public string? Impact { get; set; }

        public string? ProposedAction { get; set; }

        public double? Confidence { get; set; }

        public DateTime CreatedAt { get; set; }

        public PurchaseOrder? PurchaseOrder { get; set; }
    }
}
