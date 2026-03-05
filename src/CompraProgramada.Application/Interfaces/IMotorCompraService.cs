using CompraProgramada.Application.DTOs.Responses;

namespace CompraProgramada.Application.Interfaces;

public interface IMotorCompraService
{
    Task<ExecutarCompraResponse> ExecutarCompraAsync(DateTime dataReferencia);
}