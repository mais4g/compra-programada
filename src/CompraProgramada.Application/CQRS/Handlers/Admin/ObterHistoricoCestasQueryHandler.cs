using CompraProgramada.Application.CQRS.Queries.Admin;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using MediatR;

namespace CompraProgramada.Application.CQRS.Handlers.Admin;

public class ObterHistoricoCestasQueryHandler : IRequestHandler<ObterHistoricoCestasQuery, CestaHistoricoResponse>
{
    private readonly ICestaService _cestaService;

    public ObterHistoricoCestasQueryHandler(ICestaService cestaService)
    {
        _cestaService = cestaService;
    }

    public async Task<CestaHistoricoResponse> Handle(ObterHistoricoCestasQuery request, CancellationToken cancellationToken)
    {
        return await _cestaService.ObterHistoricoAsync();
    }
}
