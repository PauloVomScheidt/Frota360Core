import { useSyncExternalStore } from 'react'
import { tokenStorage, type StoredUser } from '../api/tokenStorage'

// localStorage não notifica a própria aba; o evento abaixo cobre login/logout
// feitos por esta aba, e 'storage' cobre as outras.
const EVENTO_SESSAO = 'frota360:sessao'

export function notificarMudancaDeSessao() {
  window.dispatchEvent(new Event(EVENTO_SESSAO))
}

function subscribe(onChange: () => void) {
  window.addEventListener(EVENTO_SESSAO, onChange)
  window.addEventListener('storage', onChange)
  return () => {
    window.removeEventListener(EVENTO_SESSAO, onChange)
    window.removeEventListener('storage', onChange)
  }
}

let cache: { raw: string | null; user: StoredUser | null } = { raw: null, user: null }

function getSnapshot(): StoredUser | null {
  // useSyncExternalStore exige identidade estável entre renders sem mudança.
  const raw = localStorage.getItem('frota360.user')
  if (raw !== cache.raw) cache = { raw, user: tokenStorage.getUser() }
  return cache.user
}

/** Usuário logado (nome, e-mail, role) reativo a login/logout. */
export function useSession(): StoredUser | null {
  return useSyncExternalStore(subscribe, getSnapshot, () => null)
}
