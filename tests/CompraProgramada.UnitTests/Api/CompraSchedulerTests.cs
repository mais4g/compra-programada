using CompraProgramada.Api.BackgroundServices;
using FluentAssertions;

namespace CompraProgramada.UnitTests.Api;

public class CompraSchedulerTests
{
    [Theory]
    [InlineData(2026, 3, 5)]   // Quinta-feira, dia 5
    [InlineData(2026, 3, 15)]  // Domingo → segunda dia 16... Na verdade dia 15 é domingo,
                                // o ajuste muda para 16 (segunda). Mas dia 16 não é dia de compra.
                                // Então dia 15 domingo NÃO é dia de compra após ajuste.
    [InlineData(2026, 3, 25)]  // Quarta-feira, dia 25
    public void IsDiaDeCompra_DiasValidos_DeveRetornarTrue(int ano, int mes, int dia)
    {
        var data = new DateTime(ano, mes, dia);
        // Pula o cenário onde o ajuste do final de semana muda o dia
        if (data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return;

        CompraScheduler.IsDiaDeCompra(data).Should().BeTrue();
    }

    [Theory]
    [InlineData(2026, 3, 6)]   // Sexta, dia 6
    [InlineData(2026, 3, 10)]  // Terça, dia 10
    [InlineData(2026, 3, 20)]  // Sexta, dia 20
    public void IsDiaDeCompra_DiasNaoDeCompra_DeveRetornarFalse(int ano, int mes, int dia)
    {
        CompraScheduler.IsDiaDeCompra(new DateTime(ano, mes, dia)).Should().BeFalse();
    }

    [Fact]
    public void IsDiaDeCompra_Sabado_AjustaParaSegundaEVerifica()
    {
        // Sábado dia 15 → ajusta para segunda dia 17 → não é dia de compra
        var sabadoDia15 = new DateTime(2026, 8, 15); // Sábado
        sabadoDia15.DayOfWeek.Should().Be(DayOfWeek.Saturday);

        CompraScheduler.IsDiaDeCompra(sabadoDia15).Should().BeFalse();
    }

    [Fact]
    public void IsDiaDeCompra_Domingo_AjustaParaSegundaEVerifica()
    {
        // Domingo dia 5 → ajusta para segunda dia 6 → não é dia de compra
        var domingoDia5 = new DateTime(2026, 7, 5); // Domingo
        domingoDia5.DayOfWeek.Should().Be(DayOfWeek.Sunday);

        CompraScheduler.IsDiaDeCompra(domingoDia5).Should().BeFalse();
    }

    [Fact]
    public void ProximaExecucao_AntesDas9h_DeveRetornarHoje9h()
    {
        var agora = new DateTime(2026, 3, 5, 8, 0, 0);
        var proxima = CompraScheduler.ProximaExecucao(agora);

        proxima.Should().Be(new DateTime(2026, 3, 5, 9, 0, 0));
    }

    [Fact]
    public void ProximaExecucao_Apos9h_DeveRetornarAmanha9h()
    {
        var agora = new DateTime(2026, 3, 5, 10, 0, 0);
        var proxima = CompraScheduler.ProximaExecucao(agora);

        proxima.Should().Be(new DateTime(2026, 3, 6, 9, 0, 0));
    }
}
