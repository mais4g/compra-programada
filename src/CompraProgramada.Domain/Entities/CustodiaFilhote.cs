namespace CompraProgramada.Domain.Entities;

public class CustodiaFilhote
{
    public int Id { get; set; }
    public int ContaGraficaId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoMedio { get; set; }
    public decimal ValorInvestido { get; set; }

    public ContaGrafica ContaGrafica { get; set; } = null!;

    public void AtualizarPrecoMedio(int novaQuantidade, decimal novoPreco)
    {
        if (Quantidade + novaQuantidade == 0) return;

        PrecoMedio = (Quantidade * PrecoMedio + novaQuantidade * novoPreco)
                     / (Quantidade + novaQuantidade);
        Quantidade += novaQuantidade;
        ValorInvestido += novaQuantidade * novoPreco;
    }

    public decimal CalcularLucro(decimal precoVenda, int quantidadeVenda)
    {
        return quantidadeVenda * (precoVenda - PrecoMedio);
    }
}