namespace ExceptionAgent.Domain.Entities
{
    public class CustomerOrder
    {
        public int Id { get; set; }

        public string Reference { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public DateTime RequiredDate { get; set; }
    }
}
