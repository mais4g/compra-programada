using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (await db.Clientes.AnyAsync())
            return;

        var contaMaster = new ContaGrafica
        {
            NumeroConta = "MST-000001",
            Tipo = TipoConta.Master,
            DataCriacao = DateTime.UtcNow
        };
        db.ContasGraficas.Add(contaMaster);

        var clientes = new[]
        {
            new Cliente
            {
                Nome = "João Silva",
                Cpf = "12345678901",
                Email = "joao@email.com",
                ValorMensal = 3000m,
                Ativo = true,
                DataAdesao = DateTime.UtcNow,
                ContaGrafica = new ContaGrafica
                {
                    NumeroConta = "FLH-000001",
                    Tipo = TipoConta.Filhote,
                    DataCriacao = DateTime.UtcNow
                }
            },
            new Cliente
            {
                Nome = "Maria Santos",
                Cpf = "98765432100",
                Email = "maria@email.com",
                ValorMensal = 6000m,
                Ativo = true,
                DataAdesao = DateTime.UtcNow,
                ContaGrafica = new ContaGrafica
                {
                    NumeroConta = "FLH-000002",
                    Tipo = TipoConta.Filhote,
                    DataCriacao = DateTime.UtcNow
                }
            },
            new Cliente
            {
                Nome = "Pedro Oliveira",
                Cpf = "11122233344",
                Email = "pedro@email.com",
                ValorMensal = 1500m,
                Ativo = true,
                DataAdesao = DateTime.UtcNow,
                ContaGrafica = new ContaGrafica
                {
                    NumeroConta = "FLH-000003",
                    Tipo = TipoConta.Filhote,
                    DataCriacao = DateTime.UtcNow
                }
            }
        };
        db.Clientes.AddRange(clientes);

        var cesta = new CestaTopFive
        {
            Nome = "Top Five - Fevereiro 2026",
            Ativa = true,
            DataCriacao = DateTime.UtcNow,
            Itens = new List<CestaItem>
            {
                new() { Ticker = "PETR4", Percentual = 30 },
                new() { Ticker = "VALE3", Percentual = 25 },
                new() { Ticker = "ITUB4", Percentual = 20 },
                new() { Ticker = "BBDC4", Percentual = 15 },
                new() { Ticker = "WEGE3", Percentual = 10 }
            }
        };
        db.CestasTopFive.Add(cesta);

        await db.SaveChangesAsync();
    }
}