import axios from 'axios';
import type {
  AdesaoRequest,
  AdesaoResponse,
  AlterarValorMensalRequest,
  AlterarValorMensalResponse,
  CarteiraResponse,
  CestaRequest,
  CestaResponse,
  CestaHistoricoResponse,
  CustodiaMasterResponse,
  ExecutarCompraRequest,
  ExecutarCompraResponse,
  RentabilidadeResponse,
  SaidaResponse,
} from '../types';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

// === Clientes ===

export const aderirProduto = (data: AdesaoRequest) =>
  api.post<AdesaoResponse>('/clientes/adesao', data);

export const sairProduto = (clienteId: number) =>
  api.post<SaidaResponse>(`/clientes/${clienteId}/saida`);

export const alterarValorMensal = (clienteId: number, data: AlterarValorMensalRequest) =>
  api.put<AlterarValorMensalResponse>(`/clientes/${clienteId}/valor-mensal`, data);

export const consultarClientePorCpf = (cpf: string) =>
  api.get<AdesaoResponse>(`/clientes/por-cpf/${cpf}`);

export const consultarCarteira = (clienteId: number) =>
  api.get<CarteiraResponse>(`/clientes/${clienteId}/carteira`);

export const consultarRentabilidade = (clienteId: number) =>
  api.get<RentabilidadeResponse>(`/clientes/${clienteId}/rentabilidade`);

// === Admin ===

export const cadastrarCesta = (data: CestaRequest) =>
  api.post<CestaResponse>('/admin/cesta', data);

export const obterCestaAtual = () =>
  api.get<CestaResponse>('/admin/cesta/atual');

export const obterHistoricoCestas = () =>
  api.get<CestaHistoricoResponse>('/admin/cesta/historico');

export const consultarCustodiaMaster = () =>
  api.get<CustodiaMasterResponse>('/admin/conta-master/custodia');

// === Motor ===

export const executarCompra = (data: ExecutarCompraRequest) =>
  api.post<ExecutarCompraResponse>('/motor/executar-compra', data);

export const rebalancearPorDesvio = (clienteId: number) =>
  api.post<{ mensagem: string }>(`/motor/rebalancear-desvio/${clienteId}`);
