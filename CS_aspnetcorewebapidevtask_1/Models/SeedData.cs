using Microsoft.AspNetCore.Identity;

namespace CS_aspnetcorewebapidevtask_1.Models
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Create roles if they don't exist
            string[] roles = { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create initial admin user if not exists
            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                adminUser = new User { UserName = "admin", Email = "admin@admin.com" };
                var result = await userManager.CreateAsync(adminUser, "Qwerty123!"); // Very very strong password
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Create initial normal user if not exists
            var normalUser = await userManager.FindByNameAsync("user1");
            if (normalUser == null)
            {
                normalUser = new User { UserName = "user1", Email = "user1@aaa.com" };
                var result = await userManager.CreateAsync(normalUser, "User1!"); // Very very strong password
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(normalUser, "User");
                }
            }
        }
    }
}
