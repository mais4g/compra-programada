using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Repositories;

public class HistoricoValorMensalRepository : IHistoricoValorMensalRepository
{
    private readonly AppDbContext _context;

    public HistoricoValorMensalRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(HistoricoValorMensal historico)
    {
        await _context.HistoricoValoresMensais.AddAsync(historico);
    }

    public async Task<List<HistoricoValorMensal>> ObterPorClienteAsync(int clienteId)
    {
        return await _context.HistoricoValoresMensais
            .Where(h => h.ClienteId == clienteId)
            .OrderByDescending(h => h.DataAlteracao)
            .ToListAsync();
    }
}