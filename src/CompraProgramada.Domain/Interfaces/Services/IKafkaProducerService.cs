namespace CompraProgramada.Domain.Interfaces.Services;

public interface IKafkaProducerService
{
    Task PublicarIRDedoDuroAsync(object payload, string partitionKey);
    Task PublicarIRVendaAsync(object payload, string partitionKey);
}