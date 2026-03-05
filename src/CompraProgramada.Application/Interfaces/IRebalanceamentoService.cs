namespace CompraProgramada.Application.Interfaces;

public interface IRebalanceamentoService
{
    Task ExecutarRebalanceamentoPorMudancaCestaAsync(int cestaAntigaId, int cestaNovaId);
    Task ExecutarRebalanceamentoPorDesvioAsync(int clienteId);
}