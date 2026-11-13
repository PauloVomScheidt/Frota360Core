import { screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Role } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { porTextoCompleto } from '../test/consultas'
import { fabricarDespesa, fabricarResumo, paginado } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { DespesasPage } from './DespesasPage'

const PEDAGIO = fabricarDespesa({
  id: 301,
  veiculoId: 1,
  veiculoPlaca: 'ABC1D01',
  tipoDespesaId: 1,
  tipoDespesaNome: 'Pedágio',
  valor: 100,
  dataDespesa: '2026-11-08T00:00:00',
  observacao: 'Praça da BR-101',
})
const MULTA = fabricarDespesa({
  id: 302,
  veiculoId: 2,
  veiculoPlaca: 'ABC1D02',
  tipoDespesaId: 2,
  tipoDespesaNome: 'IPVA',
  motoristaId: 10,
  motoristaNome: 'João Pereira',
  valor: 50,
  dataDespesa: '2026-11-09T00:00:00',
})

function montar(papel: Role = 'Admin', total = 2) {
  const lista = capturar('get', '/despesa', ({ params }) =>
    responderJson(paginado([PEDAGIO, MULTA], Number(params.pagina), Number(params.tamanhoPagina), total))(),
  )
  const resumo = capturar('get', '/despesa/resumo', responderJson(fabricarResumo({ quantidade: 37, valorTotal: 9876.5 })))
  const tela = renderizar(<DespesasPage />, { rota: '/despesas', papel })
  return { ...tela, lista, resumo }
}

async function abrirNova(user: UserEvent) {
  await user.click(await screen.findByRole('button', { name: 'Nova despesa' }))
  return screen.getByRole('dialog', { name: 'Nova despesa' })
}

afterEach(() => vi.useRealTimers())

describe('DespesasPage — listagem e rodapé', () => {
  it('mostra as despesas da página, com "—" para quem não tem motorista', async () => {
    montar()
    const pedagio = within(await screen.findByRole('row', { name: /Pedágio/ })).getAllByRole('cell')
    expect(pedagio[3]).toHaveTextContent('—')
    expect(pedagio[5]).toHaveTextContent('Praça da BR-101')
    expect(within(screen.getByRole('row', { name: /IPVA/ })).getByText('João Pereira')).toBeInTheDocument()
  })

  it('o rodapé "N lançamentos · Total" vem de /despesa/resumo, não da soma da página', async () => {
    montar()
    expect(await screen.findByText(porTextoCompleto('37 lançamentos'))).toBeInTheDocument()
    expect(screen.getByText(porTextoCompleto('Total: R$ 9.876,50'))).toBeInTheDocument()
    expect(screen.queryByText(porTextoCompleto('Total: R$ 150,00'))).not.toBeInTheDocument()
  })

  it('abre nos últimos 30 dias, e o resumo usa o mesmo recorte sem paginação', async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 12, 10, 0))
    const { lista, resumo } = montar()
    await screen.findByText(porTextoCompleto('37 lançamentos'))

    expect(lista[0].params).toMatchObject({ de: '2026-10-14', ate: '2026-11-12', pagina: '1', tamanhoPagina: '15' })
    expect(resumo[0].params).toEqual({ de: '2026-10-14', ate: '2026-11-12' })
  })

  it('trocar de página pede a página certa ao servidor e não refaz a soma', async () => {
    const { user, lista, resumo } = montar('Admin', 37)
    await screen.findByText('1–15 de 37')

    await user.click(screen.getByRole('button', { name: 'Próxima' }))
    await waitFor(() => expect(lista.at(-1)?.params).toMatchObject({ pagina: '2', tamanhoPagina: '15' }))
    await user.click(await screen.findByRole('button', { name: 'Próxima' }))
    await waitFor(() => expect(lista.at(-1)?.params.pagina).toBe('3'))

    expect(resumo).toHaveLength(1)
    expect(screen.getByText(porTextoCompleto('Total: R$ 9.876,50'))).toBeInTheDocument()
  })

  it('trocar o tamanho da página volta para a página 1', async () => {
    const { user, lista } = montar('Admin', 37)
    await screen.findByText('1–15 de 37')
    await user.click(screen.getByRole('button', { name: 'Próxima' }))
    await waitFor(() => expect(lista.at(-1)?.params.pagina).toBe('2'))

    await user.selectOptions(screen.getByRole('combobox', { name: 'Itens por página' }), '20')
    await waitFor(() => expect(lista.at(-1)?.params).toMatchObject({ pagina: '1', tamanhoPagina: '20' }))
  })

  it('filtrar por tipo recorta lista e resumo, e volta para a página 1', async () => {
    const { user, lista, resumo } = montar('Admin', 37)
    await screen.findByText('1–15 de 37')
    await user.click(screen.getByRole('button', { name: 'Próxima' }))
    await waitFor(() => expect(lista.at(-1)?.params.pagina).toBe('2'))

    await user.selectOptions(screen.getByLabelText('Tipo'), 'IPVA')

    await waitFor(() => expect(lista.at(-1)?.params).toMatchObject({ tipoDespesaId: '2', pagina: '1' }))
    expect(resumo.at(-1)?.params).toMatchObject({ tipoDespesaId: '2' })
  })

  it('filtrar por veículo e motorista vai para o servidor', async () => {
    const { user, lista } = montar()
    await screen.findByRole('row', { name: /Pedágio/ })
    await user.selectOptions(screen.getByLabelText('Veículo'), 'ABC1D02 — Caminhão 2')
    await user.selectOptions(screen.getByLabelText('Motorista'), 'Carla Souza')
    await waitFor(() => expect(lista.at(-1)?.params).toMatchObject({ veiculoId: '2', motoristaId: '11' }))
  })

  it('"Limpar filtros" tira o recorte de período', async () => {
    const { user, lista } = montar()
    await screen.findByRole('row', { name: /Pedágio/ })
    await user.click(screen.getByRole('button', { name: 'Limpar filtros' }))
    await waitFor(() => expect(lista.at(-1)?.params.de).toBeUndefined())
    expect(screen.queryByRole('button', { name: 'Limpar filtros' })).not.toBeInTheDocument()
  })

  it('sem tipo de despesa ativo, avisa e não deixa lançar', async () => {
    capturar('get', '/tipodespesa', responderJson([]))
    montar()
    expect(await screen.findByText(/Nenhum tipo de despesa ativo/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Nova despesa' })).toBeDisabled()
  })
})

describe('DespesasPage — lançar', () => {
  it('o POST leva veículo, tipo, valor e data; motorista "Não atribuída" vai como null', async () => {
    const criar = capturar('post', '/despesa', responderJson(PEDAGIO))
    const { user, lista } = montar()
    const dialogo = await abrirNova(user)

    expect(within(dialogo).getByLabelText('Motorista (opcional)')).toHaveDisplayValue('Não atribuída')
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1')
    await user.selectOptions(within(dialogo).getByLabelText('Tipo'), 'Pedágio')
    await user.type(within(dialogo).getByLabelText('Valor (R$)'), '25.4')
    await user.clear(within(dialogo).getByLabelText('Data'))
    await user.type(within(dialogo).getByLabelText('Data'), '2026-11-10')
    await user.click(within(dialogo).getByRole('button', { name: 'Lançar' }))

    await waitFor(() => expect(criar).toHaveLength(1))
    expect(criar[0].corpo).toEqual({
      veiculoId: 1,
      tipoDespesaId: 1,
      motoristaId: null,
      valor: 25.4,
      dataDespesa: '2026-11-10',
      observacao: null,
    })
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    await waitFor(() => expect(lista.length).toBeGreaterThan(1))
  })

  it('motorista escolhido vai pelo id', async () => {
    const criar = capturar('post', '/despesa', responderJson(MULTA))
    const { user } = montar()
    const dialogo = await abrirNova(user)
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D02 — Caminhão 2')
    await user.selectOptions(within(dialogo).getByLabelText('Tipo'), 'IPVA')
    await user.selectOptions(within(dialogo).getByLabelText('Motorista (opcional)'), 'João Pereira')
    await user.type(within(dialogo).getByLabelText('Valor (R$)'), '880')
    await user.type(within(dialogo).getByLabelText('Observação (opcional)'), '  parcela 1  ')
    await user.click(within(dialogo).getByRole('button', { name: 'Lançar' }))

    await waitFor(() => expect(criar).toHaveLength(1))
    expect(criar[0].corpo).toMatchObject({ motoristaId: 10, valor: 880, observacao: 'parcela 1' })
  })

  it('só oferece os tipos ativos', async () => {
    capturar(
      'get',
      '/tipodespesa',
      ({ params }) =>
        responderJson(
          [
            { id: 1, nome: 'Pedágio', ativo: true, dataInclusao: '2026-01-01T00:00:00' },
            { id: 3, nome: 'Seguro antigo', ativo: false, dataInclusao: '2026-01-01T00:00:00' },
          ].filter((t) => params.apenasAtivos !== 'true' || t.ativo),
        )(),
    )
    const { user } = montar()
    const dialogo = await abrirNova(user)
    expect(within(dialogo).queryByRole('option', { name: 'Seguro antigo' })).not.toBeInTheDocument()
  })

  it('um 422 da API aparece com a mensagem literal', async () => {
    const mensagem = 'O tipo de despesa informado está inativo.'
    capturar('post', '/despesa', responderErro(422, [mensagem]))
    const { user } = montar()
    const dialogo = await abrirNova(user)
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1')
    await user.selectOptions(within(dialogo).getByLabelText('Tipo'), 'Pedágio')
    await user.type(within(dialogo).getByLabelText('Valor (R$)'), '10')
    await user.click(within(dialogo).getByRole('button', { name: 'Lançar' }))

    expect(await within(dialogo).findByText(mensagem)).toBeInTheDocument()
  })

  it('o formulário abre com a data de hoje, mesmo que a tela tenha sido carregada noutro dia', async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2030, 0, 15, 9, 0))
    const { user } = montar()
    const dialogo = await abrirNova(user)
    expect(within(dialogo).getByLabelText('Data')).toHaveValue('2030-01-15')
  })
})

describe('DespesasPage — corrigir e excluir', () => {
  it('a correção alcança todos os campos, inclusive o veículo', async () => {
    const atualizar = capturar('put', '/despesa/:id', responderJson(PEDAGIO))
    const { user } = montar()
    await user.click(await screen.findByRole('button', { name: /^Editar a despesa de R\$\s100,00$/ }))
    const dialogo = screen.getByRole('dialog', { name: 'Corrigir despesa' })

    expect(within(dialogo).getByLabelText('Data')).toHaveValue('2026-11-08')
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D02 — Caminhão 2')
    await user.selectOptions(within(dialogo).getByLabelText('Motorista (opcional)'), 'Carla Souza')
    await user.click(within(dialogo).getByRole('button', { name: 'Salvar alterações' }))

    await waitFor(() => expect(atualizar).toHaveLength(1))
    expect(atualizar[0].url.pathname).toMatch(/\/despesa\/301$/)
    expect(atualizar[0].corpo).toEqual({
      veiculoId: 2,
      tipoDespesaId: 1,
      motoristaId: 11,
      valor: 100,
      dataDespesa: '2026-11-08',
      observacao: 'Praça da BR-101',
    })
  })

  it.each<[Role, boolean]>([
    ['Admin', true],
    ['Supervisor', true],
    ['Operador', false],
  ])('%s: excluir visível = %s (lançar e corrigir é de toda a gestão)', async (papel, excluir) => {
    montar(papel)
    await screen.findByRole('row', { name: /Pedágio/ })
    expect(screen.getByRole('button', { name: 'Nova despesa' })).toBeInTheDocument()
    expect(screen.getAllByRole('button', { name: /^Editar a despesa/ })).toHaveLength(2)
    expect(screen.queryAllByRole('button', { name: /^Excluir a despesa/ })).toHaveLength(excluir ? 2 : 0)
  })

  it('exclui depois de confirmar', async () => {
    const excluir = capturar('delete', '/despesa/:id')
    const { user } = montar()
    await user.click(await screen.findByRole('button', { name: /^Excluir a despesa de R\$\s50,00$/ }))
    const confirmacao = screen.getByRole('alertdialog', { name: 'Excluir despesa' })
    expect(confirmacao).toHaveTextContent(/A despesa de R\$\s50,00 \(IPVA\) do veículo ABC1D02 será removida/)
    await user.click(within(confirmacao).getByRole('button', { name: 'Excluir' }))

    await waitFor(() => expect(excluir).toHaveLength(1))
    expect(excluir[0].url.pathname).toMatch(/\/despesa\/302$/)
    await waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument())
  })

  it('um 422 na exclusão aparece literal', async () => {
    const mensagem = 'Não é possível excluir despesa de período já fechado.'
    capturar('delete', '/despesa/:id', responderErro(422, [mensagem]))
    const { user } = montar()
    await user.click(await screen.findByRole('button', { name: /^Excluir a despesa de R\$\s100,00$/ }))
    const confirmacao = screen.getByRole('alertdialog')
    await user.click(within(confirmacao).getByRole('button', { name: 'Excluir' }))
    expect(await within(confirmacao).findByText(mensagem)).toBeInTheDocument()
  })
})
