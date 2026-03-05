using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Application.DTOs.Requests;

public class AdesaoRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "CPF é obrigatório.")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "CPF deve ter 11 dígitos.")]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valor mensal é obrigatório.")]
    [Range(100, double.MaxValue, ErrorMessage = "O valor mensal mínimo é de R$ 100,00.")]
    public decimal ValorMensal { get; set; }
}