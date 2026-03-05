namespace CompraProgramada.Domain.Interfaces.Services;

public interface ICotacaoService
{
    Task<decimal> ObterCotacaoFechamentoAsync(string ticker);
    Task<Dictionary<string, decimal>> ObterCotacoesFechamentoAsync(IEnumerable<string> tickers);
}