using CompraProgramada.Application.DTOs.Responses;
using MediatR;

namespace CompraProgramada.Application.CQRS.Commands.Clientes;

public record SairCommand(int ClienteId) : IRequest<SaidaResponse>;
