using CompraProgramada.Domain.Entities;
using FluentAssertions;

namespace CompraProgramada.UnitTests.Domain;

public class CustodiaFilhoteTests
{
    [Fact]
    public void AtualizarPrecoMedio_PrimeiraCompra_DeveDefinirPrecoMedio()
    {
        var custodia = new CustodiaFilhote
        {
            Ticker = "PETR4",
            Quantidade = 0,
            PrecoMedio = 0
        };

        custodia.AtualizarPrecoMedio(10, 35.00m);

        custodia.Quantidade.Should().Be(10);
        custodia.PrecoMedio.Should().Be(35.00m);
    }

    [Fact]
    public void AtualizarPrecoMedio_SegundaCompra_DeveCalcularMediaPonderada()
    {
        var custodia = new CustodiaFilhote
        {
            Ticker = "PETR4",
            Quantidade = 8,
            PrecoMedio = 35.00m,
            ValorInvestido = 280.00m
        };

        custodia.AtualizarPrecoMedio(10, 37.00m);

        custodia.Quantidade.Should().Be(18);
        // PM = (8 * 35 + 10 * 37) / 18 = (280 + 370) / 18 = 36.11
        custodia.PrecoMedio.Should().BeApproximately(36.11m, 0.01m);
    }

    [Fact]
    public void AtualizarPrecoMedio_QuantidadeZero_NaoDeveAlterar()
    {
        var custodia = new CustodiaFilhote
        {
            Ticker = "PETR4",
            Quantidade = 0,
            PrecoMedio = 0
        };

        custodia.AtualizarPrecoMedio(0, 35.00m);

        custodia.Quantidade.Should().Be(0);
    }

    [Fact]
    public void CalcularLucro_ComLucro_DeveRetornarPositivo()
    {
        var custodia = new CustodiaFilhote
        {
            Ticker = "PETR4",
            Quantidade = 18,
            PrecoMedio = 36.11m
        };

        var lucro = custodia.CalcularLucro(40.00m, 5);

        // Lucro = 5 * (40.00 - 36.11) = 5 * 3.89 = 19.45
        lucro.Should().BeApproximately(19.45m, 0.01m);
    }

    [Fact]
    public void CalcularLucro_ComPrejuizo_DeveRetornarNegativo()
    {
        var custodia = new CustodiaFilhote
        {
            Ticker = "PETR4",
            Quantidade = 10,
            PrecoMedio = 35.00m
        };

        var lucro = custodia.CalcularLucro(32.00m, 10);

        // Lucro = 10 * (32.00 - 35.00) = -30.00
        lucro.Should().Be(-30.00m);
    }

    [Fact]
    public void AtualizarPrecoMedio_MultiplasCompras_DeveAcumularCorretamente()
    {
        var custodia = new CustodiaFilhote
        {
            Ticker = "VALE3",
            Quantidade = 0,
            PrecoMedio = 0
        };

        // Compra 1: 10 acoes a R$ 60.00
        custodia.AtualizarPrecoMedio(10, 60.00m);
        custodia.Quantidade.Should().Be(10);
        custodia.PrecoMedio.Should().Be(60.00m);

        // Compra 2: 5 acoes a R$ 66.00
        custodia.AtualizarPrecoMedio(5, 66.00m);
        custodia.Quantidade.Should().Be(15);
        // PM = (10*60 + 5*66) / 15 = (600 + 330) / 15 = 62.00
        custodia.PrecoMedio.Should().Be(62.00m);
    }
}