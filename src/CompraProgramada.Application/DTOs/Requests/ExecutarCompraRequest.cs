using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Application.DTOs.Requests;

public class ExecutarCompraRequest
{
    [Required(ErrorMessage = "Data de referência é obrigatória.")]
    public DateTime DataReferencia { get; set; }
}