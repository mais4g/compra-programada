using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class DistribuicaoConfiguration : IEntityTypeConfiguration<Distribuicao>
{
    public void Configure(EntityTypeBuilder<Distribuicao> builder)
    {
        builder.ToTable("Distribuicoes");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Ticker).IsRequired().HasMaxLength(12);
        builder.Property(d => d.PrecoUnitario).HasPrecision(18, 2);
        builder.Property(d => d.ValorIRDedoDuro).HasPrecision(18, 2);
        builder.Property(d => d.DataDistribuicao).IsRequired();
        builder.Ignore(d => d.ValorOperacao);

        builder.HasOne(d => d.Cliente)
            .WithMany(c => c.Distribuicoes)
            .HasForeignKey(d => d.ClienteId);
    }
}