using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces.Repositories;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(int id);
    Task<Cliente?> ObterPorIdComCustodiaAsync(int id);
    Task<Cliente?> ObterPorCpfAsync(string cpf);
    Task<List<Cliente>> ObterAtivosAsync();
    Task AdicionarAsync(Cliente cliente);
    Task AtualizarAsync(Cliente cliente);
}