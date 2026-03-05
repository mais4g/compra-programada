using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class OperacaoRebalanceamentoConfiguration : IEntityTypeConfiguration<OperacaoRebalanceamento>
{
    public void Configure(EntityTypeBuilder<OperacaoRebalanceamento> builder)
    {
        builder.ToTable("OperacoesRebalanceamento");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Ticker).IsRequired().HasMaxLength(12);
        builder.Property(o => o.PrecoUnitario).HasPrecision(18, 2);
        builder.Property(o => o.PrecoMedio).HasPrecision(18, 2);
        builder.Property(o => o.Lucro).HasPrecision(18, 2);
        builder.Property(o => o.DataOperacao).IsRequired();

        builder.HasOne(o => o.Cliente)
            .WithMany()
            .HasForeignKey(o => o.ClienteId);
    }
}