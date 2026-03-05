interface EmptyStateProps {
  icon?: 'table' | 'chart' | 'folder' | 'search';
  title: string;
  description?: string;
  action?: { label: string; onClick: () => void };
}

const icons: Record<string, JSX.Element> = {
  table: (
    <svg width="48" height="48" viewBox="0 0 48 48" fill="none" aria-hidden="true">
      <rect x="6" y="10" width="36" height="28" rx="3" stroke="currentColor" strokeWidth="2" fill="none" />
      <line x1="6" y1="18" x2="42" y2="18" stroke="currentColor" strokeWidth="2" />
      <line x1="6" y1="26" x2="42" y2="26" stroke="currentColor" strokeWidth="1.5" strokeDasharray="3 2" />
      <line x1="6" y1="32" x2="42" y2="32" stroke="currentColor" strokeWidth="1.5" strokeDasharray="3 2" />
      <line x1="18" y1="18" x2="18" y2="38" stroke="currentColor" strokeWidth="1.5" />
    </svg>
  ),
  chart: (
    <svg width="48" height="48" viewBox="0 0 48 48" fill="none" aria-hidden="true">
      <rect x="8" y="28" width="6" height="12" rx="1" stroke="currentColor" strokeWidth="2" fill="none" />
      <rect x="18" y="20" width="6" height="20" rx="1" stroke="currentColor" strokeWidth="2" fill="none" />
      <rect x="28" y="14" width="6" height="26" rx="1" stroke="currentColor" strokeWidth="2" fill="none" />
      <rect x="38" y="8" width="6" height="32" rx="1" stroke="currentColor" strokeWidth="2" fill="none" />
      <line x1="4" y1="42" x2="46" y2="42" stroke="currentColor" strokeWidth="2" />
    </svg>
  ),
  folder: (
    <svg width="48" height="48" viewBox="0 0 48 48" fill="none" aria-hidden="true">
      <path d="M6 14C6 12.8954 6.89543 12 8 12H18L22 16H40C41.1046 16 42 16.8954 42 18V36C42 37.1046 41.1046 38 40 38H8C6.89543 38 6 37.1046 6 36V14Z" stroke="currentColor" strokeWidth="2" fill="none" />
    </svg>
  ),
  search: (
    <svg width="48" height="48" viewBox="0 0 48 48" fill="none" aria-hidden="true">
      <circle cx="22" cy="22" r="10" stroke="currentColor" strokeWidth="2" fill="none" />
      <line x1="30" y1="30" x2="40" y2="40" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" />
    </svg>
  ),
};

export default function EmptyState({ icon = 'table', title, description, action }: EmptyStateProps) {
  return (
    <div className="empty-state-enhanced">
      <div className="empty-state-icon">{icons[icon]}</div>
      <p className="empty-state-title">{title}</p>
      {description && <p className="empty-state-desc">{description}</p>}
      {action && (
        <button className="btn btn-primary btn-sm" onClick={action.onClick} style={{ marginTop: 12 }}>
          {action.label}
        </button>
      )}
    </div>
  );
}
