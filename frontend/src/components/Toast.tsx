import { useEffect } from 'react';

export interface ToastData {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info';
}

let nextId = 1;
export function createToast(message: string, type: ToastData['type'] = 'success'): ToastData {
  return { id: nextId++, message, type };
}

interface ToastContainerProps {
  toasts: ToastData[];
  onRemove: (id: number) => void;
}

export default function ToastContainer({ toasts, onRemove }: ToastContainerProps) {
  return (
    <div className="toast-container" aria-live="polite" aria-label="Notificações">
      {toasts.map((toast) => (
        <ToastItem key={toast.id} toast={toast} onRemove={onRemove} />
      ))}
    </div>
  );
}

function ToastItem({ toast, onRemove }: { toast: ToastData; onRemove: (id: number) => void }) {
  useEffect(() => {
    const timer = setTimeout(() => onRemove(toast.id), 4000);
    return () => clearTimeout(timer);
  }, [toast.id, onRemove]);

  return (
    <div className={`toast toast-${toast.type}`} role="status">
      <span className="toast-icon">
        {toast.type === 'success' && '\u2713'}
        {toast.type === 'error' && '\u2717'}
        {toast.type === 'info' && 'i'}
      </span>
      <span className="toast-message">{toast.message}</span>
      <button
        className="toast-close"
        onClick={() => onRemove(toast.id)}
        aria-label="Fechar notificação"
      >
        &times;
      </button>
    </div>
  );
}
