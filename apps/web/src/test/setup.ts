import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterAll, afterEach, beforeAll } from 'vitest'
import { servidor } from './servidor'

// O jsdom não implementa showModal/close, e todo formulário do app é um <dialog> aberto por eles.
HTMLDialogElement.prototype.showModal = function () {
  this.setAttribute('open', '')
}
HTMLDialogElement.prototype.close = function () {
  this.removeAttribute('open')
  this.dispatchEvent(new Event('close'))
}

// No MSW 3 a opção se chama onUnhandledFrame (era onUnhandledRequest no 2).
beforeAll(() => servidor.listen({ onUnhandledFrame: 'error' }))

afterEach(() => {
  cleanup()
  servidor.resetHandlers()
  localStorage.clear()
})

afterAll(() => servidor.close())
