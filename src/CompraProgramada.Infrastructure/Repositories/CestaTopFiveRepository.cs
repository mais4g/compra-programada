using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Repositories;

public class CestaTopFiveRepository : ICestaTopFiveRepository
{
    private readonly AppDbContext _context;

    public CestaTopFiveRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CestaTopFive?> ObterAtivaAsync()
    {
        return await _context.CestasTopFive
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Ativa);
    }

    public async Task<CestaTopFive?> ObterPorIdAsync(int id)
    {
        return await _context.CestasTopFive
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<CestaTopFive>> ObterHistoricoAsync()
    {
        return await _context.CestasTopFive
            .Include(c => c.Itens)
            .OrderByDescending(c => c.DataCriacao)
            .ToListAsync();
    }

    public async Task AdicionarAsync(CestaTopFive cesta)
    {
        await _context.CestasTopFive.AddAsync(cesta);
    }

    public Task AtualizarAsync(CestaTopFive cesta)
    {
        _context.CestasTopFive.Update(cesta);
        return Task.CompletedTask;
    }
}