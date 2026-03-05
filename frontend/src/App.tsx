import { Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import Adesao from './pages/Adesao';
import Carteira from './pages/Carteira';
import Rentabilidade from './pages/Rentabilidade';
import AdminCesta from './pages/AdminCesta';
import CustodiaMaster from './pages/CustodiaMaster';
import Motor from './pages/Motor';

export default function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Adesao />} />
        <Route path="/carteira" element={<Carteira />} />
        <Route path="/rentabilidade" element={<Rentabilidade />} />
        <Route path="/admin/cesta" element={<AdminCesta />} />
        <Route path="/admin/custodia-master" element={<CustodiaMaster />} />
        <Route path="/motor" element={<Motor />} />
      </Routes>
    </Layout>
  );
}
