using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces.Repositories;

public interface ICustodiaFilhoteRepository
{
    Task<List<CustodiaFilhote>> ObterPorContaGraficaAsync(int contaGraficaId);
    Task<CustodiaFilhote?> ObterPorContaETickerAsync(int contaGraficaId, string ticker);
    Task AdicionarAsync(CustodiaFilhote custodia);
    Task AtualizarAsync(CustodiaFilhote custodia);
    Task RemoverAsync(CustodiaFilhote custodia);
}