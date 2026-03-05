using CompraProgramada.Application.CQRS.Commands.Motor;
using CompraProgramada.Application.Interfaces;
using MediatR;

namespace CompraProgramada.Application.CQRS.Handlers.Motor;

public class RebalancearPorDesvioCommandHandler : IRequestHandler<RebalancearPorDesvioCommand, Unit>
{
    private readonly IRebalanceamentoService _rebalanceamentoService;

    public RebalancearPorDesvioCommandHandler(IRebalanceamentoService rebalanceamentoService)
    {
        _rebalanceamentoService = rebalanceamentoService;
    }

    public async Task<Unit> Handle(RebalancearPorDesvioCommand request, CancellationToken cancellationToken)
    {
        await _rebalanceamentoService.ExecutarRebalanceamentoPorDesvioAsync(request.ClienteId);
        return Unit.Value;
    }
}
