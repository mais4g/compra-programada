using CompraProgramada.Application.CQRS.Queries.Admin;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using MediatR;

namespace CompraProgramada.Application.CQRS.Handlers.Admin;

public class ConsultarCustodiaMasterQueryHandler : IRequestHandler<ConsultarCustodiaMasterQuery, CustodiaMasterResponse>
{
    private readonly ICustodiaMasterService _custodiaMasterService;

    public ConsultarCustodiaMasterQueryHandler(ICustodiaMasterService custodiaMasterService)
    {
        _custodiaMasterService = custodiaMasterService;
    }

    public async Task<CustodiaMasterResponse> Handle(ConsultarCustodiaMasterQuery request, CancellationToken cancellationToken)
    {
        return await _custodiaMasterService.ConsultarCustodiaAsync();
    }
}
