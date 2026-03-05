import { useState, useEffect, useCallback } from 'react';
import { consultarCustodiaMaster } from '../api/api';
import { formatCurrency, extractError } from '../utils/helpers';
import { SkeletonTable, SkeletonStats } from '../components/Skeleton';
import EmptyState from '../components/EmptyState';
import ToastContainer, { createToast, type ToastData } from '../components/Toast';
import type { CustodiaMasterResponse } from '../types';

export default function CustodiaMaster() {
  const [dados, setDados] = useState<CustodiaMasterResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [toasts, setToasts] = useState<ToastData[]>([]);

  const addToast = useCallback((message: string, type: ToastData['type']) => {
    setToasts((prev) => [...prev, createToast(message, type)]);
  }, []);

  const removeToast = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const carregar = async () => {
    setLoading(true);
    try {
      const { data } = await consultarCustodiaMaster();
      setDados(data);
    } catch (err: unknown) {
      addToast(extractError(err), 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    carregar();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <div>
      <ToastContainer toasts={toasts} onRemove={removeToast} />

      <div className="page-header">
        <h2>Custódia Master</h2>
        <p>Resíduos de ações na conta master aguardando próxima distribuição</p>
      </div>

      {loading && (
        <>
          <SkeletonStats count={3} />
          <div className="card"><SkeletonTable cols={5} rows={4} /></div>
        </>
      )}

      {!loading && dados && (
        <>
          <div className="stats-grid">
            <div className="stat-card">
              <div className="stat-label">Conta Master</div>
              <div className="stat-value stat-value-sm">{dados.contaMaster.numeroConta}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Tipo</div>
              <div className="stat-value stat-value-sm">{dados.contaMaster.tipo}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Valor Total Resíduo</div>
              <div className="stat-value">{formatCurrency(dados.valorTotalResiduo)}</div>
            </div>
          </div>

          <div className="card">
            <div className="card-header">
              <h3 className="card-title">Ativos em Custódia</h3>
              <button className="btn btn-secondary btn-sm" onClick={carregar}>Atualizar</button>
            </div>
            {dados.custodia.length === 0 ? (
              <EmptyState
                icon="table"
                title="Nenhum resíduo na conta master"
                description="Resíduos aparecerão aqui após a execução de compras programadas."
              />
            ) : (
              <div className="table-container">
                <table aria-label="Custódia da conta master">
                  <thead>
                    <tr>
                      <th>Ticker</th>
                      <th>Quantidade</th>
                      <th>Preço Médio</th>
                      <th>Valor Atual</th>
                      <th>Origem</th>
                    </tr>
                  </thead>
                  <tbody>
                    {dados.custodia.map((item) => (
                      <tr key={item.ticker}>
                        <td><strong>{item.ticker}</strong></td>
                        <td>{item.quantidade}</td>
                        <td>{formatCurrency(item.precoMedio)}</td>
                        <td>{formatCurrency(item.valorAtual)}</td>
                        <td><span className="badge badge-warning">{item.origem}</span></td>
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
