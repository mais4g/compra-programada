using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Application.DTOs.Requests;

public class AlterarValorMensalRequest
{
    [Required(ErrorMessage = "Novo valor mensal é obrigatório.")]
    [Range(100, double.MaxValue, ErrorMessage = "O valor mensal mínimo é de R$ 100,00.")]
    public decimal NovoValorMensal { get; set; }
}