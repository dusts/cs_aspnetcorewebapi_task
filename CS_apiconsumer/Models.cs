using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS_apiconsumer
{
    internal class Models
    {
        public class RegisterDto
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class LoginDto
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }

        public class ProductDto
        {
            public string ItemName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal TotalPriceWithVat { get; set; }
        }

        public class ProductAuditDto
        {
            public int Id { get; set; }
            public string Operation { get; set; } = string.Empty;
            public int ProductId { get; set; }
            public string ChangedData { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public DateTime ChangedAt { get; set; }
        }

        public class ApiResponse<T>
        {
            public T? Data { get; set; }
            public string? Message { get; set; }
            public IEnumerable<string>? Errors { get; set; }
        }
    }
}
