using CompraProgramada.Application.DTOs.Responses;
using MediatR;

namespace CompraProgramada.Application.CQRS.Queries.Clientes;

public record ConsultarRentabilidadeQuery(int ClienteId) : IRequest<RentabilidadeResponse>;
