import { Navigate, Outlet } from 'react-router-dom'
import { tokenStorage } from '../api/tokenStorage'

export function RequireAuth() {
  if (!tokenStorage.getToken()) {
    return <Navigate to="/login" replace />
  }
  return <Outlet />
}
