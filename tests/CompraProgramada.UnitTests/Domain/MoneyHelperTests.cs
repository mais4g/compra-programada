using CompraProgramada.Domain.Helpers;
using FluentAssertions;

namespace CompraProgramada.UnitTests.Domain;

public class MoneyHelperTests
{
    [Theory]
    [InlineData(10.005, 10.01)]
    [InlineData(10.004, 10.00)]
    [InlineData(100.999, 101.00)]
    [InlineData(0, 0)]
    public void ArredondarMoeda_DeveArredondarPara2CasasDecimais(decimal entrada, decimal esperado)
    {
        MoneyHelper.ArredondarMoeda(entrada).Should().Be(esperado);
    }

    [Theory]
    [InlineData(33.333, 33.33)]
    [InlineData(66.666, 66.67)]
    [InlineData(100, 100)]
    public void ArredondarPercentual_DeveArredondarPara2CasasDecimais(decimal entrada, decimal esperado)
    {
        MoneyHelper.ArredondarPercentual(entrada).Should().Be(esperado);
    }

    [Theory]
    [InlineData(10.9, 10)]
    [InlineData(10.1, 10)]
    [InlineData(9.99, 9)]
    [InlineData(0.99, 0)]
    [InlineData(100, 100)]
    public void TruncarQuantidade_DeveTruncarSemArredondar(decimal entrada, int esperado)
    {
        MoneyHelper.TruncarQuantidade(entrada).Should().Be(esperado);
    }

    [Fact]
    public void TruncarQuantidade_NuncaDistribuiMaisDoQueComprou()
    {
        // 3 clientes com proporção igual, 10 ações disponíveis → cada um recebe 3, 1 fica no master
        var totalDisponivel = 10m;
        var proporcao = 1m / 3m;

        var quantidadePorCliente = MoneyHelper.TruncarQuantidade(proporcao * totalDisponivel);
        var distribuicaoTotal = quantidadePorCliente * 3;

        quantidadePorCliente.Should().Be(3);
        distribuicaoTotal.Should().BeLessOrEqualTo((int)totalDisponivel);
    }
}
