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

export type Role = 'Admin' | 'Supervisor' | 'Operador'

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

export interface MotoristaRequest {
  nome: string
  email: string
  cpf: string
  dataNascimento: string
}

export interface MotoristaResponse extends MotoristaRequest {
  id: number
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

export interface RotaRequest {
  origem: string
  destino: string
  codigoMotorista: number
  codigoVeiculo: number
  ativo: boolean
  dataInicio: string
  dataFim?: string | null
}

export interface RotaResponse extends RotaRequest {
  id: number
  dataInclusao: string
}
