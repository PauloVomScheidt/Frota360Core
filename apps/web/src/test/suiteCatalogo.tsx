import { screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import type { ReactElement } from 'react'
import { describe, expect, it } from 'vitest'
import type { Role } from '../api/types'
import { capturar, responderErro, responderJson } from './capturar'
import { renderizar } from './renderizar'

interface ItemDeCatalogo {
  id: number
  nome: string
  ativo: boolean
}

/**
 * Os quatro catálogos (`/postos`, `/tipos-combustivel`, `/tipos-despesa`, `/tipos-manutencao`)
 * têm o mesmo ciclo — listar inclusive inativos, cadastrar, editar, inativar pela linha e
 * excluir com 422 de "em uso". Esta suíte roda esse ciclo; o arquivo de teste de cada tela a
 * chama com a configuração dela e acrescenta o que só ela tem.
 */
export interface ConfigCatalogo<T extends ItemDeCatalogo> {
  tela: () => ReactElement
  titulo: string
  /** Caminho da API, ex.: `/posto`. */
  recurso: string
  botaoNovo: string
  dialogoNovo: string
  dialogoEdicao: string
  tituloExclusao: string
  textoVazio: RegExp
  /** Primeiro ativo, depois inativo. */
  itens: [T, T]
  /** Como o item aparece nos rótulos acessíveis: `o posto X`, `o tipo X`. */
  descricao: (item: T) => string
  rotuloNome: string
  rotuloSituacao: string
  situacao: { ativo: string; inativo: string }
  alternar: { desativar: string; ativar: string }
  preencherNovo: (user: UserEvent, dialogo: HTMLElement) => Promise<void>
  corpoNovo: Record<string, unknown>
  /** O PUT esperado ao renomear o item ativo para "Renomeado". */
  corpoRenomeado: (item: T) => Record<string, unknown>
  /** O PUT esperado do atalho de inativar o item ativo. */
  corpoInativado: (item: T) => Record<string, unknown>
}

export function testarCatalogo<T extends ItemDeCatalogo>(c: ConfigCatalogo<T>) {
  const [ativo, inativo] = c.itens

  function montar(papel: Role = 'Admin', itens: T[] = c.itens) {
    const lista = capturar('get', c.recurso, responderJson(itens))
    return { ...renderizar(c.tela(), { papel }), lista }
  }

  const linha = (item: T) => screen.findByRole('row', { name: new RegExp(item.nome) })

  describe(`${c.titulo} — listagem`, () => {
    it('mostra o título e os itens, inclusive os inativos, sem pedir só os ativos', async () => {
      const { lista } = montar()
      expect(screen.getByRole('heading', { name: c.titulo })).toBeInTheDocument()
      expect(await linha(ativo)).toHaveTextContent(c.situacao.ativo)
      expect(await linha(inativo)).toHaveTextContent(c.situacao.inativo)
      expect(lista[0].params.apenasAtivos).not.toBe('true')
    })

    it('catálogo vazio diz o que fazer', async () => {
      montar('Admin', [])
      expect(await screen.findByText(c.textoVazio)).toBeInTheDocument()
    })

    it('sem permissão de edição, não há novo, editar, excluir nem atalho de situação', async () => {
      montar('Operador')
      await linha(ativo)
      expect(screen.queryByRole('button', { name: c.botaoNovo })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /^Editar/ })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /^Excluir/ })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: c.alternar.desativar })).not.toBeInTheDocument()
      expect(screen.queryByRole('columnheader', { name: 'Ações' })).not.toBeInTheDocument()
    })
  })

  describe(`${c.titulo} — cadastro e edição`, () => {
    it('cadastra com o corpo esperado e recarrega a lista', async () => {
      const criar = capturar('post', c.recurso, responderJson(ativo))
      const { user, lista } = montar()
      await user.click(await screen.findByRole('button', { name: c.botaoNovo }))
      const dialogo = screen.getByRole('dialog', { name: c.dialogoNovo })
      expect(within(dialogo).queryByLabelText(c.rotuloSituacao)).not.toBeInTheDocument()

      await c.preencherNovo(user, dialogo)
      await user.click(within(dialogo).getByRole('button', { name: 'Cadastrar' }))

      await waitFor(() => expect(criar).toHaveLength(1))
      expect(criar[0].corpo).toEqual(c.corpoNovo)
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
      await waitFor(() => expect(lista.length).toBeGreaterThan(1))
    })

    it('edita pelo PUT, com a situação editável só aqui', async () => {
      const atualizar = capturar('put', `${c.recurso}/:id`, responderJson(ativo))
      const { user } = montar()
      await user.click(await screen.findByRole('button', { name: `Editar ${c.descricao(ativo)}` }))
      const dialogo = screen.getByRole('dialog', { name: c.dialogoEdicao })
      expect(within(dialogo).getByLabelText(c.rotuloSituacao)).toHaveValue('true')

      const nome = within(dialogo).getByLabelText(c.rotuloNome)
      await user.clear(nome)
      await user.type(nome, 'Renomeado')
      await user.click(within(dialogo).getByRole('button', { name: 'Salvar alterações' }))

      await waitFor(() => expect(atualizar).toHaveLength(1))
      expect(atualizar[0].url.pathname).toMatch(new RegExp(`${c.recurso}/${ativo.id}$`))
      expect(atualizar[0].corpo).toEqual(c.corpoRenomeado(ativo))
    })

    it('nome duplicado (422) aparece literal no formulário', async () => {
      const mensagem = `Já existe um item com o nome "${ativo.nome}".`
      capturar('post', c.recurso, responderErro(422, [mensagem]))
      const { user } = montar()
      await user.click(await screen.findByRole('button', { name: c.botaoNovo }))
      const dialogo = screen.getByRole('dialog', { name: c.dialogoNovo })
      await c.preencherNovo(user, dialogo)
      await user.click(within(dialogo).getByRole('button', { name: 'Cadastrar' }))

      expect(await within(dialogo).findByText(mensagem)).toBeInTheDocument()
    })
  })

  describe(`${c.titulo} — situação e exclusão`, () => {
    it('o atalho da linha inverte a situação por um PUT', async () => {
      const atualizar = capturar('put', `${c.recurso}/:id`, responderJson(ativo))
      const { user } = montar()
      await user.click(within(await linha(ativo)).getByRole('button', { name: c.alternar.desativar }))

      await waitFor(() => expect(atualizar).toHaveLength(1))
      expect(atualizar[0].corpo).toEqual(c.corpoInativado(ativo))
      expect(within(await linha(inativo)).getByRole('button', { name: c.alternar.ativar })).toBeInTheDocument()
    })

    it('exclui depois de confirmar', async () => {
      const excluir = capturar('delete', `${c.recurso}/:id`)
      const { user } = montar()
      await user.click(await screen.findByRole('button', { name: `Excluir ${c.descricao(inativo)}` }))
      await user.click(within(screen.getByRole('alertdialog', { name: c.tituloExclusao })).getByRole('button', { name: 'Excluir' }))

      await waitFor(() => expect(excluir).toHaveLength(1))
      expect(excluir[0].url.pathname).toMatch(new RegExp(`${c.recurso}/${inativo.id}$`))
      await waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument())
    })

    it('item em uso (422) não é excluído, e a mensagem pede para inativar', async () => {
      const mensagem = 'Este item está em uso. Inative-o para tirá-lo do seletor sem apagar o histórico.'
      capturar('delete', `${c.recurso}/:id`, responderErro(422, [mensagem]))
      const { user } = montar()
      await user.click(await screen.findByRole('button', { name: `Excluir ${c.descricao(ativo)}` }))
      const confirmacao = screen.getByRole('alertdialog', { name: c.tituloExclusao })
      await user.click(within(confirmacao).getByRole('button', { name: 'Excluir' }))

      expect(await within(confirmacao).findByText(mensagem)).toBeInTheDocument()
    })

    it('o Supervisor também exclui — exclusão acompanha edição', async () => {
      montar('Supervisor')
      expect(await screen.findAllByRole('button', { name: /^Excluir/ })).toHaveLength(2)
    })
  })
}
