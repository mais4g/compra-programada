using CompraProgramada.Application.DTOs.Responses;
using MediatR;

namespace CompraProgramada.Application.CQRS.Queries.Clientes;

public record ConsultarClientePorCpfQuery(string Cpf) : IRequest<AdesaoResponse>;
