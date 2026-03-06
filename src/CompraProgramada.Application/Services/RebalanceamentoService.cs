using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using CompraProgramada.Domain.Helpers;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Application.Services;

public class RebalanceamentoService : IRebalanceamentoService
{

    private readonly IClienteRepository _clienteRepository;
    private readonly ICestaTopFiveRepository _cestaRepository;
    private readonly ICustodiaFilhoteRepository _custodiaFilhoteRepository;
    private readonly IOperacaoRebalanceamentoRepository _operacaoRepository;
    private readonly ICotacaoService _cotacaoService;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RebalanceamentoService> _logger;

    public RebalanceamentoService(
        IClienteRepository clienteRepository,
        ICestaTopFiveRepository cestaRepository,
        ICustodiaFilhoteRepository custodiaFilhoteRepository,
        IOperacaoRebalanceamentoRepository operacaoRepository,
        ICotacaoService cotacaoService,
        IKafkaProducerService kafkaProducer,
        IUnitOfWork unitOfWork,
        ILogger<RebalanceamentoService> logger)
    {
        _clienteRepository = clienteRepository;
        _cestaRepository = cestaRepository;
        _custodiaFilhoteRepository = custodiaFilhoteRepository;
        _operacaoRepository = operacaoRepository;
        _cotacaoService = cotacaoService;
        _kafkaProducer = kafkaProducer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecutarRebalanceamentoPorMudancaCestaAsync(int cestaAntigaId, int cestaNovaId)
    {
        var cestaAntiga = await _cestaRepository.ObterPorIdAsync(cestaAntigaId);
        var cestaNova = await _cestaRepository.ObterPorIdAsync(cestaNovaId);
        if (cestaAntiga == null || cestaNova == null) return;

        var clientesAtivos = await _clienteRepository.ObterAtivosAsync();
        var tickersAntigos = cestaAntiga.Itens.Select(i => i.Ticker).ToHashSet();
        var tickersNovos = cestaNova.Itens.Select(i => i.Ticker).ToHashSet();
        var todosTickers = tickersAntigos.Union(tickersNovos).ToList();

        var cotacoes = await _cotacaoService.ObterCotacoesFechamentoAsync(todosTickers);

        foreach (var cliente in clientesAtivos)
        {
            if (cliente.ContaGrafica == null) continue;

            var custodia = await _custodiaFilhoteRepository
                .ObterPorContaGraficaAsync(cliente.ContaGrafica.Id);
            var operacoes = new List<OperacaoRebalanceamento>();

            decimal totalVendas = 0;
            decimal totalLucro = 0;

            var ativosSairam = tickersAntigos.Except(tickersNovos);
            foreach (var ticker in ativosSairam)
            {
                var posicao = custodia.FirstOrDefault(c => c.Ticker == ticker);
                if (posicao == null || posicao.Quantidade <= 0) continue;

                var cotacao = cotacoes.GetValueOrDefault(ticker, posicao.PrecoMedio);
                var valorVenda = posicao.Quantidade * cotacao;
                var lucro = posicao.CalcularLucro(cotacao, posicao.Quantidade);

                totalVendas += valorVenda;
                totalLucro += lucro;

                operacoes.Add(new OperacaoRebalanceamento
                {
                    ClienteId = cliente.Id,
                    Ticker = ticker,
                    TipoOperacao = TipoOperacao.Venda,
                    Quantidade = posicao.Quantidade,
                    PrecoUnitario = cotacao,
                    PrecoMedio = posicao.PrecoMedio,
                    Lucro = lucro,
                    CestaOrigemId = cestaAntigaId,
                    CestaDestinoId = cestaNovaId
                });

                await _custodiaFilhoteRepository.RemoverAsync(posicao);
            }

            var ativosEntraram = tickersNovos.Except(tickersAntigos).ToList();
            if (ativosEntraram.Any() && totalVendas > 0)
            {
                var percentualNovos = cestaNova.Itens
                    .Where(i => ativosEntraram.Contains(i.Ticker))
                    .ToList();
                var somaPercentuais = percentualNovos.Sum(p => p.Percentual);

                foreach (var item in percentualNovos)
                {
                    var proporcao = item.Percentual / somaPercentuais;
                    var valorParaCompra = totalVendas * proporcao;
                    var cotacao = cotacoes.GetValueOrDefault(item.Ticker, 0m);
                    if (cotacao == 0) continue;

                    var quantidade = MoneyHelper.TruncarQuantidade(valorParaCompra / cotacao);
                    if (quantidade <= 0) continue;

                    operacoes.Add(new OperacaoRebalanceamento
                    {
                        ClienteId = cliente.Id,
                        Ticker = item.Ticker,
                        TipoOperacao = TipoOperacao.Compra,
                        Quantidade = quantidade,
                        PrecoUnitario = cotacao,
                        PrecoMedio = cotacao,
                        Lucro = 0,
                        CestaOrigemId = cestaAntigaId,
                        CestaDestinoId = cestaNovaId
                    });

                    var custodiaExistente = await _custodiaFilhoteRepository
                        .ObterPorContaETickerAsync(cliente.ContaGrafica.Id, item.Ticker);

                    if (custodiaExistente != null)
                    {
                        custodiaExistente.AtualizarPrecoMedio(quantidade, cotacao);
                        await _custodiaFilhoteRepository.AtualizarAsync(custodiaExistente);
                    }
                    else
                    {
                        await _custodiaFilhoteRepository.AdicionarAsync(new CustodiaFilhote
                        {
                            ContaGraficaId = cliente.ContaGrafica.Id,
                            Ticker = item.Ticker,
                            Quantidade = quantidade,
                            PrecoMedio = cotacao,
                            ValorInvestido = quantidade * cotacao
                        });
                    }
                }
            }

            var ativosComuns = tickersAntigos.Intersect(tickersNovos);
            foreach (var ticker in ativosComuns)
            {
                var posicao = custodia.FirstOrDefault(c => c.Ticker == ticker);
                if (posicao == null || posicao.Quantidade <= 0) continue;

                var cotacao = cotacoes.GetValueOrDefault(ticker, posicao.PrecoMedio);
                var valorAtualTotal = custodia.Sum(c =>
                    c.Quantidade * cotacoes.GetValueOrDefault(c.Ticker, c.PrecoMedio));

                if (valorAtualTotal == 0) continue;

                var percentualAtual = (posicao.Quantidade * cotacao) / valorAtualTotal * 100m;
                var percentualAlvo = cestaNova.Itens.First(i => i.Ticker == ticker).Percentual;
                var diferenca = percentualAtual - percentualAlvo;

                if (Math.Abs(diferenca) < RegrasFinanceiras.ToleranciaRebalanceamento) continue;

                var valorAlvo = valorAtualTotal * (percentualAlvo / 100m);
                var quantidadeAlvo = MoneyHelper.TruncarQuantidade(valorAlvo / cotacao);
                var ajuste = posicao.Quantidade - quantidadeAlvo;

                if (ajuste > 0)
                {
                    var lucro = posicao.CalcularLucro(cotacao, ajuste);
                    totalVendas += ajuste * cotacao;
                    totalLucro += lucro;

                    posicao.Quantidade -= ajuste;
                    await _custodiaFilhoteRepository.AtualizarAsync(posicao);

                    operacoes.Add(new OperacaoRebalanceamento
                    {
                        ClienteId = cliente.Id,
                        Ticker = ticker,
                        TipoOperacao = TipoOperacao.Venda,
                        Quantidade = ajuste,
                        PrecoUnitario = cotacao,
                        PrecoMedio = posicao.PrecoMedio,
                        Lucro = lucro,
                        CestaOrigemId = cestaAntigaId,
                        CestaDestinoId = cestaNovaId
                    });
                }
                else if (ajuste < 0)
                {
                    var quantidadeComprar = Math.Abs(ajuste);
                    posicao.AtualizarPrecoMedio(quantidadeComprar, cotacao);
                    await _custodiaFilhoteRepository.AtualizarAsync(posicao);

                    operacoes.Add(new OperacaoRebalanceamento
                    {
                        ClienteId = cliente.Id,
                        Ticker = ticker,
                        TipoOperacao = TipoOperacao.Compra,
                        Quantidade = quantidadeComprar,
                        PrecoUnitario = cotacao,
                        PrecoMedio = posicao.PrecoMedio,
                        Lucro = 0,
                        CestaOrigemId = cestaAntigaId,
                        CestaDestinoId = cestaNovaId
                    });
                }
            }

            if (operacoes.Any())
                await _operacaoRepository.AdicionarVariosAsync(operacoes);

            await CalcularEPublicarIRVendaAsync(cliente, totalVendas, totalLucro, operacoes);
        }

        await _unitOfWork.CommitAsync();
    }

    public async Task ExecutarRebalanceamentoPorDesvioAsync(int clienteId)
    {
        var cliente = await _clienteRepository.ObterPorIdComCustodiaAsync(clienteId);
        if (cliente?.ContaGrafica == null) return;

        var cesta = await _cestaRepository.ObterAtivaAsync();
        if (cesta == null) return;

        var custodia = await _custodiaFilhoteRepository
            .ObterPorContaGraficaAsync(cliente.ContaGrafica.Id);
        if (!custodia.Any()) return;

        var tickers = custodia.Select(c => c.Ticker).ToList();
        var cotacoes = await _cotacaoService.ObterCotacoesFechamentoAsync(tickers);

        var valorTotal = custodia.Sum(c =>
            c.Quantidade * cotacoes.GetValueOrDefault(c.Ticker, c.PrecoMedio));
        if (valorTotal == 0) return;

        var operacoes = new List<OperacaoRebalanceamento>();
        decimal totalVendas = 0;
        decimal totalLucro = 0;

        foreach (var item in cesta.Itens)
        {
            var posicao = custodia.FirstOrDefault(c => c.Ticker == item.Ticker);
            if (posicao == null) continue;

            var cotacao = cotacoes.GetValueOrDefault(item.Ticker, posicao.PrecoMedio);
            var percentualAtual = (posicao.Quantidade * cotacao) / valorTotal * 100m;
            var desvio = percentualAtual - item.Percentual;

            if (Math.Abs(desvio) < RegrasFinanceiras.LimiarDesvioPercentual) continue;

            var valorAlvo = valorTotal * (item.Percentual / 100m);
            var quantidadeAlvo = (int)Math.Truncate(valorAlvo / cotacao);
            var ajuste = posicao.Quantidade - quantidadeAlvo;

            if (ajuste > 0)
            {
                var lucro = posicao.CalcularLucro(cotacao, ajuste);
                totalVendas += ajuste * cotacao;
                totalLucro += lucro;

                posicao.Quantidade -= ajuste;
                await _custodiaFilhoteRepository.AtualizarAsync(posicao);

                operacoes.Add(new OperacaoRebalanceamento
                {
                    ClienteId = clienteId,
                    Ticker = item.Ticker,
                    TipoOperacao = TipoOperacao.Venda,
                    Quantidade = ajuste,
                    PrecoUnitario = cotacao,
                    PrecoMedio = posicao.PrecoMedio,
                    Lucro = lucro
                });
            }
            else if (ajuste < 0)
            {
                var quantidadeComprar = Math.Abs(ajuste);
                posicao.AtualizarPrecoMedio(quantidadeComprar, cotacao);
                await _custodiaFilhoteRepository.AtualizarAsync(posicao);

                operacoes.Add(new OperacaoRebalanceamento
                {
                    ClienteId = clienteId,
                    Ticker = item.Ticker,
                    TipoOperacao = TipoOperacao.Compra,
                    Quantidade = quantidadeComprar,
                    PrecoUnitario = cotacao,
                    PrecoMedio = posicao.PrecoMedio,
                    Lucro = 0
                });
            }
        }

        if (operacoes.Any())
        {
            await _operacaoRepository.AdicionarVariosAsync(operacoes);
            await CalcularEPublicarIRVendaAsync(cliente, totalVendas, totalLucro, operacoes);
        }

        await _unitOfWork.CommitAsync();
    }

    private async Task CalcularEPublicarIRVendaAsync(
        Cliente cliente, decimal totalVendas, decimal totalLucro,
        List<OperacaoRebalanceamento> operacoes)
    {
        if (totalVendas <= 0) return;

        var agora = DateTime.UtcNow;
        var vendasAnteriores = await _operacaoRepository
            .ObterTotalVendasMesAsync(cliente.Id, agora.Year, agora.Month);
        var totalVendasMes = vendasAnteriores + totalVendas;

        // Lucro acumulado do mês: inclui operações anteriores + operação atual.
        // Necessário porque ao cruzar o limite de isenção de R$20k, o IR incide
        // sobre TODO o lucro do mês, não apenas o da operação que cruzou o limite.
        var lucroAnterioresMes = await _operacaoRepository
            .ObterTotalLucroMesAsync(cliente.Id, agora.Year, agora.Month);
        var lucroTotalMes = lucroAnterioresMes + totalLucro;

        decimal valorIR = 0;
        if (totalVendasMes > RegrasFinanceiras.LimiteIsencaoIR && lucroTotalMes > 0)
        {
            valorIR = MoneyHelper.ArredondarMoeda(lucroTotalMes * RegrasFinanceiras.AliquotaIRVenda);
        }

        try
        {
            await _kafkaProducer.PublicarIRVendaAsync(new
            {
                tipo = "IR_VENDA",
                clienteId = cliente.Id,
                cpf = cliente.Cpf,
                mesReferencia = agora.ToString("yyyy-MM"),
                totalVendasMes = totalVendasMes,
                lucroLiquido = lucroTotalMes,
                aliquota = totalVendasMes > RegrasFinanceiras.LimiteIsencaoIR ? RegrasFinanceiras.AliquotaIRVenda : 0m,
                valorIR = valorIR,
                detalhes = operacoes
                    .Where(o => o.TipoOperacao == TipoOperacao.Venda)
                    .Select(o => new
                    {
                        ticker = o.Ticker,
                        quantidade = o.Quantidade,
                        precoVenda = o.PrecoUnitario,
                        precoMedio = o.PrecoMedio,
                        lucro = o.Lucro
                    }),
                dataCalculo = agora
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao publicar IR venda para cliente {ClienteId}.", cliente.Id);
        }
    }
}