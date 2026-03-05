namespace CompraProgramada.Application.DTOs.Responses;

public class CustodiaMasterResponse
{
    public ContaMasterResponse ContaMaster { get; set; } = new();
    public List<CustodiaMasterItemResponse> Custodia { get; set; } = new();
    public decimal ValorTotalResiduo { get; set; }
}

public class ContaMasterResponse
{
    public int Id { get; set; } = 1;
    public string NumeroConta { get; set; } = "MST-000001";
    public string Tipo { get; set; } = "MASTER";
}

public class CustodiaMasterItemResponse
{
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoMedio { get; set; }
    public decimal ValorAtual { get; set; }
    public string Origem { get; set; } = string.Empty;
}