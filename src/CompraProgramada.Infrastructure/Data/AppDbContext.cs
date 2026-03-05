using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ContaGrafica> ContasGraficas => Set<ContaGrafica>();
    public DbSet<CustodiaFilhote> CustodiasFilhote => Set<CustodiaFilhote>();
    public DbSet<CustodiaMaster> CustodiaMaster => Set<CustodiaMaster>();
    public DbSet<CestaTopFive> CestasTopFive => Set<CestaTopFive>();
    public DbSet<CestaItem> CestaItens => Set<CestaItem>();
    public DbSet<OrdemCompra> OrdensCompra => Set<OrdemCompra>();
    public DbSet<OrdemCompraItem> OrdemCompraItens => Set<OrdemCompraItem>();
    public DbSet<Distribuicao> Distribuicoes => Set<Distribuicao>();
    public DbSet<HistoricoValorMensal> HistoricoValoresMensais => Set<HistoricoValorMensal>();
    public DbSet<OperacaoRebalanceamento> OperacoesRebalanceamento => Set<OperacaoRebalanceamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}