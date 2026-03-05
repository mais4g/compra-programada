using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Repositories;

public class CustodiaMasterRepository : ICustodiaMasterRepository
{
    private readonly AppDbContext _context;

    public CustodiaMasterRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CustodiaMaster>> ObterTodosAsync()
    {
        return await _context.CustodiaMaster.ToListAsync();
    }

    public async Task<CustodiaMaster?> ObterPorTickerAsync(string ticker)
    {
        return await _context.CustodiaMaster.FirstOrDefaultAsync(c => c.Ticker == ticker);
    }

    public async Task AdicionarAsync(CustodiaMaster custodia)
    {
        await _context.CustodiaMaster.AddAsync(custodia);
    }

    public Task AtualizarAsync(CustodiaMaster custodia)
    {
        _context.CustodiaMaster.Update(custodia);
        return Task.CompletedTask;
    }
}