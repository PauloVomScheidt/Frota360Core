# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the **front-end** of the Frota360 monorepo (`apps/web/`).

Front React 19 + Vite do Frota360 — sistema de gestão de frotas multi-tenant por empresa (motoristas / veículos / rotas / manutenções preventivas). **Escreva tudo em português**: componentes, comentários, textos de UI e documentação.

A raiz do repositório tem seu próprio [CLAUDE.md](../../CLAUDE.md), que cobre o que é transversal aos dois apps e vale aqui sem repetição: **navegação de código** (use `codegraph_explore` para perguntas estruturais), **política de documentação** (atualizar contexto/CLAUDE/README após qualquer alteração) e — o mais relevante — o **contrato entre API e front** (envelope de resposta, tipos dos DTOs, matriz de papéis, portas/CORS). Leia antes de qualquer mudança que atravesse a fronteira; este arquivo cobre só o front.

**Todos os comandos abaixo rodam a partir de `apps/web/`.**

A referência detalhada e autoritativa de comportamento tela a tela, convenções de API e cache keys do React Query é [docs/contexto-web.md](../../docs/contexto-web.md) — documento único de contexto do front (carrega também o mapa de endpoints em §6.5 e as inconsistências conhecidas em §10). Leia antes de qualquer mudança não trivial numa página. Este arquivo cobre só comandos e arquitetura transversal, para não duplicar aquele documento.

## Comandos

```powershell
npm run dev      # Vite em http://localhost:5173 (porta fixa — é a origem liberada no CORS da API)
npm run build    # tsc -b (type-check) + vite build
npm run lint     # oxlint
npm run gen:api  # regenera src/api/schema.d.ts a partir do OpenAPI — exige a API rodando localmente
```

Não há suíte de testes no front. Também não há atalho de lint/typecheck por arquivo além dos comandos completos — nem o oxlint nem o `tsc -b` têm modo single-file útil aqui.

Exige a API do Frota360 rodando localmente; ela vive neste mesmo repo: `dotnet run --project src/Api` a partir de `apps/api/` (padrão `https://localhost:7271/api/v1` conforme `.env.development`, sobrescrito por `VITE_API_URL`). Num banco zerado não há usuários — provisione uma empresa pelo backoffice da API (`POST /backoffice/empresa`) e abra o `linkConvite` retornado, que cai em `/convite?token=...`.

## Arquitetura

### Camada de API (`src/api/`)

- `http.ts` — instância única do axios (`http`). O interceptor de request injeta `Authorization: Bearer`. O de response: em **401**, tenta exatamente um `/auth/refresh` (protegido por um lock de promise em voo, já que a rotação do refresh token invalida o anterior — dois refreshes paralelos quebrariam o segundo), repete a request original uma vez e, em caso de falha, limpa a sessão e redireciona para `/login`. Uma lista fixa de rotas anônimas (`/auth/login`, `/auth/refresh`, `/auth/esqueci-senha`, `/auth/redefinir-senha`, `/convite/aceitar`) está isenta — um 401 ali significa credencial inválida, não sessão expirada.
- Toda resposta da API vem embrulhada no envelope `{ sucesso, mensagem, dados, erros }`, inclusive as de erro (401/403/422/429). O `unwrap()` em `http.ts` desempacota `dados` e lança `ApiError` (com `erros: string[]`) quando `sucesso` é falso. Os módulos por recurso (`motoristas.ts`, `veiculos.ts`, `rotas.ts`, `manutencoes.ts`, `tiposManutencao.ts`, `usuarios.ts`, `convites.ts`, `auth.ts`) chamam `http` e `unwrap()`.
- `errors.ts` — `mensagensDeErro()` transforma qualquer falha (envelope da API, erro de rede etc.) em `string[]` para exibição pelo componente compartilhado `ErrorList`.
- `types.ts` — o tipo do envelope, `Role` e os DTOs (mantidos em sincronia com a API **à mão**; o `gen:api` só regenera `schema.d.ts`).
- `tokenStorage.ts` — persiste `frota360.token`, `frota360.refreshToken` e `frota360.user` (nome/email/papel) no `localStorage`.
- O multi-tenant é transparente para o front: `empresaId` vem do JWT, o cliente nunca envia id de empresa.

### Autenticação e permissões (`src/auth/`)

- `useSession.ts` expõe o usuário logado de forma reativa via `useSyncExternalStore`, ouvindo o evento nativo `storage` (outras abas) e um evento customizado `frota360:sessao` (mesma aba, já que o `localStorage` não notifica quem escreveu).
- `permissions.ts` (`pode.*`) espelha a matriz de papéis da API (Admin / Supervisor / Operador) puramente para esconder ações que dariam 403 — **o servidor é sempre a autoridade de fato**; nunca confie na matriz do cliente para lógica sensível a segurança.
- Os guards de rota vivem em `src/components/RequireAuth.tsx`: `RequireAuth` (redireciona para `/login` preservando `location.state.from`), `RequireAdmin` e `RequireGestor` (Admin ou Supervisor). Aplicados como `<Route>` wrapper em [src/App.tsx](src/App.tsx).
- O papel usado pela UI é cacheado no login/refresh — uma mudança de papel no servidor pode levar até o tempo de vida do token para refletir na interface, mesmo que o servidor já a aplique imediatamente.

### Busca de dados

TanStack Query 5. As cache keys são arrays simples por recurso (`['motoristas']`, `['veiculos']`, `['rotas']`, `['manutencoes', filtro]`, `['tiposManutencao']`, `['tiposManutencao', 'ativos']`, `['usuarios']`, `['convites']`), invalidadas após cada mutation na página dona do recurso.

Atenção à **cross-invalidation**, quando a mutation de um recurso afeta o que outra lista exibe: excluir um motorista/veículo também invalida `['rotas']`, porque aquela tabela desnormaliza nome/placa; concluir uma manutenção também invalida `['veiculos']`, porque pode avançar o odômetro do veículo. A cadeia mais longa é **rota → veículo → manutenção**: tanto abrir uma rota (quando `kmInicial` supera o odômetro atual) quanto encerrá-la (`POST /rota/{id}/encerrar`) avançam o odômetro do veículo, que é de onde `atrasada`/`kmRestantes` derivam — então essas mutations invalidam `['rotas']`, `['veiculos']` **e** `['manutencoes']`. Ao adicionar uma mutation, consulte o mapa atual em `docs/contexto-web.md` §6.4 antes de assumir que um único `invalidateQueries` basta.

### Estrutura de páginas e componentes compartilhados

As páginas autenticadas são filhas de `RequireAuth` e envolvidas por `AppLayout` ([src/components/AppLayout.tsx](src/components/AppLayout.tsx)), que fornece a sidebar retrátil (estado no `localStorage`), o header e os blocos `PageHeader`/`ErrorList`. Páginas públicas/de autenticação usam `AuthScreen`/`AuthHeading` ([src/components/AuthScreen.tsx](src/components/AuthScreen.tsx)).

As páginas de CRUD (`MotoristasPage`, `VeiculosPage`, `RotasPage`, `ManutencoesPage`, `TiposManutencaoPage`) seguem o mesmo formato e reaproveitam `src/components/Table.tsx`:

- `InlineForm` — formulário de criação/edição renderizado acima da tabela (não é modal); a edição reusa o mesmo formulário pré-preenchido, com a página rolando para o topo.
- `TableStates` — renderização compartilhada das linhas de carregando/erro/vazio.
- `RowActions` / `ConfirmDialog` — ícones de editar/excluir por linha e confirmação de exclusão.
- `FormDialog` — formulário em modal (usado em "concluir manutenção" e "encerrar rota" — as transições de estado que carregam efeito colateral no odômetro do veículo, deliberadamente mantidas fora do formulário de edição comum).

A visibilidade dos botões de novo/editar/excluir é controlada por `pode.*` de `auth/permissions.ts`, não escondendo a página inteira.

### Design system

`src/styles/design-system.css` define o visual "Modernist" do app autenticado: fundo `#fdfaf6`, superfície `#f2ede4`, texto `#201e1d`, destaque `#1f3a5f` (rampa 100–900), perigo `#a03123`, tipografia Archivo, **`border-radius: 0` em tudo** — arestas retas, sem sombras. Classes compartilhadas: `.btn` (`.btn-primary`/`.btn-secondary`/`.btn-icon`/`.btn-danger`), `.field`+`.input`, `.tag` (`.tag-accent`/`.tag-neutral`/`.tag-danger`/`.tag-warning`), `.nav`, `.table`, `.dialog*`.

A `LandingPage` ([src/pages/LandingPage.tsx](src/pages/LandingPage.tsx)) **segue esse mesmo visual** — reto, bege, sem sombra — e tira todas as cores de `design-system.css`. Ela mantém folha própria, `src/styles/landing.css`, escopada sob a raiz `.lp` e importada só por ela, mas por causa da **escala** (display grande, faixas de ponta a ponta, o odômetro do hero), não por discordar do sistema. Três regras ao editá-la:

- O reset de `.lp` usa **`:where()`**, que não soma especificidade — escrever `.lp p { margin: 0 }` em vez de `.lp :where(p)` engole silenciosamente as margens de `.lp-lead`/`.lp-hero-sub` e o sublinhado de `.lp-cta-mail`.
- Tamanho de fonte vem dos tokens `--t-*`; **placa, km, data e rótulo de campo são IBM Plex Mono** (`--mono`), o resto é Archivo.
- `--alerta` (vermelho) e `--vencendo` (âmbar) são **estado de manutenção**, nunca decoração.
- Texto usa `--tinta-forte`/`--tinta-media`/`--tinta-fraca`, calibrados para passar de 4,5:1 sobre o papel — não invente um cinza mais claro para "dar hierarquia"; use tamanho, peso e caixa. `--regua`/`--regua-fraca` são contorno, não cor de texto.

Não renderiza dado de API: todos os números exibidos são mocks ilustrativos. O detalhamento tela a tela está em [docs/contexto-web.md §3.1](../../docs/contexto-web.md).

### Roteamento

`BrowserRouter` único em [src/App.tsx](src/App.tsx); rotas desconhecidas redirecionam para `/`. Rota autenticada nova entra dentro do wrapper `RequireAuth`, adicionalmente aninhada em `RequireAdmin`/`RequireGestor` se for restrita por papel, e precisa ser adicionada também à sidebar em `AppLayout.tsx` e à matriz de permissões em `auth/permissions.ts` caso não seja visível a todos.

### Configuração de ambiente

`VITE_API_URL` é a única variável de ambiente, definida por ambiente em `.env.development`/`.env.production` (a de produção está intencionalmente em branco e precisa ser preenchida no deploy).