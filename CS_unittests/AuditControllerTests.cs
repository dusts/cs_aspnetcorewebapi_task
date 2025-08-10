using CS_aspnetcorewebapidevtask_1.Controllers;
using CS_aspnetcorewebapidevtask_1.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CS_unittests
{
    public class AuditControllerTests
    {
        private readonly CS_DbContext _context;
        private readonly AuditController _controller;

        public AuditControllerTests()
        {
            var options = new DbContextOptionsBuilder<CS_DbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            _context = new CS_DbContext(options);
            _context.Database.OpenConnection();
            _context.Database.EnsureCreated();

            _controller = new AuditController(_context);
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
        public async Task GetAuditLogs_NoFilters_ReturnsAllLogs()
        {
            // Arrange
            SetUserRole("Admin");
            var user = new User { Id = "user1", UserName = "testuser" };
            _context.Users.Add(user);
            _context.ProductAudits.AddRange(new[]
            {
            new ProductAudit { Id = 1, Operation = "Created", ProductId = 1, ChangedData = "{}", UserId = "user1", ChangedAt = new DateTime(2025, 8, 10) },
            new ProductAudit { Id = 2, Operation = "Updated", ProductId = 1, ChangedData = "{}", UserId = "user1", ChangedAt = new DateTime(2025, 8, 11) }
        });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetAuditLogs(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var auditLogs = Assert.IsType<List<ProductAuditDto>>(okResult.Value);
            Assert.Equal(2, auditLogs.Count);
            Assert.Equal("Created", auditLogs[0].Operation);
            Assert.Equal("testuser", auditLogs[0].Username);
            Assert.Equal(new DateTime(2025, 8, 10), auditLogs[0].ChangedAt);
        }

        [Fact]
        public async Task GetAuditLogs_WithDateFilters_ReturnsFilteredLogs()
        {
            // Arrange
            SetUserRole("Admin");
            var user = new User { Id = "user1", UserName = "testuser" };
            _context.Users.Add(user);
            _context.ProductAudits.AddRange(new[]
            {
            new ProductAudit { Id = 1, Operation = "Created", ProductId = 1, ChangedData = "{}", UserId = "user1", ChangedAt = new DateTime(2025, 8, 10) },
            new ProductAudit { Id = 2, Operation = "Updated", ProductId = 1, ChangedData = "{}", UserId = "user1", ChangedAt = new DateTime(2025, 8, 12) }
        });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetAuditLogs(new DateTime(2025, 8, 10), new DateTime(2025, 8, 10));

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var auditLogs = Assert.IsType<List<ProductAuditDto>>(okResult.Value);
            Assert.Single(auditLogs);
            Assert.Equal("Created", auditLogs[0].Operation);
        }

        [Fact]
        public async Task GetAuditLogs_NonAdmin_ReturnsForbidden()
        {
            // Arrange
            SetUserRole("User");

            // Act
            var result = await _controller.GetAuditLogs(null, null);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        public void Dispose()
        {
            _context.Database.CloseConnection();
            _context.Dispose();
        }
    }
}
