namespace CompraProgramada.Domain;

public static class RegrasFinanceiras
{
    public const int ParcelasPorMes = 3;
    public const int TamanhoLotePadrao = 100;
    public const decimal TaxaIRDedoDuro = 0.00005m;
    public const decimal LimiarDesvioPercentual = 5m;
    public const decimal LimiteIsencaoIR = 20_000m;
    public const decimal AliquotaIRVenda = 0.20m;
    public const decimal ToleranciaRebalanceamento = 1m;
    public static readonly int[] DiasDeCompra = { 5, 15, 25 };
}
