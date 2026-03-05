using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class CestaItemConfiguration : IEntityTypeConfiguration<CestaItem>
{
    public void Configure(EntityTypeBuilder<CestaItem> builder)
    {
        builder.ToTable("CestaItens");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Ticker).IsRequired().HasMaxLength(12);
        builder.Property(c => c.Percentual).HasPrecision(5, 2);
    }
}