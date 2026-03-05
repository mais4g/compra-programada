namespace CompraProgramada.Application.DTOs.Responses;

public class AdesaoResponse
{
    public int ClienteId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal ValorMensal { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataAdesao { get; set; }
    public ContaGraficaResponse? ContaGrafica { get; set; }
}

public class ContaGraficaResponse
{
    public int Id { get; set; }
    public string NumeroConta { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}