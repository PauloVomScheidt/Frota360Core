import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  formatCnpj,
  formatConsumo,
  formatCpf,
  formatDate,
  formatDateTime,
  formatDistancia,
  formatDuracao,
  formatKm,
  formatLitros,
  formatMoeda,
  formatMoedaCompacta,
  hojeInputDate,
  iniciais,
  mascaraCpf,
  paraInputDate,
  somenteDigitos,
} from './format'

// O Intl separa "R$" do número com espaço não separável.
const texto = (valor: string) => valor.replace(/\s/g, ' ')

describe('datas vindas da API', () => {
  it('exibe a data como veio, sem converter fuso: meia-noite continua no mesmo dia', () => {
    expect(formatDate('2026-08-30T00:00:00')).toBe('30/08/2026')
  })

  it('exibe data e hora no padrão brasileiro', () => {
    expect(formatDateTime('2026-08-30T14:05:00')).toBe('30/08/2026, 14:05:00')
  })

  it.each([null, undefined, '', 'data-invalida'])('mostra traço para data ausente ou inválida (%s)', (valor) => {
    expect(formatDate(valor)).toBe('—')
    expect(formatDateTime(valor)).toBe('—')
  })
})

describe('paraInputDate', () => {
  it('converte para o formato aaaa-mm-dd aceito pelo input de data, com zero à esquerda', () => {
    expect(paraInputDate('2026-03-05T22:40:00')).toBe('2026-03-05')
  })

  it.each([null, undefined, '', 'xyz'])('devolve vazio para valor ausente ou inválido (%s)', (valor) => {
    expect(paraInputDate(valor)).toBe('')
  })
})

describe('hojeInputDate', () => {
  afterEach(() => vi.useRealTimers())

  it('usa o dia do calendário local, mesmo quando em UTC já é amanhã', () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 12, 23, 30))
    expect(hojeInputDate()).toBe('2026-11-12')
  })

  it('não pula para o ano seguinte na noite de 31 de dezembro', () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 11, 31, 23, 59))
    expect(hojeInputDate()).toBe('2026-12-31')
  })
})

describe('números de frota', () => {
  it('formata quilometragem com separador de milhar', () => {
    expect(formatKm(123456)).toBe('123.456 km')
  })

  it.each([
    [48.5, '48,5'],
    [48.1234, '48,123'],
    [50, '50'],
  ])('mostra litros com até 3 casas e sem zeros inúteis (%s → %s)', (litros, esperado) => {
    expect(formatLitros(litros)).toBe(esperado)
  })

  it.each([
    [8, '8,0 km/l'],
    [8.46, '8,5 km/l'],
  ])('mostra consumo sempre com uma casa (%s → %s)', (valor, esperado) => {
    expect(formatConsumo(valor)).toBe(esperado)
  })
})

describe('moeda', () => {
  it('formata em reais', () => {
    expect(texto(formatMoeda(1234.5))).toBe('R$ 1.234,50')
  })

  it('zero é valor, não ausência', () => {
    expect(texto(formatMoeda(0))).toBe('R$ 0,00')
  })

  it.each([null, undefined])('mostra traço quando o custo não foi informado (%s)', (valor) => {
    expect(formatMoeda(valor)).toBe('—')
  })

  it('abrevia valores grandes para caber no KPI', () => {
    expect(texto(formatMoedaCompacta(48200))).toBe('R$ 48,2 mil')
  })
})

describe('formatDistancia', () => {
  it.each([
    [8400, '8,4 km'],
    [412_000, '412 km'],
    [10_000, '10 km'],
  ])('usa uma casa abaixo de 10 km e nenhuma acima (%s m → %s)', (metros, esperado) => {
    expect(formatDistancia(metros)).toBe(esperado)
  })
})

describe('formatDuracao', () => {
  it.each([
    [48 * 60, '48 min'],
    [5 * 3600 + 12 * 60, '5 h 12 min'],
    [2 * 3600, '2 h'],
    [89, '1 min'],
    [3590, '1 h'],
  ])('mostra horas e minutos, sem segundos (%s s → %s)', (segundos, esperado) => {
    expect(formatDuracao(segundos)).toBe(esperado)
  })
})

describe('documentos', () => {
  it('extrai só os dígitos', () => {
    expect(somenteDigitos('123.456.789-01')).toBe('12345678901')
  })

  it.each([
    ['12345678901', '123.456.789-01'],
    ['123.456.789-01', '123.456.789-01'],
  ])('formata CPF com 11 dígitos (%s)', (cpf, esperado) => {
    expect(formatCpf(cpf)).toBe(esperado)
  })

  it('devolve o CPF como veio quando não tem 11 dígitos', () => {
    expect(formatCpf('1234')).toBe('1234')
  })

  it('formata CNPJ com 14 dígitos e devolve como veio caso contrário', () => {
    expect(formatCnpj('12345678000190')).toBe('12.345.678/0001-90')
    expect(formatCnpj('123')).toBe('123')
  })

  it.each([
    ['', ''],
    ['123', '123'],
    ['1234', '123.4'],
    ['123456', '123.456'],
    ['1234567', '123.456.7'],
    ['123456789', '123.456.789'],
    ['1234567890', '123.456.789-0'],
    ['12345678901', '123.456.789-01'],
    ['123456789012345', '123.456.789-01'],
    ['12a3.4', '123.4'],
  ])('aplica a máscara de CPF conforme a digitação ("%s" → "%s")', (digitado, esperado) => {
    expect(mascaraCpf(digitado)).toBe(esperado)
  })
})

describe('iniciais', () => {
  it.each([
    ['Maria da Silva', 'MS'],
    ['ana', 'A'],
    ['  joão   pereira  ', 'JP'],
    ['   ', '??'],
  ])('usa a primeira letra do primeiro e do último nome ("%s" → %s)', (nome, esperado) => {
    expect(iniciais(nome)).toBe(esperado)
  })
})
