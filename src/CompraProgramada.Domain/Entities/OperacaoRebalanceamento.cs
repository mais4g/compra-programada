using CompraProgramada.Domain.Enums;

namespace CompraProgramada.Domain.Entities;

public class OperacaoRebalanceamento
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public TipoOperacao TipoOperacao { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal PrecoMedio { get; set; }
    public decimal Lucro { get; set; }
    public DateTime DataOperacao { get; set; } = DateTime.UtcNow;
    public int? CestaOrigemId { get; set; }
    public int? CestaDestinoId { get; set; }

    public Cliente Cliente { get; set; } = null!;
}