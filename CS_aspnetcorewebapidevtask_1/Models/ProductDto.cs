namespace CS_aspnetcorewebapidevtask_1.Models
{
    public class ProductDto
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPriceWithVat { get; set; }
    }
}
