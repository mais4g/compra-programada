import { useState, useCallback } from 'react';
import { consultarRentabilidade, consultarClientePorCpf } from '../api/api';
import { formatCurrency, formatDate, extractError } from '../utils/helpers';
import { SkeletonTable, SkeletonStats } from '../components/Skeleton';
import EmptyState from '../components/EmptyState';
import ToastContainer, { createToast, type ToastData } from '../components/Toast';
import type { RentabilidadeResponse } from '../types';

export default function Rentabilidade() {
  const [cpf, setCpf] = useState('');
  const [dados, setDados] = useState<RentabilidadeResponse | null>(null);
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
    setDados(null);
    if (cpf.length !== 11) {
      addToast('CPF deve conter 11 dígitos.', 'error');
      return;
    }
    setLoading(true);

    try {
      const { data: cliente } = await consultarClientePorCpf(cpf);
      const { data } = await consultarRentabilidade(cliente.clienteId);
      setDados(data);
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
        <h2>Rentabilidade</h2>
        <p>Análise detalhada de rentabilidade, histórico de aportes e evolução</p>
      </div>

      <div className="card">
        <form onSubmit={handleConsultar} className="inline-form">
          <div className="form-group inline-form-field">
            <label htmlFor="rent-cpf">CPF do Cliente (11 dígitos)</label>
            <input
              id="rent-cpf"
              type="text"
              value={cpf}
              onChange={(e) => setCpf(e.target.value.replace(/\D/g, '').slice(0, 11))}
              placeholder="12345678901"
              maxLength={11}
              required
            />
          </div>
          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Consultando...' : 'Consultar Rentabilidade'}
          </button>
        </form>
      </div>

      {loading && (
        <>
          <SkeletonStats count={4} />
          <div className="card"><SkeletonTable cols={3} rows={3} /></div>
          <div className="card"><SkeletonTable cols={4} rows={3} /></div>
        </>
      )}

      {dados && (
        <>
          <div className="card card-compact">
            <p className="text-secondary">
              <strong>{dados.nome}</strong> | Consulta: {formatDate(dados.dataConsulta)}
            </p>
          </div>

          <div className="stats-grid">
            <div className="stat-card">
              <div className="stat-label">Valor Investido</div>
              <div className="stat-value">{formatCurrency(dados.rentabilidade.valorTotalInvestido)}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Valor Atual</div>
              <div className="stat-value">{formatCurrency(dados.rentabilidade.valorAtualCarteira)}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">P&L Total</div>
              <div className={`stat-value ${dados.rentabilidade.plTotal >= 0 ? 'positive' : 'negative'}`}>
                {formatCurrency(dados.rentabilidade.plTotal)}
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Rentabilidade</div>
              <div className={`stat-value ${dados.rentabilidade.rentabilidadePercentual >= 0 ? 'positive' : 'negative'}`}>
                {dados.rentabilidade.rentabilidadePercentual.toFixed(2)}%
              </div>
            </div>
          </div>

          <div className="card">
            <div className="card-header">
              <h3 className="card-title">Histórico de Aportes</h3>
            </div>
            {dados.historicoAportes.length === 0 ? (
              <EmptyState
                icon="table"
                title="Nenhum aporte registrado"
                description="Os aportes aparecerão aqui após a execução de compras."
              />
            ) : (
              <div className="table-container">
                <table aria-label="Histórico de aportes">
                  <thead>
                    <tr>
                      <th>Data</th>
                      <th>Valor</th>
                      <th>Parcela</th>
                    </tr>
                  </thead>
                  <tbody>
                    {dados.historicoAportes.map((aporte) => (
                      <tr key={`${aporte.data}-${aporte.parcela}`}>
                        <td>{formatDate(aporte.data)}</td>
                        <td>{formatCurrency(aporte.valor)}</td>
                        <td><span className="badge badge-warning">{aporte.parcela}</span></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <div className="card">
            <div className="card-header">
              <h3 className="card-title">Evolução da Carteira</h3>
            </div>
            {dados.evolucaoCarteira.length === 0 ? (
              <EmptyState
                icon="chart"
                title="Nenhum dado de evolução"
                description="A evolução da carteira será exibida após múltiplas execuções de compra."
              />
            ) : (
              <div className="table-container">
                <table aria-label="Evolução da carteira">
                  <thead>
                    <tr>
                      <th>Data</th>
                      <th>Valor Carteira</th>
                      <th>Valor Investido</th>
                      <th>Rentabilidade</th>
                    </tr>
                  </thead>
                  <tbody>
                    {dados.evolucaoCarteira.map((evo) => (
                      <tr key={evo.data}>
                        <td>{formatDate(evo.data)}</td>
                        <td>{formatCurrency(evo.valorCarteira)}</td>
                        <td>{formatCurrency(evo.valorInvestido)}</td>
                        <td className={evo.rentabilidade >= 0 ? 'positive' : 'negative'}>
                          {evo.rentabilidade.toFixed(2)}%
                        </td>
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
