using CompraProgramada.Application.DTOs.Responses;
using MediatR;

namespace CompraProgramada.Application.CQRS.Queries.Clientes;

public record ConsultarCarteiraQuery(int ClienteId) : IRequest<CarteiraResponse>;
