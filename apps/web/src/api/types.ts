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

/**
 * Página de uma listagem, dentro de `dados` do envelope — o envelope em si não muda.
 * Hoje só `/auditoria` pagina; as demais listas ainda vêm inteiras.
 */
export interface ResultadoPaginado<T> {
  itens: T[]
  pagina: number
  tamanhoPagina: number
  /** Total que satisfaz o filtro, ignorando a paginação. */
  total: number
  totalPaginas: number
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

/** Token e refreshToken não vêm mais aqui — o servidor os manda em cookie HttpOnly. */
export interface SessaoResponse {
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

/**
 * Edição do próprio cadastro (`PUT /usuario/perfil`) — o direito de correção da LGPD.
 * Não carrega id: o alvo sai do token. E-mail e papel não são editáveis por aqui.
 */
export interface AtualizarPerfilRequest {
  nome: string
  /** Opcionais. O CPF vai só com os 11 dígitos; em branco, omita para gravar nulo. */
  cpf?: string
  dataNascimento?: string
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
  /**
   * Derivado na leitura pela API (como `atrasada` na manutenção): existe rota aberta com
   * este veículo. **Não entra no `VeiculoRequest`** — quem move o estado é a rota.
   */
  emRota: boolean
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
  /**
   * Placa e nome do veículo, desnormalizados pelo servidor como em `ManutencaoResponse` e
   * `AbastecimentoResponse`. Antes as telas de rota montavam um `Map` a partir de
   * `['veiculos']` para achar a placa — dependência que a paginação no servidor inviabilizou.
   */
  veiculoPlaca?: string | null
  veiculoNome?: string | null
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
  /** Paginação do servidor. Sem eles a API assume página 1 com 15 itens; teto de 100. */
  pagina?: number
  tamanhoPagina?: number
  veiculoId?: number
  status?: StatusManutencao
  /**
   * Período `yyyy-MM-dd`, aplicado sobre a **data relevante do status**: pendência é
   * situada pela `dataPrevista`, concluída pela `dataRealizacao`. `ate` é inclusivo.
   *
   * Pendência agendada só por km (sem `dataPrevista`) não aparece com período ativo —
   * ela não está em data nenhuma. Intervalo invertido volta 422.
   */
  de?: string
  ate?: string
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

// ---------- Auditoria (Admin) ----------

/** Vocabulário fechado no servidor: um valor fora daqui volta 400 do validator. */
export type EntidadeAuditada =
  | 'Veiculo'
  | 'Rota'
  | 'Manutencao'
  | 'Abastecimento'
  | 'TipoManutencao'
  | 'Despesa'
  | 'TipoDespesa'
  | 'TipoCombustivel'
  | 'Posto'
  | 'Usuario'
  | 'Convite'

export type AcaoAuditoria =
  | 'Criou'
  | 'Atualizou'
  | 'Excluiu'
  | 'Encerrou'
  | 'Concluiu'
  | 'AlterouPermissao'
  | 'Ativou'
  | 'Desativou'
  | 'Cancelou'
  | 'Aceitou'

/** Um campo que mudou. Valores já vêm como texto — o log é histórico legível. */
export interface AlteracaoCampo {
  campo: string
  de?: string | null
  para?: string | null
}

/**
 * Uma linha da trilha. Nome, e-mail e papel de quem agiu são **desnormalizados**:
 * refletem o que era verdade no momento da ação, não o estado atual do usuário —
 * não cruzar com `['usuarios']` para "corrigir".
 */
export interface LogAuditoriaResponse {
  id: number
  usuarioId: number
  usuarioNome: string
  usuarioEmail: string
  usuarioRole: Role
  entidade: EntidadeAuditada
  acao: AcaoAuditoria
  entidadeId?: number | null
  /** Frase pronta em pt-BR, vinda do servidor — não montar no cliente. */
  descricao: string
  /** Vazio em criação e exclusão, onde não há "antes e depois". */
  alteracoes: AlteracaoCampo[]
  dataHora: string
  ipOrigem?: string | null
}

export interface AuditoriaFiltro {
  pagina?: number
  /** Teto de 100 no servidor; acima disso volta 400. */
  tamanhoPagina?: number
  entidade?: EntidadeAuditada
  acao?: AcaoAuditoria
  usuarioId?: number
  /** `yyyy-MM-dd`; `ate` é inclusivo (o servidor estende até o fim do dia). */
  de?: string
  ate?: string
}

// ---------- Abastecimento ----------

/**
 * O apontamento fiscal do abastecimento. Dois campos **não** entram no corpo: `rotaId`, que
 * a API deriva da viagem aberta do motorista naquele veículo, e `valor`, que o servidor
 * recalcula como `litros × valorLitro` — a tela o exibe como readonly.
 */
export interface AbastecimentoRequest {
  veiculoId: number
  /**
   * De quem é o gasto. Obrigatório para a gestão. Para a role `Motorista` a API **ignora**
   * este campo e usa o usuário do token — ele não lança na conta de outro.
   */
  motoristaId?: number | null
  tipoCombustivelId: number
  /** Posto credenciado. Item inativo do catálogo volta 422. */
  postoId: number
  litros: number
  valorLitro: number
  /**
   * Quilometragem no momento do abastecimento. Avança a ficha do veículo quando é maior que
   * a atual — por isso a mutation invalida `['veiculos']` e `['manutencoes']` também.
   */
  odometro: number
  notaFiscal: string
  /** Opcional: em autoatendimento não há frentista. */
  frentista?: string | null
  dataAbastecimento: string
  observacao?: string | null
}

/**
 * Correção de um lançamento. Sem `veiculoId` e sem `motoristaId`: trocar qualquer um dos
 * dois reescreveria a atribuição do gasto — nesse caso, exclua e lance de novo.
 */
export type AtualizarAbastecimentoRequest = Omit<
  AbastecimentoRequest,
  'veiculoId' | 'motoristaId'
>

export interface AbastecimentoFiltro {
  /** Paginação do servidor. Sem eles a API assume página 1 com 15 itens; teto de 100. */
  pagina?: number
  tamanhoPagina?: number
  veiculoId?: number
  /** Só vale para a gestão; para a role `Motorista` o servidor sobrescreve com o do token. */
  motoristaId?: number
  /** `yyyy-MM-dd`; `ate` é inclusivo. Intervalo invertido volta 422. */
  de?: string
  ate?: string
}

/**
 * Já vem desnormalizado: veículo, rota, motorista (de quem é o gasto) e quem lançou. Os
 * dois últimos são pessoas diferentes quando a gestão lança em nome do motorista.
 */
export interface AbastecimentoResponse {
  id: number
  veiculoId: number
  veiculoNome: string
  veiculoPlaca: string
  rotaId?: number | null
  /** "Origem → Destino" da rota vinculada, quando há uma. */
  rotaDescricao?: string | null
  motoristaId: number
  motoristaNome: string
  usuarioId: number
  usuarioNome: string
  tipoCombustivelId: number
  tipoCombustivelNome: string
  postoId: number
  postoNome: string
  litros: number
  valorLitro: number
  /** `litros × valorLitro`, calculado no servidor. */
  valor: number
  odometro: number
  notaFiscal: string
  frentista?: string | null
  dataAbastecimento: string
  observacao?: string | null
  dataInclusao: string
}

// ---------- Custo ----------

/**
 * De onde saiu um custo. Não existe tabela de custos: o valor continua sendo lançado no
 * abastecimento e na conclusão da manutenção, e `/custo` só une as duas origens na leitura.
 *
 * Espelha o enum `OrigemCusto` do Domain — mexeu numa, mexa na outra. Custo avulso
 * (pedágio, multa, IPVA) entra aqui como uma origem nova, e nada mais neste arquivo muda.
 */
export type OrigemCusto = 'Abastecimento' | 'Manutencao' | 'Despesa'

/** Uma linha de custo, já normalizada entre as origens. */
export interface LancamentoCustoResponse {
  origem: OrigemCusto
  /** Id na tabela de origem — é por ele que se volta ao registro. */
  origemId: number
  data: string
  veiculoId: number
  veiculoNome: string
  veiculoPlaca: string
  /** Nulo em manutenção, que não é atribuída a motorista. */
  motoristaId?: number | null
  motoristaNome?: string | null
  /** "Combustível" no abastecimento; o nome do tipo na manutenção. */
  categoria: string
  valor: number
  observacao?: string | null
}

export interface CustoFiltro {
  pagina?: number
  /** Teto de 100 no servidor; acima disso volta 400. */
  tamanhoPagina?: number
  veiculoId?: number
  /**
   * Preenchido, o resultado sai **só com abastecimentos**: manutenção não é atribuída a
   * motorista. A tela avisa o usuário disso.
   */
  motoristaId?: number
  origem?: OrigemCusto
  /** `yyyy-MM-dd`; `ate` é inclusivo (o servidor estende até o fim do dia). */
  de?: string
  ate?: string
}

export interface CustoPorVeiculoResponse {
  veiculoId: number
  veiculoNome: string
  veiculoPlaca: string
  totalAbastecimento: number
  totalManutencao: number
  totalDespesa: number
  total: number
  /** Km das rotas encerradas no período. Zero quando nenhuma foi encerrada. */
  km: number
  /** Nulo quando `km` é zero — não há denominador. */
  custoPorKm?: number | null
  /** Litros do período, já sem os do primeiro abastecimento (eles pagaram o trecho anterior). */
  litros: number
  /**
   * Km medido pelo **odômetro dos abastecimentos** — não é o mesmo que `km`, que vem das
   * rotas encerradas. São duas medidas diferentes, e a tela diz qual é qual.
   */
  kmOdometro: number
  /** Nulo com menos de dois abastecimentos no período, ou se o odômetro não avançou. */
  consumoMedio?: number | null
}

export interface CustoPorMesResponse {
  ano: number
  /** 1 a 12. */
  mes: number
  totalAbastecimento: number
  totalManutencao: number
  totalDespesa: number
  total: number
}

/**
 * A primeira agregação servida pela API — os totais são somados no banco, não com `reduce`
 * no cliente como nos KPIs do dashboard.
 */
export interface ResumoCustosResponse {
  total: number
  totalAbastecimento: number
  totalManutencao: number
  /** Custos avulsos: pedágio, multa, IPVA, seguro. */
  totalDespesa: number
  quantidadeLancamentos: number
  kmTotal: number
  /**
   * Nulo quando não houve rota encerrada no período. Rota ainda aberta não tem km apurado,
   * então o mês corrente subestima o km e superestima o R$/km.
   */
  custoPorKm?: number | null
  /**
   * Manutenções concluídas no período sem custo informado. Ficam fora de toda soma — a tela
   * mostra a contagem para o total não mentir por omissão.
   */
  manutencoesSemCustoInformado: number
  /** Litros da frota no período, já descontado o primeiro abastecimento de cada veículo. */
  litrosTotal: number
  /** Km pelo odômetro dos abastecimentos. **Não** é `kmTotal`, que sai das rotas encerradas. */
  kmOdometroTotal: number
  /**
   * Consumo médio da frota em km/l. Soma km e litros e divide uma vez só — média das médias
   * faria um veículo com dois abastecimentos pesar igual a um com trinta.
   */
  consumoMedio?: number | null
  /** Do maior total para o menor. Inclui veículo que rodou sem custo lançado. */
  porVeiculo: CustoPorVeiculoResponse[]
  /** Em ordem cronológica; só os meses que tiveram lançamento. */
  porMes: CustoPorMesResponse[]
}

// ---------- Tipo de combustível ----------

/** Catálogo da empresa; alimenta o select da tela de abastecimentos. */
export interface TipoCombustivelRequest {
  nome: string
}

/** O PUT aceita o mesmo shape do POST mais o `ativo` — inativar é o caminho quando o DELETE dá 422. */
export interface TipoCombustivelUpdateRequest extends TipoCombustivelRequest {
  ativo: boolean
}

export interface TipoCombustivelResponse {
  id: number
  nome: string
  ativo: boolean
  dataInclusao: string
}

// ---------- Posto ----------

/**
 * A rede credenciada da empresa. Diferente dos outros catálogos, não há conjunto padrão
 * semeado no provisionamento: cada empresa credencia os seus.
 */
export interface PostoRequest {
  nome: string
  /** Opcionais: nem todo posto credenciado é registrado com nota da empresa. */
  cnpj?: string | null
  cidade?: string | null
}

export interface PostoUpdateRequest extends PostoRequest {
  ativo: boolean
}

export interface PostoResponse {
  id: number
  nome: string
  cnpj?: string | null
  cidade?: string | null
  ativo: boolean
  dataInclusao: string
}

// ---------- Tipo de despesa ----------

/** Catálogo de tipos da empresa; alimenta o select da tela de despesas. */
export interface TipoDespesaRequest {
  nome: string
}

/** O PUT aceita o mesmo shape do POST mais o `ativo` — inativar é o caminho quando o DELETE dá 422. */
export interface TipoDespesaUpdateRequest extends TipoDespesaRequest {
  ativo: boolean
}

export interface TipoDespesaResponse {
  id: number
  nome: string
  ativo: boolean
  dataInclusao: string
}

// ---------- Despesa ----------

/**
 * Custo avulso: pedágio, multa, IPVA, seguro, licenciamento. É a terceira origem da tela
 * de custos, e a única cuja tabela é fonte de verdade — as outras duas são lidas das telas
 * de abastecimento e manutenção.
 *
 * Só a gestão lança, então não há aqui o par motorista/usuário do abastecimento: quem
 * lançou fica na trilha de auditoria.
 */
export interface DespesaRequest {
  /** Obrigatório — IPVA, seguro e licenciamento já são por veículo na prática. */
  veiculoId: number
  tipoDespesaId: number
  /** Opcional: multa tem dono, IPVA não. */
  motoristaId?: number | null
  valor: number
  dataDespesa: string
  observacao?: string | null
}

/**
 * O PUT altera **tudo**, inclusive veículo, tipo e motorista — diferente do abastecimento,
 * onde só valor, data e observação são editáveis. Lá a trava existe porque a troca
 * reatribuiria um gasto sujeito a recorte por dono; aqui não há recorte.
 */
export type AtualizarDespesaRequest = DespesaRequest

export interface DespesaFiltro {
  /** Paginação do servidor. Sem eles a API assume página 1 com 15 itens; teto de 100. */
  pagina?: number
  tamanhoPagina?: number
  veiculoId?: number
  motoristaId?: number
  tipoDespesaId?: number
  /** `yyyy-MM-dd`; `ate` é inclusivo (o servidor estende até o fim do dia). */
  de?: string
  ate?: string
}

/**
 * Contagem e soma do **filtro inteiro**, servidos por `/abastecimento/resumo` e
 * `/despesa/resumo`. É o que sustenta o rodapé "N lançamentos · Total: R$ X" depois que a
 * lista passou a vir paginada — somar a página diria outro número a cada virada.
 */
export interface ResumoLancamentos {
  quantidade: number
  valorTotal: number
}

/**
 * A referência da estimativa de km/l: o abastecimento de maior odômetro abaixo do digitado
 * naquele veículo. Só data e odômetro — a consulta enxerga o histórico do veículo inteiro,
 * inclusive lançamentos de outra pessoa, e devolver valor ou nome vazaria gasto alheio.
 */
export interface AbastecimentoAnteriorResponse {
  dataAbastecimento: string
  odometro: number
}

/** Agregados de `/rota/resumo`: rotas encerradas no período e o km que somaram. */
export interface ResumoRotas {
  quantidade: number
  kmTotal: number
}

/** Filtro de `/rota` e `/rota/minhas`. */
export interface RotaFiltro {
  /** Paginação do servidor. Sem eles a API assume página 1 com 15 itens; teto de 100. */
  pagina?: number
  tamanhoPagina?: number
  /**
   * `true` traz só as rotas em andamento, `false` só o histórico, omitido traz tudo.
   * É como a tela do motorista acha a rota ativa e como o dashboard conta as abertas
   * (pedindo `tamanhoPagina: 1` e lendo o `total`).
   */
  ativo?: boolean
}

export interface DespesaResponse {
  id: number
  veiculoId: number
  veiculoNome: string
  veiculoPlaca: string
  tipoDespesaId: number
  tipoDespesaNome: string
  /** Nulo quando a despesa não é de ninguém em particular (IPVA, seguro). */
  motoristaId?: number | null
  motoristaNome?: string | null
  valor: number
  dataDespesa: string
  observacao?: string | null
  dataInclusao: string
}
