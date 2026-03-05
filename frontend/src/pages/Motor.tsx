import { useState, useCallback } from 'react';
import { executarCompra, rebalancearPorDesvio, consultarClientePorCpf } from '../api/api';
import { formatCurrency, formatDate, extractError } from '../utils/helpers';
import ToastContainer, { createToast, type ToastData } from '../components/Toast';
import ConfirmModal from '../components/ConfirmModal';
import type { ExecutarCompraResponse } from '../types';

export default function Motor() {
  const [dataRef, setDataRef] = useState('');
  const [compraResult, setCompraResult] = useState<ExecutarCompraResponse | null>(null);
  const [compraLoading, setCompraLoading] = useState(false);
  const [showCompraConfirm, setShowCompraConfirm] = useState(false);

  const [rebalCpf, setRebalCpf] = useState('');
  const [rebalLoading, setRebalLoading] = useState(false);
  const [showRebalConfirm, setShowRebalConfirm] = useState(false);

  const [toasts, setToasts] = useState<ToastData[]>([]);

  const addToast = useCallback((message: string, type: ToastData['type']) => {
    setToasts((prev) => [...prev, createToast(message, type)]);
  }, []);

  const removeToast = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const requestCompra = (e: React.FormEvent) => {
    e.preventDefault();
    if (!dataRef) {
      addToast('Informe uma data de referência.', 'error');
      return;
    }
    setShowCompraConfirm(true);
  };

  const handleCompra = async () => {
    setShowCompraConfirm(false);
    setCompraResult(null);
    setCompraLoading(true);

    try {
      const { data } = await executarCompra({ dataReferencia: dataRef });
      setCompraResult(data);
      addToast(data.mensagem || 'Compra executada com sucesso!', 'success');
    } catch (err: unknown) {
      addToast(extractError(err), 'error');
    } finally {
      setCompraLoading(false);
    }
  };

  const requestRebalancear = (e: React.FormEvent) => {
    e.preventDefault();
    if (rebalCpf.length !== 11) {
      addToast('CPF deve conter 11 dígitos.', 'error');
      return;
    }
    setShowRebalConfirm(true);
  };

  const handleRebalancear = async () => {
    setShowRebalConfirm(false);
    setRebalLoading(true);

    try {
      const { data: cliente } = await consultarClientePorCpf(rebalCpf);
      const { data } = await rebalancearPorDesvio(cliente.clienteId);
      addToast(data.mensagem, 'success');
      setRebalCpf('');
    } catch (err: unknown) {
      addToast(extractError(err), 'error');
    } finally {
      setRebalLoading(false);
    }
  };

  return (
    <div>
      <ToastContainer toasts={toasts} onRemove={removeToast} />
      <ConfirmModal
        open={showCompraConfirm}
        title="Confirmar execução de compra"
        message={`Executar compra programada para a data ${dataRef}? Esta ação irá processar os aportes de todos os clientes ativos.`}
        confirmLabel="Executar Compra"
        variant="primary"
        onConfirm={handleCompra}
        onCancel={() => setShowCompraConfirm(false)}
      />
      <ConfirmModal
        open={showRebalConfirm}
        title="Confirmar rebalanceamento"
        message={`Rebalancear carteira do cliente com CPF ${rebalCpf}? Ordens de compra/venda serão geradas para ajustar a proporção dos ativos.`}
        confirmLabel="Rebalancear"
        variant="primary"
        onConfirm={handleRebalancear}
        onCancel={() => setShowRebalConfirm(false)}
      />

      <div className="page-header">
        <h2>Motor de Compra & Rebalanceamento</h2>
        <p>Execute compras programadas e rebalanceamento por desvio de proporção</p>
      </div>

      <div className="motor-grid">
        <div className="card">
          <div className="card-header">
            <h3 className="card-title">Executar Compra Programada</h3>
          </div>

          <div className="alert alert-info">
            Dias de compra válidos: <strong>5, 15 e 25</strong> de cada mês.
            Sábados e domingos são ajustados para segunda-feira.
          </div>

          <form onSubmit={requestCompra}>
            <div className="form-group">
              <label htmlFor="motor-data">Data de Referência</label>
              <input
                id="motor-data"
                type="date"
                value={dataRef}
                onChange={(e) => setDataRef(e.target.value)}
                required
              />
            </div>
            <button type="submit" className="btn btn-primary" disabled={compraLoading}>
              {compraLoading ? 'Executando...' : 'Executar Compra'}
            </button>
          </form>
        </div>

        <div className="card">
          <div className="card-header">
            <h3 className="card-title">Rebalancear por Desvio</h3>
          </div>

          <div className="alert alert-info">
            Dispara rebalanceamento quando desvio {'>'} 5pp na proporção dos ativos.
          </div>

          <form onSubmit={requestRebalancear}>
            <div className="form-group">
              <label htmlFor="motor-rebal-cpf">CPF do Cliente (11 dígitos)</label>
              <input
                id="motor-rebal-cpf"
                type="text"
                value={rebalCpf}
                onChange={(e) => setRebalCpf(e.target.value.replace(/\D/g, '').slice(0, 11))}
                placeholder="12345678901"
                maxLength={11}
                required
              />
            </div>
            <button type="submit" className="btn btn-primary" disabled={rebalLoading}>
              {rebalLoading ? 'Rebalanceando...' : 'Rebalancear'}
            </button>
          </form>
        </div>
      </div>

      {compraResult && (
        <>
          <div className="stats-grid">
            <div className="stat-card">
              <div className="stat-label">Data Execução</div>
              <div className="stat-value stat-value-sm">{formatDate(compraResult.dataExecucao)}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Total Clientes</div>
              <div className="stat-value">{compraResult.totalClientes}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Total Consolidado</div>
              <div className="stat-value">{formatCurrency(compraResult.totalConsolidado)}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Eventos IR Publicados</div>
              <div className="stat-value">{compraResult.eventosIRPublicados}</div>
            </div>
          </div>

          {compraResult.ordensCompra.length > 0 && (
            <div className="card">
              <div className="card-header">
                <h3 className="card-title">Ordens de Compra</h3>
              </div>
              <div className="table-container">
                <table aria-label="Ordens de compra">
                  <thead>
                    <tr>
                      <th>Ticker</th>
                      <th>Qtd Total</th>
                      <th>Preço Unit.</th>
                      <th>Valor Total</th>
                      <th>Detalhes</th>
                    </tr>
                  </thead>
                  <tbody>
                    {compraResult.ordensCompra.map((ordem) => (
                      <tr key={ordem.ticker}>
                        <td><strong>{ordem.ticker}</strong></td>
                        <td>{ordem.quantidadeTotal}</td>
                        <td>{formatCurrency(ordem.precoUnitario)}</td>
                        <td>{formatCurrency(ordem.valorTotal)}</td>
                        <td>
                          {ordem.detalhes.map((d) => (
                            <span key={`${d.tipo}-${d.ticker}`} className="badge badge-warning badge-inline">
                              {d.tipo}: {d.ticker} x{d.quantidade}
                            </span>
                          ))}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {compraResult.distribuicoes.length > 0 && (
            <div className="card">
              <div className="card-header">
                <h3 className="card-title">Distribuição por Cliente</h3>
              </div>
              <div className="table-container">
                <table aria-label="Distribuição por cliente">
                  <thead>
                    <tr>
                      <th>Cliente</th>
                      <th>Nome</th>
                      <th>Valor Aporte</th>
                      <th>Ativos Distribuídos</th>
                    </tr>
                  </thead>
                  <tbody>
                    {compraResult.distribuicoes.map((dist) => (
                      <tr key={dist.clienteId}>
                        <td>{dist.clienteId}</td>
                        <td>{dist.nome}</td>
                        <td>{formatCurrency(dist.valorAporte)}</td>
                        <td>
                          {dist.ativos.map((a) => (
                            <span key={a.ticker} className="badge badge-success badge-inline">
                              {a.ticker}: {a.quantidade}
                            </span>
                          ))}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {compraResult.residuosCustMaster.length > 0 && (
            <div className="card">
              <div className="card-header">
                <h3 className="card-title">Resíduos na Conta Master</h3>
              </div>
              <div className="badge-list">
                {compraResult.residuosCustMaster.map((r) => (
                  <span key={r.ticker} className="badge badge-warning badge-lg">
                    {r.ticker}: {r.quantidade} ações
                  </span>
                ))}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
