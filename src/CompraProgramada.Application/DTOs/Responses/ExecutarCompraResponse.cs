namespace CompraProgramada.Application.DTOs.Responses;

public class ExecutarCompraResponse
{
    public DateTime DataExecucao { get; set; }
    public int TotalClientes { get; set; }
    public decimal TotalConsolidado { get; set; }
    public List<OrdemCompraItemResponse> OrdensCompra { get; set; } = new();
    public List<DistribuicaoClienteResponse> Distribuicoes { get; set; } = new();
    public List<ResiduoMasterResponse> ResiduosCustMaster { get; set; } = new();
    public int EventosIRPublicados { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}

public class OrdemCompraItemResponse
{
    public string Ticker { get; set; } = string.Empty;
    public int QuantidadeTotal { get; set; }
    public List<DetalheCompraResponse> Detalhes { get; set; } = new();
    public decimal PrecoUnitario { get; set; }
    public decimal ValorTotal { get; set; }
}

public class DetalheCompraResponse
{
    public string Tipo { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}

public class DistribuicaoClienteResponse
{
    public int ClienteId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal ValorAporte { get; set; }
    public List<AtivoDistribuidoResponse> Ativos { get; set; } = new();
}

public class AtivoDistribuidoResponse
{
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}

public class ResiduoMasterResponse
{
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}