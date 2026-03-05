using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Application.DTOs.Requests;

public class CestaRequest
{
    [Required(ErrorMessage = "Nome da cesta é obrigatório.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Itens da cesta são obrigatórios.")]
    public List<CestaItemRequest> Itens { get; set; } = new();
}

public class CestaItemRequest
{
    [Required]
    public string Ticker { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 100)]
    public decimal Percentual { get; set; }
}