namespace CompraProgramada.Domain.Interfaces.Repositories;

public interface IUnitOfWork
{
    Task<int> CommitAsync();
}