using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Repositories;

public class OperacaoRebalanceamentoRepository : IOperacaoRebalanceamentoRepository
{
    private readonly AppDbContext _context;

    public OperacaoRebalanceamentoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(OperacaoRebalanceamento operacao)
    {
        await _context.OperacoesRebalanceamento.AddAsync(operacao);
    }

    public async Task AdicionarVariosAsync(IEnumerable<OperacaoRebalanceamento> operacoes)
    {
        await _context.OperacoesRebalanceamento.AddRangeAsync(operacoes);
    }

    public async Task<decimal> ObterTotalVendasMesAsync(int clienteId, int ano, int mes)
    {
        return await _context.OperacoesRebalanceamento
            .Where(o => o.ClienteId == clienteId
                && o.TipoOperacao == Domain.Enums.TipoOperacao.Venda
                && o.DataOperacao.Year == ano
                && o.DataOperacao.Month == mes)
            .SumAsync(o => o.Quantidade * o.PrecoUnitario);
    }

    public async Task<decimal> ObterTotalLucroMesAsync(int clienteId, int ano, int mes)
    {
        return await _context.OperacoesRebalanceamento
            .Where(o => o.ClienteId == clienteId
                && o.TipoOperacao == Domain.Enums.TipoOperacao.Venda
                && o.DataOperacao.Year == ano
                && o.DataOperacao.Month == mes)
            .SumAsync(o => o.Lucro);
    }
}