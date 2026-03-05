using System.Net;
using System.Text.Json;
using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Domain;
using CompraProgramada.Domain.Exceptions;
using Serilog.Context;

namespace CompraProgramada.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Request-Id"] = requestId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", requestId))
        {
            try
            {
                await _next(context);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Erro de domínio: {Codigo} - {Mensagem}", ex.Codigo, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado.");
                await HandleGenericExceptionAsync(context, ex);
            }
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, DomainException ex)
    {
        var statusCode = ex.Codigo switch
        {
            ErrorCodes.ClienteNaoEncontrado or ErrorCodes.CestaNaoEncontrada or ErrorCodes.CotacaoNaoEncontrada
                => HttpStatusCode.NotFound,
            ErrorCodes.CompraJaExecutada => HttpStatusCode.Conflict,
            _ => HttpStatusCode.BadRequest
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new ErroResponse(ex.Message, ex.Codigo);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static async Task HandleGenericExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new ErroResponse("Erro interno do servidor.", ErrorCodes.ErroInterno);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}