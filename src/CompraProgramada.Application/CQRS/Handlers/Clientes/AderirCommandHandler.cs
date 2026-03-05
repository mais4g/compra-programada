using CompraProgramada.Application.CQRS.Commands.Clientes;
using CompraProgramada.Application.DTOs.Requests;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using MediatR;

namespace CompraProgramada.Application.CQRS.Handlers.Clientes;

public class AderirCommandHandler : IRequestHandler<AderirCommand, AdesaoResponse>
{
    private readonly IClienteService _clienteService;

    public AderirCommandHandler(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    public async Task<AdesaoResponse> Handle(AderirCommand request, CancellationToken cancellationToken)
    {
        var dto = new AdesaoRequest
        {
            Nome = request.Nome,
            Cpf = request.Cpf,
            Email = request.Email,
            ValorMensal = request.ValorMensal
        };

        return await _clienteService.AderirAsync(dto);
    }
}
