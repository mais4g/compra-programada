using CompraProgramada.Application.DTOs.Requests;
using CompraProgramada.Application.DTOs.Responses;

namespace CompraProgramada.Application.Interfaces;

public interface ICestaService
{
    Task<CestaResponse> CadastrarOuAlterarAsync(CestaRequest request);
    Task<CestaResponse> ObterAtualAsync();
    Task<CestaHistoricoResponse> ObterHistoricoAsync();
}