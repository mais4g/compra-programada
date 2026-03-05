using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Domain.Interfaces.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CompraProgramada.UnitTests.Application;

public class CustodiaMasterServiceTests
{
    private readonly Mock<ICustodiaMasterRepository> _custodiaMasterRepoMock = new();
    private readonly Mock<ICotacaoService> _cotacaoServiceMock = new();
    private readonly Mock<ILogger<CustodiaMasterService>> _loggerMock = new();
    private readonly CustodiaMasterService _service;

    public CustodiaMasterServiceTests()
    {
        _service = new CustodiaMasterService(
            _custodiaMasterRepoMock.Object,
            _cotacaoServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ConsultarCustodia_ComItens_DeveCalcularValorAtual()
    {
        var itens = new List<CustodiaMaster>
        {
            new() { Ticker = "PETR4", Quantidade = 5, PrecoMedio = 35m, Origem = "Resíduo distribuição 2026-02-05" },
            new() { Ticker = "VALE3", Quantidade = 3, PrecoMedio = 60m, Origem = "Resíduo distribuição 2026-02-05" }
        };
        _custodiaMasterRepoMock.Setup(r => r.ObterTodosAsync()).ReturnsAsync(itens);

        var cotacoes = new Dictionary<string, decimal>
        {
            { "PETR4", 37m }, { "VALE3", 62m }
        };
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(cotacoes);

        var result = await _service.ConsultarCustodiaAsync();

        result.Custodia.Should().HaveCount(2);
        result.Custodia[0].Ticker.Should().Be("PETR4");
        result.Custodia[0].ValorAtual.Should().Be(5 * 37m); // 185
        result.Custodia[0].Origem.Should().Be("Resíduo distribuição 2026-02-05");
        result.Custodia[1].ValorAtual.Should().Be(3 * 62m); // 186
        result.ValorTotalResiduo.Should().Be(185m + 186m);
    }

    [Fact]
    public async Task ConsultarCustodia_CotacaoFalha_UsaPrecoMedio()
    {
        var itens = new List<CustodiaMaster>
        {
            new() { Ticker = "PETR4", Quantidade = 5, PrecoMedio = 35m }
        };
        _custodiaMasterRepoMock.Setup(r => r.ObterTodosAsync()).ReturnsAsync(itens);

        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ThrowsAsync(new Exception("Arquivo COTAHIST nao encontrado"));

        var result = await _service.ConsultarCustodiaAsync();

        result.Custodia.Should().HaveCount(1);
        result.Custodia[0].ValorAtual.Should().Be(5 * 35m); // Fallback para preco medio
    }
}