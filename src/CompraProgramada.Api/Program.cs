using System.Reflection;
using System.Threading.RateLimiting;
using CompraProgramada.Api.Extensions;
using CompraProgramada.Api.Middleware;
using CompraProgramada.Infrastructure.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando CompraProgramada API");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog - usa Host.UseSerilog para compatibilidade com WebApplicationFactory
    builder.Host.UseSerilog((context, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "CompraProgramada")
        .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()));

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Compra Programada API",
            Version = "v1",
            Description = "Sistema de Compra Programada de Ações - Corretora"
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            c.IncludeXmlComments(xmlPath);
    });

    var healthConnStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
    builder.Services.AddHealthChecks()
        .AddMySql(healthConnStr, name: "mysql", tags: new[] { "db", "ready" });

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplicationServices();

    var rateLimitSection = builder.Configuration.GetSection("RateLimiting");
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("fixed", opt =>
        {
            opt.PermitLimit = rateLimitSection.GetValue("PermitLimit", 100);
            opt.Window = TimeSpan.FromMinutes(rateLimitSection.GetValue("WindowMinutes", 1));
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = rateLimitSection.GetValue("QueueLimit", 10);
        });
    });

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:3000" };
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionMiddleware>();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            db.Database.Migrate();
            await SeedData.InitializeAsync(db);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Não foi possível aplicar migrations automaticamente. Verifique a conexão com o banco.");
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Compra Programada API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    app.UseCors();
    app.UseRateLimiter();
    app.MapControllers().RequireRateLimiting("fixed");

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds + "ms"
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds + "ms"
            };
            await context.Response.WriteAsJsonAsync(result);
        }
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação encerrada inesperadamente");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
