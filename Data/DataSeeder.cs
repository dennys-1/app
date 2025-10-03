using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace TiendaPc.Data;

public static class DataSeeder
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // 1. Crear rol Admin si no existe
        var roleName = "Admin";
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }

        // 2. Crear usuario admin por defecto si no existe
        var adminEmail = "admin@tiendapc.com";
        var adminPassword = "Admin123*"; // cámbialo en producción ⚠️
        var user = await userManager.FindByEmailAsync(adminEmail);

        if (user == null)
        {
            user = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, roleName);
            }
        }
        else
        {
            // Asegurar que tenga rol Admin
            if (!await userManager.IsInRoleAsync(user, roleName))
                await userManager.AddToRoleAsync(user, roleName);
        }
    }
}
