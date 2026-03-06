namespace CompraProgramada.Application.Events;

/// <summary>
/// Envelope padrão para todos os eventos Kafka.
/// Permite rastrear, versionar e rotear eventos em produção.
/// </summary>
public record EventEnvelope<TPayload>(
    string EventId,
    string SchemaVersion,
    string CorrelationId,
    DateTime TimestampUtc,
    TPayload Payload
)
{
    public static EventEnvelope<TPayload> Create(TPayload payload, string? correlationId = null) =>
        new(
            EventId: Guid.NewGuid().ToString(),
            SchemaVersion: "1",
            CorrelationId: correlationId ?? Guid.NewGuid().ToString(),
            TimestampUtc: DateTime.UtcNow,
            Payload: payload
        );
}
