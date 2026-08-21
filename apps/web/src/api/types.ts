// Tipos espelhando os contratos da API Frota360.
// Quando a API estiver rodando, é possível gerar tipos direto do OpenAPI:
//   npm run gen:api  →  src/api/schema.d.ts

/** Envelope padrão de TODA resposta da API (camelCase). */
export interface ApiResponse<T> {
  sucesso: boolean
  mensagem: string
  dados: T | null
  erros: string[] | null
}

// ---------- Auth ----------

export interface RegisterRequest {
  nome: string
  email: string
  senha: string
}

export interface LoginRequest {
  email: string
  senha: string
}

export interface AuthResponse {
  token: string
  refreshToken: string
  nome: string
  email: string
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
