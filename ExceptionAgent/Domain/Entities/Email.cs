namespace ExceptionAgent.Domain.Entities
{
    public class Email
    {
        public int Id { get; set; }

        public string Sender { get; set; } = string.Empty;

        public string Recipient { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public string Body { get; set; } = string.Empty;

        public int? SupplierId { get; set; }
    }
}
