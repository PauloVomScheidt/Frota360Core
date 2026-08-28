// Tipos espelhando os contratos da API Frota360.
// Quando a API estiver rodando, é possível conferir/gerar tipos direto do OpenAPI:
//   npm run gen:api  →  src/api/schema.d.ts

/** Envelope padrão de TODA resposta da API (camelCase), inclusive 401/403/422/429. */
export interface ApiResponse<T> {
  sucesso: boolean
  mensagem: string
  dados: T | null
  erros: string[] | null
}

// ---------- Auth ----------

/**
 * `Motorista` funciona como as demais — convite, promoção e rebaixamento iguais.
 * O que muda é a visibilidade: ele enxerga só `/minhas-rotas`.
 */
export type Role = 'Admin' | 'Supervisor' | 'Operador' | 'Motorista'

export interface LoginRequest {
  email: string
  senha: string
}

export interface AuthResponse {
  token: string
  refreshToken: string
  nome: string
  email: string
  role: Role
}

export interface EsqueciSenhaRequest {
  email: string
}

export interface RedefinirSenhaRequest {
  token: string
  novaSenha: string
}

// ---------- Convites ----------

export interface CriarConviteRequest {
  email: string
  role: Role
}

export interface AceitarConviteRequest {
  token: string
  nome: string
  senha: string
  /** Opcionais. O CPF vai só com os 11 dígitos — a máscara é da interface. */
  cpf?: string
  dataNascimento?: string
}

export interface ConviteResponse {
  id: number
  email: string
  role: Role
  expiraEm: string
  utilizadoEm?: string | null
  dataInclusao: string
  /** Vem em claro apenas na resposta de criação, para encaminhamento manual. */
  linkConvite?: string | null
}

// ---------- Usuários ----------

export interface UsuarioResponse {
  id: number
  nome: string
  email: string
  role: Role
  /** Opcionais: informados pela própria pessoa no aceite do convite. */
  cpf?: string | null
  dataNascimento?: string | null
  ativo: boolean
  dataInclusao: string
}

export interface AlterarRoleRequest {
  role: Role
}

export interface AlterarAtivoRequest {
  ativo: boolean
}

// ---------- Motorista ----------

/**
 * Um motorista é um usuário com a role `Motorista` — não existe entidade própria.
 * O `id` é o do usuário, e é ele que a rota grava em `codigoMotorista`.
 *
 * Somente leitura: conceder e remover o acesso acontece em `/convites` e `/usuarios`.
 */
export interface MotoristaResponse {
  id: number
  nome: string
  email: string
  /** Opcionais: só existem se a pessoa os informou ao aceitar o convite. */
  cpf?: string | null
  dataNascimento?: string | null
  ativo: boolean
  dataInclusao: string
}

// ---------- Veículo ----------

export interface VeiculoRequest {
  nomeVeiculo: string
  marcaVeiculo: string
  placa: string
  quilometragem: number
  ultimoMotorista?: string | null
  dataUltimaViagem?: string | null
}

export interface VeiculoResponse extends VeiculoRequest {
  id: number
  dataInclusao: string
}

// ---------- Rota ----------

/**
 * Campos comuns ao POST e ao PUT. `ativo` e `dataFim` saíram dos requests: encerrar
 * virou transição de estado própria (`POST /rota/{id}/encerrar`), e deixá-los aqui
 * permitiria "encerrar" uma rota por edição, sem calcular km nem tocar no odômetro.
 */
export interface RotaRequest {
  origem: string
  destino: string
  codigoMotorista: number
  codigoVeiculo: number
  dataInicio: string
}

/**
 * `nomeMotorista` vem desnormalizado da API, como `veiculoNome` na manutenção — é o
 * que mantém a rota identificável depois que a pessoa muda de perfil e some da lista
 * de motoristas. Nulo só se o usuário tiver sido removido do banco à força.
 */

/**
 * O POST exige o hodômetro de abertura; o PUT não toca nele (a rota preserva o que
 * gravou na criação). Não pode ser menor que a quilometragem atual do veículo — e
 * quando é maior, a API avança o odômetro já na abertura.
 */
export interface CriarRotaRequest extends RotaRequest {
  kmInicial: number
}

/**
 * O mesmo POST, visto pela tela do motorista: sem `codigoMotorista`, porque a API
 * grava o id do próprio usuário logado e ignora o do corpo. Omitir deixa explícito
 * que a escolha não é do cliente — e o validador da API dispensa o campo nessa role.
 */
export type AbrirMinhaRotaRequest = Omit<CriarRotaRequest, 'codigoMotorista'>

/** `dataFim` opcional — a API assume "agora" quando omitida. */
export interface EncerrarRotaRequest {
  kmFinal: number
  dataFim?: string | null
}

export interface RotaResponse extends RotaRequest {
  id: number
  nomeMotorista?: string | null
  ativo: boolean
  dataFim?: string | null
  kmInicial: number
  /** Nulos enquanto a rota está ativa; preenchidos no encerramento. */
  kmFinal?: number | null
  /** Fato histórico persistido pela API (`kmFinal - kmInicial`), não recalcular aqui. */
  kmPercorrido?: number | null
  dataInclusao: string
}

// ---------- Tipo de manutenção ----------

/** Catálogo de tipos da empresa; alimenta o select da tela de manutenção. */
export interface TipoManutencaoRequest {
  nome: string
  /** Opcional; quando informado, > 0. Hoje é só informativo (a recorrência automática não existe). */
  intervaloKm?: number | null
}

/** O PUT aceita o mesmo shape do POST mais o `ativo` — inativar é o caminho quando o DELETE dá 422. */
export interface TipoManutencaoUpdateRequest extends TipoManutencaoRequest {
  ativo: boolean
}

export interface TipoManutencaoResponse {
  id: number
  nome: string
  intervaloKm?: number | null
  ativo: boolean
  dataInclusao: string
}

// ---------- Manutenção ----------

/** `Cancelada` existe no enum, mas nenhum endpoint a produz ainda (§7.3.3 do CONTEXTO). */
export type StatusManutencao = 'Pendente' | 'Realizada' | 'Cancelada'

export interface ManutencaoRequest {
  veiculoId: number
  tipoManutencaoId: number
  quilometragemPrevista: number
  /** No POST não pode estar no passado; no PUT a regra não vale (permite replanejar). */
  dataPrevista?: string | null
  observacao?: string | null
}

export interface ConcluirManutencaoRequest {
  quilometragemRealizada: number
  dataRealizacao: string
  custo?: number | null
  observacao?: string | null
}

export interface ManutencaoFiltro {
  veiculoId?: number
  status?: StatusManutencao
}

/**
 * Já vem desnormalizada (nome/placa do veículo e nome do tipo) — a listagem não
 * precisa de join no cliente.
 *
 * `atrasada` e `kmRestantes` são calculados pelo servidor a cada leitura, não são
 * colunas: mudam sozinhos conforme a quilometragem do veículo sobe. Nunca recalcular
 * no front — usar o que vier.
 */
export interface ManutencaoResponse {
  id: number
  veiculoId: number
  veiculoNome: string
  veiculoPlaca: string
  tipoManutencaoId: number
  tipoManutencaoNome: string
  quilometragemPrevista: number
  dataPrevista?: string | null
  status: StatusManutencao
  observacao?: string | null
  quilometragemAtualVeiculo: number
  /** `null` fora de "Pendente"; negativo quando já passou do ponto. */
  kmRestantes?: number | null
  atrasada: boolean
  quilometragemRealizada?: number | null
  dataRealizacao?: string | null
  /** Sempre `null` para a role `Motorista` — a API omite o financeiro para ele. */
  custo?: number | null
  dataInclusao: string
}
