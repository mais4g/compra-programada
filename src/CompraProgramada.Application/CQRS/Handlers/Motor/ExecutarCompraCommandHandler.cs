using CompraProgramada.Application.CQRS.Commands.Motor;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using MediatR;

namespace CompraProgramada.Application.CQRS.Handlers.Motor;

public class ExecutarCompraCommandHandler : IRequestHandler<ExecutarCompraCommand, ExecutarCompraResponse>
{
    private readonly IMotorCompraService _motorCompraService;

    public ExecutarCompraCommandHandler(IMotorCompraService motorCompraService)
    {
        _motorCompraService = motorCompraService;
    }

    public async Task<ExecutarCompraResponse> Handle(ExecutarCompraCommand request, CancellationToken cancellationToken)
    {
        return await _motorCompraService.ExecutarCompraAsync(request.DataReferencia);
    }
}
