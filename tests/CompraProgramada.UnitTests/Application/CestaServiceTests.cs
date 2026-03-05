using CompraProgramada.Application.DTOs.Requests;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Exceptions;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Domain.Interfaces.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CompraProgramada.UnitTests.Application;

public class CestaServiceTests
{
    private readonly Mock<ICestaTopFiveRepository> _cestaRepoMock = new();
    private readonly Mock<ICotacaoService> _cotacaoServiceMock = new();
    private readonly Mock<IRebalanceamentoService> _rebalanceamentoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<CestaService>> _loggerMock = new();
    private readonly CestaService _service;

    public CestaServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _service = new CestaService(
            _cestaRepoMock.Object,
            _cotacaoServiceMock.Object,
            _rebalanceamentoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CadastrarOuAlterarAsync_PrimeiraCesta_DeveCriarSemRebalanceamento()
    {
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync((CestaTopFive?)null);

        var request = CriarCestaRequestValida();

        var result = await _service.CadastrarOuAlterarAsync(request);

        result.Ativa.Should().BeTrue();
        result.RebalanceamentoDisparado.Should().BeFalse();
        result.Mensagem.Should().Contain("Primeira cesta");
        result.Itens.Should().HaveCount(5);
    }

    [Fact]
    public async Task CadastrarOuAlterarAsync_CestaExistente_DeveDispararRebalanceamento()
    {
        var cestaAnterior = new CestaTopFive
        {
            Id = 1, Nome = "Antiga", Ativa = true,
            Itens = new List<CestaItem>
            {
                new() { Ticker = "PETR4", Percentual = 30 },
                new() { Ticker = "VALE3", Percentual = 25 },
                new() { Ticker = "ITUB4", Percentual = 20 },
                new() { Ticker = "BBDC4", Percentual = 15 },
                new() { Ticker = "WEGE3", Percentual = 10 }
            }
        };
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cestaAnterior);

        var request = CriarCestaRequestValida();

        var result = await _service.CadastrarOuAlterarAsync(request);

        result.RebalanceamentoDisparado.Should().BeTrue();
        result.CestaAnteriorDesativada.Should().NotBeNull();
        _rebalanceamentoMock.Verify(r => r.ExecutarRebalanceamentoPorMudancaCestaAsync(
            It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task CadastrarOuAlterarAsync_QuantidadeInvalida_DeveLancarExcecao()
    {
        var request = new CestaRequest
        {
            Nome = "Test",
            Itens = new List<CestaItemRequest>
            {
                new() { Ticker = "PETR4", Percentual = 50 },
                new() { Ticker = "VALE3", Percentual = 50 }
            }
        };

        var act = async () => await _service.CadastrarOuAlterarAsync(request);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*exatamente 5 ativos*");
    }

    [Fact]
    public async Task CadastrarOuAlterarAsync_PercentualInvalido_DeveLancarExcecao()
    {
        var request = new CestaRequest
        {
            Nome = "Test",
            Itens = new List<CestaItemRequest>
            {
                new() { Ticker = "PETR4", Percentual = 20 },
                new() { Ticker = "VALE3", Percentual = 20 },
                new() { Ticker = "ITUB4", Percentual = 20 },
                new() { Ticker = "BBDC4", Percentual = 20 },
                new() { Ticker = "WEGE3", Percentual = 15 } // Soma = 95%
            }
        };

        var act = async () => await _service.CadastrarOuAlterarAsync(request);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*100%*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CadastrarOuAlterarAsync_PercentualInvalidoNoItem_DeveLancarExcecao(decimal percentualInvalido)
    {
        var request = new CestaRequest
        {
            Nome = "Test",
            Itens = new List<CestaItemRequest>
            {
                new() { Ticker = "PETR4", Percentual = 30 },
                new() { Ticker = "VALE3", Percentual = 25 },
                new() { Ticker = "ITUB4", Percentual = 20 },
                new() { Ticker = "BBDC4", Percentual = 25 },
                new() { Ticker = "WEGE3", Percentual = percentualInvalido }
            }
        };

        var act = async () => await _service.CadastrarOuAlterarAsync(request);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*maior que 0%*");
    }

    [Fact]
    public async Task ObterAtualAsync_SemCesta_DeveLancarExcecao()
    {
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync((CestaTopFive?)null);

        var act = async () => await _service.ObterAtualAsync();

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Nenhuma cesta ativa encontrada.");
    }

    [Fact]
    public async Task ObterAtualAsync_ComCesta_DeveRetornarComCotacoes()
    {
        var cesta = new CestaTopFive
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
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal>
            {
                { "PETR4", 35m }, { "VALE3", 62m }, { "ITUB4", 30m },
                { "BBDC4", 15m }, { "WEGE3", 40m }
            });

        var result = await _service.ObterAtualAsync();

        result.Ativa.Should().BeTrue();
        result.Itens.Should().HaveCount(5);
        result.Itens.First(i => i.Ticker == "PETR4").CotacaoAtual.Should().Be(35m);
    }

    [Fact]
    public async Task ObterAtualAsync_CotacaoFalha_DeveRetornarSemCotacoes()
    {
        var cesta = new CestaTopFive
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
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);
        _cotacaoServiceMock.Setup(s => s.ObterCotacoesFechamentoAsync(It.IsAny<IEnumerable<string>>()))
            .ThrowsAsync(new Exception("COTAHIST nao encontrado"));

        var result = await _service.ObterAtualAsync();

        result.Ativa.Should().BeTrue();
        result.Itens.Should().HaveCount(5);
        result.Itens.First().CotacaoAtual.Should().Be(0);
    }

    [Fact]
    public async Task CadastrarOuAlterarAsync_CestaComAtivosAlterados_DeveDetectarMudancas()
    {
        var cestaAnterior = new CestaTopFive
        {
            Id = 1, Nome = "Antiga", Ativa = true,
            Itens = new List<CestaItem>
            {
                new() { Ticker = "PETR4", Percentual = 30 },
                new() { Ticker = "VALE3", Percentual = 25 },
                new() { Ticker = "ITUB4", Percentual = 20 },
                new() { Ticker = "BBDC4", Percentual = 15 },
                new() { Ticker = "WEGE3", Percentual = 10 }
            }
        };
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cestaAnterior);

        // Nova cesta com MGLU3 no lugar de WEGE3
        var request = new CestaRequest
        {
            Nome = "Top Five Nova",
            Itens = new List<CestaItemRequest>
            {
                new() { Ticker = "PETR4", Percentual = 30 },
                new() { Ticker = "VALE3", Percentual = 25 },
                new() { Ticker = "ITUB4", Percentual = 20 },
                new() { Ticker = "BBDC4", Percentual = 15 },
                new() { Ticker = "MGLU3", Percentual = 10 }
            }
        };

        var result = await _service.CadastrarOuAlterarAsync(request);

        result.AtivosRemovidos.Should().Contain("WEGE3");
        result.AtivosAdicionados.Should().Contain("MGLU3");
        result.RebalanceamentoDisparado.Should().BeTrue();
    }

    private static CestaRequest CriarCestaRequestValida() => new()
    {
        Nome = "Top Five - Teste",
        Itens = new List<CestaItemRequest>
        {
            new() { Ticker = "PETR4", Percentual = 30 },
            new() { Ticker = "VALE3", Percentual = 25 },
            new() { Ticker = "ITUB4", Percentual = 20 },
            new() { Ticker = "BBDC4", Percentual = 15 },
            new() { Ticker = "WEGE3", Percentual = 10 }
        }
    };
}