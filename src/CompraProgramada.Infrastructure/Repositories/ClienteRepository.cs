using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> ObterPorIdAsync(int id)
    {
        return await _context.Clientes
            .Include(c => c.ContaGrafica)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cliente?> ObterPorIdComCustodiaAsync(int id)
    {
        return await _context.Clientes
            .Include(c => c.ContaGrafica)
                .ThenInclude(cg => cg!.CustodiaFilhote)
            .Include(c => c.Distribuicoes)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cliente?> ObterPorCpfAsync(string cpf)
    {
        return await _context.Clientes.FirstOrDefaultAsync(c => c.Cpf == cpf);
    }

    public async Task<List<Cliente>> ObterAtivosAsync()
    {
        return await _context.Clientes
            .Include(c => c.ContaGrafica)
            .Where(c => c.Ativo)
            .ToListAsync();
    }

    public async Task AdicionarAsync(Cliente cliente)
    {
        await _context.Clientes.AddAsync(cliente);
    }

    public Task AtualizarAsync(Cliente cliente)
    {
        _context.Clientes.Update(cliente);
        return Task.CompletedTask;
    }
}