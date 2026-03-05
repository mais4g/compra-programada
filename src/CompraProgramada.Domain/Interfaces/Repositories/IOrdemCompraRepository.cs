using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces.Repositories;

public interface IOrdemCompraRepository
{
    Task<OrdemCompra?> ObterPorDataReferenciaAsync(DateTime dataReferencia);
    Task AdicionarAsync(OrdemCompra ordem);
}