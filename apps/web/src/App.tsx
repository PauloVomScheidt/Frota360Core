import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { ForgotPasswordPage } from './pages/ForgotPasswordPage'
import { ResetPasswordPage } from './pages/ResetPasswordPage'
import { AcceptInvitePage } from './pages/AcceptInvitePage'
import { DashboardPage } from './pages/DashboardPage'
import { UsuariosPage } from './pages/UsuariosPage'
import { ConvitesPage } from './pages/ConvitesPage'
import { RequireAdmin, RequireAuth } from './components/RequireAuth'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Anônimas — as duas últimas são destino dos links enviados por e-mail. */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/esqueci-senha" element={<ForgotPasswordPage />} />
        <Route path="/redefinir-senha" element={<ResetPasswordPage />} />
        <Route path="/convite" element={<AcceptInvitePage />} />

        <Route element={<RequireAuth />}>
          <Route path="/" element={<DashboardPage />} />
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
