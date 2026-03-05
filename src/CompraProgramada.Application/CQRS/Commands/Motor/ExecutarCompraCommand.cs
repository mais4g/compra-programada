using CompraProgramada.Application.DTOs.Responses;
using MediatR;

namespace CompraProgramada.Application.CQRS.Commands.Motor;

public record ExecutarCompraCommand(DateTime DataReferencia) : IRequest<ExecutarCompraResponse>;
