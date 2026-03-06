using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces.Repositories;

public interface IOperacaoRebalanceamentoRepository
{
    Task AdicionarAsync(OperacaoRebalanceamento operacao);
    Task AdicionarVariosAsync(IEnumerable<OperacaoRebalanceamento> operacoes);
    Task<decimal> ObterTotalVendasMesAsync(int clienteId, int ano, int mes);
    Task<decimal> ObterTotalLucroMesAsync(int clienteId, int ano, int mes);
}