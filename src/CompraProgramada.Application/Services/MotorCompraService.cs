using CompraProgramada.Application.DTOs.Responses;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Exceptions;
using CompraProgramada.Domain.Interfaces.Repositories;
using CompraProgramada.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Application.Services;

public class MotorCompraService : IMotorCompraService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly ICestaTopFiveRepository _cestaRepository;
    private readonly ICustodiaMasterRepository _custodiaMasterRepository;
    private readonly ICustodiaFilhoteRepository _custodiaFilhoteRepository;
    private readonly IOrdemCompraRepository _ordemCompraRepository;
    private readonly IDistribuicaoRepository _distribuicaoRepository;
    private readonly ICotacaoService _cotacaoService;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MotorCompraService> _logger;

    public MotorCompraService(
        IClienteRepository clienteRepository,
        ICestaTopFiveRepository cestaRepository,
        ICustodiaMasterRepository custodiaMasterRepository,
        ICustodiaFilhoteRepository custodiaFilhoteRepository,
        IOrdemCompraRepository ordemCompraRepository,
        IDistribuicaoRepository distribuicaoRepository,
        ICotacaoService cotacaoService,
        IKafkaProducerService kafkaProducer,
        IUnitOfWork unitOfWork,
        ILogger<MotorCompraService> logger)
    {
        _clienteRepository = clienteRepository;
        _cestaRepository = cestaRepository;
        _custodiaMasterRepository = custodiaMasterRepository;
        _custodiaFilhoteRepository = custodiaFilhoteRepository;
        _ordemCompraRepository = ordemCompraRepository;
        _distribuicaoRepository = distribuicaoRepository;
        _cotacaoService = cotacaoService;
        _kafkaProducer = kafkaProducer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ExecutarCompraResponse> ExecutarCompraAsync(DateTime dataReferencia)
    {
        ValidarDataCompra(dataReferencia);
        var dataCompra = AjustarParaDiaUtil(dataReferencia);

        await ValidarCompraJaExecutadaAsync(dataCompra);
        var cesta = await ObterCestaAtivaAsync();
        var clientesAtivos = await ObterClientesAtivosAsync();

        var aportesPorCliente = CalcularAportesPorCliente(clientesAtivos);
        var totalConsolidado = aportesPorCliente.Values.Sum();

        var tickers = cesta.Itens.Select(i => i.Ticker).ToList();
        var cotacoes = await _cotacaoService.ObterCotacoesFechamentoAsync(tickers);
        var saldosMaster = await _custodiaMasterRepository.ObterTodosAsync();

        var (ordensItens, quantidadesDisponiveis) = await CriarOrdensCompraAsync(
            cesta, totalConsolidado, cotacoes, saldosMaster);

        var ordemCompra = await PersistirOrdemCompraAsync(dataCompra, totalConsolidado, clientesAtivos.Count, ordensItens);

        var (distribuicoesResponse, eventosIR) = await DistribuirAosClientesAsync(
            aportesPorCliente, cesta, cotacoes, quantidadesDisponiveis, ordemCompra, totalConsolidado);

        var residuosResponse = await ProcessarResiduosMasterAsync(cesta, quantidadesDisponiveis, cotacoes, dataCompra);

        await _unitOfWork.CommitAsync();

        return MontarResponse(clientesAtivos.Count, totalConsolidado, ordensItens, distribuicoesResponse, residuosResponse, eventosIR);
    }

    private static void ValidarDataCompra(DateTime dataReferencia)
    {
        if (!RegrasFinanceiras.DiasDeCompra.Contains(dataReferencia.Day))
            throw new DomainException(
                $"Data de referência inválida. As compras devem ocorrer nos dias 5, 15 ou 25 do mês. Dia informado: {dataReferencia.Day}.",
                ErrorCodes.DataCompraInvalida);
    }

    private async Task ValidarCompraJaExecutadaAsync(DateTime dataCompra)
    {
        var ordemExistente = await _ordemCompraRepository.ObterPorDataReferenciaAsync(dataCompra);
        if (ordemExistente != null)
            throw new DomainException("Compra já foi executada para esta data.", ErrorCodes.CompraJaExecutada);
    }

    private async Task<CestaTopFive> ObterCestaAtivaAsync()
    {
        return await _cestaRepository.ObterAtivaAsync()
            ?? throw new DomainException("Nenhuma cesta ativa encontrada.", ErrorCodes.CestaNaoEncontrada);
    }

    private async Task<List<Cliente>> ObterClientesAtivosAsync()
    {
        var clientesAtivos = await _clienteRepository.ObterAtivosAsync();
        if (!clientesAtivos.Any())
            throw new DomainException("Nenhum cliente ativo encontrado.", ErrorCodes.SemClientesAtivos);
        return clientesAtivos;
    }

    private static Dictionary<Cliente, decimal> CalcularAportesPorCliente(List<Cliente> clientesAtivos)
    {
        return clientesAtivos.ToDictionary(
            c => c,
            c => Math.Round(c.ValorMensal / RegrasFinanceiras.ParcelasPorMes, 2));
    }

    private async Task<(List<OrdemCompraItem> Itens, Dictionary<string, int> QuantidadesDisponiveis)> CriarOrdensCompraAsync(
        CestaTopFive cesta, decimal totalConsolidado,
        Dictionary<string, decimal> cotacoes, List<CustodiaMaster> saldosMaster)
    {
        var ordensItens = new List<OrdemCompraItem>();
        var quantidadesDisponiveis = new Dictionary<string, int>();

        foreach (var item in cesta.Itens)
        {
            var valorParaAtivo = totalConsolidado * (item.Percentual / 100m);
            var cotacao = cotacoes.GetValueOrDefault(item.Ticker, 0m);
            if (cotacao == 0) continue;

            var quantidadeSemSaldo = (int)Math.Truncate(valorParaAtivo / cotacao);
            var saldoMaster = saldosMaster.FirstOrDefault(s => s.Ticker == item.Ticker);
            var saldoExistente = saldoMaster?.Quantidade ?? 0;

            var quantidadeAComprar = Math.Max(0, quantidadeSemSaldo - saldoExistente);
            quantidadesDisponiveis[item.Ticker] = quantidadeAComprar + saldoExistente;

            var lotes = quantidadeAComprar / RegrasFinanceiras.TamanhoLotePadrao;
            var fracionario = quantidadeAComprar % RegrasFinanceiras.TamanhoLotePadrao;

            ordensItens.Add(new OrdemCompraItem
            {
                Ticker = item.Ticker,
                QuantidadeLote = lotes * RegrasFinanceiras.TamanhoLotePadrao,
                QuantidadeFracionario = fracionario,
                PrecoUnitario = cotacao
            });

            if (saldoMaster != null)
            {
                saldoMaster.Quantidade = 0;
                await _custodiaMasterRepository.AtualizarAsync(saldoMaster);
            }
        }

        return (ordensItens, quantidadesDisponiveis);
    }

    private async Task<OrdemCompra> PersistirOrdemCompraAsync(
        DateTime dataCompra, decimal totalConsolidado, int totalClientes, List<OrdemCompraItem> ordensItens)
    {
        var ordemCompra = new OrdemCompra
        {
            DataExecucao = DateTime.UtcNow,
            DataReferencia = dataCompra,
            ValorTotalConsolidado = totalConsolidado,
            TotalClientes = totalClientes,
            Itens = ordensItens
        };
        await _ordemCompraRepository.AdicionarAsync(ordemCompra);
        await _unitOfWork.CommitAsync();
        return ordemCompra;
    }

    private async Task<(List<DistribuicaoClienteResponse> Response, int EventosIR)> DistribuirAosClientesAsync(
        Dictionary<Cliente, decimal> aportesPorCliente, CestaTopFive cesta,
        Dictionary<string, decimal> cotacoes, Dictionary<string, int> quantidadesDisponiveis,
        OrdemCompra ordemCompra, decimal totalConsolidado)
    {
        var distribuicoesResponse = new List<DistribuicaoClienteResponse>();
        var distribuicoesDb = new List<Distribuicao>();
        var eventosIR = 0;

        foreach (var (cliente, aporte) in aportesPorCliente)
        {
            var proporcao = totalConsolidado > 0 ? aporte / totalConsolidado : 0m;
            var ativosDistribuidos = new List<AtivoDistribuidoResponse>();

            foreach (var item in cesta.Itens)
            {
                var totalDisponivel = quantidadesDisponiveis.GetValueOrDefault(item.Ticker, 0);
                var quantidadeCliente = (int)Math.Truncate(proporcao * totalDisponivel);
                if (quantidadeCliente <= 0) continue;

                var cotacao = cotacoes.GetValueOrDefault(item.Ticker, 0m);
                var valorOperacao = quantidadeCliente * cotacao;
                var irDedoDuro = Math.Round(valorOperacao * RegrasFinanceiras.TaxaIRDedoDuro, 2);

                await AtualizarCustodiaFilhoteAsync(cliente, item.Ticker, quantidadeCliente, cotacao, valorOperacao);
                quantidadesDisponiveis[item.Ticker] -= quantidadeCliente;

                distribuicoesDb.Add(new Distribuicao
                {
                    OrdemCompraId = ordemCompra.Id,
                    ClienteId = cliente.Id,
                    Ticker = item.Ticker,
                    Quantidade = quantidadeCliente,
                    PrecoUnitario = cotacao,
                    ValorIRDedoDuro = irDedoDuro,
                    DataDistribuicao = DateTime.UtcNow
                });

                ativosDistribuidos.Add(new AtivoDistribuidoResponse
                {
                    Ticker = item.Ticker,
                    Quantidade = quantidadeCliente
                });

                eventosIR += await PublicarIRDedoDuroAsync(cliente, item.Ticker, quantidadeCliente, cotacao, valorOperacao, irDedoDuro);
            }

            distribuicoesResponse.Add(new DistribuicaoClienteResponse
            {
                ClienteId = cliente.Id,
                Nome = cliente.Nome,
                ValorAporte = aporte,
                Ativos = ativosDistribuidos
            });
        }

        await _distribuicaoRepository.AdicionarVariosAsync(distribuicoesDb);
        return (distribuicoesResponse, eventosIR);
    }

    private async Task AtualizarCustodiaFilhoteAsync(
        Cliente cliente, string ticker, int quantidade, decimal cotacao, decimal valorOperacao)
    {
        if (cliente.ContaGrafica == null)
        {
            _logger.LogWarning("Cliente {ClienteId} sem ContaGráfica. Custódia não atualizada para {Ticker}.", cliente.Id, ticker);
            return;
        }

        var custodiaFilhote = await _custodiaFilhoteRepository
            .ObterPorContaETickerAsync(cliente.ContaGrafica.Id, ticker);

        if (custodiaFilhote != null)
        {
            custodiaFilhote.AtualizarPrecoMedio(quantidade, cotacao);
            await _custodiaFilhoteRepository.AtualizarAsync(custodiaFilhote);
        }
        else
        {
            await _custodiaFilhoteRepository.AdicionarAsync(new CustodiaFilhote
            {
                ContaGraficaId = cliente.ContaGrafica.Id,
                Ticker = ticker,
                Quantidade = quantidade,
                PrecoMedio = cotacao,
                ValorInvestido = valorOperacao
            });
        }
    }

    private async Task<int> PublicarIRDedoDuroAsync(
        Cliente cliente, string ticker, int quantidade, decimal cotacao, decimal valorOperacao, decimal irDedoDuro)
    {
        try
        {
            await _kafkaProducer.PublicarIRDedoDuroAsync(new
            {
                tipo = "IR_DEDO_DURO",
                clienteId = cliente.Id,
                cpf = cliente.Cpf,
                ticker,
                tipoOperacao = "COMPRA",
                quantidade,
                precoUnitario = cotacao,
                valorOperacao,
                aliquota = RegrasFinanceiras.TaxaIRDedoDuro,
                valorIR = irDedoDuro,
                dataOperacao = DateTime.UtcNow
            });
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao publicar IR dedo-duro para cliente {ClienteId}.", cliente.Id);
            return 0;
        }
    }

    private async Task<List<ResiduoMasterResponse>> ProcessarResiduosMasterAsync(
        CestaTopFive cesta, Dictionary<string, int> quantidadesDisponiveis,
        Dictionary<string, decimal> cotacoes, DateTime dataCompra)
    {
        var residuosResponse = new List<ResiduoMasterResponse>();
        var origemDescricao = $"Resíduo distribuição {dataCompra:yyyy-MM-dd}";

        foreach (var item in cesta.Itens)
        {
            var restante = quantidadesDisponiveis.GetValueOrDefault(item.Ticker, 0);
            if (restante <= 0) continue;

            var cotacao = cotacoes.GetValueOrDefault(item.Ticker, 0m);
            var saldoMaster = await _custodiaMasterRepository.ObterPorTickerAsync(item.Ticker);

            if (saldoMaster != null)
            {
                saldoMaster.Quantidade = restante;
                saldoMaster.PrecoMedio = cotacao;
                saldoMaster.Origem = origemDescricao;
                saldoMaster.DataAtualizacao = DateTime.UtcNow;
                await _custodiaMasterRepository.AtualizarAsync(saldoMaster);
            }
            else
            {
                await _custodiaMasterRepository.AdicionarAsync(new CustodiaMaster
                {
                    Ticker = item.Ticker,
                    Quantidade = restante,
                    PrecoMedio = cotacao,
                    Origem = origemDescricao,
                    DataAtualizacao = DateTime.UtcNow
                });
            }

            residuosResponse.Add(new ResiduoMasterResponse
            {
                Ticker = item.Ticker,
                Quantidade = restante
            });
        }

        return residuosResponse;
    }

    private static ExecutarCompraResponse MontarResponse(
        int totalClientes, decimal totalConsolidado, List<OrdemCompraItem> ordensItens,
        List<DistribuicaoClienteResponse> distribuicoes, List<ResiduoMasterResponse> residuos, int eventosIR)
    {
        return new ExecutarCompraResponse
        {
            DataExecucao = DateTime.UtcNow,
            TotalClientes = totalClientes,
            TotalConsolidado = totalConsolidado,
            OrdensCompra = ordensItens.Select(o => new OrdemCompraItemResponse
            {
                Ticker = o.Ticker,
                QuantidadeTotal = o.QuantidadeTotal,
                Detalhes = CriarDetalhesCompra(o),
                PrecoUnitario = o.PrecoUnitario,
                ValorTotal = o.QuantidadeTotal * o.PrecoUnitario
            }).ToList(),
            Distribuicoes = distribuicoes,
            ResiduosCustMaster = residuos,
            EventosIRPublicados = eventosIR,
            Mensagem = $"Compra programada executada com sucesso para {totalClientes} clientes."
        };
    }

    private static DateTime AjustarParaDiaUtil(DateTime data)
    {
        while (data.DayOfWeek == DayOfWeek.Saturday || data.DayOfWeek == DayOfWeek.Sunday)
            data = data.AddDays(1);
        return data;
    }

    private static List<DetalheCompraResponse> CriarDetalhesCompra(OrdemCompraItem item)
    {
        var detalhes = new List<DetalheCompraResponse>();

        if (item.QuantidadeLote > 0)
        {
            detalhes.Add(new DetalheCompraResponse
            {
                Tipo = "LOTE_PADRAO",
                Ticker = item.Ticker,
                Quantidade = item.QuantidadeLote
            });
        }

        if (item.QuantidadeFracionario > 0)
        {
            detalhes.Add(new DetalheCompraResponse
            {
                Tipo = "FRACIONARIO",
                Ticker = $"{item.Ticker}F",
                Quantidade = item.QuantidadeFracionario
            });
        }

        return detalhes;
    }
}
