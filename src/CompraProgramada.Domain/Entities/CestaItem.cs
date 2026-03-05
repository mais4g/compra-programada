namespace CompraProgramada.Domain.Entities;

public class CestaItem
{
    public int Id { get; set; }
    public int CestaTopFiveId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal Percentual { get; set; }

    public CestaTopFive CestaTopFive { get; set; } = null!;
}