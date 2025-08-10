
using CS_aspnetcorewebapidevtask_1.Models;
using Microsoft.EntityFrameworkCore;

namespace CS_aspnetcorewebapidevtask_1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add DbContext with SQLite connection string
            builder.Services.AddDbContext<CS_DbContext>(options =>
                options.UseSqlite("Data Source=cs_db.db")); // creates cs_db file in the project root

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            using (var scope = app.Services.CreateScope()) 
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<CS_DbContext>();
                context.Database.Migrate(); // creates db if it doesnt exist and applies migrations.
            }

            app.Run();
        }
    }
}
