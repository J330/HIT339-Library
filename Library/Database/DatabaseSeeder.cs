using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Users.Domain;

namespace S379658_HIT339_Assignment2_Library.Database;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        bool includeDemoBorrower = false)
    {
        await context.Database.MigrateAsync();

        await EnsureRoleAsync(roleManager, AppRoles.Admin);
        await EnsureRoleAsync(roleManager, AppRoles.Reception);
        await EnsureRoleAsync(roleManager, AppRoles.Manager);
        await EnsureRoleAsync(roleManager, AppRoles.Borrower);

        await EnsureUserAsync(
            userManager,
            username: "admin",
            password: "Admin123!",
            fullName: "Library Administrator",
            roleName: AppRoles.Admin);

        await EnsureUserAsync(
            userManager,
            username: "reception",
            password: "Reception123!",
            fullName: "Reception Staff",
            roleName: AppRoles.Reception);

        await EnsureUserAsync(
            userManager,
            username: "manager",
            password: "Manager123!",
            fullName: "Library Manager",
            roleName: AppRoles.Manager);

        if (includeDemoBorrower)
        {
            await EnsureDemoBorrowerAsync(context, userManager, "borrower1", "demo.borrower1@example.test", "LIBDEMO000101", "Demo", "Borrower One");
            await EnsureDemoBorrowerAsync(context, userManager, "borrower2", "demo.borrower2@example.test", "LIBDEMO000102", "Demo", "Borrower Two");
        }
    }

    // Production never creates the well-known demonstration credentials.
    // Run database migrations explicitly, then optionally supply one-time bootstrap values
    // through environment variables (LibraryBootstrap__AdminUsername/Password/Email).
    public static async Task SeedProductionAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        string? adminUsername,
        string? adminPassword,
        string? adminEmail)
    {
        await EnsureRoleAsync(roleManager, AppRoles.Admin);
        await EnsureRoleAsync(roleManager, AppRoles.Reception);
        await EnsureRoleAsync(roleManager, AppRoles.Manager);
        await EnsureRoleAsync(roleManager, AppRoles.Borrower);

        if (string.IsNullOrWhiteSpace(adminUsername) && string.IsNullOrWhiteSpace(adminPassword))
            return; // Other environments may use a previously created administrator.

        if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
            throw new InvalidOperationException("Supply BOTH initial admin username and password, or neither.");
        if (adminPassword.Length < 12)
            throw new InvalidOperationException("The production bootstrap admin password must have at least 12 characters.");

        var existing = await userManager.FindByNameAsync(adminUsername);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, AppRoles.Admin))
                throw new InvalidOperationException("Bootstrap username already belongs to a non-admin account.");
            return; // Do not reset an existing admin password on restart.
        }

        var account = new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = adminUsername,
            Email = string.IsNullOrWhiteSpace(adminEmail) ? null : adminEmail.Trim(),
            FullName = "Initial Library Administrator", LockoutEnabled = true
        };
        ThrowIfFailed(await userManager.CreateAsync(account, adminPassword), "create production administrator");
        ThrowIfFailed(await userManager.AddToRoleAsync(account, AppRoles.Admin), "assign production admin role");
    }

    // These demonstration accounts are only created in Development. Do not reset existing passwords.
    // Old accounts and loan histories are deliberately left untouched.
    private static async Task EnsureDemoBorrowerAsync(AppDbContext db, UserManager<ApplicationUser> users,
        string username, string email, string membershipCode, string firstName, string lastName)
    {
        var account = await users.FindByNameAsync(username);
        if (account is null)
        {
            // Do not create a user if a different record already owns the demo email or code.
            if (await db.Borrowers.AnyAsync(b => b.Email == email || b.MembershipCode == membershipCode) ||
                await users.FindByEmailAsync(email) is not null)
                return;
            account = new ApplicationUser
            {
                Id = Guid.NewGuid(), UserName = username, Email = email,
                FullName = firstName + " " + lastName, LockoutEnabled = true
            };
            ThrowIfFailed(await users.CreateAsync(account, "Borrower123!"), $"create demo user '{username}'");
        }
        else if (!string.Equals(account.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            // Never take over an actual library account sharing a seed username.
            return;
        }

        if (!await users.IsInRoleAsync(account, AppRoles.Borrower))
            ThrowIfFailed(await users.AddToRoleAsync(account, AppRoles.Borrower), "assign borrower role");
        if (await db.Borrowers.AnyAsync(b => b.IdentityUserId == account.Id)) return;
        if (await db.Borrowers.AnyAsync(b => b.MembershipCode == membershipCode || b.Email == email)) return;
        db.Borrowers.Add(new Borrower
        {
            IdentityUserId = account.Id, MembershipCode = membershipCode,
            FirstName = firstName, LastName = lastName, Email = email,
            Phone = "0400000000", Address = "1 Example Street"
        });
        await db.SaveChangesAsync();
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await roleManager.CreateAsync(
            new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName
            });

        ThrowIfFailed(result, $"create role '{roleName}'");
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string username,
        string password,
        string fullName,
        string roleName)
    {
        var user = await userManager.FindByNameAsync(username);

        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = username,
                FullName = fullName
            };

            var createResult = await userManager.CreateAsync(user, password);
            ThrowIfFailed(createResult, $"create user '{username}'");
        }
        else if (user.FullName != fullName)
        {
            user.FullName = fullName;
            var updateResult = await userManager.UpdateAsync(user);
            ThrowIfFailed(updateResult, $"update user '{username}'");
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            var roleResult = await userManager.AddToRoleAsync(user, roleName);
            ThrowIfFailed(roleResult, $"add user '{username}' to role '{roleName}'");
        }
    }

    private static void ThrowIfFailed(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => $"{error.Code}: {error.Description}"));

        throw new InvalidOperationException(
            $"ASP.NET Core Identity could not {operation}. {errors}");
    }
}
