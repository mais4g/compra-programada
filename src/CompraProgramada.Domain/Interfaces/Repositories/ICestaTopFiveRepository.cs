using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces.Repositories;

public interface ICestaTopFiveRepository
{
    Task<CestaTopFive?> ObterAtivaAsync();
    Task<CestaTopFive?> ObterPorIdAsync(int id);
    Task<List<CestaTopFive>> ObterHistoricoAsync();
    Task AdicionarAsync(CestaTopFive cesta);
    Task AtualizarAsync(CestaTopFive cesta);
}