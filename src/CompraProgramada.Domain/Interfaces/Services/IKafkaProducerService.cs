namespace CompraProgramada.Domain.Interfaces.Services;

public interface IKafkaProducerService
{
    Task PublicarIRDedoDuroAsync(object mensagem);
    Task PublicarIRVendaAsync(object mensagem);
}