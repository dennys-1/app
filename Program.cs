using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;
using QuestPDF.Infrastructure;
using TiendaPc.Services;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Cookies (sesión no persistente)
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.SlidingExpiration = false;
    opt.ExpireTimeSpan = TimeSpan.FromHours(8);
    opt.LoginPath = "/Cuenta/Login";
});

// Identity + Roles
builder.Services
    .AddDefaultIdentity<IdentityUser>(opt =>
    {
        opt.SignIn.RequireConfirmedAccount = false;
        opt.Password.RequiredLength = 6;
        opt.Password.RequireDigit = false;
        opt.Password.RequireLowercase = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>() // <- IMPORTANTE: agrega RoleManager/RoleStore
    .AddEntityFrameworkStores<AppDbContext>();

// Rutas de login/logout personalizadas (opcional si ya pusiste arriba LoginPath)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Cuenta/Login";
    options.LogoutPath = "/Cuenta/Logout";
    options.AccessDeniedPath = "/Cuenta/Login";
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient<ISunatClient, SunatClient>();
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 🚩 Área Admin (siempre antes de la ruta default)
app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Home}/{action=Index}/{id?}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Ruta default MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ⚙️ Ejecuta el Seeder (roles + usuario admin) ANTES de Run
await DataSeeder.SeedRolesAndAdminAsync(app.Services);

app.Run();

// (No pongas nada después de app.Run())

