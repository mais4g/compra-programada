using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Cpf).IsRequired().HasMaxLength(11);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(200);
        builder.Property(c => c.ValorMensal).HasPrecision(18, 2);
        builder.Property(c => c.Ativo).IsRequired();
        builder.Property(c => c.DataAdesao).IsRequired();

        builder.HasIndex(c => c.Cpf).IsUnique();

        builder.HasOne(c => c.ContaGrafica)
            .WithOne(cg => cg.Cliente)
            .HasForeignKey<ContaGrafica>(cg => cg.ClienteId);
    }
}