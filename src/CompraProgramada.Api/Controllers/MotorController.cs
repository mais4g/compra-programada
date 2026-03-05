using CompraProgramada.Application.CQRS.Commands.Motor;
using CompraProgramada.Application.DTOs.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.Api.Controllers;

[ApiController]
[Route("api/motor")]
[Produces("application/json")]
public class MotorController : ControllerBase
{
    private readonly IMediator _mediator;

    public MotorController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Executar compra programada manualmente (para testes).
    /// </summary>
    [HttpPost("executar-compra")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ExecutarCompra([FromBody] ExecutarCompraRequest request)
    {
        var command = new ExecutarCompraCommand(request.DataReferencia);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Executar rebalanceamento por desvio de proporção para um cliente.
    /// </summary>
    [HttpPost("rebalancear-desvio/{clienteId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RebalancearPorDesvio(int clienteId)
    {
        await _mediator.Send(new RebalancearPorDesvioCommand(clienteId));
        return Ok(new { mensagem = $"Rebalanceamento por desvio executado para o cliente {clienteId}." });
    }
}
