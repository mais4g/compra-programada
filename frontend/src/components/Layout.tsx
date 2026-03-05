import { useState, useEffect } from 'react';
import { NavLink, useLocation } from 'react-router-dom';

const navItems = [
  {
    section: 'Cliente',
    items: [
      { to: '/', label: 'Adesão', breadcrumb: 'Gestão de Clientes' },
      { to: '/carteira', label: 'Carteira', breadcrumb: 'Carteira do Cliente' },
      { to: '/rentabilidade', label: 'Rentabilidade', breadcrumb: 'Rentabilidade' },
    ],
  },
  {
    section: 'Administração',
    items: [
      { to: '/admin/cesta', label: 'Cesta Top Five', breadcrumb: 'Cesta Top Five' },
      { to: '/admin/custodia-master', label: 'Custódia Master', breadcrumb: 'Custódia Master' },
    ],
  },
  {
    section: 'Motor',
    items: [
      { to: '/motor', label: 'Compra & Rebalanceamento', breadcrumb: 'Motor de Compra' },
    ],
  },
];

function getBreadcrumb(pathname: string): { section: string; label: string } {
  for (const group of navItems) {
    for (const item of group.items) {
      if (item.to === pathname || (item.to !== '/' && pathname.startsWith(item.to))) {
        return { section: group.section, label: item.breadcrumb };
      }
    }
  }
  return { section: 'Cliente', label: 'Gestão de Clientes' };
}

function getInitialTheme(): 'dark' | 'light' {
  const stored = localStorage.getItem('theme');
  if (stored === 'light' || stored === 'dark') return stored;
  return 'dark';
}

export default function Layout({ children }: { children: React.ReactNode }) {
  const [menuOpen, setMenuOpen] = useState(false);
  const [theme, setTheme] = useState<'dark' | 'light'>(getInitialTheme);
  const location = useLocation();
  const breadcrumb = getBreadcrumb(location.pathname);

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('theme', theme);
  }, [theme]);

  useEffect(() => {
    setMenuOpen(false);
  }, [location.pathname]);

  useEffect(() => {
    if (menuOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
    return () => { document.body.style.overflow = ''; };
  }, [menuOpen]);

  const toggleTheme = () => {
    setTheme((prev) => (prev === 'dark' ? 'light' : 'dark'));
  };

  return (
    <div className="app-layout">
      <button
        className="mobile-menu-btn"
        onClick={() => setMenuOpen(true)}
        aria-label="Abrir menu de navegação"
      >
        <span className="hamburger-line" />
        <span className="hamburger-line" />
        <span className="hamburger-line" />
      </button>

      {menuOpen && (
        <div
          className="sidebar-overlay"
          onClick={() => setMenuOpen(false)}
          aria-hidden="true"
        />
      )}

      <aside
        className={`sidebar ${menuOpen ? 'sidebar-open' : ''}`}
        role="navigation"
        aria-label="Menu principal"
      >
        <div className="sidebar-header">
          <div className="sidebar-header-row">
            <div>
              <h1>Compra Programada</h1>
              <p>Corretora</p>
            </div>
            <button
              className="mobile-close-btn"
              onClick={() => setMenuOpen(false)}
              aria-label="Fechar menu"
            >
              &times;
            </button>
          </div>
        </div>
        <nav className="sidebar-nav" aria-label="Navegação principal">
          {navItems.map((section) => (
            <div key={section.section} className="nav-section">
              <div className="nav-section-title" role="heading" aria-level={2}>
                {section.section}
              </div>
              {section.items.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.to === '/'}
                  className={({ isActive }) =>
                    `nav-link ${isActive ? 'active' : ''}`
                  }
                  aria-current={location.pathname === item.to ? 'page' : undefined}
                >
                  {item.label}
                </NavLink>
              ))}
            </div>
          ))}
        </nav>
        <div className="sidebar-footer">
          <span>v1.0</span>
          <button
            className="theme-toggle"
            onClick={toggleTheme}
            aria-label={theme === 'dark' ? 'Mudar para tema claro' : 'Mudar para tema escuro'}
            title={theme === 'dark' ? 'Tema claro' : 'Tema escuro'}
          >
            {theme === 'dark' ? '\u2600' : '\u263E'}
          </button>
        </div>
      </aside>

      <main className="main-content" role="main">
        <nav className="breadcrumb" aria-label="Breadcrumb">
          <span className="breadcrumb-section">{breadcrumb.section}</span>
          <span className="breadcrumb-sep" aria-hidden="true">/</span>
          <span className="breadcrumb-current">{breadcrumb.label}</span>
        </nav>
        {children}
      </main>
    </div>
  );
}
