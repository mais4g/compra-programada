using CompraProgramada.Application.CQRS.Commands.Motor;
using CompraProgramada.Domain;
using CompraProgramada.Domain.Exceptions;
using MediatR;

namespace CompraProgramada.Api.BackgroundServices;

/// <summary>
/// Executa a compra programada automaticamente nos dias 5, 15 e 25 de cada mês.
/// Roda a cada hora e verifica se é dia e horário de execução. O endpoint manual
/// continua disponível para reexecução ou testes fora do horário agendado.
/// </summary>
public class CompraScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CompraScheduler> _logger;

    // Horário de disparo: 9h (horário da abertura do mercado B3)
    private static readonly TimeOnly HorarioExecucao = new(9, 0);

    public CompraScheduler(IServiceScopeFactory scopeFactory, ILogger<CompraScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CompraScheduler iniciado. Verificação diária às {Horario}.", HorarioExecucao);

        while (!stoppingToken.IsCancellationRequested)
        {
            var agora = DateTime.Now;
            var proximaExecucao = ProximaExecucao(agora);
            var espera = proximaExecucao - agora;

            _logger.LogInformation("Próxima verificação agendada para {ProximaExecucao}.", proximaExecucao);

            await Task.Delay(espera, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            await TentarExecutarCompraAsync(DateTime.Today, stoppingToken);
        }
    }

    private async Task TentarExecutarCompraAsync(DateTime dataReferencia, CancellationToken stoppingToken)
    {
        if (!IsDiaDeCompra(dataReferencia))
        {
            _logger.LogDebug("Dia {Data} não é dia de compra programada. Nenhuma ação executada.", dataReferencia.ToShortDateString());
            return;
        }

        _logger.LogInformation("Iniciando compra automática para {Data}.", dataReferencia.ToShortDateString());

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new ExecutarCompraCommand(dataReferencia), stoppingToken);

            _logger.LogInformation("Compra automática concluída com sucesso para {Data}.", dataReferencia.ToShortDateString());
        }
        catch (DomainException ex) when (ex.Codigo == ErrorCodes.CompraJaExecutada)
        {
            _logger.LogInformation("Compra para {Data} já havia sido executada anteriormente. Ignorando.", dataReferencia.ToShortDateString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha na compra automática para {Data}. O serviço continuará rodando.", dataReferencia.ToShortDateString());
        }
    }

    public static bool IsDiaDeCompra(DateTime data)
    {
        // Ajusta fim de semana para segunda-feira (mesma regra do motor manual)
        var dataUtil = data;
        while (dataUtil.DayOfWeek == DayOfWeek.Saturday || dataUtil.DayOfWeek == DayOfWeek.Sunday)
            dataUtil = dataUtil.AddDays(1);

        return RegrasFinanceiras.DiasDeCompra.Contains(dataUtil.Day);
    }

    public static DateTime ProximaExecucao(DateTime agora)
    {
        var hoje = DateOnly.FromDateTime(agora);
        var execucaoHoje = hoje.ToDateTime(HorarioExecucao);

        return agora < execucaoHoje
            ? execucaoHoje
            : execucaoHoje.AddDays(1);
    }
}
