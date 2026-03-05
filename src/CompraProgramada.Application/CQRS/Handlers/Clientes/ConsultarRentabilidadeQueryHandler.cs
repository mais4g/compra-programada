using CompraProgramada.Application.CQRS.Queries.Clientes;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using MediatR;

namespace CompraProgramada.Application.CQRS.Handlers.Clientes;

public class ConsultarRentabilidadeQueryHandler : IRequestHandler<ConsultarRentabilidadeQuery, RentabilidadeResponse>
{
    private readonly IClienteService _clienteService;

    public ConsultarRentabilidadeQueryHandler(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    public async Task<RentabilidadeResponse> Handle(ConsultarRentabilidadeQuery request, CancellationToken cancellationToken)
    {
        return await _clienteService.ConsultarRentabilidadeAsync(request.ClienteId);
    }
}
