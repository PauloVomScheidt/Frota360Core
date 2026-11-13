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
| 2 | [Cobertura do backend](#2-cobertura-do-backend) | 🔴 reprova na prova de autoria | 77,9% (só unitários, só Application + Domain) — meta é 75% |
| 3 | [Análise estática (SonarCloud)](#3-análise-estática-sonarcloud) | 🟠 obrigatório do núcleo comum | inexistente |
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
A meta é **75%** e o número medido é **77,9%** de linhas (349 testes unitários; Application
77,1%, Domain 85,7%). Está acima, mas com folga pequena, e o número tem dois pontos cegos:

- O projeto `Frota360.Tests` só referencia **Application** e **Domain** — Infrastructure e Api
  não aparecem no relatório dos unitários.
- Os testes de integração (`Frota360.IntegrationTests`, Testcontainers) cobrem Infrastructure,
  mas geram um relatório separado e exigem Docker rodando.

Na prova, o professor vai olhar **um** relatório. Se ele for gerado de outro jeito do que o seu,
o número pode cair abaixo de 75%.

### Ferramentas recomendadas
| Ferramenta | Papel |
|---|---|
| **coverlet** (já instalado) | Coleta a cobertura de cada projeto de teste |
| **ReportGenerator** (`dotnet-reportgenerator-globaltool`) | Junta os relatórios de unitários e integração num só, em HTML e Cobertura |
| **`coverlet.runsettings`** | Exclui o que não é código autoral: `Migrations/**`, código gerado |

### Passos
1. Criar o `coverlet.runsettings` com as exclusões e gerar a cobertura nos dois projetos.
2. Consolidar com o ReportGenerator e anotar o número real do backend inteiro.
3. Se ficar abaixo de ~80%, testar os handlers e services menos cobertos (o relatório HTML aponta
   linha a linha).
4. Publicar o relatório como artefato do CI e, opcionalmente, falhar o job abaixo de 75%.

### Pronto quando
Um comando gera um relatório único do backend com ≥ 75% (ideal: ~80% de margem), e você sabe
reproduzi-lo na frente do professor.

---

## 3. Análise estática (SonarCloud)

### O problema
**Análise estática de código e segurança** é obrigatória para todas as linhas (núcleo comum de
engenharia), e SonarQube/SonarCloud/CodeClimate aparecem como ✅ Preferir. O `oxlint` do front é
lint, não análise de segurança; o backend não tem nada.

### Ferramenta recomendada: **SonarCloud**
Gratuito para repositório público, integra com o GitHub e mostra o quality gate como check no PR.
Além de bugs, code smells e vulnerabilidades, ele exibe a cobertura — o mesmo número da prova.

### Passos
1. Entrar no sonarcloud.io com o GitHub e importar o repositório. Criar **dois projetos**
   (`frota360-api` e `frota360-web`) — é o jeito mais simples de lidar com o monorepo.
2. **Desligar a "Automatic Analysis"**: ela não importa cobertura de C#. A análise roda no CI.
3. Gerar o `SONAR_TOKEN` e cadastrar como secret do repositório.
4. **API**: no job `api`, envolver build e testes com `dotnet-sonarscanner begin` / `end`, com a
   cobertura do coverlet em formato **OpenCover**.
5. **Web**: `SonarSource/sonarqube-scan-action` depois dos testes, apontando para o
   `coverage/lcov.info` do Vitest.
6. Tratar o que o primeiro relatório apontar como **bug** ou **vulnerabilidade**; code smells
   ficam a critério.

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
1. Merge da `develop` na `main` via PR revisado.
2. Criar a tag `v1.0.0` na `main` e publicar a Release com as notas.
3. Conferir que a versão no ar é exatamente a da tag.

### Pronto quando
A release `v1.0.0` existe no GitHub, aponta para a `main` e corresponde ao que está em produção.

---

## Outras pendências

Fora da lista principal, mas com prazo no mesmo 30/11.

### Documentação desatualizada
- `docs/contexto-api.md:697` diz "Sem CI. Não há `.github/workflows`" — o CI existe.

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
