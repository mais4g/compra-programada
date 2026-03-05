using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class OrdemCompraItemConfiguration : IEntityTypeConfiguration<OrdemCompraItem>
{
    public void Configure(EntityTypeBuilder<OrdemCompraItem> builder)
    {
        builder.ToTable("OrdemCompraItens");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Ticker).IsRequired().HasMaxLength(12);
        builder.Property(o => o.PrecoUnitario).HasPrecision(18, 2);
        builder.Ignore(o => o.QuantidadeTotal);
        builder.Ignore(o => o.ValorTotal);
    }
}