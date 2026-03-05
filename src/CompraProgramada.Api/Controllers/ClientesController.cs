using CompraProgramada.Application.CQRS.Commands.Clientes;
using CompraProgramada.Application.CQRS.Queries.Clientes;
using CompraProgramada.Application.DTOs.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClientesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Aderir ao produto de compra programada.
    /// </summary>
    [HttpPost("adesao")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Aderir([FromBody] AdesaoRequest request)
    {
        var command = new AderirCommand(request.Nome, request.Cpf, request.Email, request.ValorMensal);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(ConsultarCarteira), new { clienteId = result.ClienteId }, result);
    }

    /// <summary>
    /// Solicitar saída do produto.
    /// </summary>
    [HttpPost("{clienteId}/saida")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Sair(int clienteId)
    {
        var result = await _mediator.Send(new SairCommand(clienteId));
        return Ok(result);
    }

    /// <summary>
    /// Alterar valor mensal de aporte.
    /// </summary>
    [HttpPut("{clienteId}/valor-mensal")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarValorMensal(int clienteId, [FromBody] AlterarValorMensalRequest request)
    {
        var command = new AlterarValorMensalCommand(clienteId, request.NovoValorMensal);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Consultar cliente por CPF.
    /// </summary>
    [HttpGet("por-cpf/{cpf}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarPorCpf(string cpf)
    {
        var result = await _mediator.Send(new ConsultarClientePorCpfQuery(cpf));
        return Ok(result);
    }

    /// <summary>
    /// Consultar carteira (custódia) do cliente.
    /// </summary>
    [HttpGet("{clienteId}/carteira")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarCarteira(int clienteId)
    {
        var result = await _mediator.Send(new ConsultarCarteiraQuery(clienteId));
        return Ok(result);
    }

    /// <summary>
    /// Consultar rentabilidade detalhada do cliente.
    /// </summary>
    [HttpGet("{clienteId}/rentabilidade")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarRentabilidade(int clienteId)
    {
        var result = await _mediator.Send(new ConsultarRentabilidadeQuery(clienteId));
        return Ok(result);
    }
}
