using CompraProgramada.Application.DTOs.Responses;
using MediatR;

namespace CompraProgramada.Application.CQRS.Commands.Clientes;

public record AderirCommand(
    string Nome,
    string Cpf,
    string Email,
    decimal ValorMensal) : IRequest<AdesaoResponse>;
