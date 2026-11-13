import type {
  AbastecimentoResponse,
  ApiResponse,
  CustoPorMesResponse,
  CustoPorVeiculoResponse,
  DespesaResponse,
  LancamentoCustoResponse,
  ResumoCustosResponse,
  LogAuditoriaResponse,
  ManutencaoResponse,
  MotoristaResponse,
  PerfilResponse,
  PostoResponse,
  ResultadoPaginado,
  ResumoLancamentos,
  Role,
  RotaResponse,
  SessaoResponse,
  TipoCombustivelResponse,
  TipoDespesaResponse,
  TipoManutencaoResponse,
  UsuarioResponse,
  VeiculoResponse,
} from '../api/types'

const DATA_INCLUSAO = '2026-11-01T08:00:00'

let ultimoId = 0
const proximoId = () => ++ultimoId

export function envelope<T>(dados: T, mensagem = 'Operação realizada com sucesso.'): ApiResponse<T> {
  return { sucesso: true, mensagem, dados, erros: null }
}

export function envelopeDeErro(erros: string[], mensagem = 'Falha na operação.'): ApiResponse<null> {
  return { sucesso: false, mensagem, dados: null, erros }
}

/** `total` explícito simula uma página de uma lista maior — o servidor só devolve a fatia. */
export function paginado<T>(itens: T[], pagina = 1, tamanhoPagina = 15, total = itens.length): ResultadoPaginado<T> {
  return {
    itens,
    pagina,
    tamanhoPagina,
    total,
    totalPaginas: Math.max(1, Math.ceil(total / tamanhoPagina)),
  }
}

export function fabricarResumo(sobrescrita: Partial<ResumoLancamentos> = {}): ResumoLancamentos {
  return { quantidade: 0, valorTotal: 0, ...sobrescrita }
}

export function fabricarMotorista(sobrescrita: Partial<MotoristaResponse> = {}): MotoristaResponse {
  const id = sobrescrita.id ?? proximoId()
  return {
    id,
    nome: `Motorista ${id}`,
    email: `motorista${id}@frota360.teste`,
    cpf: null,
    dataNascimento: null,
    ativo: true,
    dataInclusao: DATA_INCLUSAO,
    ...sobrescrita,
  }
}

export function fabricarSessao(sobrescrita: Partial<SessaoResponse> = {}): SessaoResponse {
  const role: Role = sobrescrita.role ?? 'Admin'
  return {
    nome: `Usuário ${role}`,
    email: `${role.toLowerCase()}@frota360.teste`,
    role,
    nomeEmpresa: 'Transportes Teste Ltda',
    ...sobrescrita,
  }
}

export function fabricarUsuario(sobrescrita: Partial<UsuarioResponse> = {}): UsuarioResponse {
  const id = sobrescrita.id ?? proximoId()
  return {
    id,
    nome: `Usuário ${id}`,
    email: `usuario${id}@frota360.teste`,
    role: 'Operador',
    cpf: null,
    dataNascimento: null,
    ativo: true,
    dataInclusao: DATA_INCLUSAO,
    ...sobrescrita,
  }
}

export function fabricarPerfil(sobrescrita: Partial<PerfilResponse> = {}): PerfilResponse {
  return {
    ...fabricarUsuario({ nome: 'Ana Souza', email: 'ana@frota360.teste', role: 'Motorista' }),
    nomeEmpresa: 'Transportes Teste Ltda',
    cnpjEmpresa: '12345678000190',
    ...sobrescrita,
  }
}

export function fabricarVeiculo(sobrescrita: Partial<VeiculoResponse> = {}): VeiculoResponse {
  const id = sobrescrita.id ?? proximoId()
  return {
    id,
    nomeVeiculo: `Caminhão ${id}`,
    marcaVeiculo: 'Volvo',
    placa: `ABC1D${String(id).padStart(2, '0')}`,
    quilometragem: 50_000,
    ultimoMotorista: null,
    dataUltimaViagem: null,
    emRota: false,
    dataInclusao: DATA_INCLUSAO,
    ...sobrescrita,
  }
}

export function fabricarRota(sobrescrita: Partial<RotaResponse> = {}): RotaResponse {
  const id = sobrescrita.id ?? proximoId()
  return {
    id,
    enderecoPartida: 'Rua A, 100 — Joinville/SC',
    enderecoChegada: 'Rua B, 200 — Blumenau/SC',
    latitudePartida: null,
    longitudePartida: null,
    latitudeChegada: null,
    longitudeChegada: null,
    distanciaMetros: 0,
    duracaoEstimadaSegundos: 0,
    polylineCodificada: null,
    dataCalculoRota: null,
    codigoMotorista: 1,
    nomeMotorista: 'Motorista Teste',
    codigoVeiculo: 1,
    veiculoNome: 'Caminhão 1',
    veiculoPlaca: 'ABC1D01',
    dataInicio: '2026-11-10T07:00:00',
    dataFim: null,
    ativo: true,
    kmInicial: 50_000,
    kmFinal: null,
    kmPercorrido: null,
    dataInclusao: DATA_INCLUSAO,
    ...sobrescrita,
  }
}

export function fabricarManutencao(sobrescrita: Partial<ManutencaoResponse> = {}): ManutencaoResponse {
  const id = sobrescrita.id ?? proximoId()
  return {
    id,
    veiculoId: 1,
    veiculoNome: 'Caminhão 1',
    veiculoPlaca: 'ABC1D01',
    tipoManutencaoId: 1,
    tipoManutencaoNome: 'Troca de óleo',
    quilometragemPrevista: 60_000,
    dataPrevista: null,
    status: 'Pendente',
    observacao: null,
    quilometragemAtualVeiculo: 50_000,
    kmRestantes: 10_000,
    atrasada: false,
    quilometragemRealizada: null,
    dataRealizacao: null,
    custo: null,
    dataInclusao: DATA_INCLUSAO,
    ...sobrescrita,
  }
}

export function fabricarAbastecimento(
  sobrescrita: Partial<AbastecimentoResponse> = {},
): AbastecimentoResponse {
  const id = sobrescrita.id ?? proximoId()
  const litros = sobrescrita.litros ?? 100
  const valorLitro = sobrescrita.valorLitro ?? 6.5
  return {
    id,
    veiculoId: 1,
    veiculoNome: 'Caminhão 1',
    veiculoPlaca: 'ABC1D01',
    rotaId: null,
    rotaDescricao: null,
    motoristaId: 1,
    motoristaNome: 'Motorista Teste',
    usuarioId: 1,
    usuarioNome: 'Motorista Teste',
    tipoCombustivelId: 1,
    tipoCombustivelNome: 'Diesel S10',
    postoId: 1,
    postoNome: 'Posto Central',
    litros,
    valorLitro,
    valor: Math.round(litros * valorLitro * 100) / 100,
    odometro: 50_000,
    notaFiscal: `NF-${id}`,
    frentista: null,
    dataAbastecimento: '2026-11-10T09:00:00',
    observacao: null,
    dataInclusao: DATA_INCLUSAO,
    ...sobrescrita,
  }
}

export function fabricarDespesa(sobrescrita: Partial<DespesaResponse> = {}): DespesaResponse {
  const id = sobrescrita.id ?? proximoId()
  return {
    id,
    veiculoId: 1,
    veiculoNome: 'Caminhão 1',
    veiculoPlaca: 'ABC1D01',
    tipoDespesaId: 1,
    tipoDespesaNome: 'Pedágio',
    motoristaId: null,
    motoristaNome: null,
    valor: 25.4,
    dataDespesa: '2026-11-10T10:00:00',
    observacao: null,
    dataInclusao: DATA_INCLUSAO,
    ...sobrescrita,
  }
}

export function fabricarCustoPorVeiculo(sobrescrita: Partial<CustoPorVeiculoResponse> = {}): CustoPorVeiculoResponse {
  return {
    veiculoId: 1,
    veiculoNome: 'Caminhão 1',
    veiculoPlaca: 'ABC1D01',
    totalAbastecimento: 600,
    totalManutencao: 300,
    totalDespesa: 100,
    total: 1000,
    km: 2000,
    custoPorKm: 0.5,
    litros: 230,
    kmOdometro: 2100,
    consumoMedio: 9.1,
    ...sobrescrita,
  }
}

export function fabricarCustoPorMes(ano: number, mes: number, sobrescrita: Partial<CustoPorMesResponse> = {}): CustoPorMesResponse {
  const base = { totalAbastecimento: 600, totalManutencao: 300, totalDespesa: 100, ...sobrescrita }
  return { ano, mes, ...base, total: base.totalAbastecimento + base.totalManutencao + base.totalDespesa }
}

/** O `total` é a soma das três origens, como no servidor. */
export function fabricarResumoCustos(sobrescrita: Partial<ResumoCustosResponse> = {}): ResumoCustosResponse {
  const base = { totalAbastecimento: 600, totalManutencao: 300, totalDespesa: 100, ...sobrescrita }
  return {
    quantidadeLancamentos: 12,
    kmTotal: 2000,
    custoPorKm: 0.5,
    manutencoesSemCustoInformado: 0,
    litrosTotal: 230,
    kmOdometroTotal: 2100,
    consumoMedio: 9.1,
    porVeiculo: [fabricarCustoPorVeiculo()],
    porMes: [],
    ...base,
    total: sobrescrita.total ?? base.totalAbastecimento + base.totalManutencao + base.totalDespesa,
  }
}

export function fabricarLancamentoCusto(sobrescrita: Partial<LancamentoCustoResponse> = {}): LancamentoCustoResponse {
  return {
    origem: 'Abastecimento',
    origemId: proximoId(),
    data: '2026-11-10T09:00:00',
    veiculoId: 1,
    veiculoNome: 'Caminhão 1',
    veiculoPlaca: 'ABC1D01',
    motoristaId: 10,
    motoristaNome: 'João Pereira',
    categoria: 'Combustível',
    valor: 650,
    observacao: null,
    ...sobrescrita,
  }
}

export function fabricarLogAuditoria(sobrescrita: Partial<LogAuditoriaResponse> = {}): LogAuditoriaResponse {
  const id = sobrescrita.id ?? proximoId()
  return {
    id,
    usuarioId: 1,
    usuarioNome: 'Ana Souza',
    usuarioEmail: 'ana@frota360.teste',
    usuarioRole: 'Supervisor',
    entidade: 'Veiculo',
    acao: 'Criou',
    entidadeId: 10,
    descricao: `Registro ${id}`,
    alteracoes: [],
    dataHora: '2026-11-12T09:30:00',
    ipOrigem: null,
    ...sobrescrita,
  }
}

export function fabricarTipoCombustivel(
  sobrescrita: Partial<TipoCombustivelResponse> = {},
): TipoCombustivelResponse {
  const id = sobrescrita.id ?? proximoId()
  return { id, nome: `Combustível ${id}`, ativo: true, dataInclusao: DATA_INCLUSAO, ...sobrescrita }
}

export function fabricarPosto(sobrescrita: Partial<PostoResponse> = {}): PostoResponse {
  const id = sobrescrita.id ?? proximoId()
  return {
    id,
    nome: `Posto ${id}`,
    cnpj: null,
    cidade: 'Joinville',
    ativo: true,
    dataInclusao: DATA_INCLUSAO,
    ...sobrescrita,
  }
}

export function fabricarTipoManutencao(
  sobrescrita: Partial<TipoManutencaoResponse> = {},
): TipoManutencaoResponse {
  const id = sobrescrita.id ?? proximoId()
  return {
    id,
    nome: `Manutenção ${id}`,
    intervaloKm: 10_000,
    ativo: true,
    dataInclusao: DATA_INCLUSAO,
    ...sobrescrita,
  }
}

export function fabricarTipoDespesa(sobrescrita: Partial<TipoDespesaResponse> = {}): TipoDespesaResponse {
  const id = sobrescrita.id ?? proximoId()
  return { id, nome: `Despesa ${id}`, ativo: true, dataInclusao: DATA_INCLUSAO, ...sobrescrita }
}
