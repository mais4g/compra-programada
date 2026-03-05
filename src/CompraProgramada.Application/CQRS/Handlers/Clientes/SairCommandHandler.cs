using CompraProgramada.Application.CQRS.Commands.Clientes;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using MediatR;

namespace CompraProgramada.Application.CQRS.Handlers.Clientes;

public class SairCommandHandler : IRequestHandler<SairCommand, SaidaResponse>
{
    private readonly IClienteService _clienteService;

    public SairCommandHandler(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    public async Task<SaidaResponse> Handle(SairCommand request, CancellationToken cancellationToken)
    {
        return await _clienteService.SairAsync(request.ClienteId);
    }
}
