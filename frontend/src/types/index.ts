// === Requests ===

export interface AdesaoRequest {
  nome: string;
  cpf: string;
  email: string;
  valorMensal: number;
}

export interface AlterarValorMensalRequest {
  novoValorMensal: number;
}

export interface CestaItemRequest {
  ticker: string;
  percentual: number;
}

export interface CestaRequest {
  nome: string;
  itens: CestaItemRequest[];
}

export interface ExecutarCompraRequest {
  dataReferencia: string;
}

// === Responses ===

export interface ContaGraficaResponse {
  id: number;
  numeroConta: string;
  tipo: string;
  dataCriacao: string;
}

export interface AdesaoResponse {
  clienteId: number;
  nome: string;
  cpf: string;
  email: string;
  valorMensal: number;
  ativo: boolean;
  dataAdesao: string;
  contaGrafica?: ContaGraficaResponse;
}

export interface AlterarValorMensalResponse {
  clienteId: number;
  valorMensalAnterior: number;
  valorMensalNovo: number;
  dataAlteracao: string;
  mensagem: string;
}

export interface ResumoCarteiraResponse {
  valorTotalInvestido: number;
  valorAtualCarteira: number;
  plTotal: number;
  rentabilidadePercentual: number;
}

export interface AtivoCarteiraResponse {
  ticker: string;
  quantidade: number;
  precoMedio: number;
  cotacaoAtual: number;
  valorAtual: number;
  pl: number;
  plPercentual: number;
  composicaoCarteira: number;
}

export interface CarteiraResponse {
  clienteId: number;
  nome: string;
  contaGrafica: string;
  dataConsulta: string;
  resumo: ResumoCarteiraResponse;
  ativos: AtivoCarteiraResponse[];
}

export interface CestaItemResponse {
  ticker: string;
  percentual: number;
  cotacaoAtual?: number;
}

export interface CestaDesativadaResponse {
  cestaId: number;
  nome: string;
  dataDesativacao: string;
}

export interface CestaResponse {
  cestaId: number;
  nome: string;
  ativa: boolean;
  dataCriacao: string;
  dataDesativacao?: string;
  itens: CestaItemResponse[];
  cestaAnteriorDesativada?: CestaDesativadaResponse;
  rebalanceamentoDisparado: boolean;
  ativosRemovidos?: string[];
  ativosAdicionados?: string[];
  mensagem: string;
}

export interface CestaHistoricoResponse {
  cestas: CestaResponse[];
}

export interface ContaMasterResponse {
  id: number;
  numeroConta: string;
  tipo: string;
}

export interface CustodiaMasterItemResponse {
  ticker: string;
  quantidade: number;
  precoMedio: number;
  valorAtual: number;
  origem: string;
}

export interface CustodiaMasterResponse {
  contaMaster: ContaMasterResponse;
  custodia: CustodiaMasterItemResponse[];
  valorTotalResiduo: number;
}

export interface SaidaResponse {
  clienteId: number;
  nome: string;
  ativo: boolean;
  dataSaida?: string;
  mensagem: string;
}

export interface DetalheCompraResponse {
  tipo: string;
  ticker: string;
  quantidade: number;
}

export interface OrdemCompraItemResponse {
  ticker: string;
  quantidadeTotal: number;
  detalhes: DetalheCompraResponse[];
  precoUnitario: number;
  valorTotal: number;
}

export interface AtivoDistribuidoResponse {
  ticker: string;
  quantidade: number;
}

export interface DistribuicaoClienteResponse {
  clienteId: number;
  nome: string;
  valorAporte: number;
  ativos: AtivoDistribuidoResponse[];
}

export interface ResiduoMasterResponse {
  ticker: string;
  quantidade: number;
}

export interface ExecutarCompraResponse {
  dataExecucao: string;
  totalClientes: number;
  totalConsolidado: number;
  ordensCompra: OrdemCompraItemResponse[];
  distribuicoes: DistribuicaoClienteResponse[];
  residuosCustMaster: ResiduoMasterResponse[];
  eventosIRPublicados: number;
  mensagem: string;
}

export interface HistoricoAporteResponse {
  data: string;
  valor: number;
  parcela: string;
}

export interface EvolucaoCarteiraResponse {
  data: string;
  valorCarteira: number;
  valorInvestido: number;
  rentabilidade: number;
}

export interface RentabilidadeResponse {
  clienteId: number;
  nome: string;
  dataConsulta: string;
  rentabilidade: ResumoCarteiraResponse;
  historicoAportes: HistoricoAporteResponse[];
  evolucaoCarteira: EvolucaoCarteiraResponse[];
}

export interface ErroResponse {
  erro: string;
  codigo: string;
}
