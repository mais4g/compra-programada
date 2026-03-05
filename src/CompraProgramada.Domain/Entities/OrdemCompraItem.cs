using CompraProgramada.Domain.Enums;

namespace CompraProgramada.Domain.Entities;

public class OrdemCompraItem
{
    public int Id { get; set; }
    public int OrdemCompraId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public int QuantidadeLote { get; set; }
    public int QuantidadeFracionario { get; set; }
    public int QuantidadeTotal => QuantidadeLote + QuantidadeFracionario;
    public decimal PrecoUnitario { get; set; }
    public decimal ValorTotal => QuantidadeTotal * PrecoUnitario;

    public OrdemCompra OrdemCompra { get; set; } = null!;
}