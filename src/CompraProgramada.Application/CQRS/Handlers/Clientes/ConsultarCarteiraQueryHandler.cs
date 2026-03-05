using CompraProgramada.Application.CQRS.Queries.Clientes;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using MediatR;

namespace CompraProgramada.Application.CQRS.Handlers.Clientes;

public class ConsultarCarteiraQueryHandler : IRequestHandler<ConsultarCarteiraQuery, CarteiraResponse>
{
    private readonly IClienteService _clienteService;

    public ConsultarCarteiraQueryHandler(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    public async Task<CarteiraResponse> Handle(ConsultarCarteiraQuery request, CancellationToken cancellationToken)
    {
        return await _clienteService.ConsultarCarteiraAsync(request.ClienteId);
    }
}
