namespace CompraProgramada.Application.DTOs.Responses;

public class RentabilidadeResponse
{
    public int ClienteId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataConsulta { get; set; }
    public ResumoCarteiraResponse Rentabilidade { get; set; } = new();
    public List<HistoricoAporteResponse> HistoricoAportes { get; set; } = new();
    public List<EvolucaoCarteiraResponse> EvolucaoCarteira { get; set; } = new();
}

public class HistoricoAporteResponse
{
    public DateTime Data { get; set; }
    public decimal Valor { get; set; }
    public string Parcela { get; set; } = string.Empty;
}

public class EvolucaoCarteiraResponse
{
    public DateTime Data { get; set; }
    public decimal ValorCarteira { get; set; }
    public decimal ValorInvestido { get; set; }
    public decimal Rentabilidade { get; set; }
}