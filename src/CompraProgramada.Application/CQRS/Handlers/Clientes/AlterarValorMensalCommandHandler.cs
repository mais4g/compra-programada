using CompraProgramada.Application.CQRS.Commands.Clientes;
using CompraProgramada.Application.DTOs.Requests;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using MediatR;

namespace CompraProgramada.Application.CQRS.Handlers.Clientes;

public class AlterarValorMensalCommandHandler : IRequestHandler<AlterarValorMensalCommand, AlterarValorMensalResponse>
{
    private readonly IClienteService _clienteService;

    public AlterarValorMensalCommandHandler(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    public async Task<AlterarValorMensalResponse> Handle(AlterarValorMensalCommand request, CancellationToken cancellationToken)
    {
        var dto = new AlterarValorMensalRequest { NovoValorMensal = request.NovoValorMensal };
        return await _clienteService.AlterarValorMensalAsync(request.ClienteId, dto);
    }
}
