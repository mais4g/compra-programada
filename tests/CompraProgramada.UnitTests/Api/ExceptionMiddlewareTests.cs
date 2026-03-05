using System.Text.Json;
using CompraProgramada.Api.Middleware;
using CompraProgramada.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace CompraProgramada.UnitTests.Api;

public class ExceptionMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionMiddleware>> _loggerMock = new();

    private ExceptionMiddleware CriarMiddleware(RequestDelegate next)
    {
        return new ExceptionMiddleware(next, _loggerMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_DomainException_DeveRetornarJsonComErroECodigo()
    {
        var middleware = CriarMiddleware(_ => throw new DomainException("Cliente não encontrado.", "CLIENTE_NAO_ENCONTRADO"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var json = JsonDocument.Parse(body);

        json.RootElement.GetProperty("erro").GetString().Should().Be("Cliente não encontrado.");
        json.RootElement.GetProperty("codigo").GetString().Should().Be("CLIENTE_NAO_ENCONTRADO");
    }

    [Fact]
    public async Task InvokeAsync_ExceptionGenerica_DeveRetornar500SemExporDetalhes()
    {
        var middleware = CriarMiddleware(_ => throw new InvalidOperationException("detalhe sensivel"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var json = JsonDocument.Parse(body);

        json.RootElement.GetProperty("erro").GetString().Should().Be("Erro interno do servidor.");
        json.RootElement.GetProperty("codigo").GetString().Should().Be("ERRO_INTERNO");
        body.Should().NotContain("detalhe sensivel");
    }
}
