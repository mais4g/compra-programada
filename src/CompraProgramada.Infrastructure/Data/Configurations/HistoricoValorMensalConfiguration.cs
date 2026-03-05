using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class HistoricoValorMensalConfiguration : IEntityTypeConfiguration<HistoricoValorMensal>
{
    public void Configure(EntityTypeBuilder<HistoricoValorMensal> builder)
    {
        builder.ToTable("HistoricoValoresMensais");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.ValorAnterior).HasPrecision(18, 2);
        builder.Property(h => h.ValorNovo).HasPrecision(18, 2);
        builder.Property(h => h.DataAlteracao).IsRequired();

        builder.HasOne(h => h.Cliente)
            .WithMany(c => c.HistoricoValores)
            .HasForeignKey(h => h.ClienteId);
    }
}