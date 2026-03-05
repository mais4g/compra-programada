using CompraProgramada.Application.DTOs.Responses;

namespace CompraProgramada.Application.Interfaces;

public interface ICustodiaMasterService
{
    Task<CustodiaMasterResponse> ConsultarCustodiaAsync();
}