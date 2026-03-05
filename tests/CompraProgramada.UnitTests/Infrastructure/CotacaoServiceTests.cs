using CompraProgramada.Domain.Exceptions;
using CompraProgramada.Infrastructure.Services.Cotahist;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CompraProgramada.UnitTests.Infrastructure;

public class CotacaoServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly CotahistParser _parser = new();
    private readonly Mock<ILogger<CotacaoService>> _loggerMock = new();

    public CotacaoServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cotahist_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CriarArquivoCotahist(string nomeArquivo, params (string ticker, int tipoMercado, decimal precoFechamento)[] cotacoes)
    {
        var caminho = Path.Combine(_tempDir, nomeArquivo);
        var linhas = new List<string>();

        linhas.Add("00COTAHIST.2026" + new string(' ', 230));

        foreach (var (ticker, tipoMercado, precoFechamento) in cotacoes)
        {
            var precoInt = (long)(precoFechamento * 100);
            var tickerPadded = ticker.PadRight(12);
            var tipoStr = tipoMercado.ToString().PadLeft(3, '0');
            var bdi = "02";
            var empresa = "EMPRESA     ";

            var linha = "01" +                              // tipo registro (0-1)
                        "20260228" +                        // data pregao (2-9)
                        bdi +                               // codigo BDI (10-11)
                        tickerPadded +                      // ticker (12-23)
                        tipoStr +                           // tipo mercado (24-26)
                        empresa +                           // nome empresa (27-38)
                        new string(' ', 17) +               // padding (39-55)
                        precoInt.ToString().PadLeft(13, '0') + // preco abertura (56-68)
                        precoInt.ToString().PadLeft(13, '0') + // preco maximo (69-81)
                        precoInt.ToString().PadLeft(13, '0') + // preco minimo (82-94)
                        precoInt.ToString().PadLeft(13, '0') + // preco medio (95-107)
                        precoInt.ToString().PadLeft(13, '0') + // preco fechamento (108-120)
                        new string(' ', 31) +               // padding (121-151)
                        "000000000000100000" +               // quantidade negociada (152-169)
                        precoInt.ToString().PadLeft(18, '0'); // volume (170-187)

            while (linha.Length < 245)
                linha += " ";

            linhas.Add(linha);
        }

        linhas.Add("99" + new string(' ', 243));
        File.WriteAllLines(caminho, linhas, System.Text.Encoding.GetEncoding("ISO-8859-1"));
        return caminho;
    }

    [Fact]
    public async Task ObterCotacaoFechamento_TickerExistente_DeveRetornarPreco()
    {
        CriarArquivoCotahist("COTAHIST_D28022026.TXT",
            ("PETR4", 10, 38.50m));

        var service = new CotacaoService(_parser, _tempDir, _loggerMock.Object);
        var preco = await service.ObterCotacaoFechamentoAsync("PETR4");

        preco.Should().Be(38.50m);
    }

    [Fact]
    public async Task ObterCotacaoFechamento_TickerInexistente_DeveLancarDomainException()
    {
        CriarArquivoCotahist("COTAHIST_D28022026.TXT",
            ("VALE3", 10, 60.00m));

        var service = new CotacaoService(_parser, _tempDir, _loggerMock.Object);

        var act = async () => await service.ObterCotacaoFechamentoAsync("PETR4");

        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.Codigo == "COTACAO_NAO_ENCONTRADA");
    }

    [Fact]
    public async Task ObterCotacoesFechamento_MultiplosTickers_DeveRetornarTodosEncontrados()
    {
        CriarArquivoCotahist("COTAHIST_D28022026.TXT",
            ("PETR4", 10, 38.50m),
            ("VALE3", 10, 62.00m),
            ("ITUB4", 10, 25.30m));

        var service = new CotacaoService(_parser, _tempDir, _loggerMock.Object);
        var cotacoes = await service.ObterCotacoesFechamentoAsync(new[] { "PETR4", "VALE3", "ITUB4" });

        cotacoes.Should().HaveCount(3);
        cotacoes["PETR4"].Should().Be(38.50m);
        cotacoes["VALE3"].Should().Be(62.00m);
        cotacoes["ITUB4"].Should().Be(25.30m);
    }

    [Fact]
    public async Task ObterCotacaoFechamento_DevePriorizarArquivoMaisRecente()
    {
        CriarArquivoCotahist("COTAHIST_D27022026.TXT",
            ("PETR4", 10, 36.00m));
        CriarArquivoCotahist("COTAHIST_D28022026.TXT",
            ("PETR4", 10, 38.50m));

        var service = new CotacaoService(_parser, _tempDir, _loggerMock.Object);
        var preco = await service.ObterCotacaoFechamentoAsync("PETR4");

        preco.Should().Be(38.50m);
    }
}
