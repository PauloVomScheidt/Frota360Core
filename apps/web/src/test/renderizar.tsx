import type { ReactElement } from 'react'
import { render } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, type InitialEntry } from 'react-router-dom'
import { tokenStorage } from '../api/tokenStorage'
import type { Role } from '../api/types'
import { fabricarSessao } from './fabricas'

interface OpcoesRenderizar {
  /** Caminho inicial, ou `{ pathname, state }` para simular um redirecionamento recebido. */
  rota?: InitialEntry
  papel?: Role
  /** `false` para o `App`, que já traz o próprio `BrowserRouter`. */
  roteador?: boolean
}

export function renderizar(ui: ReactElement, { rota = '/', papel, roteador = true }: OpcoesRenderizar = {}) {
  if (papel) tokenStorage.setSession(fabricarSessao({ role: papel }))

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
  const user = userEvent.setup()

  const resultado = render(
    <QueryClientProvider client={queryClient}>
      {roteador ? <MemoryRouter initialEntries={[rota]}>{ui}</MemoryRouter> : ui}
    </QueryClientProvider>,
  )

  return { ...resultado, user, queryClient }
}
