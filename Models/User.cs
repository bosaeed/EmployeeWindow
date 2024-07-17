using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EmployeeWindow.Models
{
    public class User : IdentityUser
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string PreferredLanguage { get; set; }

        [Display(Name = "Full Name")]
        public string FullName => $"{FirstName} {LastName}";

        public static async Task SeedAdminUser(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

                // Create Admin role if it doesn't exist
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                // Create Admin user if it doesn't exist
                var adminUser = await userManager.FindByEmailAsync("admin@admin.com");
                if (adminUser == null)
                {
                    adminUser = new User
                    {
                        UserName = "admin@admin.com",
                        Email = "admin@admin.com",
                        EmailConfirmed = true,
                        FirstName = "Abdullah",
                        LastName = "Jaber",
                        PreferredLanguage = "English"
                    };
                    await userManager.CreateAsync(adminUser, "AdminPassword123!"); // Change this password
                }

                // Assign Admin role to the user
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
