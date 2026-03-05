using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Repositories;

public class CustodiaFilhoteRepository : ICustodiaFilhoteRepository
{
    private readonly AppDbContext _context;

    public CustodiaFilhoteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CustodiaFilhote>> ObterPorContaGraficaAsync(int contaGraficaId)
    {
        return await _context.CustodiasFilhote
            .Where(c => c.ContaGraficaId == contaGraficaId)
            .ToListAsync();
    }

    public async Task<CustodiaFilhote?> ObterPorContaETickerAsync(int contaGraficaId, string ticker)
    {
        return await _context.CustodiasFilhote
            .FirstOrDefaultAsync(c => c.ContaGraficaId == contaGraficaId && c.Ticker == ticker);
    }

    public async Task AdicionarAsync(CustodiaFilhote custodia)
    {
        await _context.CustodiasFilhote.AddAsync(custodia);
    }

    public Task AtualizarAsync(CustodiaFilhote custodia)
    {
        _context.CustodiasFilhote.Update(custodia);
        return Task.CompletedTask;
    }

    public Task RemoverAsync(CustodiaFilhote custodia)
    {
        _context.CustodiasFilhote.Remove(custodia);
        return Task.CompletedTask;
    }
}