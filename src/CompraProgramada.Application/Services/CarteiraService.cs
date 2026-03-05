using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Exceptions;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Domain.Interfaces.Services;

namespace CompraProgramada.Application.Services;

public class CarteiraService : ICarteiraService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IDistribuicaoRepository _distribuicaoRepository;
    private readonly ICotacaoService _cotacaoService;

    public CarteiraService(
        IClienteRepository clienteRepository,
        IDistribuicaoRepository distribuicaoRepository,
        ICotacaoService cotacaoService)
    {
        _clienteRepository = clienteRepository;
        _distribuicaoRepository = distribuicaoRepository;
        _cotacaoService = cotacaoService;
    }

    public async Task<CarteiraResponse> ConsultarCarteiraAsync(int clienteId)
    {
        var cliente = await _clienteRepository.ObterPorIdComCustodiaAsync(clienteId)
            ?? throw new DomainException("Cliente não encontrado.", ErrorCodes.ClienteNaoEncontrado);

        var custodia = cliente.ContaGrafica?.CustodiaFilhote?.ToList()
            ?? new List<CustodiaFilhote>();
        var tickers = custodia.Select(c => c.Ticker).ToList();

        var cotacoes = tickers.Any()
            ? await _cotacaoService.ObterCotacoesFechamentoAsync(tickers)
            : new Dictionary<string, decimal>();

        var ativos = CalcularAtivos(custodia, cotacoes);
        var resumo = CalcularResumo(custodia, ativos);

        return new CarteiraResponse
        {
            ClienteId = cliente.Id,
            Nome = cliente.Nome,
            ContaGrafica = cliente.ContaGrafica?.NumeroConta ?? "",
            DataConsulta = DateTime.UtcNow,
            Resumo = resumo,
            Ativos = ativos
        };
    }

    public async Task<RentabilidadeResponse> ConsultarRentabilidadeAsync(int clienteId)
    {
        var carteira = await ConsultarCarteiraAsync(clienteId);
        var distribuicoes = await _distribuicaoRepository.ObterPorClienteAsync(clienteId);

        var contagemParcelas = new Dictionary<string, int>();
        var historicoAportes = distribuicoes
            .GroupBy(d => d.DataDistribuicao.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var mesKey = g.Key.ToString("yyyy-MM");
                contagemParcelas.TryGetValue(mesKey, out var count);
                count++;
                contagemParcelas[mesKey] = count;

                return new HistoricoAporteResponse
                {
                    Data = g.Key,
                    Valor = g.Sum(d => d.Quantidade * d.PrecoUnitario),
                    Parcela = $"{count}/{RegrasFinanceiras.ParcelasPorMes}"
                };
            }).ToList();

        decimal acumuladoInvestido = 0;
        var evolucao = historicoAportes.Select(h =>
        {
            acumuladoInvestido += h.Valor;
            return new EvolucaoCarteiraResponse
            {
                Data = h.Data,
                ValorInvestido = Math.Round(acumuladoInvestido, 2),
                ValorCarteira = Math.Round(acumuladoInvestido, 2),
                Rentabilidade = 0
            };
        }).ToList();

        if (evolucao.Any())
        {
            var ultimo = evolucao.Last();
            ultimo.ValorCarteira = carteira.Resumo.ValorAtualCarteira;
            ultimo.Rentabilidade = carteira.Resumo.RentabilidadePercentual;
        }

        return new RentabilidadeResponse
        {
            ClienteId = carteira.ClienteId,
            Nome = carteira.Nome,
            DataConsulta = DateTime.UtcNow,
            Rentabilidade = carteira.Resumo,
            HistoricoAportes = historicoAportes,
            EvolucaoCarteira = evolucao
        };
    }

    private static List<AtivoCarteiraResponse> CalcularAtivos(
        List<CustodiaFilhote> custodia, Dictionary<string, decimal> cotacoes)
    {
        var ativos = custodia.Select(c =>
        {
            var cotacaoAtual = cotacoes.GetValueOrDefault(c.Ticker, c.PrecoMedio);
            var valorAtual = c.Quantidade * cotacaoAtual;
            var pl = c.Quantidade * (cotacaoAtual - c.PrecoMedio);
            var plPercentual = c.PrecoMedio > 0
                ? Math.Round((cotacaoAtual - c.PrecoMedio) / c.PrecoMedio * 100, 2)
                : 0m;

            return new AtivoCarteiraResponse
            {
                Ticker = c.Ticker,
                Quantidade = c.Quantidade,
                PrecoMedio = Math.Round(c.PrecoMedio, 2),
                CotacaoAtual = cotacaoAtual,
                ValorAtual = Math.Round(valorAtual, 2),
                Pl = Math.Round(pl, 2),
                PlPercentual = plPercentual
            };
        }).ToList();

        var valorTotalAtual = ativos.Sum(a => a.ValorAtual);
        foreach (var ativo in ativos)
        {
            ativo.ComposicaoCarteira = valorTotalAtual > 0
                ? Math.Round(ativo.ValorAtual / valorTotalAtual * 100, 2)
                : 0m;
        }

        return ativos;
    }

    private static ResumoCarteiraResponse CalcularResumo(
        List<CustodiaFilhote> custodia, List<AtivoCarteiraResponse> ativos)
    {
        var valorTotalAtual = ativos.Sum(a => a.ValorAtual);
        var valorInvestido = custodia.Sum(c => c.ValorInvestido);
        var plTotal = valorTotalAtual - valorInvestido;
        var rentabilidade = valorInvestido > 0
            ? Math.Round(plTotal / valorInvestido * 100, 2)
            : 0m;

        return new ResumoCarteiraResponse
        {
            ValorTotalInvestido = Math.Round(valorInvestido, 2),
            ValorAtualCarteira = Math.Round(valorTotalAtual, 2),
            PlTotal = Math.Round(plTotal, 2),
            RentabilidadePercentual = rentabilidade
        };
    }
}
