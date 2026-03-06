using System.Text.Json;
using Confluent.Kafka;
using CompraProgramada.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Infrastructure.Services.Kafka;

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private const string TopicIRDedoDuro = "ir-dedo-duro";
    private const string TopicIRVenda = "ir-venda";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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

    public async Task PublicarIRDedoDuroAsync(object payload, string partitionKey)
    {
        await PublicarAsync(TopicIRDedoDuro, payload, partitionKey);
    }

    public async Task PublicarIRVendaAsync(object payload, string partitionKey)
    {
        await PublicarAsync(TopicIRVenda, payload, partitionKey);
    }

    private async Task PublicarAsync(string topico, object payload, string partitionKey)
    {
        // Envelope padrão para produção:
        // - eventId: identificador único para deduplicação pelo consumidor
        // - schemaVersion: permite evoluir o contrato sem quebrar consumidores antigos
        // - partitionKey (Kafka Message.Key): garante que eventos do mesmo cliente
        //   vão para a mesma partição, preservando a ordem de processamento
        var envelope = new
        {
            eventId = Guid.NewGuid().ToString(),
            schemaVersion = "1",
            timestampUtc = DateTime.UtcNow,
            payload
        };

        var json = JsonSerializer.Serialize(envelope, SerializerOptions);

        const int maxRetries = 3;
        for (int tentativa = 1; tentativa <= maxRetries; tentativa++)
        {
            try
            {
                var result = await _producer.ProduceAsync(topico, new Message<string, string>
                {
                    Key = partitionKey,
                    Value = json
                });

                _logger.LogInformation(
                    "Mensagem publicada no tópico {Topico} - Partition: {Partition} Offset: {Offset}",
                    topico, result.Partition.Value, result.Offset);
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
