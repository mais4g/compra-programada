using CompraProgramada.Infrastructure.Services.Cotahist;
using FluentAssertions;

namespace CompraProgramada.UnitTests.Infrastructure;

public class CotahistParserTests
{
    private readonly CotahistParser _parser = new();

    // Monta linha COTAHIST com posicoes corretas
    private static string MontarLinhaCotahist(
        string ticker, int tipoMercado, string codbdi = "02",
        string data = "20260225", decimal precoAbertura = 35.20m,
        decimal precoMaximo = 36.50m, decimal precoMinimo = 34.80m,
        decimal precoMedio = 35.60m, decimal precoFechamento = 35.80m)
    {
        // TIPREG(2) DATPRE(8) CODBDI(2) CODNEG(12) TPMERC(3) NOMRES(12) ESPECI(10) PRAZOT(3) MODREF(4)
        // PREABE(13) PREMAX(13) PREMIN(13) PREMED(13) PREULT(13) PREOFC(13) PREOFV(13)
        // TOTNEG(5) QUATOT(18) VOLTOT(18) + rest padded to 245
        var sb = new System.Text.StringBuilder();
        sb.Append("01");                                          // TIPREG
        sb.Append(data);                                          // DATPRE
        sb.Append(codbdi.PadRight(2));                            // CODBDI
        sb.Append(ticker.PadRight(12));                           // CODNEG
        sb.Append(tipoMercado.ToString("D3"));                   // TPMERC
        sb.Append("PETROBRAS   ");                                // NOMRES (12)
        sb.Append("PN      N1");                                  // ESPECI (10)
        sb.Append("   ");                                         // PRAZOT (3)
        sb.Append("R$  ");                                        // MODREF (4)
        sb.Append(((long)(precoAbertura * 100)).ToString("D13")); // PREABE
        sb.Append(((long)(precoMaximo * 100)).ToString("D13"));   // PREMAX
        sb.Append(((long)(precoMinimo * 100)).ToString("D13"));   // PREMIN
        sb.Append(((long)(precoMedio * 100)).ToString("D13"));    // PREMED
        sb.Append(((long)(precoFechamento * 100)).ToString("D13")); // PREULT
        sb.Append("0000000003570");                               // PREOFC (13)
        sb.Append("0000000003590");                               // PREOFV (13)
        sb.Append("34561");                                       // TOTNEG (5)
        sb.Append("000000000150000000");                          // QUATOT (18)
        sb.Append("000000005376000000");                          // VOLTOT (18)
        return sb.ToString().PadRight(245);
    }

    [Fact]
    public void ParseArquivo_ComLinhaValida_DeveRetornarCotacao()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var linha = MontarLinhaCotahist("PETR4", 10);
        File.WriteAllText(tempFile, linha);

        try
        {
            // Act
            var cotacoes = _parser.ParseArquivo(tempFile).ToList();

            // Assert
            cotacoes.Should().HaveCount(1);
            var cotacao = cotacoes[0];
            cotacao.Ticker.Should().Be("PETR4");
            cotacao.TipoMercado.Should().Be(10);
            cotacao.DataPregao.Should().Be(new DateTime(2026, 2, 25));
            cotacao.PrecoFechamento.Should().Be(35.80m);
            cotacao.PrecoAbertura.Should().Be(35.20m);
            cotacao.PrecoMaximo.Should().Be(36.50m);
            cotacao.PrecoMinimo.Should().Be(34.80m);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseArquivo_CodigoBDIInvalido_DeveFiltrar()
    {
        var tempFile = Path.GetTempFileName();
        var linha = MontarLinhaCotahist("HGLG11", 10, codbdi: "78");
        File.WriteAllText(tempFile, linha);

        try
        {
            var cotacoes = _parser.ParseArquivo(tempFile).ToList();
            cotacoes.Should().BeEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseArquivo_MercadoFracionario_DeveIncluir()
    {
        var tempFile = Path.GetTempFileName();
        var linha = MontarLinhaCotahist("PETR4F", 20, codbdi: "96");
        File.WriteAllText(tempFile, linha);

        try
        {
            var cotacoes = _parser.ParseArquivo(tempFile).ToList();
            cotacoes.Should().HaveCount(1);
            cotacoes[0].TipoMercado.Should().Be(20);
            cotacoes[0].Ticker.Should().Be("PETR4F");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}