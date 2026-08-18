namespace ExceptionAgent.Domain.Entities
{
    public class PurchaseOrder
    {
        public int Id { get; set; }

        public string Reference { get; set; } = string.Empty;

        public int SupplierId { get; set; }

        public DateTime OrderDate { get; set; }

        public DateTime ExpectedDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public ICollection<PurchaseOrderLine> Lines { get; set; }
            = new List<PurchaseOrderLine>();
    }
}
