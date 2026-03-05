using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces.Repositories;

public interface IHistoricoValorMensalRepository
{
    Task AdicionarAsync(HistoricoValorMensal historico);
    Task<List<HistoricoValorMensal>> ObterPorClienteAsync(int clienteId);
}