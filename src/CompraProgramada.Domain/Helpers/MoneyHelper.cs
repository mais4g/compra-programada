namespace CompraProgramada.Domain.Helpers;

/// <summary>
/// Centraliza operações de arredondamento financeiro para o mercado brasileiro (BRL/B3).
/// Quantidades de ações usam truncamento para nunca distribuir mais do que foi comprado.
/// </summary>
public static class MoneyHelper
{
    public static decimal ArredondarMoeda(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    public static decimal ArredondarPercentual(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Trunca sem arredondar. Resíduos ficam na conta master para a próxima execução.
    /// </summary>
    public static int TruncarQuantidade(decimal valor) =>
        (int)Math.Truncate(valor);
}
