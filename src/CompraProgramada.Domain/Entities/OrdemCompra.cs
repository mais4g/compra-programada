namespace CompraProgramada.Domain.Entities;

public class OrdemCompra
{
    public int Id { get; set; }
    public DateTime DataExecucao { get; set; }
    public DateTime DataReferencia { get; set; }
    public decimal ValorTotalConsolidado { get; set; }
    public int TotalClientes { get; set; }

    public ICollection<OrdemCompraItem> Itens { get; set; } = new List<OrdemCompraItem>();
    public ICollection<Distribuicao> Distribuicoes { get; set; } = new List<Distribuicao>();
}