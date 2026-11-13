# Pendências para a aprovação do TCC

Mapa do que falta para o Frota360 cumprir o [Portfolio Playbook](https://github.com/CatolicaSC-Portfolio/The-Portfolio-Playbook)
na linha **Web Apps**. Levantamento feito em 12/11/2026 sobre a `develop` (a4287f1).

**Prazo de entrega: 30/11/2026 (segunda-feira).** Até essa data: aplicação no ar, todos os
obrigatórios da linha cumpridos e prova de autoria aprovada. Segunda tentativa da prova só até
03/12/2026; Poster + Demo Day em 10, 15 e 16/12/2026.

## Resumo

| # | Pendência | Gravidade | Situação hoje |
|---|---|---|---|
| 1 | [Testes do frontend](#1-testes-do-frontend) | ✅ resolvido | **93,19%** de linhas (769 testes) — exigência 25%, meta 50%, piso de 50% no CI |
| 2 | [Cobertura do backend](#2-cobertura-do-backend) | ✅ resolvido | **82,0%** de linhas no relatório consolidado (três suítes, 587 testes) — meta 75%, piso de 75% no CI. Conferir o número de `apps/api` no Sonar após o merge |
| 3 | [Análise estática (SonarCloud)](#3-análise-estática-sonarcloud) | ✅ resolvido | quality gate verde na `main`: **81,8%** de cobertura, **2,8%** de duplicação, nota A em segurança, confiabilidade e manutenibilidade |
| 4 | [Wiki](#4-wiki) | 🟠 obrigatório do núcleo comum | Wiki do GitHub vazia |
| 5 | [Monitoramento](#5-monitoramento-e-observabilidade) | 🟠 obrigatório do núcleo comum | Serilog + `/health`, sem ferramenta de monitoramento |
| 6 | [Deploy em produção](#6-deploy-em-produção) | 🔴 reprovação direta | domínios não resolvem; nada no ar |
| 7 | [Pipeline de CD](#7-pipeline-de-cd) | 🔴 reprovação direta | só CI; deploy descrito como manual |
| 8 | [Release](#8-release) | 🟡 exigido pelo PAC VIII | nenhuma release publicada |

Legenda: 🔴 reprova sozinho · 🟠 sem ele não há aprovação · 🟡 exigência do PAC VIII.

> **Sobre deixar o deploy por último.** É viável, mas comece pelo menos o DNS e a EC2 com
> antecedência: propagação de DNS, emissão do certificado e verificação de domínio no Resend têm
> tempo de espera que não depende de você. "Projeto não acessível" é reprovação direta — um
> imprevisto no dia 29/11 não tem como ser recuperado.

---

## 1. Testes do frontend

### O problema
A linha Web Apps exige **cobertura de testes unitários com TDD: 75% no backend e 25% no
frontend**, conferida na prova de autoria (o professor pede para rodar os testes e mostrar o
relatório). Meta pessoal: **50%**.

### Situação em 12/11/2026 — ✅ concluído
**93,19% de linhas** (91,89% das instruções, 89,56% dos ramos, 92,16% das funções), 769 testes em
40 arquivos — as quatro ondas do plano abaixo, todas feitas. O `coverage.thresholds.lines` está em
50: abaixo disso o `npm run test:cov` sai com erro e o job `web` do CI reprova (conferido nos dois
sentidos).

Os testes acharam **cinco bugs**, todos corrigidos com o teste no mesmo passo: `unwrap()` tratando
`dados: null` como erro, arredondamento do valor total do abastecimento divergente da API, edição de
rota de motorista rebaixado bloqueada, data sugerida em "Nova despesa" presa ao carregamento da tela
e logout travado com a API fora do ar. É um bom material para a prova de autoria: cada um tem o teste
que o provou.

**Como gerar o relatório na prova** (em `apps/web/`):

```powershell
npm run test:cov            # roda tudo e imprime a tabela por arquivo no terminal
start coverage/index.html   # relatório navegável, linha a linha
```

⚠️ Rode do **PowerShell** (ou de um caminho `C:\`): no Git Bash, com `cd /c/...`, o Vitest falha com
"failed to find the runner" — o drive em minúsculo carrega o pacote duas vezes.

Um teste de cada tipo para explicar: função pura em `src/lib/periodo.test.ts` (relógio congelado,
virada de mês e de ano); tela com MSW em `src/pages/DespesasPage.test.tsx` (o rodapé vem de
`/despesa/resumo`, não da soma da página — conferido pelos parâmetros capturados no handler).

### Ferramentas recomendadas
| Ferramenta | Papel |
|---|---|
| **Vitest** + `@vitest/coverage-v8` | Executor de testes e relatório de cobertura; reaproveita a config do Vite |
| **jsdom** | Simula o DOM no Node, sem abrir navegador |
| **React Testing Library** | Renderiza componentes e busca elementos como o usuário os vê (papel, rótulo, texto) |
| **`@testing-library/user-event`** | Simula clique e digitação realistas |
| **`@testing-library/jest-dom`** | Asserções de DOM: `toBeInTheDocument`, `toBeDisabled`, `toHaveValue` |
| **MSW** (Mock Service Worker) | Intercepta as chamadas HTTP e devolve respostas falsas — `http.ts`, `unwrap()` e interceptors entram no teste de verdade |

### Plano em ondas
| Onda | Escopo | Cobertura acumulada |
|---|---|---|
| 0 | Infraestrutura: bloco `test` no `vite.config.ts`, `src/test/setup.ts`, helper `renderizar()` (QueryClient novo com `retry: false` + `MemoryRouter` + usuário logado por papel), fábricas de dados, handlers MSW padrão, scripts `test`/`test:cov`, passo no CI | — |
| 1 | Funções puras: `lib/*`, `auth/permissions.ts` (`it.each` por papel), `api/http.ts` (`unwrap`, `ApiError`, refresh) | ~8% |
| 2 | Telas dos fluxos de negócio: Abastecimentos, Manutenções, Minhas Rotas, Rotas, Despesas | ~30% |
| 3 | Autenticação e navegação: Login, aceite de convite, redefinição de senha, `RequireAuth`/`RequirePode`, menu do `AppLayout` por papel | ~38% |
| 4 | Smoke tests das demais telas: Dashboard, Custos, Veículos, catálogos, Usuários, Convites, Auditoria, Perfil, Landing | **50–55%** |

Resultado real por onda: 0 → 2,9% · 1 → 13,5% · 2 → 49,0% · 3 → 62,2% · 4 → **93,2%**.

### Cuidados
- **`VITE_API_URL` fixa na config de teste** — o `http.ts` lança no boot sem ela, e o Vitest roda
  em `mode=test`, que não lê `.env.development`.
- **`coverage.include: ['src/**']`** — no Vitest 4, sem isso só entram no cálculo os arquivos que
  algum teste importa, e o percentual sai inflado. Exclua apenas o que não é código executável
  (`main.tsx`, `schema.d.ts`, `vite-env.d.ts`).
- **`coverage.thresholds` em 50** — o CI reprova se a cobertura cair.
- **Google Maps** (`MapaRota`, `CampoEndereco`, `lib/googleMaps.ts`): substituir com `vi.mock`.
  `TrajetoRota` dá para testar de verdade, mockando `POST /rota/calcular` no MSW.
- **TDD é obrigatório na linha.** Os testes de agora são de caracterização (código já existe).
  Daqui em diante, funcionalidade nova nasce com o teste no commit primeiro — o histórico do Git
  é a evidência.

### Pronto quando
`npm run test:cov` passa localmente e no CI com ≥ 50% de linhas, e você sabe explicar um teste
de cada tipo (função pura, tela com MSW).

---

## 2. Cobertura do backend

### O problema
A meta é **75% de linhas no backend**. O número antigo deste documento (77,9%) estava errado: em
13/11/2026 a suíte unitária sozinha mede **71,4%** (Application 70,6%, Domain 77,5%), e o
SonarCloud, que soma as três suítes e enxerga as quatro camadas, mede **63,9%** na pasta
`apps/api` — 785 de 2.177 linhas sem teste.

| Camada (SonarCloud, `main` em `3d90b6a`) | Cobertura de linhas |
|---|---|
| Domain | 67,6% |
| Application | 66,1% |
| Infrastructure | 69,7% |
| Api | 6,5% |

O número separado do backend está no painel: Measures → Coverage → `apps/api`. O 81,8% do badge
é o projeto inteiro, com o front (94,4%) puxando para cima.

Os zeros são reais — conferidos no relatório local, não é efeito da soma do Sonar. Os maiores
buracos:

| Onde | Linhas sem teste | Como cobrir |
|---|---|---|
| `InfrastructureExtensions` + `ApplicationExtensions` (DI) | ~120 | Um teste que monta o `ServiceCollection` com `IConfiguration` em memória e resolve os serviços registrados — prova a ligação de DI de uma vez |
| Validators sem teste: `UpdateAbastecimentoValidator`, `AceitarConviteValidator`, `CreateManutencaoValidator`, `ConcluirManutencaoValidator`, `AtualizarPerfilValidator`, `ValidatorHelpers` | ~110 | `*ValidatorTests` no padrão dos que já existem em `Frota360.Tests` |
| `TokenService` (Infrastructure), `CurrentUserService` e `SessaoCookies` (Api) | ~65 | Unitários com configuração em memória e `DefaultHttpContext` |
| `MigracaoDeBanco` (Api), `DespesaRepository` (Infrastructure) | ~40 | `DespesaRepository` na suíte de integração; `MigracaoDeBanco` avaliar |

Para chegar a 75% faltam ~240 linhas cobertas; as três primeiras linhas da tabela somam ~295
(≈ 77%).

Preferir **testar** a **excluir**: o `Program.cs` saiu da conta porque a última linha não é
alcançável por teste, mas o código de DI é testável, e uma exclusão é algo que o professor pode
pedir para justificar.

### Situação em 13/11/2026 — ✅ concluído
**82,0%** de linhas (3.639 de 4.437) no relatório consolidado, com as três suítes somadas — 401
unitários, 47 de integração e 139 da camada Api. Branches em 77,5%.

**Na prova:** `./scripts/cobertura-api.ps1 -Abrir` na raiz do repositório (com o Docker no ar)
roda tudo e abre o HTML; no CI, o job `api` publica o mesmo relatório como artefato
`relatorio-cobertura-api`, mostra o resumo no job summary e reprova abaixo de 75%.

**A medição estava errada, e para baixo do que parecia.** O `coverlet.runsettings` excluía
`CompilerGeneratedAttribute`, e o compilador move para classes geradas o corpo de todo método
`async` e de toda lambda: o relatório contava 2.034 linhas em vez de 4.437 — só o código síncrono.
O `ExceptionMiddleware` aparecia com 100% porque só o construtor entrava na conta. Sem a exclusão
(e com `SkipAutoProps`, que tira só os `{ get; set; }`), o ponto de partida real era **58,8%**, e
não os 68,4% que o mesmo relatório mostrava antes da correção.

| Etapa | Linhas | Cobertura |
|---|---|---|
| Ponto de partida, medição antiga (sem os `async`) | 1.392 / 2.034 | 68,4% |
| Ponto de partida, medição corrigida | 2.613 / 4.438 | 58,8% |
| DI: `InjecaoDeDependenciaTests` (Application + Infrastructure) | 2.960 / 4.438 | 66,6% |
| Validators: `UpdateAbastecimento`, `AceitarConvite`, `CreateManutencao`, `ConcluirManutencao`, `AtualizarPerfil` (e o CPF do `ValidatorHelpers`) | 2.981 / 4.437 | 67,1% |
| `TokenService`, `CurrentUserService`, `SessaoCookies` + `DespesaRepository` (integração) | 3.095 / 4.437 | 69,7% |
| Os 15 controllers restantes, em `Frota360.Api.Tests` | 3.639 / 4.437 | **82,0%** |

A tabela original terminava em ~69,7% com a medição corrigida, e por isso entraram os controllers
— o maior buraco que sobrava (a camada Api estava em 12,7%; foi a 96%). O ganho em linhas dos
validators é pequeno porque a resolução de todo serviço pela DI já executa o construtor deles,
onde as regras são declaradas; os testes novos são os que de fato exercitam as regras. O
`ValidatorHelpers.Is18YearsOld` não era chamado em lugar nenhum e foi removido em vez de testado.

| Camada | Cobertura |
|---|---|
| Api | 96,0% |
| Application | 86,2% |
| Infrastructure | 67,4% |
| Domain | 57,3% |

**O Sonar deve mostrar uns 2 pontos a menos em `apps/api`** (estimativa: ~79,8%). Para arquivo
ausente do OpenCover ele calcula as linhas executáveis sozinho e as conta como descobertas — são
~121 linhas de inicializador de propriedade em DTOs e entidades (`= string.Empty`), que o coverlet
não instrumenta. Não há teste que as alcance; está explicado em `docs/contexto-api.md` (§ Testes).

O que sobrou sem teste, se valer reforçar: `BackofficeService` (61 linhas, o provisionamento de
empresa), `GoogleRoutesClient` (53) e `ResendEmailService` (26), os handlers de
`TipoManutencao` (~80) e os repositórios de catálogo na integração (~130). `MigracaoDeBanco` (26)
continua a avaliar: só roda em Production/Staging contra um banco de verdade.

### Ferramentas
| Ferramenta | Papel |
|---|---|
| **coverlet** + `apps/api/coverlet.runsettings` (já existem) | Cobertura de cada suíte em OpenCover e Cobertura, sem as migrations |
| **ReportGenerator** (`dotnet-reportgenerator-globaltool` 5.5.12, ferramenta local em `apps/api/dotnet-tools.json`) | Junta os relatórios das três suítes num HTML só, com o número consolidado do backend |
| **SonarCloud** (já integrado) | Mesmo número por pasta; o job `sonar` já recebe as três suítes |

### Passos
1. ✅ `coverlet.runsettings` com OpenCover e as migrations fora (feito no item 3).
2. ✅ Terceira suíte, `Frota360.Api.Tests`, para a camada HTTP (feito no item 3).
3. ✅ Relatório consolidado: `scripts/cobertura-api.ps1` roda as três suítes e gera o HTML do
   ReportGenerator; no CI, o mesmo script (`-SomenteRelatorio`) publica o relatório como artefato,
   o resumo no job summary e reprova o job `api` abaixo de 75%.
4. ✅ Testes na ordem da tabela acima, mais os controllers, conferindo o número a cada etapa.
5. ✅ Este item, `docs/contexto-api.md` (§ Testes), `apps/api/CLAUDE.md` e os READMEs.
6. Depois do merge: conferir `apps/api` em Measures → Coverage no SonarCloud.

### Pronto quando
Um comando gera um relatório único do backend com ≥ 75% (ideal: ~80% de margem), o SonarCloud
mostra o mesmo patamar em `apps/api`, e você sabe reproduzi-lo na frente do professor.

---

## 3. Análise estática (SonarCloud)

### O problema
**Análise estática de código e segurança** é obrigatória para todas as linhas (núcleo comum de
engenharia), e SonarQube/SonarCloud/CodeClimate aparecem como ✅ Preferir. O `oxlint` do front é
lint, não análise de segurança; o backend não tem nada.

### Ferramenta recomendada: **SonarCloud**
Gratuito para repositório público, integra com o GitHub e mostra o quality gate como check no PR.
Além de bugs, code smells e vulnerabilidades, ele exibe a cobertura — o mesmo número da prova.

### Situação em 13/11/2026 — ✅ concluído
Quality gate **verde** na `main` (análise do `3d90b6a`, PR #23). Código geral: **81,8%** de
cobertura, **2,8%** de duplicação, 0 bugs, 0 vulnerabilidades e nota **A** em segurança,
confiabilidade e manutenibilidade. Os achados que não eram defeito foram triados no painel com
justificativa (tabela abaixo). Badges no README.

**Na prova:** o painel é
[sonarcloud.io/summary/new_code?id=PauloVomScheidt_Frota360Core](https://sonarcloud.io/summary/new_code?id=PauloVomScheidt_Frota360Core);
a aba *Overall Code* traz o número geral, e *Activity* mostra o histórico das análises.

O repositório foi importado como **projeto único** `PauloVomScheidt_Frota360Core` (organização
`paulovomscheidt`), em vez dos dois projetos do plano original: o SonarScanner for .NET analisa
também o TypeScript (`sonar.scanner.scanAll`), então um projeto só cobre o monorepo, com um
quality gate e um badge.

A primeira análise (automática, sem cobertura) reprovou o gate só por **duplicação em código
novo** (4,3% contra o limite de 3%) — quase toda em migrations geradas pelo EF, que agora ficam
fora da análise. Ela apontou 12 achados de segurança e confiabilidade:

| Achado | Tratamento |
|---|---|
| `npm ci` sem `--ignore-scripts` no CI | ✅ corrigido |
| `BackofficeController` lendo o header cru | ✅ corrigido — `[FromHeader(Name = "X-Backoffice-Key")]` |
| `ExceptionMiddleware` sem `RequestAborted` | ✅ corrigido |
| `app.Run()` síncrono no `Program.cs` | ✅ corrigido — `await app.RunAsync()` |
| Segredo JWT "em configuração" (`TokenService`) | marcar como falso positivo: a chave vem de user-secrets/variável de ambiente, nunca de arquivo versionado |
| Senha no `appsettings.example.json` | marcar como falso positivo: é o template, com a senha do Postgres de desenvolvimento |
| `autoFocus` em cinco formulários de diálogo | marcar como aceito: o padrão de diálogo da WAI-ARIA manda pôr o foco no primeiro campo |
| `tabIndex` na `Rolagem` da landing | marcar como falso positivo: região rolável **precisa** ser focável para quem navega pelo teclado |

Já está no repositório:
- `apps/api/coverlet.runsettings` — OpenCover + Cobertura, sem migrations (adianta o passo 1 do
  item 2).
- Jobs `api` e `web` publicam a cobertura como artefato; o job novo `sonar` (depois dos dois, não
  obrigatório) roda `begin` → `build` → `end` e envia os dois relatórios.
- `projectRoot: '../..'` no reporter `lcov` do Vitest, para os caminhos baterem com a raiz.
- Badges no README.

O primeiro run pelo CI (PR #22) importou a cobertura — **83,5%** do projeto, 4.761 linhas
cobríveis —, mas o gate reprovou por **0% de cobertura no código novo**: as três linhas novas
estavam em `src/Api`, que nenhuma suíte alcançava. Resolvido com a suíte `Frota360.Api.Tests`
(`ExceptionMiddleware` e `BackofficeController` a 100%) e com o `Program.cs` fora da cobertura. O
mesmo run trouxe quatro avisos do compilador, também corrigidos: `KnownNetworks`/`IPNetwork`
obsoletos no `Program.cs` (agora `KnownIPNetworks` com `System.Net.IPNetwork.Parse`) e um
`JsonSerializerOptions` criado a cada erro no `ExceptionMiddleware`.

Depois do merge do PR #22, a análise da `main` reprovou por **duplicação em código novo: 3,55%**
(890 de 25.078 linhas, limite de 3%). Todo o código conta como novo porque o squash do "Release
13/11" (#21) datou todas as linhas daquele dia. A maior fonte era real: `TiposCombustivelPage` e
`TiposDespesaPage` eram cópias com os textos trocados (125 linhas duplicadas cada). As duas viraram
configuração de um componente só, `CatalogoSimples`, o que tira ~250 linhas duplicadas e deve
deixar a densidade perto de 2,6%.

Depois do merge (#23), o gate da `main` passou com 2,5% de duplicação no código novo. A triagem
no painel zerou os achados abertos de segurança e confiabilidade, e a nota de segurança, que
estava em **E** por causa do falso positivo do `TokenService`, foi para **A**.

### Pronto quando
Os dois projetos aparecem no SonarCloud com cobertura importada e quality gate verde, e o badge
está no README.

---

## 4. Wiki

### O problema
**Documentação em Wiki junto ao repositório** é 🔑 Obrigatória (Notion/Obsidian não são
aceitos). A documentação mínima da linha Web Apps exige: **requisitos funcionais**, **casos de
uso ou user stories**, **diagrama de arquitetura C4** e **instruções de deploy**. O critério
"Documentação" do Demo Day também cobra registro de decisões (RFC, ADR).

Hoje a Wiki do GitHub está vazia. Há bastante conteúdo em `docs/`, mas sem requisitos, sem user
stories e sem C4.

### Ferramentas recomendadas
| Ferramenta | Papel |
|---|---|
| **GitHub Wiki** | Destino obrigatório. É um repositório Git à parte (`Frota360Core.wiki.git`) — a primeira página precisa ser criada pela interface web antes de clonar |
| **Mermaid** | Diagramas como texto, renderizados pelo GitHub: C4 (`C4Context`, `C4Container`), modelo de dados (`erDiagram`), sequência |
| **Structurizr / draw.io** | Alternativa para o C4, se o Mermaid ficar limitado — exportar PNG para a Wiki |

### Páginas sugeridas
| Página | Conteúdo | Fonte |
|---|---|---|
| Home | Problema, parceiro, links (app, API, SonarCloud) | RFC |
| Requisitos | RF e RNF numerados | RFC, seção 2 |
| User stories / casos de uso | Por papel (Admin, Supervisor, Operador, Motorista) | RFC + telas |
| Arquitetura (C4) | Contexto, contêineres (React, API, Postgres, Caddy, S3/CloudFront, Google, Resend) e componentes da API | código |
| Modelo de dados | Diagrama ER das entidades | migrations |
| Decisões (ADRs) | Uma página por decisão: PostgreSQL, EC2 + Compose, Caddy, read model de custos, duas chaves do Google, multi-tenant por claim | `docs/contexto-api.md` |
| Como rodar localmente | Docker, API, web, `seed-dev.ps1` | `CLAUDE.md` |
| Deploy | Roteiro de produção e rollback | `docs/deploy.md` |
| Testes e qualidade | Como rodar, como gerar cobertura, SonarCloud | itens 1–3 |
| Monitoramento | O que é medido, onde ver, quais alertas | item 5 |
| Segurança e LGPD | JWT, papéis, auditoria, dados pessoais tratados | código + RFC |

### Pronto quando
Todas as páginas acima existem, a C4 bate com o código e o README aponta para a Wiki.

---

## 5. Monitoramento e observabilidade

### O problema
**Monitoramento ou observabilidade** é obrigatório para todas as linhas, e o pilar de DevOps pede
"definição clara sobre como o produto será monitorado". Hoje há Serilog (console + arquivo) e
health checks em `/health` e `/health/detail` — boa base, mas log em arquivo dentro do container
não é monitoramento: ninguém é avisado quando algo cai, e não há métricas nem painel.

### Ferramenta recomendada: **Grafana Cloud** (plano gratuito) + **OpenTelemetry**
Prometheus/Grafana e a família Datadog/New Relic aparecem como ✅ Preferir no playbook. O Grafana
Cloud entrega a stack Grafana gerenciada, sem consumir memória da EC2.

| Peça | Como |
|---|---|
| Métricas e traces da API | `OpenTelemetry.Extensions.Hosting` + instrumentação de ASP.NET Core, HttpClient e Npgsql, exportando via OTLP para o Grafana Cloud |
| Logs | Sink do Serilog para OpenTelemetry ou Loki — os logs estruturados que já existem passam a ser pesquisáveis |
| Disponibilidade | Synthetic Monitoring do Grafana Cloud (ou UptimeRobot) consultando `/health` a cada minuto |
| Alertas | E-mail quando `/health` falhar ou a taxa de 5xx subir |
| Painel | Dashboard com requisições/s, latência p95, taxa de erro e saúde do banco |

**Alternativas:** Prometheus + Grafana auto-hospedados no mesmo Compose (mais controle, mas
disputam RAM com a API na t3.medium) ou New Relic/Datadog no plano gratuito.
**Diferencial:** Grafana Faro (ou Sentry) no front, para capturar erros de JavaScript.

### Pronto quando
Há um dashboard com dados reais, um alerta que você já viu disparar e uma página na Wiki
explicando a estratégia. A instrumentação pode ser feita antes do deploy (envia de `localhost`);
o monitor de disponibilidade depende da URL pública.

---

## 6. Deploy em produção

### O problema
A aplicação precisa estar **hospedada, pública e estável até a divulgação das notas**. "Projeto
não acessível (link quebrado, servidor fora do ar)" é **reprovação direta**, e o pôster depende
de um QR Code funcional. Hoje `api.frota360app.com.br` e `app.frota360app.com.br` nem resolvem
DNS — o `docs/deploy.md` ainda tem a seção "TODO — dependem do DNS publicado" aberta.

### Arquitetura (já decidida em `docs/deploy.md`)
| Peça | Onde |
|---|---|
| API + Postgres + Caddy | EC2 t3.medium (sa-east-1), Docker Compose, IP elástico |
| Front estático | S3 + CloudFront, com fallback de SPA (403/404 → `/index.html`) |
| DNS | Cloudflare (`api` em DNS-only, `app` → CloudFront, raiz → redirect) |
| E-mail | Resend, domínio `send.frota360app.com.br` verificado |

AWS, Docker e PostgreSQL são ✅ Preferir. Vercel, Netlify e Render são 🚫 vedados.

### Passos
1. Criar os registros DNS e aguardar a propagação.
2. Subir a EC2, liberar 80 e 443 no security group, preencher o `.env` e subir o Compose.
3. Bucket S3 + distribuição CloudFront com OAC e o fallback de SPA.
4. Concluir a verificação do Resend.
5. Provisionar a empresa do parceiro (`POST /backoffice/empresa`).
6. **Backup do Postgres** — o `docs/deploy.md` o chama de "pendência mais séria": `pg_dump` diário
   para S3 com lifecycle, e o restore testado ao menos uma vez. Precisa existir antes do primeiro
   dado real do parceiro.

### Pronto quando
As duas URLs respondem com HTTPS, `/health` devolve 200, o fluxo de convite chega por e-mail e o
QR Code do pôster abre o app.

---

## 7. Pipeline de CD

### O problema
**Deploy via CI/CD** é 🔑 Obrigatório e **"Deploy manual via SSH ou FTP"** está em "o que não pode
ter" (reprovação direta). O CI atual (`api`, `web`, `docker`) só builda e testa. O compose de
produção ainda faz `build:` na própria instância, e o roteiro do `docs/deploy.md` é manual.

A prova de autoria também pergunta sobre isso: que etapas o pipeline roda, como torná-lo manual
em vez de automático e como voltar para a versão anterior.

### Ferramentas recomendadas
| Ferramenta | Papel |
|---|---|
| **GitHub Actions** | Novo workflow `deploy.yml`, disparado por tag de release ou push na `main`, mais `workflow_dispatch` para disparo manual |
| **GHCR** (GitHub Container Registry) | Registro da imagem da API, gratuito para repositório público; imagem marcada com o SHA e a versão |
| **AWS OIDC** (`aws-actions/configure-aws-credentials`) | Actions assume uma role IAM sem chave de acesso guardada em secret |
| **AWS SSM Run Command** | Executa `docker compose pull && up -d` na EC2 **sem porta SSH aberta** — afasta qualquer discussão sobre "deploy via SSH" |
| **`aws s3 sync` + `cloudfront create-invalidation`** | Publica o build do front |
| **GitHub Environments** (`production`) | Segredos de produção isolados e, se quiser, aprovação manual antes do deploy |

### Passos
1. Trocar o `build:` da API no `docker-compose.prod.yml` por `image: ghcr.io/...:${TAG}`.
2. Criar a role IAM com OIDC e permissões mínimas (SSM na instância, S3 do bucket, invalidação do
   CloudFront); instalar o agente SSM na EC2.
3. Escrever o `deploy.yml`: build e push da imagem → `ssm send-command` → build do front → sync no
   S3 → invalidação → checagem de `/health`.
4. **Rollback**: executar o workflow manualmente com a tag anterior. Documentar na Wiki.
5. Atualizar `docs/deploy.md` e `docs/contexto-api.md` — a linha 697 ainda diz "Sem CI… manuais
   por SSH".

### Pronto quando
Um merge (ou tag) publica API e front sem nenhum comando manual, e um rollback para a versão
anterior foi testado ao menos uma vez.

---

## 8. Release

### O problema
O PAC Extensionista VIII exige, na NP3, o **software funcional em versão Release**, e o
repositório não tem nenhuma release publicada.

### Ferramentas recomendadas
| Ferramenta | Papel |
|---|---|
| **Versionamento semântico** | `v1.0.0` para a entrega; correções depois viram `v1.0.1` |
| **GitHub Releases** | Tag + notas geradas automaticamente a partir dos PRs |
| **CD (item 7)** | A tag dispara o deploy, e a imagem recebe a mesma versão — o que está no ar é rastreável até o código |

### Passos
1. Tudo entra na `main` por PR revisado — a `develop` foi aposentada em 13/11/2026, e o
   "Release 13/11" (#21) já levou o conteúdo dela para a `main`.
2. Criar a tag `v1.0.0` na `main` e publicar a Release com as notas.
3. Conferir que a versão no ar é exatamente a da tag.

### Pronto quando
A release `v1.0.0` existe no GitHub, aponta para a `main` e corresponde ao que está em produção.

---

## Outras pendências

Fora da lista principal, mas com prazo no mesmo 30/11.

### PAC Extensionista VIII
- **Peer review**: PRs revisados pelo squad e issues de QA abertas pelos colegas no seu
  repositório (e você nos deles). Os PRs #15 e #16 não têm nenhuma review.
- **Parceiro externo**: relatórios de feedback, **termo de aceite assinado** e evidência de
  socialização (treinamento, documentação de uso, entrega formal).

### Fora do código
- Cinco orientações evidenciadas (ata, e-mail ou foto).
- Prova de autoria — preparar com o
  [guia de perguntas](https://github.com/CatolicaSC-Portfolio/The-Portfolio-Playbook/blob/main/documentation/guia-prova-de-autoria.md).
- Pôster digital em PDF, A0 vertical, no template oficial, com QR Code funcional.
- Uma noite do Demo Day como visitante, avaliando os colegas pelo formulário oficial.
