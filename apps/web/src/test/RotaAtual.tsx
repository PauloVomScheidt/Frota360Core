import { useLocation } from 'react-router-dom'

/**
 * Sonda de navegação: posta como destino (`<Route path="*" element={<RotaAtual />} />`), mostra
 * para onde a tela mandou o usuário e a origem guardada em `state.from`, se houver.
 *
 *   expect(await screen.findByRole('status', { name: 'Rota atual' })).toHaveTextContent('/login')
 */
export function RotaAtual() {
  const location = useLocation()
  const origem = (location.state as { from?: string } | null)?.from
  return (
    <p role="status" aria-label="Rota atual">
      {location.pathname}
      {origem ? ` (origem: ${origem})` : ''}
    </p>
  )
}
