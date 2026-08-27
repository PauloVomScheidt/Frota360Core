# Contexto geral da API Frota360

Panorama da aplicação: arquitetura, fluxos de negócio, endpoints e infraestrutura.

## O que é

API REST .NET 10, multi-tenant por empresa, para gestão de frota: veículos, motoristas, rotas, catálogo de tipos de manutenção e manutenções. Clean Architecture em 4 projetos + testes.

Panorama visual em [`docs/arquitetura.png`](docs/arquitetura.png) — as quatro camadas, o fluxo CQRS de um request e os cinco pontos de isolamento por `EmpresaId` num desenho só, com rótulos em inglês. Gerado por `python docs/arquitetura.py` (Pillow); **regenere o PNG sempre que a arquitetura mudar**.

| Projeto | Papel |
|---|---|
| `Frota360.Domain` (`apps/api/src/Domain`) | Entidades, enums, `ApiResponse<T>`, `Roles`, interfaces de repositório/serviço. Zero pacotes. |
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

Toda entidade tem `EmpresaId`. O valor vem **só** do claim `empresaId` do JWT via `CurrentUserService` — nunca do corpo/rota/query. Não há query filter global no EF: cada método de repositório recebe e filtra por `empresaId`. Índices únicos são compostos (`(EmpresaId, CPF)`, `(EmpresaId, Nome)`); exceção é `Usuario.Email`, único global.

O padrão correto de FK está em `CreateManutencaoHandler.cs:30-33`: resolve `VeiculoId`/`TipoManutencaoId` via `GetByIdAsync(id, empresaId)` antes de gravar, então id de outra empresa simplesmente "não existe".

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

### 3. Auth
Login BCrypt → JWT de 1h (claims `sub`, `email`, `name`, `jti`, `empresaId`, `role`) + refresh token de 7 dias rotacionado a cada uso (hash no banco). `esqueci-senha` responde neutro sempre (não revela se o e-mail existe), token de 30 min; redefinir senha **derruba o refresh token**. Todos esses endpoints têm rate limit de 5/min por IP.

### 4. Gestão de usuários (só Admin)
Alterar role ou desativar. Ambos revogam a sessão (forçam novo login para o token refletir a mudança) e barram deixar a empresa sem admin ativo — `UsuarioService.cs:32`.

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
| GET | `/veiculo`, `/motorista`, `/rota`, `/tipomanutencao?apenasAtivos=`, `/manutencao?veiculoId=&status=` (+ `/{id}`) | qualquer autenticado |
| POST/PUT | `/veiculo`, `/motorista`, `/tipomanutencao`, `/manutencao`, `/manutencao/{id}/concluir` | Admin, Supervisor |
| POST/PUT | `/rota`, `/rota/{id}/encerrar` | qualquer autenticado (inclui Operador) |
| DELETE | `/{qualquer}/{id}` | **Admin** (único que exclui) |
| GET | `/health`, `/health/detail`, `/scalar/v1` | aberto |

## Infra e config

Serilog (console + arquivo diário, 7 dias), rate limit global 200/min por IP + política `auth` 5/min, CORS por `Cors:AllowedOrigins`, health check do DbContext, Scalar em `/scalar/v1` com Bearer declarado. `AddInfrastructure` **derruba a inicialização** se `Jwt:Key` faltar ou tiver menos de 32 caracteres. Sem `Resend:ApiKey`, o e-mail cai no `LogEmailService` (imprime no console) — prático em dev.

## Testes

xUnit + NSubstitute, sem banco. O projeto não referencia Infrastructure nem a API, então não há teste de repositório, DbContext ou endpoint — a cobertura é de handlers, mappings, validators e services.
