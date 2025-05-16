using HR_Products.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace HR_Products.Data.Seeder
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<Users>>();

            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await roleManager.RoleExistsAsync("User"))
                await roleManager.CreateAsync(new IdentityRole("User"));

            // Seed First Admin User
            var adminUser1 = await userManager.FindByEmailAsync("admin@gmail.com");
            if (adminUser1 == null)
            {
                var user1 = new Users
                {
                    UserName = "admin@example.com",
                    Email = "admin@example.com"
                };
                var result1 = await userManager.CreateAsync(user1, "Admin@123");
                if (result1.Succeeded)
                {
                    await userManager.AddToRoleAsync(user1, "Admin");
                }
            }

            // Seed Second Admin User
            var adminUser2 = await userManager.FindByEmailAsync("superadmin@gmail.com");
            if (adminUser2 == null)
            {
                var user2 = new Users
                {
                    UserName = "superadmin@example.com",
                    Email = "superadmin@example.com"
                };
                var result2 = await userManager.CreateAsync(user2, "SuperAdmin@123");
                if (result2.Succeeded)
                {
                    await userManager.AddToRoleAsync(user2, "Admin");
                }
            }
        }
    }
}