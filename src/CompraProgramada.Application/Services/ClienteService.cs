using CompraProgramada.Application.DTOs.Requests;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using CompraProgramada.Domain.Exceptions;
using CompraProgramada.Domain.Interfaces.Repositories;

namespace CompraProgramada.Application.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IHistoricoValorMensalRepository _historicoRepository;
    private readonly ICarteiraService _carteiraService;
    private readonly IUnitOfWork _unitOfWork;

    public ClienteService(
        IClienteRepository clienteRepository,
        IHistoricoValorMensalRepository historicoRepository,
        ICarteiraService carteiraService,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _historicoRepository = historicoRepository;
        _carteiraService = carteiraService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AdesaoResponse> AderirAsync(AdesaoRequest request)
    {
        var existente = await _clienteRepository.ObterPorCpfAsync(request.Cpf);
        if (existente != null)
            throw new DomainException("CPF já cadastrado no sistema.", ErrorCodes.ClienteCpfDuplicado);

        var cliente = new Cliente
        {
            Nome = request.Nome,
            Cpf = request.Cpf,
            Email = request.Email,
            ValorMensal = request.ValorMensal,
            Ativo = true,
            DataAdesao = DateTime.UtcNow
        };

        await _clienteRepository.AdicionarAsync(cliente);
        await _unitOfWork.CommitAsync();

        var contaGrafica = new ContaGrafica
        {
            ClienteId = cliente.Id,
            NumeroConta = $"FLH-{cliente.Id:D6}",
            Tipo = TipoConta.Filhote,
            DataCriacao = DateTime.UtcNow,
            CustodiaFilhote = new List<CustodiaFilhote>()
        };

        cliente.ContaGrafica = contaGrafica;
        await _unitOfWork.CommitAsync();

        return new AdesaoResponse
        {
            ClienteId = cliente.Id,
            Nome = cliente.Nome,
            Cpf = cliente.Cpf,
            Email = cliente.Email,
            ValorMensal = cliente.ValorMensal,
            Ativo = cliente.Ativo,
            DataAdesao = cliente.DataAdesao,
            ContaGrafica = new ContaGraficaResponse
            {
                Id = contaGrafica.Id,
                NumeroConta = contaGrafica.NumeroConta,
                Tipo = "FILHOTE",
                DataCriacao = contaGrafica.DataCriacao
            }
        };
    }

    public async Task<SaidaResponse> SairAsync(int clienteId)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(clienteId)
            ?? throw new DomainException("Cliente não encontrado.", ErrorCodes.ClienteNaoEncontrado);

        cliente.Desativar();
        await _clienteRepository.AtualizarAsync(cliente);
        await _unitOfWork.CommitAsync();

        return new SaidaResponse
        {
            ClienteId = cliente.Id,
            Nome = cliente.Nome,
            Ativo = false,
            DataSaida = cliente.DataSaida,
            Mensagem = "Adesão encerrada. Sua posição em custódia foi mantida."
        };
    }

    public async Task<AlterarValorMensalResponse> AlterarValorMensalAsync(int clienteId, AlterarValorMensalRequest request)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(clienteId)
            ?? throw new DomainException("Cliente não encontrado.", ErrorCodes.ClienteNaoEncontrado);

        var valorAnterior = cliente.AlterarValorMensal(request.NovoValorMensal);

        var historico = new HistoricoValorMensal
        {
            ClienteId = cliente.Id,
            ValorAnterior = valorAnterior,
            ValorNovo = request.NovoValorMensal,
            DataAlteracao = DateTime.UtcNow
        };

        await _historicoRepository.AdicionarAsync(historico);
        await _clienteRepository.AtualizarAsync(cliente);
        await _unitOfWork.CommitAsync();

        return new AlterarValorMensalResponse
        {
            ClienteId = cliente.Id,
            ValorMensalAnterior = valorAnterior,
            ValorMensalNovo = request.NovoValorMensal,
            DataAlteracao = historico.DataAlteracao,
            Mensagem = "Valor mensal atualizado. O novo valor será considerado a partir da próxima data de compra."
        };
    }

    public async Task<CarteiraResponse> ConsultarCarteiraAsync(int clienteId)
    {
        return await _carteiraService.ConsultarCarteiraAsync(clienteId);
    }

    public async Task<RentabilidadeResponse> ConsultarRentabilidadeAsync(int clienteId)
    {
        return await _carteiraService.ConsultarRentabilidadeAsync(clienteId);
    }

    public async Task<AdesaoResponse> ConsultarPorCpfAsync(string cpf)
    {
        var cliente = await _clienteRepository.ObterPorCpfAsync(cpf)
            ?? throw new DomainException("Cliente não encontrado.", ErrorCodes.ClienteNaoEncontrado);

        return new AdesaoResponse
        {
            ClienteId = cliente.Id,
            Nome = cliente.Nome,
            Cpf = cliente.Cpf,
            Email = cliente.Email,
            ValorMensal = cliente.ValorMensal,
            Ativo = cliente.Ativo,
            DataAdesao = cliente.DataAdesao,
            ContaGrafica = cliente.ContaGrafica != null
                ? new ContaGraficaResponse
                {
                    Id = cliente.ContaGrafica.Id,
                    NumeroConta = cliente.ContaGrafica.NumeroConta,
                    Tipo = cliente.ContaGrafica.Tipo.ToString().ToUpper(),
                    DataCriacao = cliente.ContaGrafica.DataCriacao
                }
                : null
        };
    }
}
