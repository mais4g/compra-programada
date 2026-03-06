using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Exceptions;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Domain.Interfaces.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CompraProgramada.UnitTests.Application;

public class MotorCompraServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepoMock = new();
    private readonly Mock<ICestaTopFiveRepository> _cestaRepoMock = new();
    private readonly Mock<ICustodiaMasterRepository> _custodiaMasterRepoMock = new();
    private readonly Mock<ICustodiaFilhoteRepository> _custodiaFilhoteRepoMock = new();
    private readonly Mock<IOrdemCompraRepository> _ordemCompraRepoMock = new();
    private readonly Mock<IDistribuicaoRepository> _distribuicaoRepoMock = new();
    private readonly Mock<ICotacaoService> _cotacaoServiceMock = new();
    private readonly Mock<IKafkaProducerService> _kafkaMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<MotorCompraService>> _loggerMock = new();
    private readonly MotorCompraService _service;

    public MotorCompraServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        // Executa o delegate real para que os testes exercitem a lógica interna da transação
        _unitOfWorkMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> op) => op());
        _ordemCompraRepoMock.Setup(r => r.ObterPorDataReferenciaAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((OrdemCompra?)null);
        _custodiaMasterRepoMock.Setup(r => r.ObterTodosAsync())
            .ReturnsAsync(new List<CustodiaMaster>());
        _custodiaFilhoteRepoMock.Setup(r => r.ObterPorContaETickerAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((CustodiaFilhote?)null);

        _service = new MotorCompraService(
            _clienteRepoMock.Object,
            _cestaRepoMock.Object,
            _custodiaMasterRepoMock.Object,
            _custodiaFilhoteRepoMock.Object,
            _ordemCompraRepoMock.Object,
            _distribuicaoRepoMock.Object,
            _cotacaoServiceMock.Object,
            _kafkaMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecutarCompraAsync_FluxoCompleto_DeveExecutarCorretamente()
    {
        // Arrange
        var cesta = CriarCestaValida();
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);

        var clientes = new List<Cliente>
        {
            new() { Id = 1, Nome = "Cliente A", ValorMensal = 3000m, Cpf = "111",
                ContaGrafica = new ContaGrafica { Id = 1, NumeroConta = "FLH-000001" } },
            new() { Id = 2, Nome = "Cliente B", ValorMensal = 6000m, Cpf = "222",
                ContaGrafica = new ContaGrafica { Id = 2, NumeroConta = "FLH-000002" } }
        };
        _clienteRepoMock.Setup(r => r.ObterAtivosAsync()).ReturnsAsync(clientes);

        var cotacoes = new Dictionary<string, decimal>
        {
            { "PETR4", 35m }, { "VALE3", 62m }, { "ITUB4", 30m },
            { "BBDC4", 15m }, { "WEGE3", 40m }
        };
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(cotacoes);

        // Act
        var result = await _service.ExecutarCompraAsync(new DateTime(2026, 2, 5));

        // Assert
        result.TotalClientes.Should().Be(2);
        result.TotalConsolidado.Should().Be(3000m); // (3000/3) + (6000/3) = 1000 + 2000 = 3000
        result.OrdensCompra.Should().HaveCount(5);
        result.Distribuicoes.Should().HaveCount(2);
        result.Mensagem.Should().Contain("2 clientes");
    }

    [Fact]
    public async Task ExecutarCompraAsync_CompraJaExecutada_DeveLancarExcecao()
    {
        _ordemCompraRepoMock.Setup(r => r.ObterPorDataReferenciaAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new OrdemCompra());

        var act = async () => await _service.ExecutarCompraAsync(new DateTime(2026, 2, 5));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Compra já foi executada para esta data.");
    }

    [Fact]
    public async Task ExecutarCompraAsync_SemCesta_DeveLancarExcecao()
    {
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync((CestaTopFive?)null);

        var act = async () => await _service.ExecutarCompraAsync(new DateTime(2026, 2, 5));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Nenhuma cesta ativa encontrada.");
    }

    [Fact]
    public async Task ExecutarCompraAsync_SemClientesAtivos_DeveLancarExcecao()
    {
        var cesta = CriarCestaValida();
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);
        _clienteRepoMock.Setup(r => r.ObterAtivosAsync()).ReturnsAsync(new List<Cliente>());

        var act = async () => await _service.ExecutarCompraAsync(new DateTime(2026, 2, 5));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Nenhum cliente ativo encontrado.");
    }

    [Fact]
    public async Task ExecutarCompraAsync_ComSaldoMaster_DeveDescontarDoTotal()
    {
        var cesta = CriarCestaValida();
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);

        _clienteRepoMock.Setup(r => r.ObterAtivosAsync()).ReturnsAsync(new List<Cliente>
        {
            new() { Id = 1, Nome = "A", ValorMensal = 3000m, Cpf = "111",
                ContaGrafica = new ContaGrafica { Id = 1 } }
        });

        _custodiaMasterRepoMock.Setup(r => r.ObterTodosAsync())
            .ReturnsAsync(new List<CustodiaMaster>
            {
                new() { Ticker = "PETR4", Quantidade = 5, PrecoMedio = 34m }
            });
        _custodiaMasterRepoMock.Setup(r => r.ObterPorTickerAsync(It.IsAny<string>()))
            .ReturnsAsync((CustodiaMaster?)null);

        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal>
            {
                { "PETR4", 35m }, { "VALE3", 62m }, { "ITUB4", 30m },
                { "BBDC4", 15m }, { "WEGE3", 40m }
            });

        var result = await _service.ExecutarCompraAsync(new DateTime(2026, 2, 5));

        result.Should().NotBeNull();
        _custodiaMasterRepoMock.Verify(r => r.AtualizarAsync(It.Is<CustodiaMaster>(
            cm => cm.Ticker == "PETR4" && cm.Quantidade == 0)), Times.Once);
    }

    [Fact]
    public async Task ExecutarCompraAsync_KafkaFalha_NaoDeveBloquear()
    {
        var cesta = CriarCestaValida();
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);
        _clienteRepoMock.Setup(r => r.ObterAtivosAsync()).ReturnsAsync(new List<Cliente>
        {
            new() { Id = 1, Nome = "A", ValorMensal = 3000m, Cpf = "111",
                ContaGrafica = new ContaGrafica { Id = 1 } }
        });
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal>
            {
                { "PETR4", 35m }, { "VALE3", 62m }, { "ITUB4", 30m },
                { "BBDC4", 15m }, { "WEGE3", 40m }
            });

        _kafkaMock.Setup(k => k.PublicarIRDedoDuroAsync(It.IsAny<object>()))
            .ThrowsAsync(new Exception("Kafka offline"));

        var act = async () => await _service.ExecutarCompraAsync(new DateTime(2026, 2, 5));

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(28)]
    public async Task ExecutarCompraAsync_DataForaDiasDeCompra_DeveLancarExcecao(int dia)
    {
        var act = async () => await _service.ExecutarCompraAsync(new DateTime(2026, 3, dia));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*dias 5, 15 ou 25*");
    }

    private static CestaTopFive CriarCestaValida() => new()
    {
        Id = 1, Nome = "Top Five", Ativa = true,
        Itens = new List<CestaItem>
        {
            new() { Ticker = "PETR4", Percentual = 30 },
            new() { Ticker = "VALE3", Percentual = 25 },
            new() { Ticker = "ITUB4", Percentual = 20 },
            new() { Ticker = "BBDC4", Percentual = 15 },
            new() { Ticker = "WEGE3", Percentual = 10 }
        }
    };
}