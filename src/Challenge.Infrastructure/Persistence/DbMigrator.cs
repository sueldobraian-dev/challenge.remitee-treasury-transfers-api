using Challenge.Domain.Entities.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Challenge.Infrastructure.Persistence;

public static class DbMigrator
{
    public static async Task MigrateAndSeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ChallengeDbContext>();

        // Apply migrations automatically
        await context.Database.MigrateAsync();

        // Seed data if empty
        if (!await context.Accounts.AnyAsync())
        {
            var usd = Currency.FromCode("USD");
            var ars = Currency.FromCode("ARS");
            var clp = Currency.FromCode("CLP");

            var seedAccounts = new[]
            {
                new Account("ACC-USD-1", usd, 10000.00m, AccountStatus.Active),
                new Account("ACC-USD-2", usd, 500.00m, AccountStatus.Active),
                new Account("ACC-ARS-1", ars, 1000000.00m, AccountStatus.Active),
                new Account("ACC-CLP-1", clp, 0.00m, AccountStatus.Active),
                new Account("ACC-FROZEN", usd, 9999.00m, AccountStatus.Frozen)
            };

            await context.Accounts.AddRangeAsync(seedAccounts);
            await context.SaveChangesAsync();
        }
    }
}
