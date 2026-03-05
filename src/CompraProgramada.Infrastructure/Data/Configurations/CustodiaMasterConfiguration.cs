using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class CustodiaMasterConfiguration : IEntityTypeConfiguration<CustodiaMaster>
{
    public void Configure(EntityTypeBuilder<CustodiaMaster> builder)
    {
        builder.ToTable("CustodiaMaster");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Ticker).IsRequired().HasMaxLength(12);
        builder.Property(c => c.Quantidade).IsRequired();
        builder.Property(c => c.PrecoMedio).HasPrecision(18, 2);
        builder.Property(c => c.Origem).HasMaxLength(200);

        builder.HasIndex(c => c.Ticker).IsUnique();
    }
}