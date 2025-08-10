
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CS_aspnetcorewebapidevtask_1.Models
{
    public class CS_DbContext : IdentityDbContext<User>
    {
        public CS_DbContext(DbContextOptions options) : base(options)
        {
        }

        protected CS_DbContext()
        {

        }

        public DbSet<Product> Product { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
