import { useState, useCallback } from 'react';
import { aderirProduto, sairProduto, alterarValorMensal, consultarClientePorCpf } from '../api/api';
import { formatCurrency, extractError } from '../utils/helpers';
import ToastContainer, { createToast, type ToastData } from '../components/Toast';
import ConfirmModal from '../components/ConfirmModal';
import type { AdesaoResponse } from '../types';

export default function Adesao() {
  const [form, setForm] = useState({ nome: '', cpf: '', email: '', valorMensal: '' });
  const [resultado, setResultado] = useState<AdesaoResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [toasts, setToasts] = useState<ToastData[]>([]);

  const [saidaCpf, setSaidaCpf] = useState('');
  const [saidaErrors, setSaidaErrors] = useState<Record<string, string>>({});
  const [showSaidaConfirm, setShowSaidaConfirm] = useState(false);

  const [alterarCpf, setAlterarCpf] = useState('');
  const [novoValor, setNovoValor] = useState('');
  const [alterarErrors, setAlterarErrors] = useState<Record<string, string>>({});

  const [buscaCpf, setBuscaCpf] = useState('');
  const [buscaResult, setBuscaResult] = useState<AdesaoResponse | null>(null);
  const [buscaErrors, setBuscaErrors] = useState<Record<string, string>>({});
  const [buscaLoading, setBuscaLoading] = useState(false);

  const addToast = useCallback((message: string, type: ToastData['type']) => {
    setToasts((prev) => [...prev, createToast(message, type)]);
  }, []);

  const removeToast = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const validateAdesao = (): boolean => {
    const errs: Record<string, string> = {};
    if (!form.nome.trim()) errs.nome = 'Nome é obrigatório.';
    if (form.cpf.length !== 11) errs.cpf = 'CPF deve conter 11 dígitos.';
    if (!form.email.includes('@')) errs.email = 'E-mail inválido.';
    const valor = parseFloat(form.valorMensal);
    if (isNaN(valor) || valor < 100) errs.valorMensal = 'Mínimo R$ 100,00.';
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleAdesao = async (e: React.FormEvent) => {
    e.preventDefault();
    setResultado(null);
    if (!validateAdesao()) return;

    setLoading(true);
    try {
      const valor = parseFloat(form.valorMensal);
      const { data } = await aderirProduto({
        nome: form.nome,
        cpf: form.cpf,
        email: form.email,
        valorMensal: valor,
      });
      setResultado(data);
      addToast(`Cliente ${data.nome} cadastrado com sucesso! ID: ${data.clienteId}`, 'success');
      setForm({ nome: '', cpf: '', email: '', valorMensal: '' });
      setErrors({});
    } catch (err: unknown) {
      addToast(extractError(err), 'error');
    } finally {
      setLoading(false);
    }
  };

  const handleSaida = async () => {
    setShowSaidaConfirm(false);
    try {
      const { data: cliente } = await consultarClientePorCpf(saidaCpf);
      const { data } = await sairProduto(cliente.clienteId);
      addToast(data.mensagem, 'success');
      setSaidaCpf('');
    } catch (err: unknown) {
      addToast(extractError(err), 'error');
    }
  };

  const requestSaida = (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};
    if (saidaCpf.length !== 11) errs.cpf = 'CPF deve conter 11 dígitos.';
    setSaidaErrors(errs);
    if (Object.keys(errs).length > 0) return;
    setShowSaidaConfirm(true);
  };

  const handleAlterarValor = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};
    if (alterarCpf.length !== 11) errs.cpf = 'CPF deve conter 11 dígitos.';
    const valor = parseFloat(novoValor);
    if (isNaN(valor) || valor < 100) errs.valor = 'Mínimo R$ 100,00.';
    setAlterarErrors(errs);
    if (Object.keys(errs).length > 0) return;

    try {
      const { data: cliente } = await consultarClientePorCpf(alterarCpf);
      const { data } = await alterarValorMensal(cliente.clienteId, { novoValorMensal: valor });
      addToast(data.mensagem, 'success');
      setAlterarCpf('');
      setNovoValor('');
      setAlterarErrors({});
    } catch (err: unknown) {
      addToast(extractError(err), 'error');
    }
  };

  const handleBuscaCpf = async (e: React.FormEvent) => {
    e.preventDefault();
    setBuscaResult(null);
    const errs: Record<string, string> = {};
    if (buscaCpf.length !== 11) errs.cpf = 'CPF deve conter 11 dígitos.';
    setBuscaErrors(errs);
    if (Object.keys(errs).length > 0) return;

    setBuscaLoading(true);
    try {
      const { data } = await consultarClientePorCpf(buscaCpf);
      setBuscaResult(data);
    } catch (err: unknown) {
      addToast(extractError(err), 'error');
    } finally {
      setBuscaLoading(false);
    }
  };

  return (
    <div>
      <ToastContainer toasts={toasts} onRemove={removeToast} />
      <ConfirmModal
        open={showSaidaConfirm}
        title="Confirmar saída do cliente"
        message={`Deseja realmente solicitar a saída do cliente com CPF ${saidaCpf}? Esta ação irá desativar o plano de compra programada.`}
        confirmLabel="Confirmar Saída"
        variant="danger"
        onConfirm={handleSaida}
        onCancel={() => setShowSaidaConfirm(false)}
      />

      <div className="page-header">
        <h2>Gestão de Clientes</h2>
        <p>Adesão ao produto de compra programada, saída e alteração de valor mensal</p>
      </div>

      <div className="card">
        <div className="card-header">
          <h3 className="card-title">Consultar Cliente</h3>
        </div>
        <form onSubmit={handleBuscaCpf} noValidate>
          <div className="form-row">
            <div className={`form-group ${buscaErrors.cpf ? 'has-error' : ''}`}>
              <label htmlFor="busca-cpf">CPF (11 dígitos)</label>
              <input
                id="busca-cpf"
                type="text"
                value={buscaCpf}
                onChange={(e) => {
                  const val = e.target.value.replace(/\D/g, '').slice(0, 11);
                  setBuscaCpf(val);
                  if (buscaErrors.cpf && val.length === 11) setBuscaErrors({});
                }}
                placeholder="12345678901"
                maxLength={11}
                required
              />
              {buscaErrors.cpf && <span className="form-error">{buscaErrors.cpf}</span>}
            </div>
          </div>
          <button type="submit" className="btn btn-primary" disabled={buscaLoading}>
            {buscaLoading ? 'Buscando...' : 'Buscar'}
          </button>
        </form>

        {buscaResult && (
          <div className="result-table">
            <table>
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Nome</th>
                  <th>CPF</th>
                  <th>Valor Mensal</th>
                  <th>Conta Gráfica</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>{buscaResult.clienteId}</td>
                  <td>{buscaResult.nome}</td>
                  <td>{buscaResult.cpf}</td>
                  <td>{formatCurrency(buscaResult.valorMensal)}</td>
                  <td>{buscaResult.contaGrafica?.numeroConta ?? '-'}</td>
                  <td>
                    <span className={`badge ${buscaResult.ativo ? 'badge-success' : 'badge-danger'}`}>
                      {buscaResult.ativo ? 'Ativo' : 'Inativo'}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        )}
      </div>

      <div className="card">
        <div className="card-header">
          <h3 className="card-title">Nova Adesão</h3>
        </div>

        <form onSubmit={handleAdesao} noValidate>
          <div className="form-row">
            <div className={`form-group ${errors.nome ? 'has-error' : ''}`}>
              <label htmlFor="adesao-nome">Nome Completo</label>
              <input
                id="adesao-nome"
                type="text"
                value={form.nome}
                onChange={(e) => {
                  setForm({ ...form, nome: e.target.value });
                  if (errors.nome) setErrors({ ...errors, nome: '' });
                }}
                placeholder="Ex: João Silva"
                required
              />
              {errors.nome && <span className="form-error">{errors.nome}</span>}
            </div>
            <div className={`form-group ${errors.cpf ? 'has-error' : ''}`}>
              <label htmlFor="adesao-cpf">CPF (11 dígitos)</label>
              <input
                id="adesao-cpf"
                type="text"
                value={form.cpf}
                onChange={(e) => {
                  const val = e.target.value.replace(/\D/g, '').slice(0, 11);
                  setForm({ ...form, cpf: val });
                  if (errors.cpf && val.length === 11) setErrors({ ...errors, cpf: '' });
                }}
                placeholder="12345678901"
                maxLength={11}
                required
              />
              {errors.cpf && <span className="form-error">{errors.cpf}</span>}
            </div>
          </div>
          <div className="form-row">
            <div className={`form-group ${errors.email ? 'has-error' : ''}`}>
              <label htmlFor="adesao-email">E-mail</label>
              <input
                id="adesao-email"
                type="email"
                value={form.email}
                onChange={(e) => {
                  setForm({ ...form, email: e.target.value });
                  if (errors.email && e.target.value.includes('@')) setErrors({ ...errors, email: '' });
                }}
                placeholder="joao@email.com"
                required
              />
              {errors.email && <span className="form-error">{errors.email}</span>}
            </div>
            <div className={`form-group ${errors.valorMensal ? 'has-error' : ''}`}>
              <label htmlFor="adesao-valor">Valor Mensal (min R$ 100,00)</label>
              <input
                id="adesao-valor"
                type="number"
                step="0.01"
                min="100"
                value={form.valorMensal}
                onChange={(e) => {
                  setForm({ ...form, valorMensal: e.target.value });
                  const v = parseFloat(e.target.value);
                  if (errors.valorMensal && !isNaN(v) && v >= 100) setErrors({ ...errors, valorMensal: '' });
                }}
                placeholder="500.00"
                required
              />
              {errors.valorMensal && <span className="form-error">{errors.valorMensal}</span>}
            </div>
          </div>
          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Processando...' : 'Aderir ao Produto'}
          </button>
        </form>

        {resultado && (
          <div className="result-table">
            <table>
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Nome</th>
                  <th>CPF</th>
                  <th>Valor Mensal</th>
                  <th>Conta Gráfica</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>{resultado.clienteId}</td>
                  <td>{resultado.nome}</td>
                  <td>{resultado.cpf}</td>
                  <td>{formatCurrency(resultado.valorMensal)}</td>
                  <td>{resultado.contaGrafica?.numeroConta ?? '-'}</td>
                  <td>
                    <span className={`badge ${resultado.ativo ? 'badge-success' : 'badge-danger'}`}>
                      {resultado.ativo ? 'Ativo' : 'Inativo'}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        )}
      </div>

      <div className="form-row">
        <div className="card">
          <div className="card-header">
            <h3 className="card-title">Solicitar Saída</h3>
          </div>
          <form onSubmit={requestSaida} noValidate>
            <div className={`form-group ${saidaErrors.cpf ? 'has-error' : ''}`}>
              <label htmlFor="saida-cpf">CPF do Cliente (11 dígitos)</label>
              <input
                id="saida-cpf"
                type="text"
                value={saidaCpf}
                onChange={(e) => {
                  const val = e.target.value.replace(/\D/g, '').slice(0, 11);
                  setSaidaCpf(val);
                  if (saidaErrors.cpf && val.length === 11) setSaidaErrors({});
                }}
                placeholder="12345678901"
                maxLength={11}
                required
              />
              {saidaErrors.cpf && <span className="form-error">{saidaErrors.cpf}</span>}
            </div>
            <button type="submit" className="btn btn-danger">Solicitar Saída</button>
          </form>
        </div>

        <div className="card">
          <div className="card-header">
            <h3 className="card-title">Alterar Valor Mensal</h3>
          </div>
          <form onSubmit={handleAlterarValor} noValidate>
            <div className={`form-group ${alterarErrors.cpf ? 'has-error' : ''}`}>
              <label htmlFor="alterar-cpf">CPF do Cliente (11 dígitos)</label>
              <input
                id="alterar-cpf"
                type="text"
                value={alterarCpf}
                onChange={(e) => {
                  const val = e.target.value.replace(/\D/g, '').slice(0, 11);
                  setAlterarCpf(val);
                  if (alterarErrors.cpf && val.length === 11) setAlterarErrors({});
                }}
                placeholder="12345678901"
                maxLength={11}
                required
              />
              {alterarErrors.cpf && <span className="form-error">{alterarErrors.cpf}</span>}
            </div>
            <div className={`form-group ${alterarErrors.valor ? 'has-error' : ''}`}>
              <label htmlFor="alterar-valor">Novo Valor Mensal (min R$ 100,00)</label>
              <input
                id="alterar-valor"
                type="number"
                step="0.01"
                min="100"
                value={novoValor}
                onChange={(e) => {
                  setNovoValor(e.target.value);
                  if (alterarErrors.valor) setAlterarErrors({});
                }}
                placeholder="750.00"
                required
              />
              {alterarErrors.valor && <span className="form-error">{alterarErrors.valor}</span>}
            </div>
            <button type="submit" className="btn btn-primary">Alterar Valor</button>
          </form>
        </div>
      </div>
    </div>
  );
}
