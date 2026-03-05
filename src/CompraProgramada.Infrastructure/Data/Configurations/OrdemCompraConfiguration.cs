using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class OrdemCompraConfiguration : IEntityTypeConfiguration<OrdemCompra>
{
    public void Configure(EntityTypeBuilder<OrdemCompra> builder)
    {
        builder.ToTable("OrdensCompra");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.ValorTotalConsolidado).HasPrecision(18, 2);
        builder.Property(o => o.DataExecucao).IsRequired();
        builder.Property(o => o.DataReferencia).IsRequired();

        builder.HasMany(o => o.Itens)
            .WithOne(i => i.OrdemCompra)
            .HasForeignKey(i => i.OrdemCompraId);

        builder.HasMany(o => o.Distribuicoes)
            .WithOne(d => d.OrdemCompra)
            .HasForeignKey(d => d.OrdemCompraId);
    }
}