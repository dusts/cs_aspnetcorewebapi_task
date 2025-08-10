using CS_aspnetcorewebapidevtask_1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CS_aspnetcorewebapidevtask_1.Controllers
{
    [ApiController]
    [Route("api/products")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly CS_DbContext _context;
        private readonly decimal _vatRate;

        public ProductController(CS_DbContext context, IConfigurationManager configuration)
        {
            _context = context;
            _vatRate = configuration.GetValue<decimal>("VAT"); // ToDo: this needs to change..or testing. Couldn't figure out problem.
        }

        // GET: api/productsUserOnly
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _context.Product
            .Select(p => new ProductDto
            {
                ItemName = p.Title,
                Quantity = p.Quantity,
                Price = p.Price,
                TotalPriceWithVat = (p.Quantity * p.Price) * (1 + _vatRate)
            })
            .ToListAsync();

            return Ok(products);
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Product.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { Message = "Product not found." });
            }

            var productDto = new ProductDto
            {
                ItemName = product.Title,
                Quantity = product.Quantity,
                Price = product.Price,
                TotalPriceWithVat = (product.Quantity * product.Price) * (1 + _vatRate)
            };

            return Ok(productDto);
        }

        // POST: api/products
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "Invalid product data.", Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            _context.Product.Add(product);
            await _context.SaveChangesAsync();

            // Log audit entry
            var audit = new ProductAudit
            {
                Operation = "Created",
                ProductId = product.Id,
                ChangedData = JsonSerializer.Serialize(new { NewValues = product }),
                UserId = User.Identity!.Name!, // Get username from JWT claims
                ChangedAt = DateTime.UtcNow
            };
            _context.ProductAudits.Add(audit);
            await _context.SaveChangesAsync();

            var productDto = new ProductDto
            {
                ItemName = product.Title,
                Quantity = product.Quantity,
                Price = product.Price,
                TotalPriceWithVat = (product.Quantity * product.Price) * (1 + _vatRate)
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, productDto);
        }

        // PUT: api/products/5
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest(new { Message = "Product ID mismatch." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "Invalid product data.", Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            var existingProduct = await _context.Product.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (existingProduct == null)
            {
                return NotFound(new { Message = "Product not found." });
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                // Log audit entry
                var audit = new ProductAudit
                {
                    Operation = "Updated",
                    ProductId = id,
                    ChangedData = JsonSerializer.Serialize(new { OldValues = existingProduct, NewValues = product }),
                    UserId = User.Identity!.Name!,
                    ChangedAt = DateTime.UtcNow
                };
                _context.ProductAudits.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound(new { Message = "Product not found." });
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/products/5
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Product.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { Message = "Product not found." });
            }

            _context.Product.Remove(product);

            // Log audit entry
            var audit = new ProductAudit
            {
                Operation = "Deleted",
                ProductId = id,
                ChangedData = JsonSerializer.Serialize(new { OldValues = product }),
                UserId = User.Identity!.Name!,
                ChangedAt = DateTime.UtcNow
            };
            _context.ProductAudits.Add(audit);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id) => _context.Product.Any(e => e.Id == id);
    }
}
