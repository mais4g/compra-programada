using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Repositories;

public class OrdemCompraRepository : IOrdemCompraRepository
{
    private readonly AppDbContext _context;

    public OrdemCompraRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OrdemCompra?> ObterPorDataReferenciaAsync(DateTime dataReferencia)
    {
        return await _context.OrdensCompra
            .Include(o => o.Itens)
            .Include(o => o.Distribuicoes)
            .FirstOrDefaultAsync(o => o.DataReferencia.Date == dataReferencia.Date);
    }

    public async Task AdicionarAsync(OrdemCompra ordem)
    {
        await _context.OrdensCompra.AddAsync(ordem);
    }
}