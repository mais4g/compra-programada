import { useState, useCallback } from 'react';
import { consultarCarteira, consultarClientePorCpf } from '../api/api';
import { formatCurrency, formatDate, extractError } from '../utils/helpers';
import { SkeletonTable, SkeletonStats } from '../components/Skeleton';
import EmptyState from '../components/EmptyState';
import ToastContainer, { createToast, type ToastData } from '../components/Toast';
import type { CarteiraResponse } from '../types';

export default function Carteira() {
  const [cpf, setCpf] = useState('');
  const [carteira, setCarteira] = useState<CarteiraResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [toasts, setToasts] = useState<ToastData[]>([]);

  const addToast = useCallback((message: string, type: ToastData['type']) => {
    setToasts((prev) => [...prev, createToast(message, type)]);
  }, []);

  const removeToast = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const handleConsultar = async (e: React.FormEvent) => {
    e.preventDefault();
    setCarteira(null);
    if (cpf.length !== 11) {
      addToast('CPF deve conter 11 dígitos.', 'error');
      return;
    }
    setLoading(true);

    try {
      const { data: cliente } = await consultarClientePorCpf(cpf);
      const { data } = await consultarCarteira(cliente.clienteId);
      setCarteira(data);
    } catch (err: unknown) {
      addToast(extractError(err), 'error');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <ToastContainer toasts={toasts} onRemove={removeToast} />

      <div className="page-header">
        <h2>Carteira do Cliente</h2>
        <p>Consulte a custódia e o P&L da carteira</p>
      </div>

      <div className="card">
        <form onSubmit={handleConsultar} className="inline-form">
          <div className="form-group inline-form-field">
            <label htmlFor="carteira-cpf">CPF do Cliente (11 dígitos)</label>
            <input
              id="carteira-cpf"
              type="text"
              value={cpf}
              onChange={(e) => setCpf(e.target.value.replace(/\D/g, '').slice(0, 11))}
              placeholder="12345678901"
              maxLength={11}
              required
            />
          </div>
          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Consultando...' : 'Consultar Carteira'}
          </button>
        </form>
      </div>

      {loading && (
        <>
          <SkeletonStats count={4} />
          <div className="card">
            <SkeletonTable cols={8} rows={5} />
          </div>
        </>
      )}

      {carteira && (
        <>
          <div className="card card-compact">
            <p className="text-secondary">
              <strong>{carteira.nome}</strong> | Conta: {carteira.contaGrafica} | Consulta: {formatDate(carteira.dataConsulta)}
            </p>
          </div>

          <div className="stats-grid">
            <div className="stat-card">
              <div className="stat-label">Valor Investido</div>
              <div className="stat-value">{formatCurrency(carteira.resumo.valorTotalInvestido)}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Valor Atual</div>
              <div className="stat-value">{formatCurrency(carteira.resumo.valorAtualCarteira)}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">P&L Total</div>
              <div className={`stat-value ${carteira.resumo.plTotal >= 0 ? 'positive' : 'negative'}`}>
                {formatCurrency(carteira.resumo.plTotal)}
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Rentabilidade</div>
              <div className={`stat-value ${carteira.resumo.rentabilidadePercentual >= 0 ? 'positive' : 'negative'}`}>
                {carteira.resumo.rentabilidadePercentual.toFixed(2)}%
              </div>
            </div>
          </div>

          <div className="card">
            <div className="card-header">
              <h3 className="card-title">Ativos em Custódia</h3>
            </div>
            {carteira.ativos.length === 0 ? (
              <EmptyState
                icon="chart"
                title="Nenhum ativo em custódia"
                description="Este cliente ainda não possui ativos. Execute uma compra programada para iniciar."
              />
            ) : (
              <div className="table-container">
                <table aria-label="Ativos em custódia">
                  <thead>
                    <tr>
                      <th>Ticker</th>
                      <th>Qtd</th>
                      <th>Preço Médio</th>
                      <th>Cotação Atual</th>
                      <th>Valor Atual</th>
                      <th>P&L</th>
                      <th>P&L %</th>
                      <th>Composição</th>
                    </tr>
                  </thead>
                  <tbody>
                    {carteira.ativos.map((ativo) => (
                      <tr key={ativo.ticker}>
                        <td><strong>{ativo.ticker}</strong></td>
                        <td>{ativo.quantidade}</td>
                        <td>{formatCurrency(ativo.precoMedio)}</td>
                        <td>{formatCurrency(ativo.cotacaoAtual)}</td>
                        <td>{formatCurrency(ativo.valorAtual)}</td>
                        <td className={ativo.pl >= 0 ? 'positive' : 'negative'}>
                          {formatCurrency(ativo.pl)}
                        </td>
                        <td className={ativo.plPercentual >= 0 ? 'positive' : 'negative'}>
                          {ativo.plPercentual.toFixed(2)}%
                        </td>
                        <td>{ativo.composicaoCarteira.toFixed(1)}%</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
}
