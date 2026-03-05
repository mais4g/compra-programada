using CompraProgramada.Application.DTOs.Responses;

namespace CompraProgramada.Application.Interfaces;

public interface ICarteiraService
{
    Task<CarteiraResponse> ConsultarCarteiraAsync(int clienteId);
    Task<RentabilidadeResponse> ConsultarRentabilidadeAsync(int clienteId);
}
