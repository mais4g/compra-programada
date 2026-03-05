using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class CustodiaFilhoteConfiguration : IEntityTypeConfiguration<CustodiaFilhote>
{
    public void Configure(EntityTypeBuilder<CustodiaFilhote> builder)
    {
        builder.ToTable("CustodiasFilhote");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Ticker).IsRequired().HasMaxLength(12);
        builder.Property(c => c.Quantidade).IsRequired();
        builder.Property(c => c.PrecoMedio).HasPrecision(18, 2);
        builder.Property(c => c.ValorInvestido).HasPrecision(18, 2);

        builder.HasIndex(c => new { c.ContaGraficaId, c.Ticker }).IsUnique();

        builder.HasOne(c => c.ContaGrafica)
            .WithMany(cg => cg.CustodiaFilhote)
            .HasForeignKey(c => c.ContaGraficaId);
    }
}