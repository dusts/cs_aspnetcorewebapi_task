using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using static CS_apiconsumer.Models;

namespace CS_apiconsumer
{
    internal class Program
    {
        private static readonly HttpClient _client;
        private static string _baseUrl = "https://localhost:5287/api";
        private static string? _jwtToken;

        static Program()
        {
            // Configure HttpClient with HttpClientHandler to bypass SSL errors
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = static (HttpRequestMessage httpRequestMessage, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors sslPolicyErrors) => true
            };
            _client = new HttpClient(handler);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        static async Task Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("\nTodoApi Consumer");
                Console.WriteLine("1. Register");
                Console.WriteLine("2. Login");
                Console.WriteLine("3. Get All Products");
                Console.WriteLine("4. Get Product by ID");
                Console.WriteLine("5. Create Product (Admin)");
                Console.WriteLine("6. Update Product (Admin)");
                Console.WriteLine("7. Delete Product (Admin)");
                Console.WriteLine("8. Get Audit Logs (Admin)");
                Console.WriteLine("9. Exit");
                Console.Write("Select an option: ");

                var choice = Console.ReadLine();
                try
                {
                    switch (choice)
                    {
                        case "1": await Register(); break;
                        case "2": await Login(); break;
                        case "3": await GetProducts(); break;
                        case "4": await GetProductById(); break;
                        case "5": await CreateProduct(); break;
                        case "6": await UpdateProduct(); break;
                        case "7": await DeleteProduct(); break;
                        case "8": await GetAuditLogs(); break;
                        case "9": return;
                        default: Console.WriteLine("Invalid option."); break;
                    }
                }
                catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
                {
                    Console.WriteLine("Error: Unable to connect to the API. Ensure it is running at " + _baseUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        // Rest of the methods (Register, Login, GetProducts, etc.) remain unchanged
        private static async Task Register()
        {
            Console.Write("Enter username: ");
            var username = Console.ReadLine();
            Console.Write("Enter password: ");
            var password = Console.ReadLine();

            var registerDto = new RegisterDto { Username = username!, Password = password! };
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/Auth/register", registerDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                Console.WriteLine(result?.Message ?? "Registration successful.");
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                Console.WriteLine($"Error: {error?.Message}");
                if (error?.Errors != null)
                    foreach (var err in error.Errors)
                        Console.WriteLine($"- {err}");
            }
        }

        private static async Task Login()
        {
            Console.Write("Enter username: ");
            var username = Console.ReadLine();
            Console.Write("Enter password: ");
            var password = Console.ReadLine();

            var loginDto = new LoginDto { Username = username!, Password = password! };
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/Auth/login", loginDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
                _jwtToken = result?.Data;
                Console.WriteLine("Login successful. Token acquired.");
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                Console.WriteLine($"Error: {error?.Message}");
            }
        }

        private static async Task GetProducts()
        {
            var response = await _client.GetAsync($"{_baseUrl}/Products");

            if (response.IsSuccessStatusCode)
            {
                var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
                foreach (var product in products!)
                {
                    Console.WriteLine($"Name: {product.ItemName}, Quantity: {product.Quantity}, Price: {product.Price:C}, Total with VAT: {product.TotalPriceWithVat:C}");
                }
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
        }

        private static async Task GetProductById()
        {
            Console.Write("Enter product ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                Console.WriteLine("Invalid ID.");
                return;
            }

            var response = await _client.GetAsync($"{_baseUrl}/Products/{id}");

            if (response.IsSuccessStatusCode)
            {
                var product = await response.Content.ReadFromJsonAsync<ProductDto>();
                Console.WriteLine($"Name: {product!.ItemName}, Quantity: {product.Quantity}, Price: {product.Price:C}, Total with VAT: {product.TotalPriceWithVat:C}");
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                Console.WriteLine($"Error: {error?.Message ?? response.StatusCode.ToString()}");
            }
        }

        private static async Task CreateProduct()
        {
            Console.Write("Enter product name: ");
            var name = Console.ReadLine();
            Console.Write("Enter quantity: ");
            if (!int.TryParse(Console.ReadLine(), out var quantity) || quantity < 0)
            {
                Console.WriteLine("Invalid quantity.");
                return;
            }
            Console.Write("Enter price: ");
            if (!decimal.TryParse(Console.ReadLine(), out var price) || price <= 0)
            {
                Console.WriteLine("Invalid price.");
                return;
            }

            var product = new Product { Name = name!, Quantity = quantity, Price = price };
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/Products", product);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ProductDto>();
                Console.WriteLine($"Product created: {result!.ItemName}, Total with VAT: {result.TotalPriceWithVat:C}");
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                Console.WriteLine($"Error: {error?.Message}");
                if (error?.Errors != null)
                    foreach (var err in error.Errors)
                        Console.WriteLine($"- {err}");
            }
        }

        private static async Task UpdateProduct()
        {
            Console.Write("Enter product ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                Console.WriteLine("Invalid ID.");
                return;
            }
            Console.Write("Enter new product name: ");
            var name = Console.ReadLine();
            Console.Write("Enter new quantity: ");
            if (!int.TryParse(Console.ReadLine(), out var quantity) || quantity < 0)
            {
                Console.WriteLine("Invalid quantity.");
                return;
            }
            Console.Write("Enter new price: ");
            if (!decimal.TryParse(Console.ReadLine(), out var price) || price <= 0)
            {
                Console.WriteLine("Invalid price.");
                return;
            }

            var product = new Product { Id = id, Name = name!, Quantity = quantity, Price = price };
            var response = await _client.PutAsJsonAsync($"{_baseUrl}/Products/{id}", product);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Product updated successfully.");
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                Console.WriteLine($"Error: {error?.Message}");
                if (error?.Errors != null)
                    foreach (var err in error.Errors)
                        Console.WriteLine($"- {err}");
            }
        }

        private static async Task DeleteProduct()
        {
            Console.Write("Enter product ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                Console.WriteLine("Invalid ID.");
                return;
            }

            var response = await _client.DeleteAsync($"{_baseUrl}/Products/{id}");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Product deleted successfully.");
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                Console.WriteLine($"Error: {error?.Message ?? response.StatusCode.ToString()}");
            }
        }

        private static async Task GetAuditLogs()
        {
            Console.Write("Enter from date (yyyy-MM-dd, optional, press Enter to skip): ");
            var fromInput = Console.ReadLine();
            DateTime? from = string.IsNullOrEmpty(fromInput) ? null : DateTime.Parse(fromInput);
            Console.Write("Enter to date (yyyy-MM-dd, optional, press Enter to skip): ");
            var toInput = Console.ReadLine();
            DateTime? to = string.IsNullOrEmpty(toInput) ? null : DateTime.Parse(toInput);

            var url = $"{_baseUrl}/Audit";
            if (from.HasValue || to.HasValue)
            {
                var query = new List<string>();
                if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
                if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");
                url += $"?{string.Join("&", query)}";
            }

            var response = await _client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var auditLogs = await response.Content.ReadFromJsonAsync<List<ProductAuditDto>>();
                foreach (var log in auditLogs!)
                {
                    Console.WriteLine($"ID: {log.Id}, Operation: {log.Operation}, Product ID: {log.ProductId}, User: {log.Username}, Changed At: {log.ChangedAt}, Data: {log.ChangedData}");
                }
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
        }
    }
}