using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class ContaGraficaConfiguration : IEntityTypeConfiguration<ContaGrafica>
{
    public void Configure(EntityTypeBuilder<ContaGrafica> builder)
    {
        builder.ToTable("ContasGraficas");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.NumeroConta).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Tipo).IsRequired();
        builder.Property(c => c.DataCriacao).IsRequired();

        builder.HasIndex(c => c.NumeroConta).IsUnique();
    }
}