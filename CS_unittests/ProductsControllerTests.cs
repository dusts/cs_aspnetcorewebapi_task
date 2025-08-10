using CS_aspnetcorewebapidevtask_1.Controllers;
using CS_aspnetcorewebapidevtask_1.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;

namespace CS_unittests
{
    public class ProductsControllerTests
    {
        private readonly CS_DbContext _context;
        private readonly Mock<IConfigurationManager> _configurationMock;
        private readonly ProductController _controller;

        public ProductsControllerTests()
        {
            // Setup in-memory SQLite database
            var options = new DbContextOptionsBuilder<CS_DbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            _context = new CS_DbContext(options);
            _context.Database.OpenConnection();
            _context.Database.EnsureCreated();

            // Mock IConfiguration for VAT rate
            _configurationMock = new Mock<IConfigurationManager>();
            _configurationMock.Setup(c => c[It.Is<string>(key => key == "VAT")]).Returns("0.20"); // Mock indexer

            _controller = new ProductController(_context, _configurationMock.Object);
        }


        private void SetUserRole(string role)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, role)
        };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task GetProducts_ReturnsListOfProducts()
        {
            // Arrange
            SetUserRole("User");
            _context.Product.AddRange(new[]
            {
            new Product { Id = 1, Title = "Laptop", Quantity = 2, Price = 1000.00m },
            new Product { Id = 2, Title = "Mouse", Quantity = 5, Price = 20.00m }
        });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProducts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var products = Assert.IsType<List<ProductDto>>(okResult.Value);
            Assert.Equal(2, products.Count);
            Assert.Equal("Laptop", products[0].ItemName);
            Assert.Equal(2400.00m, products[0].TotalPriceWithVat); // (2 * 1000) * 1.20
            Assert.Equal("Mouse", products[1].ItemName);
            Assert.Equal(120.00m, products[1].TotalPriceWithVat); // (5 * 20) * 1.20
        }

        [Fact]
        public async Task GetProduct_ValidId_ReturnsProduct()
        {
            // Arrange
            SetUserRole("User");
            var product = new Product { Id = 1, Title = "Laptop", Quantity = 2, Price = 1000.00m };
            _context.Product.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProduct(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var productDto = Assert.IsType<ProductDto>(okResult.Value);
            Assert.Equal("Laptop", productDto.ItemName);
            Assert.Equal(2400.00m, productDto.TotalPriceWithVat);
        }

        [Fact]
        public async Task GetProduct_InvalidId_ReturnsNotFound()
        {
            // Arrange
            SetUserRole("User");

            // Act
            var result = await _controller.GetProduct(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Product not found.", notFoundResult.Value!.GetType().GetProperty("Message")!.GetValue(notFoundResult.Value));
        }

        [Fact]
        public async Task PostProduct_Admin_Success_CreatesProductAndAudit()
        {
            // Arrange
            SetUserRole("Admin");
            var product = new Product { Title = "Keyboard", Quantity = 3, Price = 50.00m };

            // Act
            var result = await _controller.PostProduct(product);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var productDto = Assert.IsType<ProductDto>(createdResult.Value);
            Assert.Equal("Keyboard", productDto.ItemName);
            Assert.Equal(180.00m, productDto.TotalPriceWithVat); // (3 * 50) * 1.20

            var audit = await _context.ProductAudits.FirstOrDefaultAsync(a => a.ProductId == product.Id);
            Assert.NotNull(audit);
            Assert.Equal("Created", audit!.Operation);
            Assert.Contains("Keyboard", audit.ChangedData);
            Assert.Equal("testuser", audit.UserId);
        }

        [Fact]
        public async Task PostProduct_NonAdmin_ReturnsForbidden()
        {
            // Arrange
            SetUserRole("User");
            var product = new Product { Title = "Keyboard", Quantity = 3, Price = 50.00m };

            // Act
            var result = await _controller.PostProduct(product);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task PostProduct_InvalidData_ReturnsBadRequest()
        {
            // Arrange
            SetUserRole("Admin");
            var product = new Product { Title = "", Quantity = -1, Price = 0 }; // Invalid data

            // Act
            var result = await _controller.PostProduct(product);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errors = badRequestResult.Value!.GetType().GetProperty("Errors")!.GetValue(badRequestResult.Value) as IEnumerable<string>;
            Assert.Contains("Item Title is required.", errors);
            Assert.Contains("Quantity must be non-negative.", errors);
            Assert.Contains("Price must be greater than 0.", errors);
        }

        [Fact]
        public async Task PutProduct_Admin_Success_UpdatesProductAndAudit()
        {
            // Arrange
            SetUserRole("Admin");
            var product = new Product { Id = 1, Title = "Laptop", Quantity = 2, Price = 1000.00m };
            _context.Product.Add(product);
            await _context.SaveChangesAsync();
            var updatedProduct = new Product { Id = 1, Title = "Laptop Pro", Quantity = 3, Price = 1200.00m };

            // Act
            var result = await _controller.PutProduct(1, updatedProduct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var savedProduct = await _context.Product.FindAsync(1);
            Assert.Equal("Laptop Pro", savedProduct!.Title);
            Assert.Equal(3, savedProduct.Quantity);
            Assert.Equal(1200.00m, savedProduct.Price);

            var audit = await _context.ProductAudits.FirstOrDefaultAsync(a => a.ProductId == 1);
            Assert.NotNull(audit);
            Assert.Equal("Updated", audit!.Operation);
            Assert.Contains("Laptop Pro", audit.ChangedData);
            Assert.Equal("testuser", audit.UserId);
        }

        [Fact]
        public async Task PutProduct_IdMismatch_ReturnsBadRequest()
        {
            // Arrange
            SetUserRole("Admin");
            var product = new Product { Id = 2, Title = "Laptop", Quantity = 2, Price = 1000.00m };

            // Act
            var result = await _controller.PutProduct(1, product);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Product ID mismatch.", badRequestResult.Value!.GetType().GetProperty("Message")!.GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task DeleteProduct_Admin_Success_DeletesProductAndAudit()
        {
            // Arrange
            SetUserRole("Admin");
            var product = new Product { Id = 1, Title = "Laptop", Quantity = 2, Price = 1000.00m };
            _context.Product.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteProduct(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _context.Product.FindAsync(1));

            var audit = await _context.ProductAudits.FirstOrDefaultAsync(a => a.ProductId == 1);
            Assert.NotNull(audit);
            Assert.Equal("Deleted", audit!.Operation);
            Assert.Contains("Laptop", audit.ChangedData);
            Assert.Equal("testuser", audit.UserId);
        }

        public void Dispose()
        {
            _context.Database.CloseConnection();
            _context.Dispose();
        }
    }
}