using CompraProgramada.Application.CQRS.Commands.Admin;
using CompraProgramada.Application.DTOs.Requests;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using MediatR;

namespace CompraProgramada.Application.CQRS.Handlers.Admin;

public class CadastrarCestaCommandHandler : IRequestHandler<CadastrarCestaCommand, CestaResponse>
{
    private readonly ICestaService _cestaService;

    public CadastrarCestaCommandHandler(ICestaService cestaService)
    {
        _cestaService = cestaService;
    }

    public async Task<CestaResponse> Handle(CadastrarCestaCommand request, CancellationToken cancellationToken)
    {
        var dto = new CestaRequest
        {
            Nome = request.Nome,
            Itens = request.Itens
        };

        return await _cestaService.CadastrarOuAlterarAsync(dto);
    }
}
