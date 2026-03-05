using CompraProgramada.Application.DTOs.Requests;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Exceptions;
using CompraProgramada.Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace CompraProgramada.UnitTests.Application;

public class ClienteServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepoMock = new();
    private readonly Mock<IHistoricoValorMensalRepository> _historicoRepoMock = new();
    private readonly Mock<ICarteiraService> _carteiraServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ClienteService _service;

    public ClienteServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _service = new ClienteService(
            _clienteRepoMock.Object,
            _historicoRepoMock.Object,
            _carteiraServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task AderirAsync_CpfUnico_DeveCriarCliente()
    {
        _clienteRepoMock.Setup(r => r.ObterPorCpfAsync(It.IsAny<string>()))
            .ReturnsAsync((Cliente?)null);

        var request = new AdesaoRequest
        {
            Nome = "João Silva",
            Cpf = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 3000m
        };

        var result = await _service.AderirAsync(request);

        result.Should().NotBeNull();
        result.Nome.Should().Be("João Silva");
        result.Cpf.Should().Be("12345678901");
        result.ValorMensal.Should().Be(3000m);
        result.Ativo.Should().BeTrue();
        result.ContaGrafica.Should().NotBeNull();
        result.ContaGrafica!.Tipo.Should().Be("FILHOTE");

        _clienteRepoMock.Verify(r => r.AdicionarAsync(It.IsAny<Cliente>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task AderirAsync_CpfDuplicado_DeveLancarExcecao()
    {
        _clienteRepoMock.Setup(r => r.ObterPorCpfAsync("12345678901"))
            .ReturnsAsync(new Cliente { Cpf = "12345678901" });

        var request = new AdesaoRequest
        {
            Nome = "João",
            Cpf = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 3000m
        };

        var act = async () => await _service.AderirAsync(request);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("CPF já cadastrado no sistema.");
    }

    [Fact]
    public async Task SairAsync_ClienteAtivo_DeveDesativar()
    {
        var cliente = new Cliente { Id = 1, Nome = "João", Ativo = true };
        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cliente);

        var result = await _service.SairAsync(1);

        result.Ativo.Should().BeFalse();
        result.DataSaida.Should().NotBeNull();
        result.Mensagem.Should().Contain("custódia foi mantida");
    }

    [Fact]
    public async Task SairAsync_ClienteInexistente_DeveLancarExcecao()
    {
        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(999)).ReturnsAsync((Cliente?)null);

        var act = async () => await _service.SairAsync(999);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Cliente não encontrado.");
    }

    [Fact]
    public async Task SairAsync_ClienteJaInativo_DeveLancarExcecao()
    {
        var cliente = new Cliente { Id = 1, Nome = "João", Ativo = false };
        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cliente);

        var act = async () => await _service.SairAsync(1);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Cliente já havia saído do produto.");
    }

    [Fact]
    public async Task AlterarValorMensalAsync_ClienteExistente_DeveAtualizar()
    {
        var cliente = new Cliente { Id = 1, Nome = "João", ValorMensal = 3000m };
        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cliente);

        var request = new AlterarValorMensalRequest { NovoValorMensal = 6000m };

        var result = await _service.AlterarValorMensalAsync(1, request);

        result.ValorMensalAnterior.Should().Be(3000m);
        result.ValorMensalNovo.Should().Be(6000m);
        _historicoRepoMock.Verify(r => r.AdicionarAsync(It.IsAny<HistoricoValorMensal>()), Times.Once);
    }

    [Fact]
    public async Task AlterarValorMensalAsync_ClienteInexistente_DeveLancarExcecao()
    {
        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(999)).ReturnsAsync((Cliente?)null);

        var request = new AlterarValorMensalRequest { NovoValorMensal = 5000m };
        var act = async () => await _service.AlterarValorMensalAsync(999, request);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Cliente não encontrado.");
    }

    [Fact]
    public async Task ConsultarPorCpfAsync_CpfExistente_DeveRetornarDados()
    {
        var cliente = new Cliente
        {
            Id = 1,
            Nome = "João Silva",
            Cpf = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 3000m,
            Ativo = true,
            DataAdesao = DateTime.UtcNow
        };
        _clienteRepoMock.Setup(r => r.ObterPorCpfAsync("12345678901")).ReturnsAsync(cliente);

        var result = await _service.ConsultarPorCpfAsync("12345678901");

        result.Should().NotBeNull();
        result.ClienteId.Should().Be(1);
        result.Nome.Should().Be("João Silva");
        result.Cpf.Should().Be("12345678901");
        result.ValorMensal.Should().Be(3000m);
    }

    [Fact]
    public async Task ConsultarPorCpfAsync_CpfInexistente_DeveLancarExcecao()
    {
        _clienteRepoMock.Setup(r => r.ObterPorCpfAsync("99999999999")).ReturnsAsync((Cliente?)null);

        var act = async () => await _service.ConsultarPorCpfAsync("99999999999");

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Cliente não encontrado.");
    }
}
