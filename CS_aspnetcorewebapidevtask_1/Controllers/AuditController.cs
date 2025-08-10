using CS_aspnetcorewebapidevtask_1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS_aspnetcorewebapidevtask_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")] // Admin only
    public class AuditController : ControllerBase
    {
        private readonly CS_DbContext _context;

        public AuditController(CS_DbContext context)
        {
            _context = context;
        }

        // GET: api/Audit?from=2025-08-01&to=2025-08-10
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductAuditDto>>> GetAuditLogs(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            try
            {
                var query = _context.ProductAudits
                    .Include(a => a.User)
                    .Select(a => new ProductAuditDto
                    {
                        Id = a.Id,
                        Operation = a.Operation,
                        ProductId = a.ProductId,
                        ChangedData = a.ChangedData,
                        Username = a.User.UserName,
                        ChangedAt = a.ChangedAt
                    });

                // Apply date filters
                if (from.HasValue)
                {
                    query = query.Where(a => a.ChangedAt >= from.Value);
                }
                if (to.HasValue)
                {
                    query = query.Where(a => a.ChangedAt <= to.Value);
                }

                var auditLogs = await query.OrderBy(a => a.ChangedAt).ToListAsync();

                return Ok(auditLogs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving audit logs.", Error = ex.Message });
            }
        }
    }
}
