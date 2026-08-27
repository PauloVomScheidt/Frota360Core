import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { tokenStorage } from '../api/tokenStorage'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'

/** Bloqueia rotas protegidas quando não há sessão; o 401 do servidor cobre o resto. */
export function RequireAuth() {
  const location = useLocation()
  if (!tokenStorage.getToken()) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }
  return <Outlet />
}

/** Rotas de administração: sem role Admin, volta para a visão geral. */
export function RequireAdmin() {
  const user = useSession()
  if (!pode.gerenciarUsuarios(user?.role)) {
    return <Navigate to="/dashboard" replace />
  }
  return <Outlet />
}

/** Catálogo de tipos de manutenção: sem Admin/Supervisor, volta para a visão geral. */
export function RequireGestor() {
  const user = useSession()
  if (!pode.editarTiposManutencao(user?.role)) {
    return <Navigate to="/dashboard" replace />
  }
  return <Outlet />
}
