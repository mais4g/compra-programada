interface SkeletonRowProps {
  cols: number;
  rows?: number;
}

export function SkeletonTable({ cols, rows = 4 }: SkeletonRowProps) {
  return (
    <div className="skeleton-table" aria-busy="true" aria-label="Carregando dados">
      <div className="skeleton-header">
        {Array.from({ length: cols }, (_, i) => (
          <div key={i} className="skeleton-cell skeleton-pulse" />
        ))}
      </div>
      {Array.from({ length: rows }, (_, r) => (
        <div key={r} className="skeleton-row">
          {Array.from({ length: cols }, (_, c) => (
            <div key={c} className="skeleton-cell skeleton-pulse" />
          ))}
        </div>
      ))}
    </div>
  );
}

export function SkeletonStats({ count = 4 }: { count?: number }) {
  return (
    <div className="stats-grid" aria-busy="true" aria-label="Carregando estatísticas">
      {Array.from({ length: count }, (_, i) => (
        <div key={i} className="stat-card">
          <div className="skeleton-line skeleton-line-sm skeleton-pulse" />
          <div className="skeleton-line skeleton-line-lg skeleton-pulse" />
        </div>
      ))}
    </div>
  );
}
