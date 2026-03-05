namespace CompraProgramada.Domain.Exceptions;

public class DomainException : Exception
{
    public string Codigo { get; }

    public DomainException(string mensagem, string codigo) : base(mensagem)
    {
        Codigo = codigo;
    }
}