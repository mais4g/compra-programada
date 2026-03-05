using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Infrastructure.Services.Cotahist;

public class CotahistParser
{
    private readonly ILogger<CotahistParser>? _logger;

    public CotahistParser() { }

    public CotahistParser(ILogger<CotahistParser> logger)
    {
        _logger = logger;
    }

    public IEnumerable<CotacaoB3> ParseArquivo(string caminhoArquivo)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding("ISO-8859-1");
        var cotacoes = new List<CotacaoB3>();

        if (!File.Exists(caminhoArquivo))
        {
            _logger?.LogWarning("Arquivo COTAHIST não encontrado: {Caminho}", caminhoArquivo);
            return cotacoes;
        }

        var linhaNum = 0;
        foreach (var linha in File.ReadLines(caminhoArquivo, encoding))
        {
            linhaNum++;

            if (linha.Length < 245)
                continue;

            var tipoRegistro = linha.Substring(0, 2);
            if (tipoRegistro != "01")
                continue;

            var codigoBDI = linha.Substring(10, 2).Trim();
            if (codigoBDI != "02" && codigoBDI != "96")
                continue;

            if (!int.TryParse(linha.Substring(24, 3).Trim(), out var tipoMercado))
                continue;
            if (tipoMercado != 10 && tipoMercado != 20)
                continue;

            if (!DateTime.TryParseExact(linha.Substring(2, 8), "yyyyMMdd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataPregao))
            {
                _logger?.LogWarning("Linha {Linha} ignorada: data inválida em {Arquivo}", linhaNum, caminhoArquivo);
                continue;
            }

            if (!long.TryParse(linha.Substring(152, 18).Trim(), out var quantidadeNegociada))
                continue;

            var cotacao = new CotacaoB3
            {
                DataPregao = dataPregao,
                CodigoBDI = codigoBDI,
                Ticker = linha.Substring(12, 12).Trim(),
                TipoMercado = tipoMercado,
                NomeEmpresa = linha.Substring(27, 12).Trim(),
                PrecoAbertura = ParsePreco(linha.Substring(56, 13)),
                PrecoMaximo = ParsePreco(linha.Substring(69, 13)),
                PrecoMinimo = ParsePreco(linha.Substring(82, 13)),
                PrecoMedio = ParsePreco(linha.Substring(95, 13)),
                PrecoFechamento = ParsePreco(linha.Substring(108, 13)),
                QuantidadeNegociada = quantidadeNegociada,
                VolumeNegociado = ParsePreco(linha.Substring(170, 18))
            };

            cotacoes.Add(cotacao);
        }

        return cotacoes;
    }

    private static decimal ParsePreco(string valorBruto)
    {
        if (long.TryParse(valorBruto.Trim(), out var valor))
            return valor / 100m;
        return 0m;
    }
}
