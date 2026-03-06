using CompraProgramada.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Extensions.Hosting;

namespace CompraProgramada.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Usa o diretório do assembly de teste como content root para evitar
        // dependência do caminho absoluto do projeto na máquina de build.
        builder.UseContentRoot(AppContext.BaseDirectory);

        builder.ConfigureServices(services =>
        {
            // Remove TODAS as registracoes de DbContext do MySQL
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                           d.ServiceType == typeof(DbContextOptions))
                .ToList();
            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            // Substitui por InMemory com nome fixo (compartilhado entre requests)
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Garante que DiagnosticContext do Serilog esta registrado para testes
            if (!services.Any(d => d.ServiceType == typeof(DiagnosticContext)))
            {
                services.AddSingleton(new DiagnosticContext(Log.Logger));
            }

            // Remove RequestLoggingMiddleware hosted service (nao necessario em testes)
            var serilogHosted = services
                .Where(d => d.ServiceType == typeof(IHostedLifecycleService) &&
                           d.ImplementationType?.FullName?.Contains("Serilog") == true)
                .ToList();
            foreach (var d in serilogHosted)
                services.Remove(d);
        });

        builder.UseEnvironment("Development");
    }
}
