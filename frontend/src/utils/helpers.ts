export function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('pt-BR');
}

export function extractError(err: unknown): string {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const response = (err as { response?: { data?: { erro?: string; title?: string } } }).response;
    return response?.data?.erro ?? response?.data?.title ?? 'Erro ao processar requisição.';
  }
  return 'Erro de conexão com o servidor.';
}
