import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { LandingPage } from './pages/LandingPage'
import { LoginPage } from './pages/LoginPage'
import { ForgotPasswordPage } from './pages/ForgotPasswordPage'
import { ResetPasswordPage } from './pages/ResetPasswordPage'
import { AcceptInvitePage } from './pages/AcceptInvitePage'
import { DashboardPage } from './pages/DashboardPage'
import { MotoristasPage } from './pages/MotoristasPage'
import { VeiculosPage } from './pages/VeiculosPage'
import { RotasPage } from './pages/RotasPage'
import { ManutencoesPage } from './pages/ManutencoesPage'
import { TiposManutencaoPage } from './pages/TiposManutencaoPage'
import { UsuariosPage } from './pages/UsuariosPage'
import { ConvitesPage } from './pages/ConvitesPage'
import { RequireAdmin, RequireAuth, RequireGestor } from './components/RequireAuth'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Públicas — as duas últimas são destino dos links enviados por e-mail. */}
        <Route path="/" element={<LandingPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/esqueci-senha" element={<ForgotPasswordPage />} />
        <Route path="/redefinir-senha" element={<ResetPasswordPage />} />
        <Route path="/convite" element={<AcceptInvitePage />} />

        <Route element={<RequireAuth />}>
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/motoristas" element={<MotoristasPage />} />
          <Route path="/veiculos" element={<VeiculosPage />} />
          <Route path="/rotas" element={<RotasPage />} />
          <Route path="/manutencoes" element={<ManutencoesPage />} />
          {/* O catálogo de tipos é tela de gestão: Admin e Supervisor. */}
          <Route element={<RequireGestor />}>
            <Route path="/tipos-manutencao" element={<TiposManutencaoPage />} />
          </Route>
          <Route element={<RequireAdmin />}>
            <Route path="/usuarios" element={<UsuariosPage />} />
            <Route path="/convites" element={<ConvitesPage />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
