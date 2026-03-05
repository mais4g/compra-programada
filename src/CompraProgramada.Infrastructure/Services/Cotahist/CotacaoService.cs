using CompraProgramada.Domain;
using CompraProgramada.Domain.Exceptions;
using CompraProgramada.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Infrastructure.Services.Cotahist;

public class CotacaoService : ICotacaoService
{
    private readonly CotahistParser _parser;
    private readonly string _pastaCotacoes;
    private readonly ILogger<CotacaoService> _logger;

    public CotacaoService(CotahistParser parser, string pastaCotacoes, ILogger<CotacaoService> logger)
    {
        _parser = parser;
        _pastaCotacoes = pastaCotacoes;
        _logger = logger;
    }

    public Task<decimal> ObterCotacaoFechamentoAsync(string ticker)
    {
        var cotacao = ObterCotacaoMaisRecente(ticker);
        if (cotacao == null)
            throw new DomainException($"Cotação não encontrada para o ticker {ticker}.", ErrorCodes.CotacaoNaoEncontrada);

        return Task.FromResult(cotacao.PrecoFechamento);
    }

    public Task<Dictionary<string, decimal>> ObterCotacoesFechamentoAsync(IEnumerable<string> tickers)
    {
        var resultado = new Dictionary<string, decimal>();
        var tickerList = tickers.ToList();

        var arquivos = ObterArquivosOrdenados();
        if (!arquivos.Any())
        {
            _logger.LogWarning("Nenhum arquivo COTAHIST encontrado na pasta {Pasta}.", _pastaCotacoes);
            throw new DomainException("Nenhum arquivo COTAHIST encontrado.", ErrorCodes.CotacaoNaoEncontrada);
        }

        foreach (var arquivo in arquivos)
        {
            var cotacoes = _parser.ParseArquivo(arquivo);

            foreach (var ticker in tickerList.ToList())
            {
                var cotacao = cotacoes
                    .Where(c => c.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase))
                    .Where(c => c.TipoMercado == 10)
                    .FirstOrDefault();

                if (cotacao != null)
                {
                    resultado[ticker] = cotacao.PrecoFechamento;
                    tickerList.Remove(ticker);
                }
            }

            if (!tickerList.Any()) break;
        }

        if (tickerList.Any())
        {
            _logger.LogWarning("Cotações não encontradas para: {Tickers}", string.Join(", ", tickerList));
        }

        return Task.FromResult(resultado);
    }

    private CotacaoB3? ObterCotacaoMaisRecente(string ticker)
    {
        var arquivos = ObterArquivosOrdenados();

        foreach (var arquivo in arquivos)
        {
            var cotacao = _parser.ParseArquivo(arquivo)
                .Where(c => c.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase))
                .Where(c => c.TipoMercado == 10)
                .FirstOrDefault();

            if (cotacao != null) return cotacao;
        }

        return null;
    }

    private string[] ObterArquivosOrdenados()
    {
        if (!Directory.Exists(_pastaCotacoes))
            return Array.Empty<string>();

        return Directory.GetFiles(_pastaCotacoes, "COTAHIST_D*.TXT")
            .OrderByDescending(f => ExtrairDataDoNomeArquivo(f))
            .ToArray();
    }

    private static DateTime ExtrairDataDoNomeArquivo(string caminho)
    {
        var nome = Path.GetFileNameWithoutExtension(caminho);
        var dataParte = nome.Replace("COTAHIST_D", "");

        // Suporta DDMMYYYY (formato real da B3) e YYYYMMDD
        if (dataParte.Length == 8)
        {
            if (DateTime.TryParseExact(dataParte, "ddMMyyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dataDDMM))
                return dataDDMM;

            if (DateTime.TryParseExact(dataParte, "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dataYYYY))
                return dataYYYY;
        }

        return DateTime.MinValue;
    }
}