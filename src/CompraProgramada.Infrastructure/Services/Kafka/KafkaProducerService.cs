using System.Text.Json;
using Confluent.Kafka;
using CompraProgramada.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Infrastructure.Services.Kafka;

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private const string TopicIRDedoDuro = "ir-dedo-duro";
    private const string TopicIRVenda = "ir-venda";

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(string bootstrapServers, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublicarIRDedoDuroAsync(object mensagem)
    {
        await PublicarAsync(TopicIRDedoDuro, mensagem);
    }

    public async Task PublicarIRVendaAsync(object mensagem)
    {
        await PublicarAsync(TopicIRVenda, mensagem);
    }

    private async Task PublicarAsync(string topico, object mensagem)
    {
        var json = JsonSerializer.Serialize(mensagem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        const int maxRetries = 3;
        for (int tentativa = 1; tentativa <= maxRetries; tentativa++)
        {
            try
            {
                var result = await _producer.ProduceAsync(topico, new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = json
                });

                _logger.LogInformation(
                    "Mensagem publicada no tópico {Topico} - Offset: {Offset}",
                    topico, result.Offset);
                return;
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogWarning(ex,
                    "Tentativa {Tentativa}/{MaxRetries} falhou ao publicar no tópico {Topico}.",
                    tentativa, maxRetries, topico);

                if (tentativa == maxRetries)
                {
                    _logger.LogError(ex, "Todas as tentativas falharam ao publicar no tópico {Topico}.", topico);
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, tentativa - 1)));
            }
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}