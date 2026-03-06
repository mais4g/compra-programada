namespace CompraProgramada.Application.Events;

public record IrVendaEventV1(
    string Tipo,
    int ClienteId,
    string Cpf,
    string MesReferencia,
    decimal TotalVendasMes,
    decimal LucroLiquido,
    decimal Aliquota,
    decimal ValorIR,
    IEnumerable<DetalheVendaIR> Detalhes,
    DateTime DataCalculo
);

public record DetalheVendaIR(
    string Ticker,
    int Quantidade,
    decimal PrecoVenda,
    decimal PrecoMedio,
    decimal Lucro
);
