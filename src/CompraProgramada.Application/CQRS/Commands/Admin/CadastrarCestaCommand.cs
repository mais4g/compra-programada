using CompraProgramada.Application.DTOs.Requests;
using CompraProgramada.Application.DTOs.Responses;
using MediatR;

namespace CompraProgramada.Application.CQRS.Commands.Admin;

public record CadastrarCestaCommand(
    string Nome,
    List<CestaItemRequest> Itens) : IRequest<CestaResponse>;
