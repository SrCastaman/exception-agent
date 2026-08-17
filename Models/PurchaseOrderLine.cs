namespace ExceptionAgent.Models
{
    public class PurchaseOrderLine
    {
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }

        public int ProductId { get; set; }

        public int OrderedQuantity { get; set; }

        public int ReceivedQuantity { get; set; }
    }
}
