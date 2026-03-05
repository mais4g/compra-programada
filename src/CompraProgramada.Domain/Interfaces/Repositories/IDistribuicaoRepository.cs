using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces.Repositories;

public interface IDistribuicaoRepository
{
    Task<List<Distribuicao>> ObterPorClienteAsync(int clienteId);
    Task AdicionarAsync(Distribuicao distribuicao);
    Task AdicionarVariosAsync(IEnumerable<Distribuicao> distribuicoes);
}