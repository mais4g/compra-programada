using CompraProgramada.Application.DTOs.Responses;
using MediatR;

namespace CompraProgramada.Application.CQRS.Queries.Admin;

public record ConsultarCustodiaMasterQuery : IRequest<CustodiaMasterResponse>;
