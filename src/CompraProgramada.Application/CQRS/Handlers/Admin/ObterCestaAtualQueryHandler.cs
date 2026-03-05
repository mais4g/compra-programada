using CompraProgramada.Application.CQRS.Queries.Admin;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using MediatR;

namespace CompraProgramada.Application.CQRS.Handlers.Admin;

public class ObterCestaAtualQueryHandler : IRequestHandler<ObterCestaAtualQuery, CestaResponse>
{
    private readonly ICestaService _cestaService;

    public ObterCestaAtualQueryHandler(ICestaService cestaService)
    {
        _cestaService = cestaService;
    }

    public async Task<CestaResponse> Handle(ObterCestaAtualQuery request, CancellationToken cancellationToken)
    {
        return await _cestaService.ObterAtualAsync();
    }
}
