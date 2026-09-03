# Contexto geral da API Frota360

Panorama da aplicação: arquitetura, fluxos de negócio, endpoints e infraestrutura.

## O que é

API REST .NET 10, multi-tenant por empresa, para gestão de frota: veículos, motoristas, rotas, catálogo de tipos de manutenção e manutenções. Clean Architecture em 4 projetos + testes.

| Projeto | Papel |
|---|---|
| `Frota360.Domain` (`apps/api/src/Domain`) | Entidades, enums, `ApiResponse<T>`, `ResultadoPaginado<T>`, `Roles`, `AcoesAuditoria`/`EntidadesAuditadas`, interfaces de repositório/serviço. Zero pacotes. |
| `Frota360.Application` (`apps/api/src/Application`) | CQRS manual (`UseCases/`), `Services/` (auth/convite/usuário/backoffice), DTOs, validators FluentValidation |
| `Frota360.Infrastructure` (`apps/api/src/Infrastructure`) | EF Core + PostgreSQL (Npgsql), repositórios, JWT, e-mail (Resend), migrations |
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

- `GET /abastecimento` → o handler aplica `motoristaId` do token quando quem consulta é motorista, sobrescrevendo o filtro do cliente; `PUT`/`GET /{id}` de lançamento de **outro motorista** devolvem `null` → **404**. `POST` ignora o `MotoristaId` do corpo para essa role e grava o do token, e recusa veículo diferente do da rota aberta (422).

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
Admin cria convite (token aleatório de 64 bytes, **só o hash SHA vai ao banco**, validade 7 dias, convites pendentes anteriores do mesmo e-mail são apagados) → e-mail com link `{Frontend}/convite?token=` → `POST /convite/aceitar` (anônimo) cria o `Usuario` já com role do convite e devolve sessão autenticada — token e refresh token via cookie (ver §3), sem exigir login.

**Convite de motorista não tem nada de especial:** é e-mail + `Role = Motorista`, como qualquer outra role. O aceite (`POST /convite/aceitar`) aceita ainda `CPF` e `DataNascimento` **opcionais**, que a própria pessoa informa. Em branco viram nulo, e não string vazia: o índice único filtrado `(EmpresaId, CPF)` depende disso para não colidir entre quem não informou. Depois do aceite, esses mesmos campos se corrigem em `PUT /usuario/perfil` (§4.2).

### 3. Auth
Login BCrypt → JWT de 1h (claims `sub`, `email`, `name`, `jti`, `empresaId`, `role`) + refresh token de 7 dias rotacionado a cada uso (hash no banco). `esqueci-senha` responde neutro sempre (não revela se o e-mail existe), token de 30 min; redefinir senha **derruba o refresh token**. Todos esses endpoints têm rate limit de 5/min por IP.

**Token e refresh token nunca chegam ao corpo da resposta nem a `localStorage`/`sessionStorage` do front** (mitiga exfiltração por XSS — era o achado do React Doctor `auth-token-in-web-storage`). `Login`, `Refresh` e `Convite.Aceitar` devolvem só `SessaoResponse` (nome/e-mail/role) no JSON; os dois segredos saem em cookie `HttpOnly; Secure; SameSite=None` (`Frota360.Api.Services.SessaoCookies`, nomes em `Domain.Common.CookiesDeSessao`), com validade espelhando a do próprio token (1h) e do refresh (7 dias). `SameSite=None` porque front e API vivem em origens diferentes (portas em dev, domínios em produção) — exige `Secure`, e as duas bindings de dev (7271/5062) e a de produção já falam HTTPS. `JwtBearerEvents.OnMessageReceived` lê o JWT do cookie quando não há header `Authorization` (que continua funcionando para Scalar/curl/clientes externos). `POST /auth/refresh` não recebe mais corpo: o refresh token vem só do cookie, que o navegador anexa sozinho — JavaScript nunca o vê. `Logout` limpa os dois cookies além de revogar o refresh token no banco.

Internamente, `AuthService`/`ConviteService` continuam devolvendo o DTO `AuthResponse` (com `Token`/`RefreshToken`) como sempre — só o `AuthController`/`ConviteController` o convertem para `SessaoResponse` (`AuthResponseMappings.ToSessaoResponse()`) depois de mover os dois segredos para o cookie. Layers e testes de `AuthService`/`ConviteService` não mudaram.

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

### 4.2 Perfil — o próprio cadastro (qualquer autenticado)

`GET /usuario/perfil` e `PUT /usuario/perfil` são as **duas únicas ações do `UsuarioController` que não são Admin** — por isso o `[Authorize(Roles = Admin)]` saiu da classe e foi para cada ação administrativa: atributos de classe e de ação combinam por **E**, e na classe ele barraria o motorista aqui também.

O `PUT` edita **nome, CPF e data de nascimento** do dono do token. Três decisões, todas defensáveis no §6.5 do RFC:

- **O alvo vem do claim `sub`, nunca do corpo.** Não existe `PUT /usuario/{id}` administrativo: o direito de correção (LGPD, Art. 18, III) é do titular, e um Admin editando dado pessoal alheio abriria uma superfície que precisaria de justificativa própria. É autoatendimento ou nada.
- **O e-mail fica de fora.** É a chave de login e mexeria em convite, refresh token e no índice único global de `Usuario.Email`. Trocar e-mail seria outro caso de uso, com reverificação.
- **CPF em branco grava `null`.** Colisão com outro usuário da mesma empresa vira 422 antes de o índice único estourar (`IUsuarioRepository.ExisteCpfNaEmpresaAsync`).

O `GET` existe porque `GET /usuario` é Admin-only: sem ele um Motorista não teria **nenhum** caminho para ler os próprios dados, e a tela de correção abriria em branco — a "correção" viraria sobrescrita cega.

A sessão **não** é revogada (diferente da troca de papel): mudar o próprio nome não amplia nem reduz acesso. O efeito colateral é que o claim `name` do token — e portanto o nome desnormalizado nas linhas de auditoria seguintes — só reflete o novo valor no próximo refresh.

### 4.3 Veículo — placa, exclusão e "em rota"

- **`EmRota` é derivado na leitura**, como `Atrasada` na manutenção: existe rota com aquele veículo em que `Ativo && DataFim is null`. **Não é coluna** — o estado vive na tabela `Rota`, e persistir uma cópia daria um "em rota" envelhecido na primeira rota encerrada fora do fluxo. `VeiculoMappings.ToResponse(this Veiculo v, bool emRota)` recebe o valor por **parâmetro obrigatório**, sem default: o dado vem do lado oposto da FK (`Veiculo` não tem coleção de rotas), e um `false` implícito num handler novo passaria despercebido, mostrando "Disponível" para um carro na estrada.
- Dois métodos alimentam isso, ambos filtrando `Ativo && DataFim == null`: `IRotaRepository.GetVeiculosEmRotaAsync(empresaId)` — uma consulta para a listagem inteira, evitando N+1 — e `ExisteRotaAtivaComVeiculoAsync(empresaId, veiculoId)` para leitura e correção de um registro. ⚠️ **Não confunda com `ExisteComVeiculoAsync`**, que ignora o estado da rota de propósito: é a guarda da RN08, onde uma rota encerrada continua sendo histórico que aponta para o veículo.
- O índice `(EmpresaId, Ativo, CodigoVeiculo)` na tabela `Rota` existe por causa dessa consulta, que roda em toda listagem de veículos. Ele substitui o índice de FK `(EmpresaId)` que o EF criava sozinho — o composto o cobre como prefixo.
- **Nada impede duas rotas ativas no mesmo veículo**: `CreateRotaHandler` valida motorista, veículo e `KmInicial`, mas não checa se o carro já está rodando. Por isso `EmRota` é um booleano de "existe alguma", nunca um vínculo 1-1.


- **Placa (RN09).** Formato Mercosul (`ABC1D23`) ou antigo (`ABC1234`), nos dois validators. A comparação é **case-insensitive** e o handler grava sempre em maiúsculas (`Trim().ToUpperInvariant()`): a RN09 é regra de formato, não de caixa — recusar `abc1d23` com 422 puniria um cliente da API por algo que só o front normalizava. No update, a normalização acontece **antes** do `AlteracoesBuilder`, senão reenviar a mesma placa em caixa diferente entraria no diff como alteração fantasma.
- **Exclusão (RN08).** Veículo com rota associada não é excluído: 422 com *"Não é possível excluir um veículo com rotas associadas. Encerre ou remova as rotas antes."* — `DeleteVeiculoHandler` consulta `IRotaRepository.ExisteComVeiculoAsync`, mesmo desenho de `DeleteTipoManutencaoHandler`. A rota guarda o histórico de quilometragem da frota e ficaria apontando para um registro inexistente.
- **A metade "motorista" da RN08 não se aplica ao modelo atual**: não há DELETE de motorista, e a FK `Restrict` de `Rota.CodigoMotorista` → `Usuario` já garante o mesmo efeito pelo banco. Usuário é desativado, nunca excluído.

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

### 7.1 Filtro de período na manutenção

`GET /manutencao?de=&ate=` recorta pela **data relevante do status**, não por um campo fixo: uma pendência é situada pela `DataPrevista`, uma manutenção feita pela `DataRealizacao`. Escolher uma só das duas deixaria metade da tela fora do filtro — só pendências ou só concluídas.

No repositório as duas pernas do OR são escritas explicitamente, em vez de um ternário, porque `Status` tem conversão para texto e a comparação simples é a que o EF traduz sem surpresa.

Duas consequências assumidas: `ate` é **inclusivo** (estende até o fim do dia), e **pendência agendada só por km, sem `DataPrevista`, não aparece enquanto houver período** — ela não está em data nenhuma. A tela avisa isso quando o filtro está ativo. Intervalo invertido é `InvalidOperationException` → 422, não lista vazia silenciosa.

### 8. Encerrar rota alimenta a ficha do veículo

Além do odômetro, o encerramento grava `Veiculo.UltimoMotorista` e `Veiculo.DataUltimaViagem` — que antes **só eram preenchidos à mão** pelo CRUD de veículo, apesar de a documentação afirmar o contrário. Os três campos seguem a mesma política de "só avança": encerrar hoje uma rota de mês passado não reescreve a ficha com dado velho (`AtualizarVeiculoAsync` compara `DataUltimaViagem` antes de gravar). O odômetro é independente da data — ele avança sempre que o km final for maior.

### 8.1 Abastecimento — o gasto com combustível

**O apontamento é curto de propósito.** A primeira versão pedia litros e odômetro além de veículo, valor e data, para calcular consumo — precisão que não se paga no posto e que fazia o lançamento ser evitado. O contrato hoje é **veículo, motorista, valor, data e observação**: o dado entra sempre, e serve de base para relatório de **gasto** por veículo, por motorista, por rota e por período.

**O abastecimento não mexe no odômetro.** São **dois** os fluxos que o avançam — abertura/encerramento de rota e conclusão de manutenção. Sem litros nem odômetro no apontamento, consumo (km/l) e preço por litro continuam impossíveis, e a `AbastecimentoResponse` não tem campo derivado nenhum. Custo por km, esse sim, é calculável — mas fora daqui, cruzando com `Rota.KmPercorrido` (§ 8.2).

**Duas pessoas por lançamento, e elas podem ser diferentes:**

| Coluna | Significado |
|---|---|
| `MotoristaId` | **de quem é o gasto** — é o segundo eixo de isolamento e o eixo do relatório por pessoa |
| `UsuarioId` | **quem digitou** — vem sempre do token; a gestão lança em nome do motorista |

A gestão escolhe o motorista, e ele é **obrigatório**: sem ele o handler recusa com 422 `"Informe o motorista do abastecimento."`. O id é resolvido por `IUsuarioRepository.GetMotoristaByIdAsync(id, empresaId)`, que filtra **empresa e role** — usuário de outra empresa ou que não é motorista cai no mesmo `"Motorista {id} não encontrado."`.

**Segundo eixo, como em `/rota/minhas`:** a role `Motorista` vê e corrige só o que é dela — **inclusive o que a gestão lançou para ela**, já que o recorte é por `MotoristaId` e não por quem digitou. O recorte sai do token dentro do handler (`GetAllAbastecimentosHandler`), nunca de parâmetro do cliente: um `motoristaId` na query string é sobrescrito para a role Motorista, e serve só à gestão. Lançamento de outro motorista devolve `null` → **404**, não 403.

**No create, para a role Motorista, o `MotoristaId` do corpo é ignorado** em favor do usuário do token — mesma lógica do `CreateRotaHandler`: o cliente não escolhe de quem é o registro.

**Trava de veículo por rota aberta (RN11).** `CreateAbastecimentoHandler.ResolverRotaAsync` busca a rota aberta do motorista (`Ativo && DataFim is null` sobre `GetAllByMotoristaAsync` — "ativa" não é estado persistido, é essa derivação) e:

- **motorista com rota aberta em outro veículo** → 422 `"Você está em rota com outro veículo. Lance o abastecimento no veículo da sua rota aberta."` A trava vale só para quem está dirigindo; a gestão pode lançar por ele em qualquer veículo (troca, apoio).
- **veículo bate com o da rota aberta** (motorista ou gestão) → `RotaId` vinculado.
- **sem rota aberta** → `RotaId` nulo e qualquer veículo da empresa é aceito.

`RotaId` **saiu do contrato de entrada**: é sempre derivado no servidor. A FK continua `SetNull` — excluir a rota não pode levar junto o gasto, que aconteceu de verdade.

Escrita é aberta a **todos os papéis** (quem abastece na estrada é o motorista, no pátio é o operador); exclusão é só Admin, como no resto. Veículo, motorista e rota não entram no `PUT` — só valor, data e observação: trocar qualquer um dos três reatribuiria o gasto. Nesse caso, exclua e lance de novo.

**RN08 estendida:** veículo com abastecimento lançado não pode ser excluído (a FK é `Restrict`, então sem a guarda o usuário veria 500 em vez da explicação).

### 8.2 Custos — a visão consolidada (01/09/2026)

O custo da frota nasceu espalhado em duas telas que não conversavam: `/abastecimentos` somava no cliente, `/manutencoes` nem isso. `GET /custo` e `GET /custo/resumo` unem as duas origens numa visão só, filtrável por veículo, motorista, origem e período.

**Não há tabela de custos, e isso é a decisão central.** O custo são duas colunas — `Abastecimento.Valor` (`numeric(10,2)`, NOT NULL) e `Manutencao.Custo` (`numeric(10,2)`, nulável, só na conclusão). Uma tabela de custos seria um **espelho**: o valor continua editável em `PUT /abastecimento/{id}` e em `POST /manutencao/{id}/concluir`, e o espelho precisaria de sincronia em create/update/delete/concluir — cada bug de sincronia virando número errado no relatório, em silêncio. A justificativa de performance também não existe neste volume. O desenho é um **read model**: as origens são unidas na leitura, e corrigir o valor na tela de origem já corrige o relatório.

O DTO carrega um discriminador `origem` desde o primeiro dia justamente para que uma origem nova entre sem mexer no contrato — ver **Evolução prevista** no fim desta seção.

**`ICustoRepository` é um repositório de read model**, e por isso atravessa três tabelas (`Abastecimento`, `Manutencao` e `Rota`). É exceção consciente ao "um repositório por agregado" do resto do projeto; nada nele escreve.

**O que conta como custo:**

| Origem | Fonte | Data | Categoria |
|---|---|---|---|
| `Abastecimento` | `Valor` | `DataAbastecimento` | literal `"Combustível"` |
| `Manutencao` | `Custo` — **só** com `Status == Realizada` e `Custo`/`DataRealizacao` preenchidos | `DataRealizacao` | nome do `TipoManutencao` |

**Três armadilhas do domínio que a API expõe explicitamente**, porque sem elas o número mente:

- **Manutenção não é atribuída a motorista.** Filtrar por `motoristaId` descarta a perna de manutenção inteira — deduzi-la pela rota do veículo seria um chute. O recorte impossível (`motoristaId` + `origem=Manutencao`) devolve lista vazia, não erro.
- **`Custo` é opcional ao concluir manutenção.** Concluída sem valor informado, ela fica fora de toda soma. Daí `ManutencoesSemCustoInformado` no resumo: a tela mostra a contagem para o total não mentir por omissão.
- **Rota aberta não tem `KmPercorrido`.** O período corrente subestima o km e, portanto, **superestima** o R$/km.

**Custo por km** sai de `Rota.KmPercorrido` das rotas **encerradas** no período, recortadas por `DataFim` (o momento em que a quilometragem foi apurada — mesmo critério do KPI do dashboard). É nulo quando o km é zero: sem denominador não existe métrica, e devolver zero afirmaria que a frota rodou de graça. A origem do custo é ignorada nessa soma — o km rodado é o mesmo, seja qual for o gasto dividido por ele.

O resumo por veículo inclui **veículo que rodou sem custo lançado**, com total zero. É o caso que mais merece ser visto (ninguém lançou o abastecimento), e mantê-lo faz as colunas fecharem com os totais gerais.

**A união é feita em memória, não com `Concat`.** A primeira implementação usava `UNION ALL`; o EF Core não traduz operação de conjunto depois de uma projeção com constantes — e a origem e a categoria são literais —, nem ordena por elas. `TraducaoDeConsultaTests` provou os dois erros contra o banco de verdade e é onde descobrir se um dia mudar. O custo disso é limitado e não é "trazer tudo": nenhuma linha além da `pagina × tamanhoPagina`-ésima de cada origem pode entrar na página pedida, então cada consulta lê no máximo isso — e o validator ainda limita a página a 100. Os agregados (`SomarPorVeiculoAsync`, `SomarPorMesAsync`) rodam **um `GroupBy` por tabela**, no banco; quem pivota as origens em colunas é o handler, onde a divisão do R$/km também mora — é lá que está o zero no denominador.

**A ordenação desempata por origem antes do id.** Ids de tabelas diferentes colidem; sem isso a ordem é instável e a paginação repete linhas entre páginas.

**Esta é a primeira agregação servida pela API.** Até aqui não havia um `Sum`/`GroupBy` em `apps/api/src` — todo KPI do dashboard é `reduce` sobre listas inteiras baixadas do servidor. As telas de relatório futuras saem daqui.

**`Roles.Gestao` no controller** barra o Motorista na porta, e é por isso que os handlers **não** replicam a regra de `ManutencaoVisibilidade.SemCustoParaMotorista`. Se um dia a tela abrir para a role Motorista, o recorte tem que voltar para dentro dos handlers — um total de frota vazaria sem ele.

**Índice:** `(EmpresaId, DataRealizacao)` filtrado em `"DataRealizacao" IS NOT NULL` (migration `IndiceDeCustoPorPeriodo`). O índice que já existia em `Manutencao` não tem data, e pendência é a maioria das linhas.

#### Evolução prevista: despesas avulsas (`Despesa`)

**O gatilho** é o primeiro custo que **não tem tela própria** — pedágio, multa, IPVA, seguro, licenciamento, pneu, lavagem. Aí a tabela não é espelho de nada: é fonte de verdade de um terceiro tipo de lançamento, e passa a ser a decisão certa.

```
TipoDespesa   Id, EmpresaId, Nome (100), Ativo, DataInclusao
              índice único (EmpresaId, Nome) — molde de TipoManutencao

Despesa       Id, EmpresaId
              TipoDespesaId : int
              VeiculoId     : int?   nulo = custo da frota (seguro do grupo)
              MotoristaId   : int?   nulo = não atribuível a pessoa (IPVA); multa tem dono
              RotaId        : int?   derivado no servidor, como em Abastecimento
              Valor         : decimal(10,2)
              DataDespesa   : DateTime
              Observacao    : string? (500)
              DataInclusao
              índices (EmpresaId, VeiculoId, DataDespesa) e (EmpresaId, MotoristaId, DataDespesa)
```

**O que o desenho atual já garante:** `OrigemCusto` ganha `Despesa = 2`, o repositório ganha uma terceira perna na união com `Categoria = TipoDespesa.Nome`, e **`LancamentoCustoResponse` não muda**. No front, só a união `OrigemCusto` ganha `'Despesa'` e o select de origem ganha uma opção — a tela de custos e os relatórios absorvem a origem nova sem reescrita. É exatamente o que o discriminador compra.

**O que não vem de graça:**

- despesa com `VeiculoId` nulo não cabe no resumo por veículo — ou vira linha "Sem veículo", ou fica só no total geral;
- `Despesa` é escrita, então precisa de CRUD, validators, tela e **auditoria**: `EntidadesAuditadas.Despesa` + `Criou`/`Atualizou`/`Excluiu`, com `AlteracoesBuilder` montado antes da mutação;
- RN08 (exclusão de veículo) hoje barra por rota e abastecimento — teria que considerar despesa também.

### 9. Auditoria — quem alterou o quê

Tabela `LogAuditoria`, **append-only**: só insert e select. Não existe endpoint de update nem de delete, nem para o Admin, e o repositório também não os expõe.

**Escopo.** Administração (usuário, convite) **e** domínio (veículo, rota, manutenção, tipo de manutenção). **Login/logout ficam de fora** de propósito: seria o maior volume da tabela e o menor valor — continuam só no Serilog.

**Como é capturado.** Explicitamente, no handler/serviço, via `IAuditoriaService.RegistrarAsync` chamado ao final do caminho feliz — 20 pontos, listados abaixo. Não é um interceptor do EF: o objetivo é registrar a **intenção de negócio** ("Encerrou a rota"), que um `UPDATE Rota` não expressa, e evitar o ruído de `RefreshTokenHash` mudando a cada login.

**Modelo.** `Entidade` + `Acao` (`EntidadesAuditadas` / `AcoesAuditoria`, em `Domain/Common`) são dois eixos de filtro com poucos valores distintos, no lugar de uma constante por evento. `Descricao` é uma frase pronta em português, montada no servidor. `Alteracoes` é o diff em JSON (`[{campo, de, para}]`), nulo em criação e exclusão.

Nome, e-mail e papel de quem agiu ficam **desnormalizados** na linha — o log é histórico e não pode mudar de sentido quando a pessoa é renomeada ou rebaixada. Saem das claims `name`/`email` do JWT, que já existiam: nenhuma ida a mais ao banco por operação auditada.

**Duas garantias de desenho:**

1. **Auditoria não derruba negócio.** `AuditoriaService` roda depois de o repositório já ter feito `SaveChangesAsync`, portanto fora daquela transação, e engole a exceção em log de erro. Perder uma linha de trilha é ruim; devolver 500 numa edição que já foi persistida é pior.
2. **Nada de segredo no diff.** Ele é montado à mão, campo a campo, por `AlteracoesBuilder` — chamado **antes** de a entidade ser mutada, já que os handlers a alteram in-place. Hash de senha, refresh token, token de reset e de convite nunca entram.

**Os 23 pontos de registro:**

| Origem | Entidade · Ação | Diff |
|---|---|---|
| `Create/Update/DeleteVeiculoHandler` | Veiculo · Criou/Atualizou/Excluiu | ✅ no update |
| `CreateRotaHandler` | Rota · Criou | ✅ quando a abertura avança o odômetro |
| `UpdateRotaHandler` | Rota · Atualizou | ✅ (motorista e veículo pelo nome, não pelo id) |
| `EncerrarRotaHandler` | Rota · Encerrou | ✅ km final + avanço do odômetro |
| `DeleteRotaHandler` | Rota · Excluiu | — |
| `Create/Update/DeleteManutencaoHandler` | Manutencao · Criou/Atualizou/Excluiu | ✅ no update |
| `ConcluirManutencaoHandler` | Manutencao · Concluiu | ✅ status, km, custo, odômetro |
| `Create/Update/DeleteAbastecimentoHandler` | Abastecimento · Criou/Atualizou/Excluiu | ✅ no update (valor, data, observação) |
| `Create/Update/DeleteTipoManutencaoHandler` | TipoManutencao · Criou/Atualizou/Excluiu | ✅ no update |
| `UsuarioService.AtualizarPerfilAsync` | Usuario · Atualizou | ✅ nome, CPF e nascimento — o ator é também o objeto |
| `UsuarioService.AlterarRoleAsync` | Usuario · AlterouPermissao | ✅ papel anterior → novo |
| `UsuarioService.DefinirAtivoAsync` | Usuario · Ativou/Desativou | — |
| `ConviteService.CriarParaEmpresaAsync` / `CancelarAsync` | Convite · Criou/Cancelou | — |
| `ConviteService.AceitarAsync` | Convite · Aceitou | — |

Dois casos fogem do padrão:

- **Provisionamento pelo backoffice** chega em `CriarParaEmpresaAsync` com `criadoPorUsuarioId: null` — sem sessão, sem ator. Ali **não registra**; fica no Serilog. Vindo de `CriarAsync` (Admin logado), registra normal.
- **Aceite de convite** é anônimo e o usuário nasce na própria operação. Usa `RegistrarComoAsync(empresaId, ator, ...)`, que recebe o ator à mão — é o único evento em que o ator é também o objeto.

**Consulta.** `GET /auditoria` (Admin), via query CQRS `GetLogsAuditoriaQuery`, filtrando por entidade, ação, usuário e período. Três índices, um por consulta real: `(EmpresaId, DataHora)` para a listagem, `(EmpresaId, Entidade, EntidadeId)` para o histórico de um registro e `(EmpresaId, UsuarioId, DataHora)` para o de uma pessoa.

**Retenção: 12 meses**, contados a partir de `DataHora`. O prazo é um ciclo de auditoria anual — tempo suficiente para investigar qualquer incidente do exercício e curto o bastante para satisfazer o princípio da necessidade da LGPD (Art. 6º, III), que pesa aqui porque a linha guarda dado pessoal: nome, e-mail, papel **e IP de origem** do ator.

**A purga ainda não está implementada** — hoje nada apaga linha nenhuma. É trabalho futuro consciente, não esquecimento: a rotina cabe sem mexer no schema (`Id` é `long`, os três índices suportam o crescimento) e sem tocar no caráter append-only, porque expurgo por prazo é operação de manutenção, não endpoint. Até ela existir, a política está declarada e a limitação, registrada.

### Paginação — `ResultadoPaginado<T>`

`/auditoria` e `/custo` são os **dois endpoints paginados** do sistema; as demais listas ainda vêm inteiras. `ResultadoPaginado<T>` (`Domain/Common`) traz `Itens`, `Pagina`, `TamanhoPagina`, `Total` e `TotalPaginas`, e vai **dentro** de `ApiResponse<T>.Dados` — o envelope não muda: `ApiResponse<ResultadoPaginado<LogAuditoriaResponse>>`.

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
| GET/PUT | `/usuario/perfil` — o próprio cadastro (nome, CPF, nascimento); alvo pelo `sub` do token | **qualquer autenticado** (inclui Motorista) |
| GET | `/auditoria?pagina=&tamanhoPagina=&entidade=&acao=&usuarioId=&de=&ate=` — paginado; somente leitura | Admin |
| GET | `/custo?pagina=&tamanhoPagina=&veiculoId=&motoristaId=&origem=&de=&ate=` — paginado; read model das duas origens, sem tabela própria | Admin, Supervisor, Operador |
| GET | `/custo/resumo?veiculoId=&motoristaId=&origem=&de=&ate=` — **a única agregação da API**: totais por origem, por veículo (com R$/km) e por mês | Admin, Supervisor, Operador |
| GET | `/veiculo` (+ `/{id}`) | qualquer autenticado (**inclui Motorista**) — a resposta traz `emRota` derivado |
| GET | `/manutencao?veiculoId=&status=&de=&ate=` (+ `/{id}`) | qualquer autenticado (**inclui Motorista**, sem `custo`) |
| GET/POST/PUT | `/abastecimento?veiculoId=&motoristaId=&de=&ate=` (+ `/{id}`) | qualquer autenticado — o Motorista só alcança o que é dele, e `motoristaId` é ignorado para ele |
| GET | `/motorista` (+ `/{id}`) — **somente leitura**: os usuários com a role Motorista | Admin, Supervisor, Operador |
| GET | `/rota`, `/tipomanutencao?apenasAtivos=` (+ `/{id}`) | Admin, Supervisor, Operador |
| GET | `/rota/minhas` | **Motorista** (rotas do próprio, pelo `sub` do token) |
| POST/PUT | `/veiculo`, `/tipomanutencao`, `/manutencao`, `/manutencao/{id}/concluir` | Admin, Supervisor |
| PUT | `/rota/{id}` | Admin, Supervisor, Operador |
| POST | `/rota`, `/rota/{id}/encerrar` | qualquer autenticado — o Motorista só alcança as próprias rotas |
| DELETE | `/{qualquer}/{id}` (não há DELETE de motorista) — `/veiculo/{id}` responde 422 se houver rota associada (RN08) | **Admin** (único que exclui) |
| GET | `/health`, `/health/detail`, `/scalar/v1` | aberto |

## Infra e config

Serilog (console + arquivo diário, 7 dias), rate limit global 200/min por IP + política `auth` 5/min, CORS por `Cors:AllowedOrigins`, health check do DbContext, Scalar em `/scalar/v1` com Bearer declarado. `AddInfrastructure` **derruba a inicialização** se `Jwt:Key` faltar ou tiver menos de 32 caracteres. Sem `Resend:ApiKey`, o e-mail cai no `LogEmailService` (imprime no console) — prático em dev.

### Banco: PostgreSQL (migração de 30/08/2026)

O banco era SQL Server e passou a ser **PostgreSQL 17** (provider `Npgsql.EntityFrameworkCore.PostgreSQL`), por custo e portabilidade: o Express tem teto de 10 GB, exige 2 GB de RAM e não roda em arm64, o que impedia uma instância Graviton menor na AWS. Não havia banco em produção, então as 12 migrations do SQL Server foram **apagadas e regeradas** como uma `Initial` única — o histórico antigo vive só no git. `docker compose up -d` na raiz sobe o banco de desenvolvimento.

Três acoplamentos precisaram de decisão, e os três valem para quem for mexer aqui:

**1. Fuso — hora local de Brasília, gravada sem fuso.** O Npgsql é estrito nos dois sentidos: `timestamptz` recusa `DateTimeKind.Unspecified` e `timestamp without time zone` recusa `Utc`, lançando exceção em vez de converter. O sistema grava **dois Kinds diferentes na mesma coluna** — `DateTime.Now` (`Local`) em `DataInclusao`/`DataHora`/`ExpiraEm`, e o `"aaaa-MM-dd"` que o front manda (desserializado como `Unspecified`) em `DataAbastecimento`/`DataInicio`/`DataPrevista`. `EncerrarRotaHandler` mistura os dois na mesma propriedade (`request.DataFim ?? DateTime.Now`), então nenhuma escolha de tipo de coluna resolve sozinha.

A solução é o `DataSemFusoConverter` (`src/Infrastructure/Data/`), aplicado a **todo** `DateTime`/`DateTime?` via `ConfigureConventions` no `Frota360DbContext`: ele descarta o Kind na escrita e mapeia tudo para `timestamp without time zone`. A leitura devolve `Unspecified`, então a API serializa as datas **sem sufixo `Z`** e o front as exibe verbatim — é por isso que as telas mostram o horário certo sem nenhuma conversão no cliente.

> **O fuso do processo faz parte da semântica dos dados.** Como tudo é `DateTime.Now`, o mesmo código grava BRT numa máquina brasileira e **UTC num container sem `TZ`** — sem erro nenhum, só 3 h de diferença. Por isso o `Dockerfile` fixa `ENV TZ=America/Sao_Paulo` (a imagem `aspnet:10.0` já traz o `tzdata`) e o `Program.cs` **registra o fuso efetivo na segunda linha do log**. Se aparecer `UTC` ali, o deploy está errado.

**A única data em UTC** é o `expires` do JWT (`TokenService`), porque o claim `exp` é epoch UTC por definição do protocolo — é um instante que sai do sistema num formato UTC-nativo. Os demais campos de expiração (`RefreshTokenExpiraEm`, `ResetSenhaExpiraEm`, `Convite.ExpiraEm`) usam `Now`: são gravados e comparados com o mesmo relógio, e mudá-los reintroduziria semântica misturada no banco.

Os defaults SQL das nove colunas de data usam `CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'`, e não `'UTC'`, pelo mesmo motivo — o container do Postgres roda em UTC, então `LOCALTIMESTAMP` ali **não** seria hora de Brasília. Na prática o default nunca dispara (todo insert seta a data no C#); manter a expressão coerente evita que ele se torne uma segunda semântica de fuso escondida.

**2. Collation — e-mail é normalizado no código.** A collation padrão do SQL Server era case-insensitive e fazia `Email == email` casar `Fulano@X.com` com `fulano@x.com` por acidente, sem nada no código pedir isso. O PostgreSQL compara texto de forma case-sensitive, então a garantia sumiria em silêncio: quem digitasse outra caixa não logaria, e dois cadastros "iguais" passariam pelo índice único. `EmailNormalizado.De` (`src/Domain/Common/`) reduz o e-mail à forma canônica, aplicado na escrita (`ConviteService.CriarParaEmpresaAsync`, único ponto de entrada — o `Usuario` nasce copiando `convite.Email`) e na leitura (`UsuarioRepository.GetByEmailAsync`/`ExisteEmailAsync`, `ConviteRepository.GetPendentesByEmailAsync`). A normalização mora no código, e não em `citext` ou numa collation da coluna, para a regra ficar explícita e independente de fornecedor. **Todo lookup novo por e-mail passa por ela.** Os hashes de token são Base64 e seguem case-sensitive de verdade — comparação exata é o correto para eles.

**3. Defaults e índices.** Os nove `HasDefaultValueSql("GETDATE()")` viraram `CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'` (ver o item 1). Os índices filtrados passaram de `[CPF] IS NOT NULL` para `"CPF" IS NOT NULL` (aspas duplas).

**Dívida de fuso que sobra.** A política de hora local fechou os dois bugs de exibição de 3 h que existiam em `ConvitesPage.tsx` e `AuditoriaPage.tsx`. Continuam abertos:

- Alguns validators (`CreateVeiculoValidator`, `UpdateVeiculoValidator`, `AceitarConviteValidator`, `AtualizarPerfilValidator`) avaliam `DateTime.Now`/`Today` na **construção** e não numa lambda, congelando o "agora" pelo tempo de vida da instância no DI.
- `UpdateManutencaoValidator` não valida `DataPrevista`, embora o `Create` valide.
- Hora local **não serve multi-região**. Se um dia houver cliente fora do Brasil, o caminho é separar instantes reais (→ `timestamptz` em UTC) de datas de calendário (→ `DateOnly`/`date`), o que atinge entidades, DTOs, validators e `types.ts`. Hoje o recorte é uma transportadora brasileira, e o Brasil não tem horário de verão desde 2019 — não há transição a tratar.

### Deploy: EC2 única com Docker Compose

Três serviços em `docker-compose.prod.yml`: `db` (postgres:17), `api` e `caddy`. O Caddy faz
proxy reverso e termina o TLS com certificado automático do Let's Encrypt, substituindo um ALB.
**Só as portas 80 e 443 são publicadas no host** — nem o banco nem a API expõem porta, e a rede
`interna` tem sub-rede fixa (`172.28.0.0/16`).

O roteiro de subida e as variáveis obrigatórias estão em [deploy.md](deploy.md); o modelo de
configuração, em `.env.example` na raiz.

**Quatro decisões que não são óbvias e têm motivo:**

**1. A sub-rede é fixa porque o `ForwardedHeaders` depende dela.** Atrás do proxy, toda
requisição chega com o IP do Caddy. Sem tratar os `X-Forwarded-*`, `LogAuditoria.IpOrigem`
gravaria sempre o mesmo IP, o rate limiter transformaria o teto de 5/min da política `auth` num
limite **compartilhado por todos os usuários**, e o `Location` das respostas 201 devolveria a
URL interna do Docker. O `Program.cs` limpa `KnownNetworks`/`KnownProxies` — cujos padrões são
só loopback, e por isso ignorariam o Caddy em silêncio — e declara a sub-rede vinda de
`ProxyReverso__RedeConfiavel`. **Mudou a sub-rede no compose? Mude a variável junto.**
A trava contra forja é dupla: o middleware só aceita os headers de `KnownNetworks`, e a porta
8080 nunca é publicada, então o proxy é o único caminho até a API.

**2. Log só no console, com rotação no compose.** O sink de arquivo do Serilog fica desativado
em Production ([Program.cs](../apps/api/src/Api/Program.cs)) por dois motivos: em container o
arquivo se perde a cada deploy, e sem escrita em disco a API pode rodar como o usuário `app`,
sem privilégio. O log vai para o stdout, e **os três serviços declaram `max-size`/`max-file`** —
o driver `json-file` do Docker não tem limite por padrão, e log sem teto enche o disco da
instância, derrubando tudo.

**3. Migrations no boot** (`MigracaoDeBanco`), só em Production e Staging, com retry de até 10
tentativas e backoff. O `depends_on: service_healthy` cobre apenas a primeira subida; se o
Postgres reiniciar depois, é o retry que evita o loop de reinício. A limitação é real e está
documentada na própria classe: aplicação automática, sem revisão humana nem rollback.

**4. `caddy_data` precisa ser persistido.** É onde ficam os certificados. Sem o volume, cada
redeploy pede certificado novo e o Let's Encrypt corta em 5 emissões duplicadas por semana.

### Dívida técnica

**Backup do Postgres — não existe. É o maior risco isolado do deploy.** O banco roda em
container numa instância única, com volume EBS e nenhum dump, snapshot ou réplica. Corrupção do
volume ou um `DELETE` errado são irreversíveis. O caminho decidido é `pg_dump` diário
comprimido para um bucket S3 com lifecycle, mais o roteiro de restauração **testado ao menos
uma vez** — restore que nunca foi exercitado não é backup. Precisa estar resolvido **antes do
primeiro dado real do cliente**, não antes da defesa.

Outras pendências conhecidas:

- **Sem CI.** Não há `.github/workflows`; build, push e `docker compose up` são manuais por SSH.
- **Segredos em arquivo.** O `.env` na instância é o caminho da primeira fase; a migração para o
  AWS SSM Parameter Store não muda o compose — basta um script de boot que gere o mesmo arquivo.
- **Rate limiter em memória.** Zera a cada redeploy e não é compartilhado entre réplicas. Não é
  problema com uma instância só.
- Validators que congelam o "agora" na construção, e `UpdateManutencaoValidator` sem validação
  de `DataPrevista` (ver a dívida de fuso acima).

## Testes

Duas suítes, com papéis distintos.

### `tests/Frota360.Tests` — unitários, sem banco

xUnit + NSubstitute. Referencia apenas Application e Domain, então cobre handlers, mappings, validators e services com repositório mockado. É a suíte rápida: roda em ~1 s e não precisa de Docker.

O recorte que isso impõe: `VeiculoValidatorTests` cobre o **formato** da placa (RN09) e `VeiculoHandlersTests` cobre a **normalização** e a recusa da RN08, mas as garantias que moram no banco não aparecem aqui — a checagem em `UsuarioService`/`DeleteVeiculoHandler` é o que se testa, e ela existe justamente para transformar a violação em 422 com mensagem antes de o banco estourar.

### `tests/Frota360.IntegrationTests` — contra PostgreSQL de verdade

O único projeto que referencia Infrastructure. Sobe um `postgres:17` descartável por execução via **Testcontainers**, aplica as migrations com `MigrateAsync` (e não `EnsureCreated`, que pularia justamente o artefato sob teste) e roda os repositórios reais. Não usa o container de desenvolvimento nem os appsettings, então rodar os testes nunca mexe nos dados de quem está desenvolvendo.

Existe porque `dotnet test` da suíte unitária **não prova nada** sobre provider, mapeamento de tipo, `DateTimeKind`, collation, índice filtrado ou tradução de query — tudo o que a migração para PostgreSQL mexeu. Cobre:

| Arquivo | O que garante |
|---|---|
| `PoliticaDeDataTests` | os três Kinds (`Local` do `DateTime.Now`, `Unspecified` do front, `Utc` como regressão) gravam na mesma coluna; o relógio de parede sobrevive ao round-trip e volta `Unspecified`; a coluna é mesmo `timestamp without time zone` |
| `SchemaERestricoesTests` | as migrations criam as 9 tabelas; lookup de e-mail ignora caixa; índice filtrado `(EmpresaId, CPF)` aceita vários nulos, barra duplicata e permite o mesmo CPF em empresas diferentes; `Usuario.Email` é único **global**; `numeric(10,2)` preserva centavo |
| `TraducaoDeConsultaTests` | filtro `de`/`ate` inclui o próprio dia final; `Status` persiste como texto e volta como enum; o `OrderBy` com condicional traz pendentes primeiro apesar do texto; `GetAllAsync` recorta pela empresa. **Custos:** as duas origens viram uma lista só, a paginação não repete linha entre elas, o recorte por motorista descarta manutenção, o `GroupBy` com navegação na chave traz nome e placa, e as cinco consultas do read model recortam pela empresa |

Dois detalhes de infraestrutura, ambos comentados no código: o **resource reaper do Testcontainers fica desligado** (no Docker Desktop em Windows ele falha ao baixar e derruba a suíte antes do primeiro teste — a fixture descarta o container por conta própria), e o gerador de valores únicos é **compartilhado entre as classes**, porque elas dividem o mesmo banco e contadores separados colidem no índice único de e-mail.

Exige Docker no ar, que já é pré-requisito do projeto — inclusive **no CI**, e é por isso que o
job `api` do workflow roda em `ubuntu-latest` (runner Linux, com daemon Docker) e **não** declara
um bloco `services: postgres:`. O container é gerenciado pela própria `BancoFixture`; um service
do GitHub Actions ficaria de pé sem ninguém usar. As duas suítes são passos separados no job
para o log dizer de imediato se quebrou lógica ou banco.

### O que continua manual

O que os testes de integração não alcançam é a jornada pela interface. Vale repetir à mão depois de mexer em `Frota360DbContext`, migration ou provider:

| Passo | O que prova |
|---|---|
| `POST /backoffice/empresa` → abrir o `linkConvite` → criar o Admin (ou `./scripts/seed-dev.ps1`) | o bootstrap inteiro: sequence, seed dos 10 `TiposManutencaoPadrao`, envio do convite |
| Lançar abastecimento com a data de hoje e conferir a data **exibida** | erro de um dia aqui significa que o mapeamento saiu como `timestamptz` |
| Abrir `/auditoria` como Admin | `LogAuditoria.Id` como `bigint`, os 3 índices compostos e a paginação na tela |
| `GET /health` | health check do `DbContext` contra o banco real |
