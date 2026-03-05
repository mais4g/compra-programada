import { useState, useEffect, useCallback } from 'react';
import { cadastrarCesta, obterCestaAtual, obterHistoricoCestas } from '../api/api';
import { formatCurrency, formatDate, extractError } from '../utils/helpers';
import EmptyState from '../components/EmptyState';
import ToastContainer, { createToast, type ToastData } from '../components/Toast';
import type { CestaResponse, CestaItemRequest } from '../types';

export default function AdminCesta() {
  const [cestaAtual, setCestaAtual] = useState<CestaResponse | null>(null);
  const [historico, setHistorico] = useState<CestaResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [toasts, setToasts] = useState<ToastData[]>([]);

  const [nome, setNome] = useState('');
  const [itens, setItens] = useState<CestaItemRequest[]>([
    { ticker: '', percentual: 0 },
    { ticker: '', percentual: 0 },
    { ticker: '', percentual: 0 },
    { ticker: '', percentual: 0 },
    { ticker: '', percentual: 0 },
  ]);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  const [tab, setTab] = useState<'atual' | 'nova' | 'historico'>('atual');

  const addToast = useCallback((message: string, type: ToastData['type']) => {
    setToasts((prev) => [...prev, createToast(message, type)]);
  }, []);

  const removeToast = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  useEffect(() => {
    carregarCestaAtual();
  }, []);

  const carregarCestaAtual = async () => {
    try {
      const { data } = await obterCestaAtual();
      setCestaAtual(data);
    } catch {
      setCestaAtual(null);
    }
  };

  const carregarHistorico = async () => {
    try {
      const { data } = await obterHistoricoCestas();
      setHistorico(data.cestas);
    } catch {
      setHistorico([]);
    }
  };

  const handleTabChange = (newTab: 'atual' | 'nova' | 'historico') => {
    setTab(newTab);
    if (newTab === 'historico') carregarHistorico();
    if (newTab === 'atual') carregarCestaAtual();
  };

  const updateItem = (index: number, field: keyof CestaItemRequest, value: string) => {
    const updated = [...itens];
    if (field === 'percentual') {
      const parsed = parseFloat(value) || 0;
      updated[index] = { ...updated[index], percentual: Math.min(100, Math.max(0, parsed)) };
    } else {
      updated[index] = { ...updated[index], [field]: value.toUpperCase() };
    }
    setItens(updated);
  };

  const totalPercentual = itens.reduce((sum, item) => sum + item.percentual, 0);

  const handleCadastrar = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};

    if (!nome.trim()) errs.nome = 'Nome é obrigatório.';
    const itensValidos = itens.filter((i) => i.ticker.trim() !== '' && i.percentual > 0);
    if (itensValidos.length < 2) errs.itens = 'Informe pelo menos 2 ativos com percentual válido.';
    if (Math.abs(totalPercentual - 100) >= 0.1) errs.total = 'O total dos percentuais deve somar 100%.';
    setFormErrors(errs);
    if (Object.keys(errs).length > 0) return;

    setLoading(true);
    try {
      const { data } = await cadastrarCesta({ nome, itens: itensValidos });
      addToast(data.mensagem || 'Cesta cadastrada com sucesso!', 'success');
      setCestaAtual(data);
      setTab('atual');
      setNome('');
      setItens([
        { ticker: '', percentual: 0 },
        { ticker: '', percentual: 0 },
        { ticker: '', percentual: 0 },
        { ticker: '', percentual: 0 },
        { ticker: '', percentual: 0 },
      ]);
      setFormErrors({});
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
        <h2>Cesta Top Five</h2>
        <p>Gerencie a carteira recomendada de ações</p>
      </div>

      <div className="tab-group" role="tablist" aria-label="Abas da cesta">
        {(['atual', 'nova', 'historico'] as const).map((t) => (
          <button
            key={t}
            className={`tab-btn ${tab === t ? 'tab-active' : ''}`}
            onClick={() => handleTabChange(t)}
            role="tab"
            aria-selected={tab === t}
            aria-controls={`panel-${t}`}
          >
            {t === 'atual' ? 'Cesta Atual' : t === 'nova' ? 'Nova Cesta' : 'Histórico'}
          </button>
        ))}
      </div>

      {tab === 'atual' && (
        <div className="card" role="tabpanel" id="panel-atual">
          <div className="card-header">
            <h3 className="card-title">Cesta Ativa</h3>
            <button className="btn btn-secondary btn-sm" onClick={carregarCestaAtual}>Atualizar</button>
          </div>
          {cestaAtual ? (
            <>
              <div style={{ marginBottom: 16 }}>
                <p><strong>Nome:</strong> {cestaAtual.nome}</p>
                <p><strong>Data de criação:</strong> {formatDate(cestaAtual.dataCriacao)}</p>
                <p>
                  <strong>Status:</strong>{' '}
                  <span className={`badge ${cestaAtual.ativa ? 'badge-success' : 'badge-danger'}`}>
                    {cestaAtual.ativa ? 'Ativa' : 'Inativa'}
                  </span>
                </p>
              </div>
              <div className="table-container">
                <table aria-label="Composição da cesta">
                  <thead>
                    <tr>
                      <th>Ticker</th>
                      <th>Percentual</th>
                      <th>Cotação Atual</th>
                    </tr>
                  </thead>
                  <tbody>
                    {cestaAtual.itens.map((item) => (
                      <tr key={item.ticker}>
                        <td><strong>{item.ticker}</strong></td>
                        <td>{item.percentual.toFixed(1)}%</td>
                        <td>{item.cotacaoAtual ? formatCurrency(item.cotacaoAtual) : '-'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          ) : (
            <EmptyState
              icon="folder"
              title="Nenhuma cesta ativa"
              description="Cadastre uma nova cesta para iniciar as compras programadas."
              action={{ label: 'Criar Cesta', onClick: () => setTab('nova') }}
            />
          )}
        </div>
      )}

      {tab === 'nova' && (
        <div className="card" role="tabpanel" id="panel-nova">
          <div className="card-header">
            <h3 className="card-title">Cadastrar Nova Cesta</h3>
          </div>

          <form onSubmit={handleCadastrar} noValidate>
            <div className={`form-group ${formErrors.nome ? 'has-error' : ''}`}>
              <label htmlFor="cesta-nome">Nome da Cesta</label>
              <input
                id="cesta-nome"
                type="text"
                value={nome}
                onChange={(e) => {
                  setNome(e.target.value);
                  if (formErrors.nome) setFormErrors({ ...formErrors, nome: '' });
                }}
                placeholder="Ex: Top Five Fevereiro 2026"
                required
              />
              {formErrors.nome && <span className="form-error">{formErrors.nome}</span>}
            </div>

            <label style={{ display: 'block', fontSize: 13, fontWeight: 600, marginBottom: 8 }}>
              Ativos (Ticker + Percentual)
            </label>

            {formErrors.itens && <div className="alert alert-error">{formErrors.itens}</div>}

            {itens.map((item, idx) => (
              <div className="ticker-row" key={idx}>
                <input
                  type="text"
                  placeholder={`Ticker ${idx + 1} (ex: PETR4)`}
                  value={item.ticker}
                  onChange={(e) => updateItem(idx, 'ticker', e.target.value)}
                  maxLength={6}
                  aria-label={`Ticker do ativo ${idx + 1}`}
                />
                <input
                  type="number"
                  step="0.1"
                  min="0"
                  max="100"
                  placeholder="%"
                  value={item.percentual || ''}
                  onChange={(e) => updateItem(idx, 'percentual', e.target.value)}
                  aria-label={`Percentual do ativo ${idx + 1}`}
                />
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={() => {
                    const updated = [...itens];
                    updated[idx] = { ticker: '', percentual: 0 };
                    setItens(updated);
                  }}
                >
                  Limpar
                </button>
              </div>
            ))}

            <div style={{ marginTop: 12, marginBottom: 16, fontSize: 14 }}>
              <strong>Total:</strong>{' '}
              <span style={{ color: Math.abs(totalPercentual - 100) < 0.1 ? 'var(--success)' : 'var(--danger)' }}>
                {totalPercentual.toFixed(1)}%
              </span>
              {formErrors.total && (
                <span className="form-error" style={{ marginLeft: 8 }}>{formErrors.total}</span>
              )}
            </div>

            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Cadastrando...' : 'Cadastrar Cesta'}
            </button>
          </form>
        </div>
      )}

      {tab === 'historico' && (
        <div className="card" role="tabpanel" id="panel-historico">
          <div className="card-header">
            <h3 className="card-title">Histórico de Cestas</h3>
          </div>
          {historico.length === 0 ? (
            <EmptyState
              icon="folder"
              title="Nenhum histórico disponível"
              description="O histórico será exibido quando cestas forem substituídas."
            />
          ) : (
            historico.map((cesta) => (
              <div key={cesta.cestaId} style={{ borderBottom: '1px solid var(--border)', paddingBottom: 16, marginBottom: 16 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                  <div>
                    <strong>{cesta.nome}</strong>
                    <span className={`badge ${cesta.ativa ? 'badge-success' : 'badge-danger'}`} style={{ marginLeft: 8 }}>
                      {cesta.ativa ? 'Ativa' : 'Inativa'}
                    </span>
                  </div>
                  <span style={{ fontSize: 12, color: 'var(--text-secondary)' }}>
                    {formatDate(cesta.dataCriacao)}
                    {cesta.dataDesativacao && ` - ${formatDate(cesta.dataDesativacao)}`}
                  </span>
                </div>
                <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap' }}>
                  {cesta.itens.map((item) => (
                    <span key={item.ticker} className="badge" style={{ background: 'var(--bg-elevated)', color: 'var(--text-secondary)' }}>
                      {item.ticker}: {item.percentual.toFixed(1)}%
                    </span>
                  ))}
                </div>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
}
