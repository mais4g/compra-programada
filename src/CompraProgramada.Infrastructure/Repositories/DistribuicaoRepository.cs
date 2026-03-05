using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Repositories;

public class DistribuicaoRepository : IDistribuicaoRepository
{
    private readonly AppDbContext _context;

    public DistribuicaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Distribuicao>> ObterPorClienteAsync(int clienteId)
    {
        return await _context.Distribuicoes
            .Where(d => d.ClienteId == clienteId)
            .OrderByDescending(d => d.DataDistribuicao)
            .ToListAsync();
    }

    public async Task AdicionarAsync(Distribuicao distribuicao)
    {
        await _context.Distribuicoes.AddAsync(distribuicao);
    }

    public async Task AdicionarVariosAsync(IEnumerable<Distribuicao> distribuicoes)
    {
        await _context.Distribuicoes.AddRangeAsync(distribuicoes);
    }
}