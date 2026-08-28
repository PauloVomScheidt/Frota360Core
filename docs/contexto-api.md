# Contexto geral da API Frota360

Panorama da aplicação: arquitetura, fluxos de negócio, endpoints e infraestrutura.

## O que é

API REST .NET 10, multi-tenant por empresa, para gestão de frota: veículos, motoristas, rotas, catálogo de tipos de manutenção e manutenções. Clean Architecture em 4 projetos + testes.

Panorama visual em [`docs/arquitetura.png`](docs/arquitetura.png) — as quatro camadas, o fluxo CQRS de um request e os cinco pontos de isolamento por `EmpresaId` num desenho só, com rótulos em inglês. Gerado por `python docs/arquitetura.py` (Pillow); **regenere o PNG sempre que a arquitetura mudar**.

| Projeto | Papel |
|---|---|
| `Frota360.Domain` (`apps/api/src/Domain`) | Entidades, enums, `ApiResponse<T>`, `ResultadoPaginado<T>`, `Roles`, `AcoesAuditoria`/`EntidadesAuditadas`, interfaces de repositório/serviço. Zero pacotes. |
| `Frota360.Application` (`apps/api/src/Application`) | CQRS manual (`UseCases/`), `Services/` (auth/convite/usuário/backoffice), DTOs, validators FluentValidation |
| `Frota360.Infrastructure` (`apps/api/src/Infrastructure`) | EF Core + SQL Server, repositórios, JWT, e-mail (Resend), migrations |
| `Frota360.Api` (`apps/api/src/Api`) | Controllers, `ExceptionMiddleware`, `CurrentUserService`, Program |

## Pipeline de um request

```
HTTP → ExceptionMiddleware → Serilog → CORS → RateLimiter → Auth/Authorization → Controller
Controller: valida IValidator<T> → 400 | dispatcher.SendAsync(Command/Query)
  Dispatcher (reflexão sobre DI) → IRequestHandler<TReq,TResp>
    Handler: lê currentUser.EmpresaId → repositório → entidade → .ToResponse()
Controller embrulha em ApiResponse<T>
```

O `Dispatcher` (`apps/api/src/Application/Abstractions/Messaging/Dispatcher.cs`) resolve `IRequestHandler<,>` fechado no contêiner — handlers são varridos por assembly em `ApplicationExtensions.cs:41`, então nunca precisam de registro manual (o preço: handler faltando quebra em runtime, não em compilação).

**Erros** (`apps/api/src/Api/Middlewares/ExceptionMiddleware.cs:33`): `InvalidOperationException` → **422 com a mensagem literal** (é texto para o usuário final), `KeyNotFoundException` → 404, `ArgumentNullException` → 400, resto → 500 genérico. Handler retorna `null`/`false` para "não encontrado" e o controller devolve 404.

## Isolamento multi-tenant

Toda entidade tem `EmpresaId`. O valor vem **só** do claim `empresaId` do JWT via `CurrentUserService` — nunca do corpo/rota/query. Não há query filter global no EF: cada método de repositório recebe e filtra por `empresaId`. Índices únicos são compostos (`(EmpresaId, Nome)`, `(EmpresaId, CPF)` filtrado em `Usuario`); exceção é `Usuario.Email`, único global.

O padrão correto de FK está em `CreateManutencaoHandler.cs:30-33`: resolve `VeiculoId`/`TipoManutencaoId` via `GetByIdAsync(id, empresaId)` antes de gravar, então id de outra empresa simplesmente "não existe".

`LogAuditoria` entrou como mais um ponto de isolamento: o `EmpresaId` é gravado do claim por `AuditoriaService` e `ILogAuditoriaRepository.ConsultarAsync(empresaId, filtro)` não tem sobrecarga sem ele — o `empresaId` é parâmetro separado, fora do record de filtro, justamente para que nenhum caminho consiga montar uma consulta sem escopo.

### Segundo eixo: o próprio usuário (role Motorista)

**Não existe entidade `Motorista`.** Um motorista é um `Usuario` com `Role = Motorista`, e `Rota.CodigoMotorista` é uma FK para `Usuario` (`Restrict`: usuário nunca é excluído, só desativado, então o histórico de rotas não some por acidente).

Para essa role o escopo é duplo — empresa **e** o próprio usuário, os dois vindos do token, sem claim extra: o `sub` já identifica o motorista.

- `GET /rota/minhas` → `IRotaRepository.GetAllByMotoristaAsync(empresaId, currentUser.UsuarioId)`.
- `POST /rota` → `CreateRotaHandler` ignora o `CodigoMotorista` do corpo e grava o `UsuarioId`.
- `POST /rota/{id}/encerrar` → rota de outro motorista devolve `null` → **404**, não 403: para quem não é dono dela, a rota não existe.

`CurrentUserExtensions.EhMotorista()` (`Application/Common`) é o único auxiliar que sobrou — não há vínculo a resolver nem estado inconsistente possível.

Quem resolve o `CodigoMotorista` de uma rota usa `IUsuarioRepository.GetMotoristaByIdAsync(id, empresaId)`, que filtra **empresa e role**: um usuário de outra empresa, ou um Supervisor, simplesmente "não existe" como motorista e cai no mesmo 422 `"Motorista {id} não encontrado."`.

### Auditoria de isolamento por EmpresaId (RN07) — 25/08/2026

Varredura de toda chamada a `GetByIdAsync` / `GetAllAsync` / `Existe*Async` na camada Application, procurando FK vinda do request que fosse gravada sem ser resolvida com `empresaId`. Os dois handlers de rota eram os **únicos** casos, e foram corrigidos.

| Handler / serviço | FK gravada a partir do request | Escopada por `EmpresaId` |
|---|---|---|
| `CreateManutencaoHandler` | `VeiculoId`, `TipoManutencaoId` | ✅ baseline correta |
| `UpdateManutencaoHandler` | `VeiculoId`, `TipoManutencaoId` | ✅ |
| `ConcluirManutencaoHandler` | — (só lê o veículo) | ✅ |
| `Create/Update/Delete` de Veículo, Motorista, TipoManutenção | — (sem FK de request) | ✅ |
| Todas as Queries (`GetAll*`, `Get*ById`) | — | ✅ |
| `ConviteService` | — | ✅ |
| `CreateRotaHandler` | `CodigoMotorista`, `CodigoVeiculo` | ✅ **corrigido (RN07)** |
| `UpdateRotaHandler` | `CodigoMotorista`, `CodigoVeiculo` | ✅ **corrigido (RN07)** |

Rota agora resolve motorista e veículo por `GetByIdAsync(id, currentUser.EmpresaId)` antes de gravar: id inexistente **ou de outra empresa** cai no mesmo 422 (`"Motorista {id} não encontrado."` / `"Veículo {id} não encontrado."`), sem distinguir os dois casos para quem chama.

`UsuarioService.cs:70` e `AuthService.cs:113` chamam `GetByIdAsync(usuarioId)` sem `empresaId` — não é o mesmo padrão: `usuarioId` vem do claim do próprio JWT, não do corpo, e `Usuario` é justamente a entidade que resolve a empresa.

## Fluxos

### 1. Provisionamento (venda assistida)
`POST /backoffice/empresa` com header `X-Backoffice-Key` → cria `Empresa`, semeia os 10 `TiposManutencaoPadrao` e dispara convite de Admin. Sem `Backoffice:ApiKey` configurada, o endpoint responde 401 sempre.

### 2. Convite → conta
Admin cria convite (token aleatório de 64 bytes, **só o hash SHA vai ao banco**, validade 7 dias, convites pendentes anteriores do mesmo e-mail são apagados) → e-mail com link `{Frontend}/convite?token=` → `POST /convite/aceitar` (anônimo) cria o `Usuario` já com role do convite e devolve token+refresh direto, sem exigir login.

**Convite de motorista não tem nada de especial:** é e-mail + `Role = Motorista`, como qualquer outra role. O aceite (`POST /convite/aceitar`) aceita ainda `CPF` e `DataNascimento` **opcionais**, que a própria pessoa informa — hoje é o único ponto de entrada desses dados (não há tela de perfil). Em branco viram nulo, e não string vazia: o índice único filtrado `(EmpresaId, CPF)` depende disso para não colidir entre quem não informou.

### 3. Auth
Login BCrypt → JWT de 1h (claims `sub`, `email`, `name`, `jti`, `empresaId`, `role`) + refresh token de 7 dias rotacionado a cada uso (hash no banco). `esqueci-senha` responde neutro sempre (não revela se o e-mail existe), token de 30 min; redefinir senha **derruba o refresh token**. Todos esses endpoints têm rate limit de 5/min por IP.

### 4. Gestão de usuários (só Admin)
Alterar role ou desativar. Ambos revogam a sessão (forçam novo login para o token refletir a mudança) e barram deixar a empresa sem admin ativo — `UsuarioService.cs`. `Motorista` é uma role como as outras: promover e rebaixar funcionam igual.

### 4.1 Papéis

| Role | Alcance |
|---|---|
| `Admin` | Tudo. Único que exclui e que administra usuários/convites. |
| `Supervisor` | Cadastra e edita veículos, rotas e manutenções. |
| `Operador` | Visualiza a frota e opera rotas (abre, edita, encerra). |
| `Motorista` | Opera **só as próprias rotas** (`GET /rota/minhas`, `POST /rota`, `POST /rota/{id}/encerrar`) e **lê** veículos e manutenções (`GET /veiculo`, `GET /manutencao`) — precisa saber o estado do caminhão que vai pegar. O `Custo` da manutenção é omitido para ele. Convite, promoção e rebaixamento são iguais aos das demais roles. |

`Roles.Gestao` (`Admin,Supervisor,Operador`) é a constante usada nos `[Authorize]` que barram o motorista: controllers de motorista, manutenção e tipo de manutenção inteiros, e os endpoints de gestão de rota. `VeiculoController` fica de fora de propósito — leitura aberta, escrita restrita como antes.

### 5. Manutenção — a parte mais interessante do domínio
- Nasce **Pendente** com `QuilometragemPrevista` e opcionalmente `DataPrevista`; vence no que vier primeiro.
- **"Atrasada" não existe no banco.** É derivada na leitura em `ManutencaoMappings.cs:47`, comparando o previsto com o km atual do veículo — dispensa job de envelhecimento. O enum só tem `Pendente/Realizada/Cancelada`, persistido como texto.
- Bloqueia duplicata (mesmo veículo + tipo + km pendente) e tipo inativo.
- **Concluir** (`ConcluirManutencaoHandler.cs:69`) grava km/data/custo e aproveita para atualizar o odômetro do veículo — só para frente, nunca retrocede.
- Excluir um tipo em uso é proibido (422, "inative-o"): apagar levaria o histórico junto.

### 6. Rota — ciclo de hodômetro (RN10)

A rota tem `KmInicial` (obrigatório na abertura), `KmFinal` e `KmPercorrido` (nulos até o encerramento).

- **Abrir** (`POST /rota`): `KmInicial` **não pode ser menor** que o odômetro atual do veículo → 422 com o valor atual na mensagem. Se for **maior**, o veículo já é atualizado na abertura — rodou fora do sistema, o número mais recente vence.
- **Encerrar** (`POST /rota/{id}/encerrar`, corpo `{ kmFinal, dataFim? }` — `dataFim` omitida vira agora): grava `KmFinal`, `DataFim`, `KmPercorrido = KmFinal - KmInicial`, `Ativo = false`, e avança o odômetro do veículo **só para frente** (mesma política de `ConcluirManutencaoHandler.cs:69`). Quatro recusas em 422: rota já encerrada, `KmFinal` < `KmInicial`, `DataFim` < `DataInicio` (RN06) e — via 404 — rota de outra empresa.
- **`KmPercorrido` é persistido, não derivado.** Contraste deliberado com `atrasada`: é fato histórico da rota, não muda depois de gravado e não depende do estado atual do veículo. `atrasada` é derivado justamente porque depende do odômetro corrente.
- **Encerrar é a única transição de estado.** `Ativo` e `DataFim` saíram de `CreateRotaRequest` e `UpdateRotaRequest`: sem isso dava para "encerrar" uma rota pelo PUT sem calcular km nem tocar no odômetro. A `RotaResponse` continua expondo os dois.
- **Por que isso importa:** era o elo que faltava. Rodar rota é diário, concluir manutenção é eventual — sem o encerramento alimentando o odômetro, `atrasada` e `kmRestantes` das manutenções nunca acendiam.

### 7. Rota vista pelo motorista

`GET /rota/minhas` é endpoint dedicado, e não um filtro no `GET /rota`, justamente para não haver um mesmo endpoint com dois comportamentos por role — é impossível vazar a lista da frota por engano. Ele não recebe parâmetro nenhum: o motorista é o usuário do token.

Abrir e encerrar são os mesmos endpoints da gestão, com a diferença aplicada no handler (ver § Isolamento multi-tenant): o `CodigoMotorista` do corpo é ignorado, e encerrar rota alheia é 404. `CreateRotaValidator` recebe `ICurrentUserService` e dispensa o `CodigoMotorista` para essa role — exigir um campo que o handler ignora seria pedir um dado inútil.

Editar (`PUT`) e excluir continuam fora do alcance dele: correção de rota é da gestão.

**Motorista rebaixado:** as rotas dele continuam apontando para o usuário (a FK é `Restrict`), mas ele some de `GET /motorista`, que lista só quem tem a role. Para que a rota continue identificável, `RotaResponse` traz **`NomeMotorista` desnormalizado** — mesma técnica de `veiculoNome`/`veiculoPlaca` em `ManutencaoResponse`. O repositório carrega por `Include(r => r.Motorista)`; nos handlers de create/update a navegação não vem preenchida (ou aponta para o dono anterior), então eles atribuem o nome à mão.

### 8. Encerrar rota alimenta a ficha do veículo

Além do odômetro, o encerramento grava `Veiculo.UltimoMotorista` e `Veiculo.DataUltimaViagem` — que antes **só eram preenchidos à mão** pelo CRUD de veículo, apesar de a documentação afirmar o contrário. Os três campos seguem a mesma política de "só avança": encerrar hoje uma rota de mês passado não reescreve a ficha com dado velho (`AtualizarVeiculoAsync` compara `DataUltimaViagem` antes de gravar). O odômetro é independente da data — ele avança sempre que o km final for maior.

### 9. Auditoria — quem alterou o quê

Tabela `LogAuditoria`, **append-only**: só insert e select. Não existe endpoint de update nem de delete, nem para o Admin, e o repositório também não os expõe.

**Escopo.** Administração (usuário, convite) **e** domínio (veículo, rota, manutenção, tipo de manutenção). **Login/logout ficam de fora** de propósito: seria o maior volume da tabela e o menor valor — continuam só no Serilog.

**Como é capturado.** Explicitamente, no handler/serviço, via `IAuditoriaService.RegistrarAsync` chamado ao final do caminho feliz — 15 pontos, listados abaixo. Não é um interceptor do EF: o objetivo é registrar a **intenção de negócio** ("Encerrou a rota"), que um `UPDATE Rota` não expressa, e evitar o ruído de `RefreshTokenHash` mudando a cada login.

**Modelo.** `Entidade` + `Acao` (`EntidadesAuditadas` / `AcoesAuditoria`, em `Domain/Common`) são dois eixos de filtro com poucos valores distintos, no lugar de uma constante por evento. `Descricao` é uma frase pronta em português, montada no servidor. `Alteracoes` é o diff em JSON (`[{campo, de, para}]`), nulo em criação e exclusão.

Nome, e-mail e papel de quem agiu ficam **desnormalizados** na linha — o log é histórico e não pode mudar de sentido quando a pessoa é renomeada ou rebaixada. Saem das claims `name`/`email` do JWT, que já existiam: nenhuma ida a mais ao banco por operação auditada.

**Duas garantias de desenho:**

1. **Auditoria não derruba negócio.** `AuditoriaService` roda depois de o repositório já ter feito `SaveChangesAsync`, portanto fora daquela transação, e engole a exceção em log de erro. Perder uma linha de trilha é ruim; devolver 500 numa edição que já foi persistida é pior.
2. **Nada de segredo no diff.** Ele é montado à mão, campo a campo, por `AlteracoesBuilder` — chamado **antes** de a entidade ser mutada, já que os handlers a alteram in-place. Hash de senha, refresh token, token de reset e de convite nunca entram.

**Os 15 pontos de registro:**

| Origem | Entidade · Ação | Diff |
|---|---|---|
| `Create/Update/DeleteVeiculoHandler` | Veiculo · Criou/Atualizou/Excluiu | ✅ no update |
| `CreateRotaHandler` | Rota · Criou | ✅ quando a abertura avança o odômetro |
| `UpdateRotaHandler` | Rota · Atualizou | ✅ (motorista e veículo pelo nome, não pelo id) |
| `EncerrarRotaHandler` | Rota · Encerrou | ✅ km final + avanço do odômetro |
| `DeleteRotaHandler` | Rota · Excluiu | — |
| `Create/Update/DeleteManutencaoHandler` | Manutencao · Criou/Atualizou/Excluiu | ✅ no update |
| `ConcluirManutencaoHandler` | Manutencao · Concluiu | ✅ status, km, custo, odômetro |
| `Create/Update/DeleteTipoManutencaoHandler` | TipoManutencao · Criou/Atualizou/Excluiu | ✅ no update |
| `UsuarioService.AlterarRoleAsync` | Usuario · AlterouPermissao | ✅ papel anterior → novo |
| `UsuarioService.DefinirAtivoAsync` | Usuario · Ativou/Desativou | — |
| `ConviteService.CriarParaEmpresaAsync` / `CancelarAsync` | Convite · Criou/Cancelou | — |
| `ConviteService.AceitarAsync` | Convite · Aceitou | — |

Dois casos fogem do padrão:

- **Provisionamento pelo backoffice** chega em `CriarParaEmpresaAsync` com `criadoPorUsuarioId: null` — sem sessão, sem ator. Ali **não registra**; fica no Serilog. Vindo de `CriarAsync` (Admin logado), registra normal.
- **Aceite de convite** é anônimo e o usuário nasce na própria operação. Usa `RegistrarComoAsync(empresaId, ator, ...)`, que recebe o ator à mão — é o único evento em que o ator é também o objeto.

**Consulta.** `GET /auditoria` (Admin), via query CQRS `GetLogsAuditoriaQuery`, filtrando por entidade, ação, usuário e período. Três índices, um por consulta real: `(EmpresaId, DataHora)` para a listagem, `(EmpresaId, Entidade, EntidadeId)` para o histórico de um registro e `(EmpresaId, UsuarioId, DataHora)` para o de uma pessoa.

Retenção é **infinita** por ora — o `Id` é `long` e os índices suportam o crescimento; uma rotina de purga cabe depois sem mexer no schema.

### Paginação — `ResultadoPaginado<T>`

`/auditoria` é o **primeiro e único endpoint paginado** do sistema; as demais listas ainda vêm inteiras. `ResultadoPaginado<T>` (`Domain/Common`) traz `Itens`, `Pagina`, `TamanhoPagina`, `Total` e `TotalPaginas`, e vai **dentro** de `ApiResponse<T>.Dados` — o envelope não muda: `ApiResponse<ResultadoPaginado<LogAuditoriaResponse>>`.

`ConsultarAuditoriaValidator` impõe teto de **100** por página. Sem ele, um `tamanhoPagina=999999` materializaria a trilha inteira da empresa em memória. Reutilize esse tipo ao paginar qualquer outra lista.

## Endpoints

Base: `api/v1/{controller}` (versão também aceita via header `api-version` ou `?version=`).

| Método | Rota | Acesso |
|---|---|---|
| POST | `/auth/login`, `/refresh`, `/esqueci-senha`, `/redefinir-senha` | anônimo, 5 req/min |
| POST | `/auth/logout` | autenticado |
| POST | `/backoffice/empresa` | `X-Backoffice-Key` |
| GET/POST/DELETE | `/convite`, `/convite/{id}` | Admin |
| POST | `/convite/aceitar` | anônimo |
| GET | `/usuario` · PUT `/usuario/{id}/role` · `/{id}/ativo` | Admin |
| GET | `/auditoria?pagina=&tamanhoPagina=&entidade=&acao=&usuarioId=&de=&ate=` — **único endpoint paginado**; somente leitura | Admin |
| GET | `/veiculo` (+ `/{id}`) | qualquer autenticado (**inclui Motorista**) |
| GET | `/manutencao?veiculoId=&status=` (+ `/{id}`) | qualquer autenticado (**inclui Motorista**, sem `custo`) |
| GET | `/motorista` (+ `/{id}`) — **somente leitura**: os usuários com a role Motorista | Admin, Supervisor, Operador |
| GET | `/rota`, `/tipomanutencao?apenasAtivos=` (+ `/{id}`) | Admin, Supervisor, Operador |
| GET | `/rota/minhas` | **Motorista** (rotas do próprio, pelo `sub` do token) |
| POST/PUT | `/veiculo`, `/tipomanutencao`, `/manutencao`, `/manutencao/{id}/concluir` | Admin, Supervisor |
| PUT | `/rota/{id}` | Admin, Supervisor, Operador |
| POST | `/rota`, `/rota/{id}/encerrar` | qualquer autenticado — o Motorista só alcança as próprias rotas |
| DELETE | `/{qualquer}/{id}` (não há DELETE de motorista) | **Admin** (único que exclui) |
| GET | `/health`, `/health/detail`, `/scalar/v1` | aberto |

## Infra e config

Serilog (console + arquivo diário, 7 dias), rate limit global 200/min por IP + política `auth` 5/min, CORS por `Cors:AllowedOrigins`, health check do DbContext, Scalar em `/scalar/v1` com Bearer declarado. `AddInfrastructure` **derruba a inicialização** se `Jwt:Key` faltar ou tiver menos de 32 caracteres. Sem `Resend:ApiKey`, o e-mail cai no `LogEmailService` (imprime no console) — prático em dev.

## Testes

xUnit + NSubstitute, sem banco. O projeto não referencia Infrastructure nem a API, então não há teste de repositório, DbContext ou endpoint — a cobertura é de handlers, mappings, validators e services.
