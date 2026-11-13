import { screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Role } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { fabricarManutencao, paginado } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { ManutencoesPage } from './ManutencoesPage'

const ATRASADA = fabricarManutencao({
  id: 1,
  tipoManutencaoNome: 'Troca de óleo',
  veiculoPlaca: 'ABC1D01',
  status: 'Pendente',
  atrasada: true,
  kmRestantes: -1320,
})
const VENCENDO = fabricarManutencao({
  id: 2,
  tipoManutencaoNome: 'Revisão de freios',
  veiculoPlaca: 'ABC1D01',
  status: 'Pendente',
  atrasada: false,
  kmRestantes: 500,
})
const PENDENTE = fabricarManutencao({
  id: 3,
  tipoManutencaoNome: 'Alinhamento',
  veiculoPlaca: 'ABC1D02',
  veiculoNome: 'Caminhão 2',
  quilometragemAtualVeiculo: 120_000,
  status: 'Pendente',
  atrasada: false,
  kmRestantes: 2000,
})
const CONCLUIDA = fabricarManutencao({
  id: 4,
  tipoManutencaoNome: 'Troca de pneus',
  veiculoPlaca: 'ABC1D02',
  status: 'Realizada',
  atrasada: false,
  kmRestantes: null,
  quilometragemRealizada: 118_000,
  dataRealizacao: '2026-10-20T00:00:00',
  custo: 3200,
})

function montar(papel: Role = 'Admin') {
  const lista = capturar('get', '/manutencao', responderJson(paginado([ATRASADA, VENCENDO, PENDENTE, CONCLUIDA])))
  const tela = renderizar(<ManutencoesPage />, { rota: '/manutencoes', papel })
  return { ...tela, lista }
}

const linha = (tipo: string) => screen.findByRole('row', { name: new RegExp(tipo) })

afterEach(() => vi.useRealTimers())

describe('ManutencoesPage — situação', () => {
  it.each([
    ['Troca de óleo', 'Atrasada', '1.320 km em atraso'],
    ['Revisão de freios', 'Vencendo', 'faltam 500 km'],
    ['Alinhamento', 'Pendente', 'faltam 2.000 km'],
    ['Troca de pneus', 'Concluída', '20/10/2026 · 118.000 km'],
  ])('%s aparece como %s, com o andamento "%s"', async (tipo, situacao, andamento) => {
    montar()
    const celulas = within(await linha(tipo)).getAllByRole('cell')
    expect(celulas[3]).toHaveTextContent(situacao)
    expect(celulas[4]).toHaveTextContent(andamento)
  })

  it('atrasada prevalece mesmo com km sobrando (vencida pelo prazo)', async () => {
    capturar(
      'get',
      '/manutencao',
      responderJson(paginado([fabricarManutencao({ tipoManutencaoNome: 'Lubrificação', atrasada: true, kmRestantes: 300 })])),
    )
    renderizar(<ManutencoesPage />, { papel: 'Admin' })
    expect(within(await linha('Lubrificação')).getAllByRole('cell')[3]).toHaveTextContent('Atrasada')
  })

  it('linha concluída é histórico: sem concluir e sem editar', async () => {
    montar()
    const concluida = await linha('Troca de pneus')
    expect(within(concluida).queryByRole('button', { name: 'Concluir' })).not.toBeInTheDocument()
    expect(within(concluida).queryByRole('button', { name: /^Editar/ })).not.toBeInTheDocument()
    expect(within(concluida).getByRole('button', { name: /^Excluir/ })).toBeInTheDocument()
  })

  it('a gestão vê o custo', async () => {
    montar('Supervisor')
    expect(await screen.findByRole('columnheader', { name: 'Custo' })).toBeInTheDocument()
    expect(within(await linha('Troca de pneus')).getByText(/R\$\s3\.200,00/)).toBeInTheDocument()
  })

  it('o motorista lê a lista sem a coluna de custo e sem nenhuma ação', async () => {
    montar('Motorista')
    await linha('Troca de óleo')
    expect(screen.queryByRole('columnheader', { name: 'Custo' })).not.toBeInTheDocument()
    expect(screen.queryByRole('columnheader', { name: 'Ações' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Nova manutenção' })).not.toBeInTheDocument()
  })
})

describe('ManutencoesPage — excluir só para quem pode.excluir', () => {
  it.each<[Role, boolean]>([
    ['Admin', true],
    ['Supervisor', true],
    ['Operador', false],
    ['Motorista', false],
  ])('%s: botão excluir visível = %s', async (papel, visivel) => {
    montar(papel)
    await linha('Troca de óleo')
    expect(screen.queryAllByRole('button', { name: /^Excluir a manutenção/ })).toHaveLength(visivel ? 4 : 0)
  })

  it('exclui depois de confirmar', async () => {
    const excluir = capturar('delete', '/manutencao/:id')
    const { user, lista } = montar()
    await user.click(within(await linha('Alinhamento')).getByRole('button', { name: /^Excluir/ }))
    await user.click(within(screen.getByRole('alertdialog', { name: 'Excluir manutenção' })).getByRole('button', { name: 'Excluir' }))

    await waitFor(() => expect(excluir).toHaveLength(1))
    expect(excluir[0].url.pathname).toMatch(/\/manutencao\/3$/)
    await waitFor(() => expect(lista.length).toBeGreaterThan(1))
  })

  it('um 422 na exclusão aparece literal', async () => {
    const mensagem = 'Manutenção concluída com custo não pode ser excluída.'
    capturar('delete', '/manutencao/:id', responderErro(422, [mensagem]))
    const { user } = montar()
    await user.click(within(await linha('Troca de pneus')).getByRole('button', { name: /^Excluir/ }))
    const confirmacao = screen.getByRole('alertdialog')
    await user.click(within(confirmacao).getByRole('button', { name: 'Excluir' }))
    expect(await within(confirmacao).findByText(mensagem)).toBeInTheDocument()
  })
})

describe('ManutencoesPage — concluir', () => {
  async function abrirConclusao(user: UserEvent) {
    await user.click(within(await linha('Alinhamento')).getByRole('button', { name: 'Concluir' }))
    return screen.getByRole('dialog', { name: 'Concluir manutenção' })
  }

  it('abre com o km atual do veículo e a data de hoje', async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 12, 10, 0))
    const { user } = montar()
    const dialogo = await abrirConclusao(user)

    expect(within(dialogo).getByLabelText('Quilometragem realizada')).toHaveValue(120_000)
    expect(within(dialogo).getByLabelText('Data da realização')).toHaveValue('2026-11-12')
  })

  it('envia km, data, custo e observação, e recarrega a lista', async () => {
    const concluir = capturar('post', '/manutencao/:id/concluir', responderJson(CONCLUIDA))
    const { user, lista } = montar()
    const dialogo = await abrirConclusao(user)

    await user.clear(within(dialogo).getByLabelText('Quilometragem realizada'))
    await user.type(within(dialogo).getByLabelText('Quilometragem realizada'), '120350')
    await user.type(within(dialogo).getByLabelText('Custo (opcional)'), '450.5')
    await user.type(within(dialogo).getByLabelText('Observação (opcional)'), '  trocado filtro junto  ')
    await user.click(within(dialogo).getByRole('button', { name: 'Concluir' }))

    await waitFor(() => expect(concluir).toHaveLength(1))
    expect(concluir[0].url.pathname).toMatch(/\/manutencao\/3\/concluir$/)
    expect(concluir[0].corpo).toMatchObject({
      quilometragemRealizada: 120_350,
      custo: 450.5,
      observacao: 'trocado filtro junto',
    })
    expect(concluir[0].corpo?.dataRealizacao).toMatch(/^\d{4}-\d{2}-\d{2}$/)
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    await waitFor(() => expect(lista.length).toBeGreaterThan(1))
  })

  it('custo em branco vai como null — não é custo zero', async () => {
    const concluir = capturar('post', '/manutencao/:id/concluir', responderJson(CONCLUIDA))
    const { user } = montar()
    const dialogo = await abrirConclusao(user)
    await user.click(within(dialogo).getByRole('button', { name: 'Concluir' }))

    await waitFor(() => expect(concluir).toHaveLength(1))
    expect(concluir[0].corpo).toMatchObject({ custo: null, observacao: null })
  })

  it('um 422 na conclusão aparece literal e mantém o diálogo aberto', async () => {
    const mensagem = 'A quilometragem realizada não pode ser menor que a do último registro do veículo.'
    capturar('post', '/manutencao/:id/concluir', responderErro(422, [mensagem]))
    const { user } = montar()
    const dialogo = await abrirConclusao(user)
    await user.click(within(dialogo).getByRole('button', { name: 'Concluir' }))

    expect(await within(dialogo).findByText(mensagem)).toBeInTheDocument()
  })

  it('cancelar fecha sem enviar nada', async () => {
    const { user } = montar()
    const dialogo = await abrirConclusao(user)
    await user.click(within(dialogo).getByRole('button', { name: 'Cancelar' }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})

describe('ManutencoesPage — agendar e editar', () => {
  async function abrirNova(user: UserEvent) {
    await user.click(await screen.findByRole('button', { name: 'Nova manutenção' }))
    return screen.getByRole('dialog', { name: 'Nova manutenção' })
  }

  it('sugere o km previsto como km atual do veículo + intervalo do tipo', async () => {
    const { user } = montar()
    const dialogo = await abrirNova(user)
    const km = within(dialogo).getByLabelText('Quilometragem prevista')

    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1 (50.000 km)')
    await user.selectOptions(within(dialogo).getByLabelText('Tipo'), 'Troca de óleo (a cada 10.000 km)')
    expect(km).toHaveValue(60_000)

    await user.selectOptions(within(dialogo).getByLabelText('Tipo'), 'Revisão de freios (a cada 30.000 km)')
    expect(km).toHaveValue(80_000)
  })

  it('a sugestão nunca sobrescreve um km digitado à mão', async () => {
    const { user } = montar()
    const dialogo = await abrirNova(user)
    const km = within(dialogo).getByLabelText('Quilometragem prevista')

    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1 (50.000 km)')
    await user.selectOptions(within(dialogo).getByLabelText('Tipo'), 'Troca de óleo (a cada 10.000 km)')
    await user.clear(km)
    await user.type(km, '55000')
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D02 — Caminhão 2 (120.000 km)')

    expect(km).toHaveValue(55_000)
  })

  it('agenda com o corpo esperado', async () => {
    const criar = capturar('post', '/manutencao', responderJson(PENDENTE))
    const { user } = montar()
    const dialogo = await abrirNova(user)

    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D02 — Caminhão 2 (120.000 km)')
    await user.selectOptions(within(dialogo).getByLabelText('Tipo'), 'Revisão de freios (a cada 30.000 km)')
    await user.type(within(dialogo).getByLabelText('Observação (opcional)'), 'Levar pastilhas')
    await user.click(within(dialogo).getByRole('button', { name: 'Agendar' }))

    await waitFor(() => expect(criar).toHaveLength(1))
    expect(criar[0].corpo).toEqual({
      veiculoId: 2,
      tipoManutencaoId: 2,
      quilometragemPrevista: 150_000,
      dataPrevista: null,
      observacao: 'Levar pastilhas',
    })
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
  })

  it('um 422 no agendamento aparece literal', async () => {
    const mensagem = 'O tipo de manutenção informado está inativo.'
    capturar('post', '/manutencao', responderErro(422, [mensagem]))
    const { user } = montar()
    const dialogo = await abrirNova(user)
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1 (50.000 km)')
    await user.selectOptions(within(dialogo).getByLabelText('Tipo'), 'Troca de óleo (a cada 10.000 km)')
    await user.click(within(dialogo).getByRole('button', { name: 'Agendar' }))

    expect(await within(dialogo).findByText(mensagem)).toBeInTheDocument()
  })

  it('editar replaneja a pendência pelo PUT', async () => {
    const atualizar = capturar('put', '/manutencao/:id', responderJson(VENCENDO))
    const { user } = montar()
    await user.click(within(await linha('Revisão de freios')).getByRole('button', { name: /^Editar/ }))
    const dialogo = screen.getByRole('dialog', { name: 'Editar manutenção' })

    const km = within(dialogo).getByLabelText('Quilometragem prevista')
    expect(km).toHaveValue(60_000)
    await user.clear(km)
    await user.type(km, '61000')
    await user.click(within(dialogo).getByRole('button', { name: 'Salvar alterações' }))

    await waitFor(() => expect(atualizar).toHaveLength(1))
    expect(atualizar[0].url.pathname).toMatch(/\/manutencao\/2$/)
    expect(atualizar[0].corpo).toMatchObject({ veiculoId: 1, tipoManutencaoId: 1, quilometragemPrevista: 61_000 })
  })

  it('sem tipo ativo, a tela explica e não deixa agendar', async () => {
    capturar('get', '/tipomanutencao', responderJson([]))
    montar('Admin')
    expect(await screen.findByText('Nenhum tipo de manutenção ativo.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Cadastre o primeiro tipo' })).toHaveAttribute('href', '/tipos-manutencao')
    expect(screen.getByRole('button', { name: 'Nova manutenção' })).toBeDisabled()
  })

  it('sem tipo ativo, quem não mantém o catálogo é orientado a pedir', async () => {
    capturar('get', '/tipomanutencao', responderJson([]))
    montar('Operador')
    expect(await screen.findByText(/Peça a um administrador ou supervisor/)).toBeInTheDocument()
  })
})

describe('ManutencoesPage — filtros', () => {
  it('filtrar por situação pede o status ao servidor e volta para a página 1', async () => {
    const { user, lista } = montar()
    await linha('Troca de óleo')
    await user.selectOptions(screen.getByLabelText('Situação'), 'Concluídas')
    await waitFor(() => expect(lista.at(-1)?.params).toMatchObject({ status: 'Realizada', pagina: '1' }))
  })

  it('com período ativo, avisa qual data cada situação considera, e "Limpar filtros" desfaz tudo', async () => {
    const { user, lista } = montar()
    await linha('Troca de óleo')

    await user.selectOptions(screen.getByLabelText('Período'), 'Este mês')
    expect(screen.getByText(/considera a/)).toBeInTheDocument()
    await waitFor(() => expect(lista.at(-1)?.params.de).toBeDefined())

    await user.click(screen.getByRole('button', { name: 'Limpar filtros' }))
    await waitFor(() => expect(lista.at(-1)?.params.de).toBeUndefined())
    expect(screen.queryByText(/considera a/)).not.toBeInTheDocument()
  })

  it('filtrar por veículo manda o veiculoId', async () => {
    const { user, lista } = montar()
    await linha('Troca de óleo')
    await user.selectOptions(screen.getByLabelText('Filtrar por veículo'), 'ABC1D02 — Caminhão 2')
    await waitFor(() => expect(lista.at(-1)?.params).toMatchObject({ veiculoId: '2' }))
  })
})
