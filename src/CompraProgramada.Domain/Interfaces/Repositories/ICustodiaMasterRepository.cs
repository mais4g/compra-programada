using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces.Repositories;

public interface ICustodiaMasterRepository
{
    Task<List<CustodiaMaster>> ObterTodosAsync();
    Task<CustodiaMaster?> ObterPorTickerAsync(string ticker);
    Task AdicionarAsync(CustodiaMaster custodia);
    Task AtualizarAsync(CustodiaMaster custodia);
}