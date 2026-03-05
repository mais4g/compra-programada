using CompraProgramada.Application.DTOs.Responses;
using MediatR;

namespace CompraProgramada.Application.CQRS.Commands.Clientes;

public record AlterarValorMensalCommand(
    int ClienteId,
    decimal NovoValorMensal) : IRequest<AlterarValorMensalResponse>;
