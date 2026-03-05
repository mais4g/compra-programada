using CompraProgramada.Domain.Exceptions;

namespace CompraProgramada.Domain.Entities;


public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal ValorMensal { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataAdesao { get; set; } = DateTime.UtcNow;
    public DateTime? DataSaida { get; set; }

    public ContaGrafica? ContaGrafica { get; set; }
    public ICollection<HistoricoValorMensal> HistoricoValores { get; set; } = new List<HistoricoValorMensal>();
    public ICollection<Distribuicao> Distribuicoes { get; set; } = new List<Distribuicao>();

    public void Desativar()
    {
        if (!Ativo)
            throw new DomainException("Cliente já havia saído do produto.", ErrorCodes.ClienteJaInativo);

        Ativo = false;
        DataSaida = DateTime.UtcNow;
    }

    public decimal AlterarValorMensal(decimal novoValor)
    {
        var valorAnterior = ValorMensal;
        ValorMensal = novoValor;
        return valorAnterior;
    }
}
