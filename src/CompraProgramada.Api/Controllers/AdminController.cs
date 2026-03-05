using CompraProgramada.Application.CQRS.Commands.Admin;
using CompraProgramada.Application.CQRS.Queries.Admin;
using CompraProgramada.Application.DTOs.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Cadastrar ou alterar a cesta Top Five.
    /// </summary>
    [HttpPost("cesta")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CadastrarCesta([FromBody] CestaRequest request)
    {
        var command = new CadastrarCestaCommand(request.Nome, request.Itens);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(ObterCestaAtual), null, result);
    }

    /// <summary>
    /// Consultar a cesta Top Five atual.
    /// </summary>
    [HttpGet("cesta/atual")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterCestaAtual()
    {
        var result = await _mediator.Send(new ObterCestaAtualQuery());
        return Ok(result);
    }

    /// <summary>
    /// Consultar histórico de cestas.
    /// </summary>
    [HttpGet("cesta/historico")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterHistoricoCestas()
    {
        var result = await _mediator.Send(new ObterHistoricoCestasQuery());
        return Ok(result);
    }

    /// <summary>
    /// Consultar custódia da conta master (resíduos).
    /// </summary>
    [HttpGet("conta-master/custodia")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ConsultarCustodiaMaster()
    {
        var result = await _mediator.Send(new ConsultarCustodiaMasterQuery());
        return Ok(result);
    }
}
