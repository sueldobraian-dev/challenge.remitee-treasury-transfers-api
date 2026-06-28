using Challenge.Domain.Aggregates;
using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Infrastructure.Persistence;

public class ChallengeDbContext : DbContext, IUnitOfWork
{
    public ChallengeDbContext(DbContextOptions<ChallengeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<LedgerTransaction> LedgerTransactions => Set<LedgerTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChallengeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
