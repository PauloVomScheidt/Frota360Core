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
import { MinhasRotasPage } from './pages/MinhasRotasPage'
import { ManutencoesPage } from './pages/ManutencoesPage'
import { TiposManutencaoPage } from './pages/TiposManutencaoPage'
import { UsuariosPage } from './pages/UsuariosPage'
import { ConvitesPage } from './pages/ConvitesPage'
import { AuditoriaPage } from './pages/AuditoriaPage'
import { AbastecimentosPage } from './pages/AbastecimentosPage'
import { PerfilPage } from './pages/PerfilPage'
import { RequireAuth, RequirePode } from './components/RequireAuth'
import { pode } from './auth/permissions'

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

        {/* Cada tela declara a própria permissão: o motorista vê parte do painel
            (veículos e manutenções, só leitura), então não há um bloco único. */}
        <Route element={<RequireAuth />}>
          {/* Única tela sem `RequirePode`: editar o próprio cadastro é direito de qualquer
              autenticado, o Motorista inclusive — é justamente ele quem tem CPF. */}
          <Route path="/perfil" element={<PerfilPage />} />

          <Route element={<RequirePode permitido={pode.verDashboard} />}>
            <Route path="/dashboard" element={<DashboardPage />} />
          </Route>
          <Route element={<RequirePode permitido={pode.verMotoristas} />}>
            <Route path="/motoristas" element={<MotoristasPage />} />
          </Route>
          <Route element={<RequirePode permitido={pode.verVeiculos} />}>
            <Route path="/veiculos" element={<VeiculosPage />} />
          </Route>
          <Route element={<RequirePode permitido={pode.verRotas} />}>
            <Route path="/rotas" element={<RotasPage />} />
          </Route>
          <Route element={<RequirePode permitido={pode.verManutencoes} />}>
            <Route path="/manutencoes" element={<ManutencoesPage />} />
          </Route>
          <Route element={<RequirePode permitido={pode.verAbastecimentos} />}>
            <Route path="/abastecimentos" element={<AbastecimentosPage />} />
          </Route>
          <Route element={<RequirePode permitido={pode.verMinhasRotas} />}>
            <Route path="/minhas-rotas" element={<MinhasRotasPage />} />
          </Route>
          <Route element={<RequirePode permitido={pode.editarTiposManutencao} />}>
            <Route path="/tipos-manutencao" element={<TiposManutencaoPage />} />
          </Route>
          <Route element={<RequirePode permitido={pode.gerenciarUsuarios} />}>
            <Route path="/usuarios" element={<UsuariosPage />} />
            <Route path="/convites" element={<ConvitesPage />} />
          </Route>
          <Route element={<RequirePode permitido={pode.verAuditoria} />}>
            <Route path="/auditoria" element={<AuditoriaPage />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
