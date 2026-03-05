using MediatR;

namespace CompraProgramada.Application.CQRS.Commands.Motor;

public record RebalancearPorDesvioCommand(int ClienteId) : IRequest<Unit>;
