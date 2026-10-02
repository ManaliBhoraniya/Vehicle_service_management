using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Data;
using VehicleServiceManagement.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

var app = builder.Build();


// ======================================================
// CREATE ROLES AND DEFAULT MANAGER ACCOUNT
// ======================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager =
        services.GetRequiredService<
            RoleManager<IdentityRole<int>>>();

    var userManager =
        services.GetRequiredService<
            UserManager<ApplicationUser>>();


    // ==================================================
    // CREATE ROLES
    // ==================================================

    string[] roles =
    {
        "Customer",
        "Worker",
        "Manager"
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult =
                await roleManager.CreateAsync(
                    new IdentityRole<int>(role));

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    Console.WriteLine(
                        $"Role creation error: {error.Description}");
                }
            }
        }
    }


    // ==================================================
    // DEFAULT MANAGER ACCOUNT
    // ==================================================

    // This is the ONLY Manager account created by the system.
    // Managers cannot register from the Register page.

    string managerEmail =
        "manager@vehicle.com";

    string managerPassword =
        "Manager@123";


    var managerUser =
        await userManager.FindByEmailAsync(
            managerEmail);


    // ==================================================
    // CREATE MANAGER IF IT DOES NOT EXIST
    // ==================================================

    if (managerUser == null)
    {
        managerUser = new ApplicationUser
        {
            UserName = managerEmail,

            Email = managerEmail,

            Name = "Service Manager",

            EmailConfirmed = true
        };


        var createResult =
            await userManager.CreateAsync(
                managerUser,
                managerPassword);


        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(
                managerUser,
                "Manager");

            Console.WriteLine(
                "Default Manager account created.");
        }
        else
        {
            foreach (var error in createResult.Errors)
            {
                Console.WriteLine(
                    $"Manager creation error: {error.Description}");
            }
        }
    }
    else
    {
        // ==================================================
        // EXISTING MANAGER
        // ==================================================

        // Make sure the existing account has
        // the Manager role.

        if (!await userManager.IsInRoleAsync(
                managerUser,
                "Manager"))
        {
            await userManager.AddToRoleAsync(
                managerUser,
                "Manager");
        }

        // Make sure Manager email is confirmed.

        if (!managerUser.EmailConfirmed)
        {
            managerUser.EmailConfirmed = true;

            await userManager.UpdateAsync(
                managerUser);
        }
    }
}


// ======================================================
// HTTP PIPELINE
// ======================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Account}/{action=Login}/{id?}");

app.Run();