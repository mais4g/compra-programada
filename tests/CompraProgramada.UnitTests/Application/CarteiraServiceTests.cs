using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Exceptions;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Domain.Interfaces.Services;
using FluentAssertions;
using Moq;

namespace CompraProgramada.UnitTests.Application;

public class CarteiraServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepoMock = new();
    private readonly Mock<IDistribuicaoRepository> _distribuicaoRepoMock = new();
    private readonly Mock<ICotacaoService> _cotacaoServiceMock = new();
    private readonly CarteiraService _service;

    public CarteiraServiceTests()
    {
        _service = new CarteiraService(
            _clienteRepoMock.Object,
            _distribuicaoRepoMock.Object,
            _cotacaoServiceMock.Object);
    }

    [Fact]
    public async Task ConsultarCarteiraAsync_ClienteComAtivos_DeveRetornarCarteira()
    {
        var cliente = CriarClienteComCustodia(
            new CustodiaFilhote { Ticker = "PETR4", Quantidade = 10, PrecoMedio = 35m, ValorInvestido = 350m });

        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(1)).ReturnsAsync(cliente);
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal> { { "PETR4", 37m } });

        var result = await _service.ConsultarCarteiraAsync(1);

        result.Ativos.Should().HaveCount(1);
        result.Ativos[0].Ticker.Should().Be("PETR4");
        result.Ativos[0].CotacaoAtual.Should().Be(37m);
        result.Ativos[0].Pl.Should().Be(20m);
        result.Resumo.ValorAtualCarteira.Should().Be(370m);
    }

    [Fact]
    public async Task ConsultarCarteiraAsync_ClienteInexistente_DeveLancarExcecao()
    {
        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(999))
            .ReturnsAsync((Cliente?)null);

        var act = async () => await _service.ConsultarCarteiraAsync(999);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Cliente não encontrado.");
    }

    [Fact]
    public async Task ConsultarCarteiraAsync_SemCustodia_DeveRetornarCarteiraVazia()
    {
        var cliente = CriarClienteComCustodia();

        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(1)).ReturnsAsync(cliente);

        var result = await _service.ConsultarCarteiraAsync(1);

        result.Ativos.Should().BeEmpty();
        result.Resumo.ValorAtualCarteira.Should().Be(0);
        result.Resumo.RentabilidadePercentual.Should().Be(0);
    }

    [Fact]
    public async Task ConsultarCarteiraAsync_PrecoMedioZero_PlPercentualDeveSerZero()
    {
        var cliente = CriarClienteComCustodia(
            new CustodiaFilhote { Ticker = "PETR4", Quantidade = 10, PrecoMedio = 0m, ValorInvestido = 0m });

        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(1)).ReturnsAsync(cliente);
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal> { { "PETR4", 35m } });

        var result = await _service.ConsultarCarteiraAsync(1);

        result.Ativos[0].PlPercentual.Should().Be(0m);
    }

    [Fact]
    public async Task ConsultarCarteiraAsync_ComPrejuizo_PlDeveSerNegativo()
    {
        var cliente = CriarClienteComCustodia(
            new CustodiaFilhote { Ticker = "PETR4", Quantidade = 10, PrecoMedio = 40m, ValorInvestido = 400m });

        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(1)).ReturnsAsync(cliente);
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal> { { "PETR4", 35m } });

        var result = await _service.ConsultarCarteiraAsync(1);

        result.Ativos[0].Pl.Should().Be(-50m);
        result.Ativos[0].PlPercentual.Should().Be(-12.5m);
        result.Resumo.PlTotal.Should().Be(-50m);
    }

    [Fact]
    public async Task ConsultarCarteiraAsync_MultiplosAtivos_ComposicaoDeveSomar100()
    {
        var cliente = CriarClienteComCustodia(
            new CustodiaFilhote { Ticker = "PETR4", Quantidade = 10, PrecoMedio = 35m, ValorInvestido = 350m },
            new CustodiaFilhote { Ticker = "VALE3", Quantidade = 5, PrecoMedio = 60m, ValorInvestido = 300m });

        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(1)).ReturnsAsync(cliente);
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal> { { "PETR4", 37m }, { "VALE3", 65m } });

        var result = await _service.ConsultarCarteiraAsync(1);

        var totalComposicao = result.Ativos.Sum(a => a.ComposicaoCarteira);
        totalComposicao.Should().Be(100m);
    }

    [Fact]
    public async Task ConsultarCarteiraAsync_CotacaoNaoDisponivel_DeveFallbackParaPrecoMedio()
    {
        var cliente = CriarClienteComCustodia(
            new CustodiaFilhote { Ticker = "PETR4", Quantidade = 10, PrecoMedio = 35m, ValorInvestido = 350m });

        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(1)).ReturnsAsync(cliente);
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal>());

        var result = await _service.ConsultarCarteiraAsync(1);

        result.Ativos[0].CotacaoAtual.Should().Be(35m);
        result.Ativos[0].Pl.Should().Be(0m);
    }

    [Fact]
    public async Task ConsultarRentabilidadeAsync_ComDistribuicoes_DeveRetornarEvolucao()
    {
        var cliente = CriarClienteComCustodia(
            new CustodiaFilhote { Ticker = "PETR4", Quantidade = 20, PrecoMedio = 35m, ValorInvestido = 700m });

        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(1)).ReturnsAsync(cliente);
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal> { { "PETR4", 37m } });

        var distribuicoes = new List<Distribuicao>
        {
            new() { ClienteId = 1, Ticker = "PETR4", Quantidade = 10, PrecoUnitario = 35m, DataDistribuicao = new DateTime(2026, 1, 5) },
            new() { ClienteId = 1, Ticker = "PETR4", Quantidade = 10, PrecoUnitario = 35m, DataDistribuicao = new DateTime(2026, 1, 15) }
        };
        _distribuicaoRepoMock.Setup(r => r.ObterPorClienteAsync(1)).ReturnsAsync(distribuicoes);

        var result = await _service.ConsultarRentabilidadeAsync(1);

        result.ClienteId.Should().Be(1);
        result.HistoricoAportes.Should().HaveCount(2);
        result.HistoricoAportes[0].Parcela.Should().Be("1/3");
        result.HistoricoAportes[1].Parcela.Should().Be("2/3");
        result.EvolucaoCarteira.Should().HaveCount(2);
    }

    [Fact]
    public async Task ConsultarRentabilidadeAsync_SemDistribuicoes_DeveRetornarVazio()
    {
        var cliente = CriarClienteComCustodia();

        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(1)).ReturnsAsync(cliente);
        _distribuicaoRepoMock.Setup(r => r.ObterPorClienteAsync(1)).ReturnsAsync(new List<Distribuicao>());

        var result = await _service.ConsultarRentabilidadeAsync(1);

        result.HistoricoAportes.Should().BeEmpty();
        result.EvolucaoCarteira.Should().BeEmpty();
    }

    private static Cliente CriarClienteComCustodia(params CustodiaFilhote[] custodia)
    {
        return new Cliente
        {
            Id = 1,
            Nome = "Joao",
            ContaGrafica = new ContaGrafica
            {
                Id = 1,
                NumeroConta = "FLH-000001",
                CustodiaFilhote = custodia.ToList()
            },
            Distribuicoes = new List<Distribuicao>()
        };
    }
}
