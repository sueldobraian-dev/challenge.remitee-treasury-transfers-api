using Challenge.Domain.Aggregates;
using Challenge.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Challenge.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(a => a.Balance)
            .HasColumnType("decimal(18, 4)")
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion(
                s => s.ToString().ToUpperInvariant(),
                s => (AccountStatus)Enum.Parse(typeof(AccountStatus), s, true))
            .HasMaxLength(20)
            .IsRequired();

        // Optimistic Concurrency check
        builder.Property(a => a.Version)
            .IsRowVersion()
            .IsRequired();
    }
}
