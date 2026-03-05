using CompraProgramada.Application.DTOs.Requests;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Exceptions;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Application.Services;

public class CestaService : ICestaService
{
    private readonly ICestaTopFiveRepository _cestaRepository;
    private readonly ICotacaoService _cotacaoService;
    private readonly IRebalanceamentoService _rebalanceamentoService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CestaService> _logger;

    public CestaService(
        ICestaTopFiveRepository cestaRepository,
        ICotacaoService cotacaoService,
        IRebalanceamentoService rebalanceamentoService,
        IUnitOfWork unitOfWork,
        ILogger<CestaService> logger)
    {
        _cestaRepository = cestaRepository;
        _cotacaoService = cotacaoService;
        _rebalanceamentoService = rebalanceamentoService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CestaResponse> CadastrarOuAlterarAsync(CestaRequest request)
    {
        if (request.Itens.Count != 5)
            throw new DomainException(
                $"A cesta deve conter exatamente 5 ativos. Quantidade informada: {request.Itens.Count}.",
                ErrorCodes.QuantidadeAtivosInvalida);

        var tickersDuplicados = request.Itens
            .GroupBy(i => i.Ticker.ToUpperInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (tickersDuplicados.Any())
            throw new DomainException(
                $"Tickers duplicados na cesta: {string.Join(", ", tickersDuplicados)}.",
                ErrorCodes.TickersDuplicados);

        var itemComPercentualInvalido = request.Itens.FirstOrDefault(i => i.Percentual <= 0);
        if (itemComPercentualInvalido != null)
            throw new DomainException(
                $"Cada percentual deve ser maior que 0%. Ativo {itemComPercentualInvalido.Ticker} possui percentual {itemComPercentualInvalido.Percentual}%.",
                ErrorCodes.PercentualZeroOuNegativo);

        var somaPercentuais = request.Itens.Sum(i => i.Percentual);
        if (somaPercentuais != 100m)
            throw new DomainException(
                $"A soma dos percentuais deve ser exatamente 100%. Soma atual: {somaPercentuais}%.",
                ErrorCodes.PercentuaisInvalidos);

        var cestaAnterior = await _cestaRepository.ObterAtivaAsync();
        CestaDesativadaResponse? cestaDesativadaResponse = null;
        var rebalanceamentoDisparado = false;
        List<string>? ativosRemovidos = null;
        List<string>? ativosAdicionados = null;

        if (cestaAnterior != null)
        {
            cestaAnterior.Desativar();
            await _cestaRepository.AtualizarAsync(cestaAnterior);

            cestaDesativadaResponse = new CestaDesativadaResponse
            {
                CestaId = cestaAnterior.Id,
                Nome = cestaAnterior.Nome,
                DataDesativacao = cestaAnterior.DataDesativacao!.Value
            };

            var tickersAntigos = cestaAnterior.Itens.Select(i => i.Ticker).ToHashSet();
            var tickersNovos = request.Itens.Select(i => i.Ticker).ToHashSet();

            ativosRemovidos = tickersAntigos.Except(tickersNovos).ToList();
            ativosAdicionados = tickersNovos.Except(tickersAntigos).ToList();
            rebalanceamentoDisparado = true;
        }

        var novaCesta = new CestaTopFive
        {
            Nome = request.Nome,
            Ativa = true,
            DataCriacao = DateTime.UtcNow,
            Itens = request.Itens.Select(i => new CestaItem
            {
                Ticker = i.Ticker.ToUpper(),
                Percentual = i.Percentual
            }).ToList()
        };

        await _cestaRepository.AdicionarAsync(novaCesta);
        await _unitOfWork.CommitAsync();

        if (rebalanceamentoDisparado && cestaAnterior != null)
        {
            await _rebalanceamentoService.ExecutarRebalanceamentoPorMudancaCestaAsync(
                cestaAnterior.Id, novaCesta.Id);
        }

        var mensagem = cestaAnterior == null
            ? "Primeira cesta cadastrada com sucesso."
            : $"Cesta atualizada. Rebalanceamento disparado para clientes ativos.";

        return new CestaResponse
        {
            CestaId = novaCesta.Id,
            Nome = novaCesta.Nome,
            Ativa = true,
            DataCriacao = novaCesta.DataCriacao,
            Itens = novaCesta.Itens.Select(i => new CestaItemResponse
            {
                Ticker = i.Ticker,
                Percentual = i.Percentual
            }).ToList(),
            CestaAnteriorDesativada = cestaDesativadaResponse,
            RebalanceamentoDisparado = rebalanceamentoDisparado,
            AtivosRemovidos = ativosRemovidos,
            AtivosAdicionados = ativosAdicionados,
            Mensagem = mensagem
        };
    }

    public async Task<CestaResponse> ObterAtualAsync()
    {
        var cesta = await _cestaRepository.ObterAtivaAsync()
            ?? throw new DomainException("Nenhuma cesta ativa encontrada.", ErrorCodes.CestaNaoEncontrada);

        var tickers = cesta.Itens.Select(i => i.Ticker).ToList();
        Dictionary<string, decimal> cotacoes;
        try
        {
            cotacoes = await _cotacaoService.ObterCotacoesFechamentoAsync(tickers);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao obter cotações para cesta ativa.");
            cotacoes = new Dictionary<string, decimal>();
        }

        return new CestaResponse
        {
            CestaId = cesta.Id,
            Nome = cesta.Nome,
            Ativa = true,
            DataCriacao = cesta.DataCriacao,
            Itens = cesta.Itens.Select(i => new CestaItemResponse
            {
                Ticker = i.Ticker,
                Percentual = i.Percentual,
                CotacaoAtual = cotacoes.GetValueOrDefault(i.Ticker)
            }).ToList()
        };
    }

    public async Task<CestaHistoricoResponse> ObterHistoricoAsync()
    {
        var cestas = await _cestaRepository.ObterHistoricoAsync();

        return new CestaHistoricoResponse
        {
            Cestas = cestas.Select(c => new CestaResponse
            {
                CestaId = c.Id,
                Nome = c.Nome,
                Ativa = c.Ativa,
                DataCriacao = c.DataCriacao,
                DataDesativacao = c.DataDesativacao,
                Itens = c.Itens.Select(i => new CestaItemResponse
                {
                    Ticker = i.Ticker,
                    Percentual = i.Percentual
                }).ToList()
            }).ToList()
        };
    }
}