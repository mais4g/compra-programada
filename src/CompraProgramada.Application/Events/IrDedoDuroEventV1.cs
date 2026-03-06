namespace CompraProgramada.Application.Events;

public record IrDedoDuroEventV1(
    string Tipo,
    int ClienteId,
    string Cpf,
    string Ticker,
    string TipoOperacao,
    int Quantidade,
    decimal PrecoUnitario,
    decimal ValorOperacao,
    decimal Aliquota,
    decimal ValorIR,
    DateTime DataOperacao
);
