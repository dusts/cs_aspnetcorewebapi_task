
using CS_aspnetcorewebapidevtask_1.Models;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using System.Threading.Tasks;

namespace CS_aspnetcorewebapidevtask_1
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                // Add JWT Bearer security definition
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter 'Bearer' followed by a space and your JWT token (e.g., 'Bearer eyJhbGci...')."
                });
                // Apply security requirement to endpoints with [Authorize]
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
                // Optional: Filter to show padlocks only for [Authorize] endpoints
                options.OperationFilter<SecurityRequirementsOperationFilter>();
            });

            // Add DbContext with SQLite connection string
            builder.Services.AddDbContext<CS_DbContext>(options =>
                options.UseSqlite("Data Source=cs_db.db")); // creates cs_db file in the project root

            // Add identity
            builder.Services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<CS_DbContext>()
                .AddDefaultTokenProviders();

            // Add token stuff
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "me",
                    ValidAudience = "CS_checkers",
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes("X7kPq9mW3zT2rY6nL8vJ5tF4hB2dC9xA")) // random key
                };
            });

            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
                .AddPolicy("UserOnly", policy => policy.RequireRole("User"));


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<CS_DbContext>();
                context.Database.EnsureCreated(); // creates db if it doesnt exist and applies migrations.

                // Seed data for db
                await SeedData.Initialize(services);
            }

            app.Run();
        }
    }
}
