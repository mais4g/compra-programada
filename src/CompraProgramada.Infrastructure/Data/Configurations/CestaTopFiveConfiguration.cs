using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class CestaTopFiveConfiguration : IEntityTypeConfiguration<CestaTopFive>
{
    public void Configure(EntityTypeBuilder<CestaTopFive> builder)
    {
        builder.ToTable("CestasTopFive");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Ativa).IsRequired();
        builder.Property(c => c.DataCriacao).IsRequired();

        builder.HasMany(c => c.Itens)
            .WithOne(i => i.CestaTopFive)
            .HasForeignKey(i => i.CestaTopFiveId);
    }
}