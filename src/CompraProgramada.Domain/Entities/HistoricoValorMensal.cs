namespace CompraProgramada.Domain.Entities;

public class HistoricoValorMensal
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public decimal ValorAnterior { get; set; }
    public decimal ValorNovo { get; set; }
    public DateTime DataAlteracao { get; set; } = DateTime.UtcNow;

    public Cliente Cliente { get; set; } = null!;
}