import { act, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/http'
import { filtroDePeriodo, type FiltroDePeriodo } from '../lib/periodo'
import {
  ConfirmDialog,
  FiltroPeriodo,
  FormDialog,
  PainelDialog,
  Paginacao,
  RowActions,
  SecaoCampos,
  TableStates,
} from './Table'

const tabela = (corpo: React.ReactNode) =>
  render(
    <table>
      <tbody>{corpo}</tbody>
    </table>,
  )

afterEach(() => vi.useRealTimers())

describe('TableStates', () => {
  const textos = { textoCarregando: 'Carregando…', textoErro: 'Falhou.', textoVazio: 'Nada aqui.' }

  it('carregando vem antes de tudo', () => {
    tabela(<TableStates colSpan={3} pending error={new Error('x')} empty {...textos} />)
    expect(screen.getByRole('cell')).toHaveTextContent('Carregando…')
    expect(screen.getByRole('cell')).toHaveAttribute('colspan', '3')
  })

  it('erro mostra a mensagem da API, ou o texto da tela quando não há', () => {
    const { unmount } = tabela(<TableStates colSpan={2} pending={false} error={new ApiError('x', ['Sem permissão.'])} empty={false} {...textos} />)
    expect(screen.getByRole('cell')).toHaveTextContent('Sem permissão.')
    unmount()
    tabela(<TableStates colSpan={2} pending={false} error={new Error('x')} empty={false} {...textos} />)
    expect(screen.getByRole('cell')).toHaveTextContent('Falhou.')
  })

  it('vazio mostra o texto de vazio; com dados, não renderiza nada', () => {
    const { unmount } = tabela(<TableStates colSpan={2} pending={false} error={null} empty {...textos} />)
    expect(screen.getByRole('cell')).toHaveTextContent('Nada aqui.')
    unmount()
    tabela(<TableStates colSpan={2} pending={false} error={null} empty={false} {...textos} />)
    expect(screen.queryByRole('cell')).not.toBeInTheDocument()
  })
})

describe('RowActions', () => {
  it('cada ação só aparece com o handler, e o rótulo diz o alvo', async () => {
    const onEditar = vi.fn()
    const onExcluir = vi.fn()
    render(<RowActions descricao="o veículo ABC1D01" onEditar={onEditar} onExcluir={onExcluir} />)

    await userEvent.click(screen.getByRole('button', { name: 'Editar o veículo ABC1D01' }))
    await userEvent.click(screen.getByRole('button', { name: 'Excluir o veículo ABC1D01' }))

    expect(onEditar).toHaveBeenCalledOnce()
    expect(onExcluir).toHaveBeenCalledOnce()
  })

  it('sem permissão de excluir, a lixeira some', () => {
    render(<RowActions descricao="o posto X" onEditar={vi.fn()} />)
    expect(screen.getByRole('button', { name: 'Editar o posto X' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Excluir/ })).not.toBeInTheDocument()
  })

  it('sem nenhuma permissão, nenhum botão', () => {
    render(<RowActions descricao="o posto X" />)
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })
})

describe('ConfirmDialog', () => {
  function confirmar(props: Partial<React.ComponentProps<typeof ConfirmDialog>> = {}) {
    const onConfirmar = vi.fn()
    const onCancelar = vi.fn()
    render(
      <ConfirmDialog
        titulo="Excluir veículo"
        mensagem="ABC1D01 será removido."
        pending={false}
        erros={[]}
        onConfirmar={onConfirmar}
        onCancelar={onCancelar}
        {...props}
      />,
    )
    return { onConfirmar, onCancelar, dialogo: screen.getByRole('alertdialog', { name: 'Excluir veículo' }) }
  }

  // O foco inicial em "Cancelar" vem dos passos de foco do `showModal()` nativo, que o jsdom
  // não tem — é verificação do Playwright, não daqui.
  it('abre já aberto, com título e mensagem', () => {
    const { dialogo } = confirmar()
    expect(dialogo).toHaveAttribute('open')
    expect(dialogo).toHaveTextContent('ABC1D01 será removido.')
  })

  it('confirmar e cancelar chamam cada um o seu', async () => {
    const { onConfirmar, onCancelar, dialogo } = confirmar()
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Excluir' }))
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Cancelar' }))
    expect(onConfirmar).toHaveBeenCalledOnce()
    expect(onCancelar).toHaveBeenCalledOnce()
  })

  it('fechar o diálogo pelo navegador (Escape) avisa quem o abriu', () => {
    const { onCancelar, dialogo } = confirmar()
    act(() => (dialogo as HTMLDialogElement).close())
    expect(onCancelar).toHaveBeenCalledOnce()
  })

  it('pendente: texto de andamento e os dois botões travados', () => {
    const { dialogo } = confirmar({ pending: true })
    expect(within(dialogo).getByRole('button', { name: 'Excluindo…' })).toBeDisabled()
    expect(within(dialogo).getByRole('button', { name: 'Cancelar' })).toBeDisabled()
  })

  it('mostra os erros dentro do diálogo e aceita textos próprios', () => {
    const { dialogo } = confirmar({ erros: ['Veículo com rotas.'], textoConfirmar: 'Alterar para Admin', variante: 'padrao' })
    expect(within(dialogo).getByText('Veículo com rotas.')).toBeInTheDocument()
    expect(within(dialogo).getByRole('button', { name: 'Alterar para Admin' })).toBeInTheDocument()
  })
})

describe('FormDialog e SecaoCampos', () => {
  it('envia pelo submit, com descrição, seções e erros', async () => {
    const onSubmit = vi.fn((e: React.FormEvent) => e.preventDefault())
    const onCancelar = vi.fn()
    render(
      <FormDialog
        titulo="Novo posto"
        descricao="Preencha o posto."
        textoConfirmar="Cadastrar"
        textoPendente="Salvando…"
        pending={false}
        erros={['Nome duplicado.']}
        onSubmit={onSubmit}
        onCancelar={onCancelar}
      >
        <SecaoCampos titulo="Identificação">
          <label htmlFor="nome">Nome</label>
          <input id="nome" />
        </SecaoCampos>
      </FormDialog>,
    )
    const dialogo = screen.getByRole('dialog', { name: 'Novo posto' })
    expect(within(dialogo).getByRole('heading', { name: 'Identificação' })).toBeInTheDocument()
    expect(within(dialogo).getByText('Preencha o posto.')).toBeInTheDocument()
    expect(within(dialogo).getByText('Nome duplicado.')).toBeInTheDocument()

    await userEvent.click(within(dialogo).getByRole('button', { name: 'Cadastrar' }))
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Cancelar' }))
    expect(onSubmit).toHaveBeenCalledOnce()
    expect(onCancelar).toHaveBeenCalledOnce()
  })

  it('pendente trava os dois botões', () => {
    render(
      <FormDialog titulo="Novo posto" textoConfirmar="Cadastrar" textoPendente="Salvando…" pending erros={[]} onSubmit={vi.fn()} onCancelar={vi.fn()}>
        <SecaoCampos>
          <span>campo</span>
        </SecaoCampos>
      </FormDialog>,
    )
    expect(screen.getByRole('button', { name: 'Salvando…' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancelar' })).toBeDisabled()
    expect(screen.queryByRole('heading', { level: 4 })).not.toBeInTheDocument()
  })
})

describe('PainelDialog', () => {
  it('só mostra conteúdo, e "Fechar" é a única ação', async () => {
    const onFechar = vi.fn()
    render(
      <PainelDialog titulo="Lançamentos — ABC1D01" descricao="No recorte atual." onFechar={onFechar}>
        <p>conteúdo</p>
      </PainelDialog>,
    )
    const dialogo = screen.getByRole('dialog', { name: 'Lançamentos — ABC1D01' })
    expect(within(dialogo).getAllByRole('button')).toHaveLength(1)
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Fechar' }))
    expect(onFechar).toHaveBeenCalledOnce()
  })
})

describe('Paginacao', () => {
  function paginacao(props: Partial<React.ComponentProps<typeof Paginacao>> = {}) {
    const onMudar = vi.fn()
    const onMudarTamanho = vi.fn()
    render(<Paginacao pagina={2} totalPaginas={3} total={37} tamanhoPagina={15} onMudar={onMudar} onMudarTamanho={onMudarTamanho} {...props} />)
    return { onMudar, onMudarTamanho }
  }

  it('some quando o total cabe na menor opção — não quando há uma página só', () => {
    const { unmount } = render(<Paginacao pagina={1} totalPaginas={1} total={10} tamanhoPagina={15} onMudar={vi.fn()} />)
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
    unmount()
    render(<Paginacao pagina={1} totalPaginas={1} total={12} tamanhoPagina={15} onMudar={vi.fn()} onMudarTamanho={vi.fn()} />)
    expect(screen.getByRole('combobox', { name: 'Itens por página' })).toBeInTheDocument()
  })

  it('mostra o intervalo exibido e a página', () => {
    paginacao()
    expect(screen.getByText('16–30 de 37')).toBeInTheDocument()
    expect(screen.getByText('2 / 3')).toBeInTheDocument()
  })

  it('anterior e próxima pedem a página vizinha', async () => {
    const { onMudar } = paginacao()
    await userEvent.click(screen.getByRole('button', { name: 'Anterior' }))
    await userEvent.click(screen.getByRole('button', { name: 'Próxima' }))
    expect(onMudar.mock.calls).toEqual([[1], [3]])
  })

  it('na primeira não volta, na última não avança, e a última mostra só o que sobrou', () => {
    const { unmount } = render(<Paginacao pagina={1} totalPaginas={3} total={37} tamanhoPagina={15} onMudar={vi.fn()} />)
    expect(screen.getByRole('button', { name: 'Anterior' })).toBeDisabled()
    unmount()
    paginacao({ pagina: 3 })
    expect(screen.getByRole('button', { name: 'Próxima' })).toBeDisabled()
    expect(screen.getByText('31–37 de 37')).toBeInTheDocument()
  })

  it('trocar o tamanho avisa o novo tamanho', async () => {
    const { onMudarTamanho } = paginacao()
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Itens por página' }), '20')
    expect(onMudarTamanho).toHaveBeenCalledWith(20)
  })

  it('sem onMudarTamanho, sem seletor; carregando, sem navegação', () => {
    paginacao({ onMudarTamanho: undefined, pending: true })
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Anterior' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Próxima' })).toBeDisabled()
  })
})

describe('FiltroPeriodo', () => {
  function Filtro({ inicial, onMudar }: { inicial: FiltroDePeriodo; onMudar: (f: FiltroDePeriodo) => void }) {
    const [valor, setValor] = useState(inicial)
    return (
      <FiltroPeriodo
        valor={valor}
        onMudar={(f) => {
          setValor(f)
          onMudar(f)
        }}
      />
    )
  }

  it('períodos prontos sobem direto', async () => {
    const onMudar = vi.fn()
    render(<Filtro inicial={filtroDePeriodo('todos')} onMudar={onMudar} />)
    await userEvent.selectOptions(screen.getByLabelText('Período'), 'Últimos 7 dias')
    expect(onMudar).toHaveBeenCalledWith({ periodo: '7dias', de: '', ate: '' })
    expect(screen.queryByLabelText('De')).not.toBeInTheDocument()
  })

  it('"Personalizado…" abre De e Até já com o intervalo que estava na tela', async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 12, 10, 0))
    const onMudar = vi.fn()
    render(<Filtro inicial={filtroDePeriodo('esteMes')} onMudar={onMudar} />)

    await userEvent.selectOptions(screen.getByLabelText('Período'), 'Personalizado…')

    expect(screen.getByLabelText('De')).toHaveValue('2026-11-01')
    expect(screen.getByLabelText('Até')).toHaveValue('2026-11-30')
    expect(onMudar).toHaveBeenLastCalledWith({ periodo: 'personalizado', de: '2026-11-01', ate: '2026-11-30' })
  })

  it('intervalo invertido avisa e não vira consulta; corrigido, sobe', async () => {
    const onMudar = vi.fn()
    render(<Filtro inicial={{ periodo: 'personalizado', de: '2026-11-10', ate: '2026-11-20' }} onMudar={onMudar} />)
    const ate = screen.getByLabelText('Até')

    await userEvent.clear(ate)
    await userEvent.type(ate, '2026-11-05')

    expect(screen.getByRole('alert')).toHaveTextContent('A data final não pode ser anterior à inicial.')
    expect(ate).toHaveAttribute('aria-invalid', 'true')
    expect(onMudar).not.toHaveBeenCalledWith(expect.objectContaining({ ate: '2026-11-05' }))

    await userEvent.clear(screen.getByLabelText('De'))
    await userEvent.type(screen.getByLabelText('De'), '2026-11-01')

    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    expect(onMudar).toHaveBeenLastCalledWith({ periodo: 'personalizado', de: '2026-11-01', ate: '2026-11-05' })
  })
})
