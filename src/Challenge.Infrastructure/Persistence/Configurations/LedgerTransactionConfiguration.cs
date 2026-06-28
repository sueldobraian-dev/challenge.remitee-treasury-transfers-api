using Challenge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Challenge.Infrastructure.Persistence.Configurations;

public class LedgerTransactionConfiguration : IEntityTypeConfiguration<LedgerTransaction>
{
    public void Configure(EntityTypeBuilder<LedgerTransaction> builder)
    {
        builder.ToTable("LedgerTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.OperationId)
            .IsRequired();

        // Idempotency: unique index on OperationId
        builder.HasIndex(t => t.OperationId)
            .IsUnique();

        builder.Property(t => t.SourceAccountId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.TargetAccountId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.AmountDebited)
            .HasColumnType("decimal(18, 4)")
            .IsRequired();

        builder.Property(t => t.AmountCredited)
            .HasColumnType("decimal(18, 4)")
            .IsRequired();

        builder.Property(t => t.FxRate)
            .HasColumnType("decimal(18, 6)")
            .IsRequired(false);

        builder.Property(t => t.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();
    }
}
