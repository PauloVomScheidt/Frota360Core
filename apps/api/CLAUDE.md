# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the **backend** of the Frota360 monorepo (`apps/api/`).

API REST .NET 10 de gestão de frotas, multi-tenant por empresa. **Escreva tudo em português**: classes, métodos, DTOs, comentários, logs e mensagens de resposta.

A raiz do repositório tem seu próprio [CLAUDE.md](../../CLAUDE.md), que cobre o que é transversal aos dois apps e vale aqui sem repetição: **navegação de código** (use `codegraph_explore` para perguntas estruturais), **política de documentação** (atualizar contexto/CLAUDE/README após qualquer alteração) e — o mais relevante — o **contrato entre API e front** (envelope de resposta, tipos dos DTOs, matriz de papéis, portas/CORS). Leia antes de qualquer mudança que atravesse a fronteira; este arquivo cobre só o backend.

**Todos os comandos e caminhos abaixo são relativos a `apps/api/`.** O aprofundamento de domínio está em [docs/contexto-api.md](../../docs/contexto-api.md).

## Comandos

```powershell
dotnet build Frota360.slnx
dotnet test
dotnet test --filter "FullyQualifiedName~ManutencaoHandlersTests"   # uma classe
dotnet test --filter "DisplayName~Create_DevePersistir"             # um teste
dotnet run --project src/Api                                        # localhost:5062 → /scalar/v1

dotnet ef migrations add <Nome> --project src/Infrastructure --startup-project src/Api
dotnet ef database update --project src/Infrastructure --startup-project src/Api

dotnet user-secrets set "Jwt:Key" "<32+ caracteres>" --project src/Api
```

Os `appsettings*.json` não são versionados — copie `src/Api/appsettings.example.json` para `appsettings.json` e `appsettings.Development.json` ao clonar. Segredos ficam em user-secrets, nunca no arquivo.

`AddInfrastructure` lança na inicialização se `Jwt:Key` faltar ou tiver menos de 32 caracteres. Em produção: `Jwt__Key`, `Resend__ApiKey`, `Backoffice__ApiKey`, `ConnectionStrings__DefaultConnection` por variável de ambiente.

O `Dockerfile` builda só a API e tem **`apps/api/` como contexto** — os `COPY` são relativos a esta pasta.

## Banco: PostgreSQL

O banco é **PostgreSQL 17** (provider Npgsql) desde 30/08/2026 — `docker compose up -d` na raiz do monorepo sobe o de desenvolvimento. Três regras que decorrem disso e que valem para código novo:

- **Data**: use **`DateTime.Now`** em tudo que é persistido — o sistema grava **hora local de Brasília**, e o front exibe o valor verbatim. A única exceção é o `expires` do JWT em `TokenService`, que é `UtcNow` porque o claim `exp` é epoch UTC por protocolo. Não use `HasColumnType` em `DateTime`: a convenção global no `Frota360DbContext` já mapeia todo `DateTime`/`DateTime?` para `timestamp without time zone` e aplica o `DataSemFusoConverter`, que descarta o `DateTimeKind` na escrita.
- **E-mail**: todo lookup ou gravação de e-mail passa por `EmailNormalizado.De` (`src/Domain/Common`). O PostgreSQL compara texto case-sensitive; sem isso, quem digitar outra caixa não loga. Hashes de token são Base64 e continuam com comparação exata.
- **Default SQL**: use `CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'`, não `GETDATE()` nem `'UTC'` — o container do Postgres roda em UTC. Índice filtrado usa aspas duplas (`"CPF" IS NOT NULL`), não colchetes.

O aprofundamento — inclusive a dívida de fuso deixada para depois — está em [docs/contexto-api.md](../../docs/contexto-api.md) (§ Banco: PostgreSQL).

## Camadas

| Projeto | Pode referenciar | Nunca referencia |
|---|---|---|
| **Frota360.Domain** (`src/Domain`) — entidades, enums, `ApiResponse<T>`, `ResultadoPaginado<T>`, `Roles`, `AcoesAuditoria`/`EntidadesAuditadas`, interfaces de repositório/serviço | nada (sem pacotes) | EF Core, ASP.NET |
| **Frota360.Application** (`src/Application`) — `UseCases/` (CQRS), `Services/`, `DTOs/`, validators | Domain | Infrastructure, ASP.NET, `DbContext` |
| **Frota360.Infrastructure** (`src/Infrastructure`) — `Frota360DbContext`, repositórios, `TokenService`, e-mail, config do JWT | Domain | Application |
| **Frota360.Api** (`src/Api`) — controllers, `ExceptionMiddleware`, `CurrentUserService` | Application + Infrastructure | — |

Os `.csproj` mantêm o prefixo `Frota360.` (`src/Api/Frota360.Api.csproj`); só os diretórios são curtos.

Interface de repositório nova vai em `src/Domain/Interfaces/Repositories`, implementação em `src/Infrastructure/Repositories`, registro em `InfrastructureExtensions.AddInfrastructure`.

### Fluxo de um request

```
Controller (valida com IValidator<T> → 400)
  → dispatcher.SendAsync(new XCommand(request))
  → Dispatcher resolve IRequestHandler<XCommand, TResponse> no DI (reflexão)
  → Handler: lê currentUser.EmpresaId, chama repositório, monta entidade, .ToResponse()
  → Controller embrulha em ApiResponse<T>
```

`AddCqrsHandlers` varre a assembly da Application e registra todo `IRequestHandler<,>` — **handler novo não precisa de registro manual**. Um `IDispatcher` sem handler correspondente falha em runtime, não em compilação.

**Onde colocar código novo:** CRUD de domínio (veículo, motorista, rota, manutenção, tipo de manutenção, abastecimento) → CQRS. Autenticação, convite, usuário e onboarding → serviços em `Application/Services` com interface em `Application/Interfaces`, injetados direto no controller (é o critério atual, não legado).

## Convenções

Estrutura de uma fatia vertical, replique exatamente:

```
UseCases/<Agregado>s/
  Commands/<Acao><Agregado>/<Acao><Agregado>Command.cs   sealed record CreateVeiculoCommand(CreateVeiculoRequest Data) : ICommand<VeiculoResponse>
                            <Acao><Agregado>Handler.cs   sealed class ... : ICommandHandler<Command, TResponse>
  Queries/Get<X>/Get<X>Query.cs                          sealed record GetVeiculoByIdQuery(int Id) : IQuery<VeiculoResponse?>
  Validator/<Acao><Agregado>Validator.cs                 AbstractValidator<CreateVeiculoRequest>
  <Agregado>Mappings.cs                                  static ToResponse() por extensão
```

- Command com payload recebe o DTO inteiro como `Data`; update recebe `(int Id, UpdateXRequest Data)`. Handler começa com `var request = command.Data;`.
- **DTOs**: `DTOs/<Agregado>/Request/{Create,Update}XRequest.cs` e `DTOs/<Agregado>/Response/XResponse.cs` — classes com propriedades `{ get; set; }`, não records. Response nunca expõe `EmpresaId`.
- **Primary constructors em tudo**: handlers, repositórios, controllers, serviços, middleware. Nada de campos `_repository` atribuídos em construtor.
- Handlers são `sealed`, logam início e fim (`logger.LogInformation`), e envolvem o corpo em `try/catch` que loga e faz `throw;`. Em Manutenção e Rota o catch é `catch (Exception ex) when (ex is not InvalidOperationException)`, para não logar como erro o que é violação de regra.
- **O odômetro do veículo só avança.** Dois fluxos o movem — abertura/encerramento de rota e conclusão de manutenção — e ambos comparam antes de gravar: um lançamento retroativo nunca reescreve a ficha com número velho. O abastecimento **não** mexe no odômetro (o apontamento é só o gasto). Ao criar um terceiro caminho, siga a mesma política e invalide `['veiculos']`/`['manutencoes']` do lado do front.
- **Transição de estado é endpoint próprio**, não PUT: `POST /manutencao/{id}/concluir`, `POST /rota/{id}/encerrar`. O request de update não carrega os campos de estado (`Ativo`, `DataFim`, `Status`) — quem os move é a ação dedicada, que também aplica o efeito colateral (avançar o odômetro do veículo, sempre só para frente).
- **Todo handler de escrita registra auditoria.** Injete `IAuditoriaService` e chame `RegistrarAsync(EntidadesAuditadas.X, AcoesAuditoria.Y, id, "frase em português", alteracoes)` ao final do caminho feliz, depois de a persistência ter sucedido — nunca quando o handler devolve `null`/`false` ("não encontrado" não é evento de trilha). O diff sai de `AlteracoesBuilder`, montado **antes** de a entidade ser mutada (ela é alterada in-place; depois o "antes" se perde). **Nunca** coloque hash de senha, refresh token, token de reset ou de convite em `Descricao` ou no diff. Detalhes e a lista dos 23 pontos em [docs/contexto-api.md](../../docs/contexto-api.md) (§9 Auditoria).
- **Cuidado ao projetar uma consulta com `Include`**: o EF Core descarta os `Include` quando o `Select` produz algo que não é a entidade (tipo anônimo incluído), e as navegações voltam nulas em silêncio. Já custou um bug de listagem sem placa em `AbastecimentoRepository`. Se precisar de um dado calculado ao lado da entidade, prefira uma segunda consulta.
- **Mapeamento é manual** via `ToResponse()`. AutoMapper está no csproj mas não é usado em lugar nenhum — não introduza `IMapper`.
- **Controllers**: `[Authorize]` na classe, `[ApiVersion("1.0")]`, rota `api/v{version:apiVersion}/[controller]`, `[Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]` por ação (Admin é o único que exclui; Operador cria/edita/encerra rota). Cada ação declara `[ProducesResponseType<ApiResponse<T>>(...)]` por status. Use `Roles.Gestao` (`Admin,Supervisor,Operador`) para barrar a role `Motorista` — atributos de classe e de ação são combinados por **E**, então não coloque `Roles.Gestao` na classe de um controller que também tenha ação aberta ao motorista.

### ApiResponse e erros

Toda resposta usa `ApiResponse<T>` (`Sucesso`/`Mensagem`/`Dados`/`Erros`), montado **no controller**:

```csharp
var validation = await createValidator.ValidateAsync(request);
if (!validation.IsValid)
    return BadRequest(ApiResponse<object>.Fail("Dados inválidos.",
        validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

var criado = await dispatcher.SendAsync(new CreateVeiculoCommand(request));
return CreatedAtAction(nameof(GetById), new { id = criado.Id },
    ApiResponse<VeiculoResponse>.Ok(criado, "Veículo cadastrado com sucesso."));
```

Não há filtro global de validação — o controller valida explicitamente. Handler **não** monta `ApiResponse`.

Sinalização de falha, do handler para fora:

| Situação | Handler faz | Resultado HTTP |
|---|---|---|
| Registro não encontrado | retorna `null` (ou `false` no delete) | controller devolve `NotFound(ApiResponse<object>.Fail(...))` |
| Regra de negócio violada | `throw new InvalidOperationException("mensagem ao usuário")` | `ExceptionMiddleware` → **422**, com a mensagem da exceção |
| Erro inesperado | deixa propagar | 500 com mensagem genérica |

`ExceptionMiddleware` também mapeia `KeyNotFoundException` → 404, `ArgumentNullException` → 400, `UnauthorizedAccessException` → 401. `OnChallenge`/`OnForbidden` do JwtBearer emitem o mesmo envelope em 401/403. Mensagem de `InvalidOperationException` vai direto ao cliente — escreva-a como texto para o usuário final.

## Isolamento por EmpresaId (regra crítica)

Toda entidade de negócio tem `EmpresaId`; o valor vem **sempre** de `ICurrentUserService.EmpresaId` (claim `empresaId` do JWT) e **nunca** do corpo, rota ou query string.

Ao escrever qualquer acesso a dados novo:

1. Injete `ICurrentUserService` no handler e passe `currentUser.EmpresaId` ao repositório.
2. Assinatura de repositório inclui `empresaId` e filtra por ele: `GetAllAsync(int empresaId)`, `GetByIdAsync(int id, int empresaId)`, `ExisteXAsync(int empresaId, ...)`. Não crie sobrecarga sem `empresaId`.
3. No create, `EmpresaId = currentUser.EmpresaId` na entidade.
4. **Todo id de FK vindo do request é resolvido por `GetByIdAsync(id, currentUser.EmpresaId)` antes de gravar** — assim um id de outra empresa simplesmente "não existe". `CreateManutencaoHandler` é o modelo a copiar; `CreateRotaHandler`/`UpdateRotaHandler` seguem o mesmo padrão desde a RN07. A auditoria completa da regra está em [docs/contexto-api.md](../../docs/contexto-api.md) (§ Auditoria de isolamento por EmpresaId).
5. Índice único no `DbContext` é composto com `EmpresaId` (`(EmpresaId, CPF)`, `(EmpresaId, Nome)`). Exceção: `Usuario.Email` é único global.

Não há query filter global no EF — o filtro é responsabilidade de cada método de repositório.

**Segundo eixo, só para a role `Motorista`:** não existe entidade `Motorista` — o motorista **é** o `Usuario`, e tanto `Rota.CodigoMotorista` quanto `Abastecimento.MotoristaId` são FK para `Usuario`. Onde a role vale, o escopo é empresa **e** o próprio usuário, os dois do token e sem claim extra: `GetAllByMotoristaAsync(empresaId, currentUser.UsuarioId)`, id de motorista do corpo ignorado no create, e registro de outro dono devolvendo `null` (404). Quando o registro guarda **as duas** pessoas — como o abastecimento, que separa `MotoristaId` (de quem é o gasto) de `UsuarioId` (quem digitou) —, o eixo do recorte é o dono, não quem digitou: o motorista enxerga o que a gestão lançou **para** ele. Use `currentUser.EhMotorista()` (`Application/Common/CurrentUserExtensions.cs`). Para resolver um `CodigoMotorista` vindo do request use `IUsuarioRepository.GetMotoristaByIdAsync(id, empresaId)`, que filtra empresa **e** role. Detalhes em [docs/contexto-api.md](../../docs/contexto-api.md) (§ Isolamento multi-tenant).

## Testes

Duas suítes, com papéis distintos — veja [docs/contexto-api.md](../../docs/contexto-api.md) (§ Testes).

**`tests/Frota360.Tests/`** — unitários, espelhando a Application: `UseCases/<Agregado>/<Agregado>HandlersTests.cs`, `<Agregado>MappingsTests.cs`, `<X>ValidatorTests.cs`; `Services/<Servico>Tests.cs`; `Abstractions/DispatcherTests.cs`. Referencia Application e Domain — **não referencia Infrastructure nem a API**. É aqui que vai teste de handler, mapping, validator e service.

**`tests/Frota360.IntegrationTests/`** — o único que referencia Infrastructure. Sobe um `postgres:17` descartável com Testcontainers e roda os repositórios de verdade. É aqui que vai o que só o banco prova: mapeamento de tipo, `DateTimeKind`, collation, índice, tradução de query. Exige Docker no ar. **Mexeu em `Frota360DbContext`, migration ou provider? O teste vai neste projeto.**

Padrão dos unitários: xUnit + NSubstitute, sem banco.

```csharp
private readonly IVeiculoRepository _repository = Substitute.For<IVeiculoRepository>();
private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

public XHandlersTests() => _currentUser.EmpresaId.Returns(1);

private CreateVeiculoHandler CreateHandler() =>
    new(_repository, _currentUser, NullLogger<CreateVeiculoHandler>.Instance);
```

- Logger sempre `NullLogger<T>.Instance`.
- Fábricas privadas estáticas para entidades (`NovoVeiculo(...)`, `NovaManutencao(...)`) com defaults e parâmetros nomeados.
- Nome do teste em português: `Metodo_Cenario_DeveResultado` (ex.: `Create_DevePersistirEscopadoNaEmpresaEMapearResposta`).
- Todo handler novo precisa de um teste que prove o escopo por empresa — asserção sobre o `empresaId` recebido (`_repository.Received(1).GetByIdAsync(1, 1)`) ou `Arg.Is<T>(x => x.EmpresaId == 1)`.
- Handler de escrita carrega `IAuditoriaService` no construtor: as fixtures têm um `_auditoria = Substitute.For<IAuditoriaService>()` ao lado do `_currentUser`. Um teste por fixture basta para provar o registro (`_auditoria.Received(1).RegistrarAsync(EntidadesAuditadas.X, AcoesAuditoria.Y, ...)`), não um por método.
- Regra de negócio: `await Assert.ThrowsAsync<InvalidOperationException>(...)`.