using CompraProgramada.Application.DTOs.Responses;
using MediatR;

namespace CompraProgramada.Application.CQRS.Queries.Admin;

public record ObterCestaAtualQuery : IRequest<CestaResponse>;
