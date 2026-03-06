namespace CompraProgramada.Domain.Interfaces.Repositories;

public interface IUnitOfWork
{
    Task<int> CommitAsync();

    /// <summary>
    /// Executa a operação dentro de uma transação de banco de dados.
    /// Se qualquer passo falhar, a transação é revertida automaticamente.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation);
}