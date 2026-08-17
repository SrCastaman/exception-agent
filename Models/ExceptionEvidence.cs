namespace ExceptionAgent.Models
{
    public class ExceptionEvidence
    {
        public int Id { get; set; }

        public int OperationalExceptionId { get; set; }

        public string SourceType { get; set; } = string.Empty;

        public int SourceId { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
