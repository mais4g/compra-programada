using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Application.Services;

public class CustodiaMasterService : ICustodiaMasterService
{
    private readonly ICustodiaMasterRepository _custodiaMasterRepository;
    private readonly ICotacaoService _cotacaoService;
    private readonly ILogger<CustodiaMasterService> _logger;

    public CustodiaMasterService(
        ICustodiaMasterRepository custodiaMasterRepository,
        ICotacaoService cotacaoService,
        ILogger<CustodiaMasterService> logger)
    {
        _custodiaMasterRepository = custodiaMasterRepository;
        _cotacaoService = cotacaoService;
        _logger = logger;
    }

    public async Task<CustodiaMasterResponse> ConsultarCustodiaAsync()
    {
        var itens = await _custodiaMasterRepository.ObterTodosAsync();
        var tickers = itens.Where(i => i.Quantidade > 0).Select(i => i.Ticker).ToList();

        Dictionary<string, decimal> cotacoes;
        try
        {
            cotacoes = tickers.Any()
                ? await _cotacaoService.ObterCotacoesFechamentoAsync(tickers)
                : new Dictionary<string, decimal>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao obter cotações para custódia master.");
            cotacoes = new Dictionary<string, decimal>();
        }

        var custodiaItens = itens
            .Where(i => i.Quantidade > 0)
            .Select(i => new CustodiaMasterItemResponse
            {
                Ticker = i.Ticker,
                Quantidade = i.Quantidade,
                PrecoMedio = i.PrecoMedio,
                ValorAtual = i.Quantidade * cotacoes.GetValueOrDefault(i.Ticker, i.PrecoMedio),
                Origem = i.Origem
            }).ToList();

        return new CustodiaMasterResponse
        {
            ContaMaster = new ContaMasterResponse(),
            Custodia = custodiaItens,
            ValorTotalResiduo = custodiaItens.Sum(c => c.ValorAtual)
        };
    }
}
