import { Navigate, Outlet, useLocation } from 'react-router-dom'
import type { Role } from '../api/types'
import { rotaInicial } from '../auth/permissions'
import { useSession } from '../auth/useSession'

/**
 * Bloqueia rotas protegidas quando não há sessão; o 401 do servidor cobre o resto.
 * O JWT em si vive num cookie HttpOnly, invisível ao JS — quem sinaliza sessão aqui é a
 * identidade (`useSession`), guardada à parte só para a UI exibir nome/e-mail/papel.
 */
export function RequireAuth() {
  const location = useLocation()
  const user = useSession()
  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }
  return <Outlet />
}

/**
 * Guarda genérico por permissão: recebe um predicado de `auth/permissions.ts` e manda
 * quem não passa para a home do papel dele — nunca para `/dashboard` fixo, que para o
 * motorista é justamente uma tela bloqueada (os dois guards ficariam em pingue-pongue).
 *
 * Um guarda por papel deixaria de fazer sentido agora que o motorista enxerga parte do
 * painel: quem manda é a tela, não um bloco de papéis.
 */
export function RequirePode({ permitido }: { permitido: (role?: Role) => boolean }) {
  const user = useSession()

  if (!permitido(user?.role)) {
    return <Navigate to={rotaInicial(user?.role)} replace />
  }

  return <Outlet />
}
