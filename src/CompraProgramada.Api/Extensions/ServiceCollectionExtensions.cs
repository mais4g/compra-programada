using CompraProgramada.Application.CQRS.Behaviors;
using CompraProgramada.Application.CQRS.Commands.Clientes;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Domain.Interfaces.Services;
using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Infrastructure.Repositories;
using CompraProgramada.Infrastructure.Services.Cotahist;
using CompraProgramada.Infrastructure.Services.Kafka;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

        // Repositories
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<ICestaTopFiveRepository, CestaTopFiveRepository>();
        services.AddScoped<ICustodiaFilhoteRepository, CustodiaFilhoteRepository>();
        services.AddScoped<ICustodiaMasterRepository, CustodiaMasterRepository>();
        services.AddScoped<IOrdemCompraRepository, OrdemCompraRepository>();
        services.AddScoped<IDistribuicaoRepository, DistribuicaoRepository>();
        services.AddScoped<IHistoricoValorMensalRepository, HistoricoValorMensalRepository>();
        services.AddScoped<IOperacaoRebalanceamentoRepository, OperacaoRebalanceamentoRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Cotahist
        var cotacoesConfig = configuration.GetValue<string>("CotacoesPath") ?? "cotacoes";
        var pastaCotacoes = Path.IsPathRooted(cotacoesConfig)
            ? cotacoesConfig
            : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), cotacoesConfig));
        services.AddSingleton(new CotahistParser());
        services.AddSingleton<ICotacaoService>(sp =>
            new CotacaoService(
                sp.GetRequiredService<CotahistParser>(),
                pastaCotacoes,
                sp.GetRequiredService<ILogger<CotacaoService>>()));

        // Kafka
        var kafkaBootstrap = configuration.GetValue<string>("Kafka:BootstrapServers")
            ?? throw new InvalidOperationException("Kafka:BootstrapServers não configurado.");
        services.AddSingleton<IKafkaProducerService>(sp =>
            new KafkaProducerService(kafkaBootstrap, sp.GetRequiredService<ILogger<KafkaProducerService>>()));

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICarteiraService, CarteiraService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<ICestaService, CestaService>();
        services.AddScoped<IMotorCompraService, MotorCompraService>();
        services.AddScoped<IRebalanceamentoService, RebalanceamentoService>();
        services.AddScoped<ICustodiaMasterService, CustodiaMasterService>();

        // CQRS - MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<AderirCommand>();
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        return services;
    }
}