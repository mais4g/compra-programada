using CompraProgramada.Application.CQRS.Queries.Clientes;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using MediatR;

namespace CompraProgramada.Application.CQRS.Handlers.Clientes;

public class ConsultarClientePorCpfQueryHandler : IRequestHandler<ConsultarClientePorCpfQuery, AdesaoResponse>
{
    private readonly IClienteService _clienteService;

    public ConsultarClientePorCpfQueryHandler(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    public async Task<AdesaoResponse> Handle(ConsultarClientePorCpfQuery request, CancellationToken cancellationToken)
    {
        return await _clienteService.ConsultarPorCpfAsync(request.Cpf);
    }
}
