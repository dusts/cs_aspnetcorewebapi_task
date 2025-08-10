using Microsoft.EntityFrameworkCore;

namespace CS_aspnetcorewebapidevtask_1.Models
{
    public class CS_DbContext : DbContext
    {
        public CS_DbContext(DbContextOptions options) : base(options)
        {
        }

        protected CS_DbContext()
        {

        }

        public DbSet<Product> Product { get; set; } = null!;
    }
}
