namespace CS_aspnetcorewebapidevtask_1.Models
{
    public class ProductAuditDto
    {
        public int Id { get; set; }
        public string Operation { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ChangedData { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
    }
}
