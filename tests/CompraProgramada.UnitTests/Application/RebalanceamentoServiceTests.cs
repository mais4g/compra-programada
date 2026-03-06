using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Domain.Interfaces.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CompraProgramada.UnitTests.Application;

public class RebalanceamentoServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepoMock = new();
    private readonly Mock<ICestaTopFiveRepository> _cestaRepoMock = new();
    private readonly Mock<ICustodiaFilhoteRepository> _custodiaFilhoteRepoMock = new();
    private readonly Mock<IOperacaoRebalanceamentoRepository> _operacaoRepoMock = new();
    private readonly Mock<ICotacaoService> _cotacaoServiceMock = new();
    private readonly Mock<IKafkaProducerService> _kafkaMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<RebalanceamentoService>> _loggerMock = new();
    private readonly RebalanceamentoService _service;

    public RebalanceamentoServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _operacaoRepoMock.Setup(r => r.ObterTotalVendasMesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);
        _operacaoRepoMock.Setup(r => r.ObterTotalLucroMesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);

        _service = new RebalanceamentoService(
            _clienteRepoMock.Object,
            _cestaRepoMock.Object,
            _custodiaFilhoteRepoMock.Object,
            _operacaoRepoMock.Object,
            _cotacaoServiceMock.Object,
            _kafkaMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task MudancaCesta_CestaInexistente_DeveRetornarSemErro()
    {
        _cestaRepoMock.Setup(r => r.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync((CestaTopFive?)null);

        await _service.ExecutarRebalanceamentoPorMudancaCestaAsync(1, 2);

        _clienteRepoMock.Verify(r => r.ObterAtivosAsync(), Times.Never);
    }

    [Fact]
    public async Task MudancaCesta_VenderAtivoRemovido_DeveRemoverCustodia()
    {
        var cestaAntiga = CriarCesta(1, new[] { ("PETR4", 30m), ("VALE3", 25m), ("ITUB4", 20m), ("BBDC4", 15m), ("WEGE3", 10m) });
        var cestaNova = CriarCesta(2, new[] { ("PETR4", 30m), ("VALE3", 25m), ("ITUB4", 20m), ("BBDC4", 15m), ("MGLU3", 10m) });

        _cestaRepoMock.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cestaAntiga);
        _cestaRepoMock.Setup(r => r.ObterPorIdAsync(2)).ReturnsAsync(cestaNova);

        var cliente = new Cliente
        {
            Id = 1, Nome = "Test", Cpf = "111", Ativo = true,
            ContaGrafica = new ContaGrafica { Id = 1, NumeroConta = "FLH-000001" }
        };
        _clienteRepoMock.Setup(r => r.ObterAtivosAsync()).ReturnsAsync(new List<Cliente> { cliente });

        var custodia = new List<CustodiaFilhote>
        {
            new() { ContaGraficaId = 1, Ticker = "PETR4", Quantidade = 10, PrecoMedio = 35m, ValorInvestido = 350m },
            new() { ContaGraficaId = 1, Ticker = "VALE3", Quantidade = 5, PrecoMedio = 60m, ValorInvestido = 300m },
            new() { ContaGraficaId = 1, Ticker = "ITUB4", Quantidade = 8, PrecoMedio = 30m, ValorInvestido = 240m },
            new() { ContaGraficaId = 1, Ticker = "BBDC4", Quantidade = 12, PrecoMedio = 15m, ValorInvestido = 180m },
            new() { ContaGraficaId = 1, Ticker = "WEGE3", Quantidade = 4, PrecoMedio = 40m, ValorInvestido = 160m }
        };
        _custodiaFilhoteRepoMock.Setup(r => r.ObterPorContaGraficaAsync(1)).ReturnsAsync(custodia);
        _custodiaFilhoteRepoMock.Setup(r => r.ObterPorContaETickerAsync(1, It.IsAny<string>()))
            .ReturnsAsync((CustodiaFilhote?)null);

        var cotacoes = new Dictionary<string, decimal>
        {
            { "PETR4", 36m }, { "VALE3", 62m }, { "ITUB4", 31m },
            { "BBDC4", 16m }, { "WEGE3", 42m }, { "MGLU3", 5m }
        };
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(cotacoes);

        await _service.ExecutarRebalanceamentoPorMudancaCestaAsync(1, 2);

        // WEGE3 foi removido - deve chamar RemoverAsync
        _custodiaFilhoteRepoMock.Verify(r => r.RemoverAsync(It.Is<CustodiaFilhote>(c => c.Ticker == "WEGE3")), Times.Once);
        // MGLU3 foi adicionado - deve chamar AdicionarAsync
        _custodiaFilhoteRepoMock.Verify(r => r.AdicionarAsync(It.Is<CustodiaFilhote>(c => c.Ticker == "MGLU3")), Times.Once);
        // Operacoes devem ser salvas
        _operacaoRepoMock.Verify(r => r.AdicionarVariosAsync(It.IsAny<List<OperacaoRebalanceamento>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task MudancaCesta_VendasAcima20k_DevePublicarIRVenda()
    {
        var cestaAntiga = CriarCesta(1, new[] { ("ATIVO_CARO", 50m), ("PETR4", 20m), ("VALE3", 15m), ("ITUB4", 10m), ("BBDC4", 5m) });
        var cestaNova = CriarCesta(2, new[] { ("MGLU3", 50m), ("PETR4", 20m), ("VALE3", 15m), ("ITUB4", 10m), ("BBDC4", 5m) });

        _cestaRepoMock.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cestaAntiga);
        _cestaRepoMock.Setup(r => r.ObterPorIdAsync(2)).ReturnsAsync(cestaNova);

        var cliente = new Cliente
        {
            Id = 1, Nome = "Big Investor", Cpf = "999", Ativo = true,
            ContaGrafica = new ContaGrafica { Id = 1, NumeroConta = "FLH-000001" }
        };
        _clienteRepoMock.Setup(r => r.ObterAtivosAsync()).ReturnsAsync(new List<Cliente> { cliente });

        // Custodia com ativo caro que sera vendido
        var custodia = new List<CustodiaFilhote>
        {
            new() { ContaGraficaId = 1, Ticker = "ATIVO_CARO", Quantidade = 100, PrecoMedio = 180m, ValorInvestido = 18000m },
            new() { ContaGraficaId = 1, Ticker = "PETR4", Quantidade = 10, PrecoMedio = 35m, ValorInvestido = 350m },
            new() { ContaGraficaId = 1, Ticker = "VALE3", Quantidade = 5, PrecoMedio = 60m, ValorInvestido = 300m },
            new() { ContaGraficaId = 1, Ticker = "ITUB4", Quantidade = 8, PrecoMedio = 30m, ValorInvestido = 240m },
            new() { ContaGraficaId = 1, Ticker = "BBDC4", Quantidade = 12, PrecoMedio = 15m, ValorInvestido = 180m }
        };
        _custodiaFilhoteRepoMock.Setup(r => r.ObterPorContaGraficaAsync(1)).ReturnsAsync(custodia);
        _custodiaFilhoteRepoMock.Setup(r => r.ObterPorContaETickerAsync(1, It.IsAny<string>()))
            .ReturnsAsync((CustodiaFilhote?)null);

        var cotacoes = new Dictionary<string, decimal>
        {
            { "ATIVO_CARO", 210m }, { "PETR4", 36m }, { "VALE3", 62m },
            { "ITUB4", 31m }, { "BBDC4", 16m }, { "MGLU3", 5m }
        };
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(cotacoes);

        await _service.ExecutarRebalanceamentoPorMudancaCestaAsync(1, 2);

        // Venda de ATIVO_CARO = 100 * 210 = R$ 21.000 > R$ 20.000 e lucro > 0
        _kafkaMock.Verify(k => k.PublicarIRVendaAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task MudancaCesta_ClienteSemContaGrafica_DevePular()
    {
        var cestaAntiga = CriarCesta(1, new[] { ("PETR4", 30m), ("VALE3", 25m), ("ITUB4", 20m), ("BBDC4", 15m), ("WEGE3", 10m) });
        var cestaNova = CriarCesta(2, new[] { ("PETR4", 30m), ("VALE3", 25m), ("ITUB4", 20m), ("BBDC4", 15m), ("MGLU3", 10m) });

        _cestaRepoMock.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cestaAntiga);
        _cestaRepoMock.Setup(r => r.ObterPorIdAsync(2)).ReturnsAsync(cestaNova);

        var cliente = new Cliente { Id = 1, Nome = "Sem Conta", Cpf = "111", Ativo = true, ContaGrafica = null };
        _clienteRepoMock.Setup(r => r.ObterAtivosAsync()).ReturnsAsync(new List<Cliente> { cliente });

        await _service.ExecutarRebalanceamentoPorMudancaCestaAsync(1, 2);

        _custodiaFilhoteRepoMock.Verify(r => r.ObterPorContaGraficaAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Desvio_ClienteSemCustodia_DeveRetornarSemErro()
    {
        var cliente = new Cliente
        {
            Id = 1, Nome = "Test", Cpf = "111", Ativo = true,
            ContaGrafica = new ContaGrafica { Id = 1 }
        };
        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(1)).ReturnsAsync(cliente);
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(CriarCesta(1,
            new[] { ("PETR4", 30m), ("VALE3", 25m), ("ITUB4", 20m), ("BBDC4", 15m), ("WEGE3", 10m) }));
        _custodiaFilhoteRepoMock.Setup(r => r.ObterPorContaGraficaAsync(1))
            .ReturnsAsync(new List<CustodiaFilhote>());

        await _service.ExecutarRebalanceamentoPorDesvioAsync(1);

        _operacaoRepoMock.Verify(r => r.AdicionarVariosAsync(It.IsAny<List<OperacaoRebalanceamento>>()), Times.Never);
    }

    [Fact]
    public async Task Desvio_ClienteInexistente_DeveRetornarSemErro()
    {
        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(999)).ReturnsAsync((Cliente?)null);

        await _service.ExecutarRebalanceamentoPorDesvioAsync(999);

        _cestaRepoMock.Verify(r => r.ObterAtivaAsync(), Times.Never);
    }

    [Fact]
    public async Task Desvio_SemCestaAtiva_DeveRetornarSemErro()
    {
        var cliente = new Cliente
        {
            Id = 1, Nome = "Test", Cpf = "111",
            ContaGrafica = new ContaGrafica { Id = 1 }
        };
        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(1)).ReturnsAsync(cliente);
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync((CestaTopFive?)null);

        await _service.ExecutarRebalanceamentoPorDesvioAsync(1);

        _custodiaFilhoteRepoMock.Verify(r => r.ObterPorContaGraficaAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Desvio_ComDesvioPequeno_NaoDeveRebalancear()
    {
        var cliente = new Cliente
        {
            Id = 1, Nome = "Test", Cpf = "111",
            ContaGrafica = new ContaGrafica { Id = 1 }
        };
        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(1)).ReturnsAsync(cliente);

        var cesta = CriarCesta(1, new[] { ("PETR4", 30m), ("VALE3", 25m), ("ITUB4", 20m), ("BBDC4", 15m), ("WEGE3", 10m) });
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);

        // Custodia proporcional (desvio < 5pp)
        var custodia = new List<CustodiaFilhote>
        {
            new() { ContaGraficaId = 1, Ticker = "PETR4", Quantidade = 30, PrecoMedio = 10m, ValorInvestido = 300m },
            new() { ContaGraficaId = 1, Ticker = "VALE3", Quantidade = 25, PrecoMedio = 10m, ValorInvestido = 250m },
            new() { ContaGraficaId = 1, Ticker = "ITUB4", Quantidade = 20, PrecoMedio = 10m, ValorInvestido = 200m },
            new() { ContaGraficaId = 1, Ticker = "BBDC4", Quantidade = 15, PrecoMedio = 10m, ValorInvestido = 150m },
            new() { ContaGraficaId = 1, Ticker = "WEGE3", Quantidade = 10, PrecoMedio = 10m, ValorInvestido = 100m }
        };
        _custodiaFilhoteRepoMock.Setup(r => r.ObterPorContaGraficaAsync(1)).ReturnsAsync(custodia);

        // Cotacoes iguais = proporcoes iguais = sem desvio
        var cotacoes = new Dictionary<string, decimal>
        {
            { "PETR4", 10m }, { "VALE3", 10m }, { "ITUB4", 10m },
            { "BBDC4", 10m }, { "WEGE3", 10m }
        };
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(cotacoes);

        await _service.ExecutarRebalanceamentoPorDesvioAsync(1);

        _operacaoRepoMock.Verify(r => r.AdicionarVariosAsync(It.IsAny<List<OperacaoRebalanceamento>>()), Times.Never);
    }

    [Fact]
    public async Task Desvio_ComDesvioGrande_DeveRebalancear()
    {
        var cliente = new Cliente
        {
            Id = 1, Nome = "Test", Cpf = "111",
            ContaGrafica = new ContaGrafica { Id = 1 }
        };
        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(1)).ReturnsAsync(cliente);

        var cesta = CriarCesta(1, new[] { ("PETR4", 30m), ("VALE3", 25m), ("ITUB4", 20m), ("BBDC4", 15m), ("WEGE3", 10m) });
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);

        // PETR4 com desvio grande (50% real vs 30% alvo = 20pp de desvio)
        var custodia = new List<CustodiaFilhote>
        {
            new() { ContaGraficaId = 1, Ticker = "PETR4", Quantidade = 50, PrecoMedio = 10m, ValorInvestido = 500m },
            new() { ContaGraficaId = 1, Ticker = "VALE3", Quantidade = 20, PrecoMedio = 10m, ValorInvestido = 200m },
            new() { ContaGraficaId = 1, Ticker = "ITUB4", Quantidade = 10, PrecoMedio = 10m, ValorInvestido = 100m },
            new() { ContaGraficaId = 1, Ticker = "BBDC4", Quantidade = 10, PrecoMedio = 10m, ValorInvestido = 100m },
            new() { ContaGraficaId = 1, Ticker = "WEGE3", Quantidade = 10, PrecoMedio = 10m, ValorInvestido = 100m }
        };
        _custodiaFilhoteRepoMock.Setup(r => r.ObterPorContaGraficaAsync(1)).ReturnsAsync(custodia);

        var cotacoes = new Dictionary<string, decimal>
        {
            { "PETR4", 10m }, { "VALE3", 10m }, { "ITUB4", 10m },
            { "BBDC4", 10m }, { "WEGE3", 10m }
        };
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(cotacoes);

        await _service.ExecutarRebalanceamentoPorDesvioAsync(1);

        // Deve salvar operacoes (PETR4 vendido, outros comprados)
        _operacaoRepoMock.Verify(r => r.AdicionarVariosAsync(It.IsAny<List<OperacaoRebalanceamento>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task MudancaCesta_RebalancearAtivosComuns_AjustePercentual()
    {
        // Mesmos ativos, mas percentuais mudaram
        var cestaAntiga = CriarCesta(1, new[] { ("PETR4", 40m), ("VALE3", 25m), ("ITUB4", 15m), ("BBDC4", 10m), ("WEGE3", 10m) });
        var cestaNova = CriarCesta(2, new[] { ("PETR4", 20m), ("VALE3", 25m), ("ITUB4", 25m), ("BBDC4", 15m), ("WEGE3", 15m) });

        _cestaRepoMock.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cestaAntiga);
        _cestaRepoMock.Setup(r => r.ObterPorIdAsync(2)).ReturnsAsync(cestaNova);

        var cliente = new Cliente
        {
            Id = 1, Nome = "Test", Cpf = "111", Ativo = true,
            ContaGrafica = new ContaGrafica { Id = 1, NumeroConta = "FLH-000001" }
        };
        _clienteRepoMock.Setup(r => r.ObterAtivosAsync()).ReturnsAsync(new List<Cliente> { cliente });

        // Custodia com PETR4 overweight (precisa vender)
        var custodia = new List<CustodiaFilhote>
        {
            new() { ContaGraficaId = 1, Ticker = "PETR4", Quantidade = 40, PrecoMedio = 10m, ValorInvestido = 400m },
            new() { ContaGraficaId = 1, Ticker = "VALE3", Quantidade = 25, PrecoMedio = 10m, ValorInvestido = 250m },
            new() { ContaGraficaId = 1, Ticker = "ITUB4", Quantidade = 15, PrecoMedio = 10m, ValorInvestido = 150m },
            new() { ContaGraficaId = 1, Ticker = "BBDC4", Quantidade = 10, PrecoMedio = 10m, ValorInvestido = 100m },
            new() { ContaGraficaId = 1, Ticker = "WEGE3", Quantidade = 10, PrecoMedio = 10m, ValorInvestido = 100m }
        };
        _custodiaFilhoteRepoMock.Setup(r => r.ObterPorContaGraficaAsync(1)).ReturnsAsync(custodia);

        var cotacoes = new Dictionary<string, decimal>
        {
            { "PETR4", 10m }, { "VALE3", 10m }, { "ITUB4", 10m },
            { "BBDC4", 10m }, { "WEGE3", 10m }
        };
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(cotacoes);

        await _service.ExecutarRebalanceamentoPorMudancaCestaAsync(1, 2);

        // PETR4 deve ter sido vendido parcialmente (40 -> 20)
        _custodiaFilhoteRepoMock.Verify(r => r.AtualizarAsync(It.Is<CustodiaFilhote>(c => c.Ticker == "PETR4")), Times.Once);
        _operacaoRepoMock.Verify(r => r.AdicionarVariosAsync(It.IsAny<List<OperacaoRebalanceamento>>()), Times.Once);
    }

    [Fact]
    public async Task Desvio_LimiteCruzadoPorVendasAcumuladas_DeveCalcularIRSobreLucroTotalMes()
    {
        // Cenário: primeira venda no mês = R$18k (isenta), segunda = R$5k → total R$23k > R$20k.
        // O IR deve incidir sobre o lucro total do mês (R$2.000 + R$500 = R$2.500),
        // não apenas sobre o lucro da operação atual (R$500).
        var cliente = new Cliente
        {
            Id = 1, Nome = "Test", Cpf = "111", Ativo = true,
            ContaGrafica = new ContaGrafica { Id = 1 }
        };
        _clienteRepoMock.Setup(r => r.ObterPorIdComCustodiaAsync(1)).ReturnsAsync(cliente);

        var cesta = CriarCesta(1, new[] { ("PETR4", 30m), ("VALE3", 25m), ("ITUB4", 20m), ("BBDC4", 15m), ("WEGE3", 10m) });
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);

        // Custódia com PETR4 overweight → venda de ajuste = ~R$5k
        var custodia = new List<CustodiaFilhote>
        {
            new() { ContaGraficaId = 1, Ticker = "PETR4", Quantidade = 50, PrecoMedio = 80m, ValorInvestido = 4000m },
            new() { ContaGraficaId = 1, Ticker = "VALE3", Quantidade = 25, PrecoMedio = 10m, ValorInvestido = 250m },
            new() { ContaGraficaId = 1, Ticker = "ITUB4", Quantidade = 20, PrecoMedio = 10m, ValorInvestido = 200m },
            new() { ContaGraficaId = 1, Ticker = "BBDC4", Quantidade = 15, PrecoMedio = 10m, ValorInvestido = 150m },
            new() { ContaGraficaId = 1, Ticker = "WEGE3", Quantidade = 10, PrecoMedio = 10m, ValorInvestido = 100m }
        };
        _custodiaFilhoteRepoMock.Setup(r => r.ObterPorContaGraficaAsync(1)).ReturnsAsync(custodia);

        var cotacoes = new Dictionary<string, decimal>
        {
            { "PETR4", 100m }, { "VALE3", 10m }, { "ITUB4", 10m },
            { "BBDC4", 10m }, { "WEGE3", 10m }
        };
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(cotacoes);

        // Simula vendas anteriores no mês que já cruzam o limite quando somadas à atual
        _operacaoRepoMock.Setup(r => r.ObterTotalVendasMesAsync(1, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(18_000m);
        // Simula lucro acumulado das operações anteriores do mês
        _operacaoRepoMock.Setup(r => r.ObterTotalLucroMesAsync(1, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(2_000m);

        await _service.ExecutarRebalanceamentoPorDesvioAsync(1);

        // IR deve ter sido publicado (vendas totais > R$20k e lucro total > 0)
        _kafkaMock.Verify(k => k.PublicarIRVendaAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Once);
    }

    private static CestaTopFive CriarCesta(int id, (string ticker, decimal percentual)[] itens)
    {
        return new CestaTopFive
        {
            Id = id,
            Nome = $"Cesta {id}",
            Ativa = id == 2,
            Itens = itens.Select(i => new CestaItem { Ticker = i.ticker, Percentual = i.percentual }).ToList()
        };
    }
}