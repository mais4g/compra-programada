namespace CompraProgramada.Domain.Entities;

public class Distribuicao
{
    public int Id { get; set; }
    public int OrdemCompraId { get; set; }
    public int ClienteId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal ValorOperacao => Quantidade * PrecoUnitario;
    public decimal ValorIRDedoDuro { get; set; }
    public DateTime DataDistribuicao { get; set; } = DateTime.UtcNow;

    public OrdemCompra OrdemCompra { get; set; } = null!;
    public Cliente Cliente { get; set; } = null!;
}