# Frota360

API REST de **gestão de frotas multi-tenant**, construída em .NET 10. Cada empresa cliente enxerga apenas os seus veículos, motoristas, rotas e manutenções — o isolamento é aplicado em cada acesso a dados, a partir da claim `empresaId` do JWT.

> Este é o backend do monorepo **Frota360** (`apps/api/`). O front-end React que o consome vive em [`apps/web`](../web). Visão geral e o contrato entre os dois: [README](../../README.md) e [CLAUDE.md](../../CLAUDE.md) da raiz.
>
> **Todos os comandos e caminhos deste README são relativos a `apps/api/`.**

---

## Sumário

- [Estrutura](#estrutura)
- [O que a API faz](#o-que-a-api-faz)
- [Stack](#stack)
- [Arquitetura](#arquitetura)
- [Como rodar](#como-rodar)
- [Configuração](#configuração)
- [Autenticação e papéis](#autenticação-e-papéis)
- [Endpoints](#endpoints)
- [Formato das respostas](#formato-das-respostas)
- [Regras de negócio principais](#regras-de-negócio-principais)
- [Testes](#testes)
- [Docker](#docker)

---

## Estrutura

```
apps/api/
├── Frota360.slnx          solution
├── Dockerfile             build da API (contexto = esta pasta)
├── src/
│   ├── Domain/            Frota360.Domain
│   ├── Application/       Frota360.Application
│   ├── Infrastructure/    Frota360.Infrastructure
│   └── Api/               Frota360.Api  ← ponto de entrada
└── tests/
    ├── Frota360.Tests/            xUnit + NSubstitute, sem banco
    └── Frota360.IntegrationTests/ Testcontainers + PostgreSQL real
```

Os `.csproj` mantêm o prefixo `Frota360.` (`src/Api/Frota360.Api.csproj`); só os diretórios são curtos. O contexto profundo de domínio está em [`docs/contexto-api.md`](../../docs/contexto-api.md), na raiz do monorepo.

---

## O que a API faz

| Módulo | Responsabilidade |
|---|---|
| **Empresa / Backoffice** | Provisionamento de empresa nova (venda assistida), protegido por API key |
| **Convite / Usuário** | Convite por e-mail com token, criação de conta, gestão de papéis e ativação |
| **Auth** | Login, refresh token rotacionado, esqueci/redefinir senha, logout |
| **Veículo** | Cadastro da frota, com odômetro alimentado pelas rotas e manutenções |
| **Motorista** | Cadastro de condutores (CPF único por empresa) |
| **Rota** | Abertura e encerramento de viagens, com ciclo de hodômetro |
| **Tipo de manutenção** | Catálogo por empresa ("troca de óleo", com intervalo em km) |
| **Manutenção** | Planejamento por km/data, conclusão com custo e atualização do odômetro |

---

## Stack

- **.NET 10** / ASP.NET Core
- **Entity Framework Core 10** + PostgreSQL (provider Npgsql)
- **JWT Bearer** + BCrypt para senhas
- **FluentValidation** para validação de entrada
- **Serilog** (console + arquivo diário, retenção de 7 dias)
- **Scalar** para documentação interativa OpenAPI
- **Asp.Versioning** (versionamento por URL, header ou query string)
- **Resend** para envio de e-mail (com fallback para log em desenvolvimento)
- **xUnit + NSubstitute** nos testes

---

## Arquitetura

Clean Architecture em quatro projetos, com CQRS manual na camada de aplicação.

```
src/Domain           Frota360.Domain — entidades, enums, ApiResponse<T>, Roles, interfaces
      ^
src/Application      Frota360.Application — UseCases (Commands/Queries/Handlers), Services, DTOs, Validators
      ^                                        ^
src/Infrastructure   Frota360.Infrastructure — DbContext, repositórios, TokenService, e-mail, config do JWT
      ^
src/Api              Frota360.Api — Controllers, ExceptionMiddleware, CurrentUserService
```

| Projeto | Pode referenciar | Nunca referencia |
|---|---|---|
| `Frota360.Domain` (`src/Domain`) | nada (sem pacotes) | EF Core, ASP.NET |
| `Frota360.Application` (`src/Application`) | Domain | Infrastructure, ASP.NET, `DbContext` |
| `Frota360.Infrastructure` (`src/Infrastructure`) | Domain | Application |
| `Frota360.Api` (`src/Api`) | Application + Infrastructure | — |

### Fluxo de um request

```
Controller (valida com IValidator<T> -> 400 se inválido)
  -> dispatcher.SendAsync(new XCommand(request))
  -> Dispatcher resolve IRequestHandler<XCommand, TResponse> no DI (reflexão)
  -> Handler: lê currentUser.EmpresaId, chama o repositório, monta a entidade, .ToResponse()
  -> Controller embrulha o resultado em ApiResponse<T>
```

`AddCqrsHandlers` varre a assembly da Application e registra todo `IRequestHandler<,>` — handler novo não precisa de registro manual.

**CRUD de domínio** (veículo, motorista, rota, manutenção, tipo de manutenção) vive em `UseCases/` no padrão CQRS. **Autenticação, convite, usuário e onboarding** ficam em `Application/Services`, injetados direto no controller.

### Isolamento por EmpresaId

Toda entidade de negócio tem `EmpresaId`, e o valor vem **sempre** de `ICurrentUserService.EmpresaId` — nunca do corpo, rota ou query string:

- Todo método de repositório recebe e filtra por `empresaId` (`GetByIdAsync(int id, int empresaId)`).
- Todo id de FK vindo do request é resolvido por `GetByIdAsync(id, empresaId)` antes de gravar — um id de outra empresa simplesmente "não existe" (404).
- Índices únicos são compostos com `EmpresaId` (`(EmpresaId, CPF)`, `(EmpresaId, Nome)`). Exceção: `Usuario.Email` é único global.

Não há query filter global no EF; o filtro é responsabilidade explícita de cada método de repositório.

---

## Como rodar

Pré-requisitos: **.NET 10 SDK** e uma instância de **PostgreSQL 17** (`docker compose up -d` na raiz do monorepo sobe uma).

```powershell
# 1. Restaurar e compilar
dotnet build Frota360.slnx

# 2. Configurar a chave do JWT (obrigatória, mínimo 32 caracteres)
dotnet user-secrets set "Jwt:Key" "uma-chave-secreta-com-pelo-menos-32-caracteres" --project src/Api

# 3. Aplicar as migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Api

# 4. Subir a API
dotnet run --project src/Api
```

Todos os comandos rodam a partir de **`apps/api/`**.

A API sobe em `http://localhost:5062`. A documentação interativa fica em **`/scalar/v1`**.

Para subir o front junto, veja [`apps/web/README.md`](../web/README.md) — são dois terminais.

> `AddInfrastructure` derruba a inicialização se `Jwt:Key` faltar ou tiver menos de 32 caracteres.

### Nova migration

```powershell
dotnet ef migrations add <Nome> --project src/Infrastructure --startup-project src/Api
```

### Primeiro acesso

Não há usuário semeado. O caminho de entrada é o provisionamento pelo backoffice:

1. Configure `Backoffice:ApiKey`.
2. `POST /api/v1/backoffice/empresa` com o header `X-Backoffice-Key` — cria a empresa, semeia os tipos de manutenção padrão e dispara o convite do primeiro Admin.
3. `POST /api/v1/convite/aceitar` com o token do e-mail cria o usuário e já devolve o par token + refresh, sem exigir login.

Em desenvolvimento, sem `Resend:ApiKey`, o e-mail cai no `LogEmailService` e o link do convite é impresso no console.

---

## Configuração

Os três `appsettings*.json` **não são versionados** (estão no `.gitignore`, porque carregam
connection string e chaves). O que vai para o git é o template
[`src/Api/appsettings.example.json`](src/Api/appsettings.example.json) — ao clonar o repo,
copie-o e preencha:

```powershell
copy src/Api/appsettings.example.json src/Api/appsettings.json
copy src/Api/appsettings.example.json src/Api/appsettings.Development.json
```

Os campos de segredo ficam **vazios** no template de propósito: eles não moram em arquivo,
e sim em `dotnet user-secrets` (dev) ou variável de ambiente (produção).

| Chave | Para que serve |
|---|---|
| `ConnectionStrings:DefaultConnection` | Conexão com o PostgreSQL (`Host=...;Port=5432;Database=...;Username=...;Password=...`) |
| `Jwt:Key` | Assinatura do token — **32+ caracteres, obrigatória** |
| `Jwt:Issuer` / `Jwt:Audience` | Emissor e audiência do JWT |
| `Cors:AllowedOrigins` | Array de origens liberadas |
| `Frontend:BaseUrl` | Base dos links de convite e reset de senha enviados por e-mail |
| `Resend:ApiKey` / `Resend:From` | Envio de e-mail. Sem a chave, cai no log |
| `Backoffice:ApiKey` | Protege o provisionamento. Sem ela, o endpoint responde 401 sempre |

Em **desenvolvimento** use `dotnet user-secrets`. Em **produção**, variáveis de ambiente:

```
Jwt__Key
Resend__ApiKey
Backoffice__ApiKey
ConnectionStrings__DefaultConnection
```

### Observabilidade e proteções

- **Serilog**: console + `logs/frota360-{data}.log`, rolagem diária, 7 arquivos retidos.
- **Rate limit**: 200 req/min por IP globalmente; política `auth` de 5 req/min nos endpoints sensíveis.
- **Health checks**: `GET /health` e `GET /health/detail` (com o status do DbContext).
- **CORS**: origens vindas de `Cors:AllowedOrigins`, aplicado antes do rate limiter para que o 429 também leve os headers.

---

## Autenticação e papéis

Login com BCrypt devolve um **JWT de 1 hora** (claims `sub`, `email`, `name`, `jti`, `empresaId`, `role`) mais um **refresh token de 7 dias**, rotacionado a cada uso e guardado como hash no banco.

| Papel | Pode |
|---|---|
| **Admin** | Tudo. É o **único que exclui** e o único que administra usuários e convites |
| **Supervisor** | Cria e edita motoristas, veículos, tipos de manutenção e manutenções |
| **Operador** | Cria, edita e encerra **rotas**; leitura no restante |

Alterar o papel ou desativar um usuário revoga a sessão dele (força novo login, para o token refletir a mudança) e é barrado se deixar a empresa sem nenhum admin ativo.

---

## Endpoints

Base: `api/v1/{controller}`. A versão também é aceita via header `api-version` ou query `?version=`.

| Método | Rota | Acesso |
|---|---|---|
| POST | `/auth/login`, `/auth/refresh`, `/auth/esqueci-senha`, `/auth/redefinir-senha` | anônimo, 5 req/min |
| POST | `/auth/logout` | autenticado |
| POST | `/backoffice/empresa` | header `X-Backoffice-Key` |
| GET/POST/DELETE | `/convite`, `/convite/{id}` | Admin |
| POST | `/convite/aceitar` | anônimo |
| GET | `/usuario` · PUT `/usuario/{id}/role` · `/usuario/{id}/ativo` | Admin |
| GET/PUT | `/usuario/perfil` — o próprio cadastro | qualquer autenticado |
| GET | `/veiculo`, `/tipomanutencao?apenasAtivos=`, `/manutencao?veiculoId=&status=&de=&ate=` (+ `/{id}`) | qualquer autenticado (o Motorista não recebe `custo` da manutenção) |
| GET | `/motorista`, `/rota` (+ `/{id}`) | Admin, Supervisor, Operador |
| GET | `/rota/minhas` | **Motorista** (as próprias, pelo `sub` do token) |
| GET/POST/PUT | `/abastecimento?veiculoId=&motoristaId=&de=&ate=` (+ `/{id}`) | qualquer autenticado — o Motorista só alcança o que é dele |
| GET | `/custo?pagina=&tamanhoPagina=&veiculoId=&motoristaId=&origem=&de=&ate=` · `/custo/resumo?…` | Admin, Supervisor, Operador |
| GET | `/auditoria?pagina=&tamanhoPagina=&entidade=&acao=&usuarioId=&de=&ate=` | Admin |
| POST/PUT | `/veiculo`, `/tipomanutencao`, `/manutencao`, `/manutencao/{id}/concluir` | Admin, Supervisor |
| POST/PUT | `/rota`, `/rota/{id}/encerrar` | qualquer autenticado (inclui Operador) |
| DELETE | `/{qualquer}/{id}` | **Admin** |
| GET | `/health`, `/health/detail`, `/scalar/v1` | aberto |

Transição de estado é sempre **endpoint próprio**, nunca PUT: `POST /manutencao/{id}/concluir`, `POST /rota/{id}/encerrar`.

`/auditoria` e `/custo` são os dois endpoints paginados — o `dados` do envelope vem como `ResultadoPaginado<T>`, não como array. **`/custo` não tem tabela por trás**: é um read model que une `Abastecimento.Valor` e `Manutencao.Custo` na leitura, e `/custo/resumo` é a única agregação servida pela API.

---

## Formato das respostas

Toda resposta usa o envelope `ApiResponse<T>`, montado no controller:

```json
{
  "sucesso": true,
  "mensagem": "Veículo cadastrado com sucesso.",
  "dados": { "id": 12, "placa": "ABC1D23" },
  "erros": null
}
```

| Situação | Status | Origem |
|---|---|---|
| Validação de entrada falhou | **400** | `IValidator<T>` no controller, com a lista em `erros` |
| Registro não encontrado (ou de outra empresa) | **404** | handler retorna `null` |
| Regra de negócio violada | **422** | `InvalidOperationException` → `ExceptionMiddleware` |
| Sem token / token inválido | **401** | JwtBearer, mesmo envelope |
| Papel insuficiente | **403** | JwtBearer, mesmo envelope |
| Excesso de requisições | **429** | rate limiter, mesmo envelope |
| Erro inesperado | **500** | mensagem genérica |

A mensagem de uma `InvalidOperationException` vai direto ao cliente — é escrita como texto para o usuário final.

---

## Regras de negócio principais

### Rota — ciclo de hodômetro

- **Abrir**: `KmInicial` é obrigatório e **não pode ser menor** que o odômetro atual do veículo (422, com o valor atual na mensagem). Se for maior, o veículo é atualizado já na abertura — rodou fora do sistema, o número mais recente vence.
- **Encerrar** (`POST /rota/{id}/encerrar`, corpo `{ kmFinal, dataFim? }` — `dataFim` omitida vira agora): grava `KmFinal`, `DataFim`, `KmPercorrido = KmFinal - KmInicial`, marca `Ativo = false` e avança o odômetro do veículo — **só para frente, nunca retrocede**. Recusa em 422: rota já encerrada, `KmFinal` < `KmInicial`, `DataFim` < `DataInicio`.
- `KmPercorrido` é **persistido**, não derivado: é fato histórico da rota e não depende do estado atual do veículo.
- Encerrar é a **única** transição de estado da rota — `Ativo` e `DataFim` não fazem parte do request de update.

### Manutenção

- Nasce **Pendente**, com `QuilometragemPrevista` e opcionalmente `DataPrevista` — vence no que vier primeiro.
- **"Atrasada" não existe no banco**: é derivada na leitura, comparando o previsto com o km atual do veículo. O enum persistido só tem `Pendente`, `Realizada` e `Cancelada`.
- Duplicata (mesmo veículo + tipo + km, ainda pendente) e tipo inativo são bloqueados.
- **Concluir** grava km, data e custo e aproveita para avançar o odômetro do veículo, também só para frente.
- Excluir um tipo de manutenção em uso é proibido (422, "inative-o") — apagar levaria o histórico junto.

### Convite e senha

Token aleatório de 64 bytes, com **apenas o hash indo ao banco**, validade de 7 dias; convites pendentes anteriores para o mesmo e-mail são apagados. `esqueci-senha` responde de forma neutra sempre, sem revelar se o e-mail existe (token de 30 min); redefinir a senha derruba o refresh token.

---

## Testes

```powershell
dotnet test
dotnet test --filter "FullyQualifiedName~ManutencaoHandlersTests"   # uma classe
dotnet test --filter "DisplayName~Create_DevePersistir"             # um teste
```

Duas suítes:

- **`tests/Frota360.Tests`** — xUnit + NSubstitute, **sem banco**. Referencia apenas Application e Domain; cobre handlers, mappings, validators e services. Roda em ~1 s, sem Docker.
- **`tests/Frota360.IntegrationTests`** — sobe um `postgres:17` descartável com **Testcontainers**, aplica as migrations reais e exercita os repositórios. Cobre o que só o banco prova: política de `DateTimeKind`, collation de e-mail, índices únicos filtrados, precisão decimal e tradução de consulta. **Exige Docker no ar.**

`dotnet test` roda as duas.

Nomes em português, no padrão `Metodo_Cenario_DeveResultado`. Todo handler tem ao menos um teste que prova o escopo por empresa.

---

## Docker

```powershell
docker build -t frota360 .
docker run -p 8080:8080 `
  -e ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=frota360;Username=...;Password=..." `
  -e Jwt__Key="uma-chave-secreta-com-pelo-menos-32-caracteres" `
  frota360
```

Build multi-estágio sobre `dotnet/sdk:10.0` → `dotnet/aspnet:10.0`, expondo a porta **8080**. O contexto do build é **`apps/api/`** — rode o `docker build` de dentro desta pasta.

---

## Convenções de código

O projeto é escrito **inteiramente em português** — classes, métodos, DTOs, comentários, logs e mensagens de resposta.

Estrutura de uma fatia vertical:

```
UseCases/<Agregado>s/
  Commands/<Acao><Agregado>/<Acao><Agregado>Command.cs
                            <Acao><Agregado>Handler.cs
  Queries/Get<X>/Get<X>Query.cs
  Validator/<Acao><Agregado>Validator.cs
  <Agregado>Mappings.cs
```

- **Primary constructors** em handlers, repositórios, controllers, serviços e middleware.
- Handlers são `sealed`, logam início e fim, e envolvem o corpo em `try/catch` que loga e relança.
- **Mapeamento é manual** via extensão `ToResponse()` — não há `IMapper` em uso.
- DTOs são classes com `{ get; set; }`, não records. Nenhuma Response expõe `EmpresaId`.

Detalhes completos em [CLAUDE.md](CLAUDE.md) e o aprofundamento de domínio em [`docs/contexto-api.md`](../../docs/contexto-api.md). Para o front, [`apps/web/CLAUDE.md`](../web/CLAUDE.md) e [`docs/contexto-web.md`](../../docs/contexto-web.md).
