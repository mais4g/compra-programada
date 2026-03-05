namespace CompraProgramada.Domain.Entities;

public class CestaTopFive
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativa { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataDesativacao { get; set; }

    public ICollection<CestaItem> Itens { get; set; } = new List<CestaItem>();

    public void Desativar()
    {
        Ativa = false;
        DataDesativacao = DateTime.UtcNow;
    }
}
