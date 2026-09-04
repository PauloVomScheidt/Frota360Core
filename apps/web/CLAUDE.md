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

### Verificação visual — Playwright

Não havendo testes, **`npm run build` + `npm run lint` não provam que a tela ficou certa**: nada aqui detecta um modal fora do lugar, um grid que não colapsa ou um anel de foco no elemento errado. Para isso há o **Playwright MCP**, registrado neste repositório:

```powershell
claude mcp list          # confere se `playwright` aparece Connected
claude mcp get playwright
```

**Escopo `user`** — vale para as suas sessões em **todos os projetos** desta máquina, gravado na raiz de `~/.claude.json`. Duas ressalvas: **não vai para quem clonar o repo** (não existe `.mcp.json` versionado — isso seria `--scope project`), e **não aparece numa sessão que já estava aberta antes da instalação**, porque os servidores MCP são carregados na inicialização; nesse caso `claude --continue` retoma a conversa com o servidor já disponível.

Se `claude mcp list` não listar o `playwright`, registre com:

```powershell
claude mcp add --scope user playwright -- npx github:microsoft/playwright-mcp
```

⚠️ A precedência é **local > project > user**. Uma entrada `local` sobrevivente sombreia a global e faz o escopo `user` parecer quebrado dentro daquele projeto — `claude mcp get playwright` diz qual está valendo.

**Quando usar.** Qualquer mudança que só se julga olhando: layout, posicionamento, rolagem, foco, responsividade, estado visual. Foi assim que apareceram os dois defeitos de diálogo documentados no Design system — o `margin` zerado pelo preflight do Tailwind e o foco pousando no contêiner de rolagem; nenhum dos dois quebra o build.

**Duas rotas, e a mais barata quase nunca é o app inteiro:**

1. **Página isolada com o CSS compilado.** Para um bug que é só de CSS, monte um `.html` no scratchpad que faça `<link>` para `apps/web/dist/assets/index-*.css` (o bundle já tem o preflight do Tailwind + o design system) e renderize só o componente em questão. Dispensa API, banco e login, e o `boundingBox()` do elemento dá número em vez de impressão — foi o que provou que o diálogo saía em `x:0, y:0` e passou a sair centrado.
2. **App de verdade**, quando o que se testa é o fluxo (salvar, 422, recorte por papel): exige o Postgres (`docker compose up -d` na raiz), a API (`dotnet run --project src/Api` em `apps/api/`) e `npm run dev`. O `./scripts/seed-dev.ps1` provisiona `admin@dev.com` / `SenhaForte123` para o login.

Ao capturar, **compare viewports** — 1440×900 para o layout, uma altura baixa (620) para forçar a rolagem do diálogo e 390 para o celular. Vários bugs só existem num deles.

Exige a API do Frota360 rodando localmente; ela vive neste mesmo repo: `dotnet run --project src/Api` a partir de `apps/api/` (padrão `https://localhost:7271/api/v1` conforme `.env.development`, sobrescrito por `VITE_API_URL`). Num banco zerado não há usuários — provisione uma empresa pelo backoffice da API (`POST /backoffice/empresa`) e abra o `linkConvite` retornado, que cai em `/convite?token=...`.

## Arquitetura

### Camada de API (`src/api/`)

- `http.ts` — instância única do axios (`http`), com `withCredentials: true`: token e refresh token viajam em cookie `HttpOnly; Secure; SameSite=None` setado pelo servidor, nunca em header `Authorization` nem em storage acessível a JS (mitiga exfiltração por XSS). O interceptor de response: em **401**, tenta exatamente um `/auth/refresh` sem corpo — o refresh token vai no cookie, anexado sozinho pelo navegador — (protegido por um lock de promise em voo, já que a rotação do refresh token invalida o anterior — dois refreshes paralelos quebrariam o segundo), repete a request original uma vez e, em caso de falha, limpa a sessão e redireciona para `/login`. Uma lista fixa de rotas anônimas (`/auth/login`, `/auth/refresh`, `/auth/esqueci-senha`, `/auth/redefinir-senha`, `/convite/aceitar`) está isenta — um 401 ali significa credencial inválida, não sessão expirada.
- Toda resposta da API vem embrulhada no envelope `{ sucesso, mensagem, dados, erros }`, inclusive as de erro (401/403/422/429). O `unwrap()` em `http.ts` desempacota `dados` e lança `ApiError` (com `erros: string[]`) quando `sucesso` é falso. Os módulos por recurso (`motoristas.ts`, `veiculos.ts`, `rotas.ts`, `manutencoes.ts`, `tiposManutencao.ts`, `usuarios.ts`, `convites.ts`, `auth.ts`) chamam `http` e `unwrap()`.
- `errors.ts` — `mensagensDeErro()` transforma qualquer falha (envelope da API, erro de rede etc.) em `string[]` para exibição pelo componente compartilhado `ErrorList`.
- `types.ts` — o tipo do envelope, `Role` e os DTOs (mantidos em sincronia com a API **à mão**; o `gen:api` só regenera `schema.d.ts`).
- `tokenStorage.ts` — persiste só `frota360.user` (nome/email/papel) no `localStorage`, para a UI. Token e refresh token não passam pelo front: chegam do servidor em cookie `HttpOnly`, e nenhum código aqui os lê ou escreve.
- O multi-tenant é transparente para o front: `empresaId` vem do JWT, o cliente nunca envia id de empresa.

### Autenticação e permissões (`src/auth/`)

- `useSession.ts` expõe o usuário logado de forma reativa via `useSyncExternalStore`, ouvindo o evento nativo `storage` (outras abas) e um evento customizado `frota360:sessao` (mesma aba, já que o `localStorage` não notifica quem escreveu).
- `permissions.ts` (`pode.*`) espelha a matriz de papéis da API (Admin / Supervisor / Operador / Motorista — um motorista é um usuário com essa role, não um cadastro à parte) puramente para esconder ações que dariam 403 — **o servidor é sempre a autoridade de fato**; nunca confie na matriz do cliente para lógica sensível a segurança. ⚠️ `pode.excluirDespesa` (Admin **e** Supervisor) é entrada **separada** de `pode.excluir`, que segue Admin-only e serve todas as outras telas: a exceção é da despesa, não da regra. ⚠️ `pode.editarTiposCombustivel`/`pode.editarPostos` escondem só as **telas** `/tipos-combustivel` e `/postos`: na API a **leitura** desses dois catálogos é aberta a todos os papéis, porque o motorista precisa deles para lançar abastecimento — por isso `AbastecimentosPage` os busca sem `enabled`, ao contrário de `['motoristas']`.
- Os guards de rota vivem em `src/components/RequireAuth.tsx` e são dois: `RequireAuth` (redireciona para `/login` preservando `location.state.from`) e `RequirePode`, que recebe um predicado de `permissions.ts`. Cada rota declara a própria permissão em [src/App.tsx](src/App.tsx) — `<RequirePode permitido={pode.verVeiculos} />`. Guarda por bloco de papéis não serve mais: o motorista enxerga parte do painel.
- **Todo redirecionamento por papel usa `rotaInicial(role)`**, nunca `/dashboard` fixo: o motorista só enxerga `/minhas-rotas`, e mandá-lo ao dashboard faria os guards ficarem em pingue-pongue. Vale também para os destinos pós-login e pós-aceite de convite.
- O papel usado pela UI é cacheado no login/refresh — uma mudança de papel no servidor pode levar até o tempo de vida do token para refletir na interface, mesmo que o servidor já a aplique imediatamente.

### Busca de dados

TanStack Query 5. As cache keys são arrays simples por recurso (`['motoristas']`, `['veiculos']`, `['rotas']`, `['rotas', 'minhas']`, `['manutencoes', filtro]`, `['abastecimentos', filtro]`, `['tiposCombustivel']`, `['tiposCombustivel', 'ativos']`, `['postos']`, `['postos', 'ativos']`, `['auditoria', filtro]`, `['custos', filtro]`, `['custos', 'resumo', recorte]`, `['despesas', filtro]`, `['tiposDespesa']`, `['tiposDespesa', 'ativos']`, `['tiposManutencao']`, `['tiposManutencao', 'ativos']`, `['usuarios']`, `['convites']`), invalidadas após cada mutation na página dona do recurso. `['rotas']` e `['rotas','minhas']` vêm de **endpoints diferentes** e não são pai e filho: invalide a chave exata.

**`['auditoria']` é a única que ninguém invalida, de propósito**: quase toda mutation do app gera uma linha de trilha, e chamar `invalidateQueries(['auditoria'])` de dentro de cada tela faria cada mutation conhecer uma tela que ela não afeta. O `staleTime` de 30 s e o botão "Atualizar" da própria tela resolvem.

**Paginação — toda listagem tem, e o corte é no cliente.** `lib/paginacao.ts` traz o `usePaginacao(itens)`, que recebe a lista **já filtrada e ordenada** e devolve a fatia junto com as props exatas do `Paginacao`:

```tsx
const p = usePaginacao(veiculosFiltrados)
{p.itensDaPagina.map(...)}
<Paginacao {...p} pending={query.isFetching} />
```

O tamanho (10/15/20, padrão 15) é **uma preferência do painel inteiro**, não uma por tela: vive no `localStorage` via `useTamanhoPagina()`. O rodapé some sozinho quando o total cabe na menor opção, e por isso vale ligar em qualquer lista — inclusive nos catálogos curtos.

Três regras ao paginar uma tela:

- ⚠️ **Total de rodapé sai da lista inteira, nunca da página.** `/abastecimentos` e `/despesas` mostram "N lançamentos · Total: R$ X" — virar de página não pode mexer nesses números. Foi o motivo de o corte ser no cliente em vez de no servidor. Em `/abastecimentos` a contagem chega ao componente da tabela como a prop **`quantidade`**, separada das linhas.
- **Nunca chame `resetarPaginacao()` ao filtrar.** O `usePaginacao` **clampa a página no render** (`Math.min(pagina, totalPaginas)`), então uma lista que encolhe nunca deixa a tela vazia. A regra antiga vale só para quem pagina no servidor.
- **Passe a lista depois do filtro.** Paginar antes de filtrar mostraria a página 1 de uma lista que não é a exibida.

**Seis telas paginam no servidor** e usam o outro hook, `usePaginacaoServidor()`: `/abastecimentos`, `/despesas`, `/manutencoes`, `/rotas` (e `/minhas-rotas`), mais `/auditoria` e `/custos`. Nelas o `dados` do envelope é um `ResultadoPaginado<T>` (`itens`/`pagina`/`tamanhoPagina`/`total`/`totalPaginas`), não um array, e o rodapé sai de `paginacao.props(dados)`.

⚠️ **Nessas seis, `resetar()` a cada `onChange` de filtro é obrigatório** — o clamp do cliente não alcança o que o banco recortou, e sem isso filtrar estando na página 4 abre a tela vazia.

⚠️ **O total do rodapé de `/abastecimentos` e `/despesas` vem de `/resumo`, não da lista.** A chave de cache do resumo carrega só o recorte (sem paginação), então virar de página não a invalida — é o que faz "N lançamentos · Total: R$ X" ficar parado enquanto as linhas trocam. Somar `itens` ali seria a regressão que os endpoints vieram evitar.

Atenção à **cross-invalidation**, quando a mutation de um recurso afeta o que outra lista exibe: excluir um veículo também invalida `['rotas']`, porque aquela tabela desnormaliza nome/placa; **excluir uma rota** invalida `['veiculos']`, porque a coluna Situação de `/veiculos` sai de `emRota`; concluir uma manutenção também invalida `['veiculos']`, porque pode avançar o odômetro do veículo. A cadeia mais longa é **rota → veículo → manutenção**: abrir uma rota (quando `kmInicial` supera o odômetro atual) e encerrá-la (`POST /rota/{id}/encerrar`) avançam o odômetro do veículo, que é de onde `atrasada`/`kmRestantes` derivam — então essas mutations invalidam a própria lista, `['veiculos']` **e** `['manutencoes']`. São **três** os caminhos que mexem no odômetro — e o **abastecimento é um deles** desde que passou a registrar a quilometragem: lançar ou corrigir um abastecimento invalida `['abastecimentos']`, `['custos']`, `['veiculos']` **e** `['manutencoes']`. Ao criar um quarto, lembre da cadeia inteira. A segunda cadeia longa é a de **custo**: `['custos']` é alimentada por quatro telas (abastecimento, manutenção, **despesa** e **encerrar rota**, que apura o `kmPercorrido` usado como denominador do R$/km), então encerrar rota invalida **quatro** chaves. Mutação de **tipo de despesa** invalida três (`['tiposDespesa']`, `['despesas']`, `['custos']`): o nome do tipo é desnormalizado na despesa e é a `categoria` da linha de custo. Já **tipo de combustível** e **posto** invalidam só duas (a própria chave e `['abastecimentos']`) — o nome é desnormalizado na listagem, mas a categoria de custo do abastecimento é a constante `"Combustível"`, não o nome do tipo. Ao adicionar uma mutation, consulte o mapa atual em `docs/contexto-web.md` §6.4 antes de assumir que um único `invalidateQueries` basta.

### Estrutura de páginas e componentes compartilhados

As páginas autenticadas são filhas de `RequireAuth` e envolvidas por `AppLayout` ([src/components/AppLayout.tsx](src/components/AppLayout.tsx)), que fornece a sidebar retrátil (estado no `localStorage`), o header e os blocos `PageHeader`/`ErrorList`. Páginas públicas/de autenticação usam `AuthScreen`/`AuthHeading` ([src/components/AuthScreen.tsx](src/components/AuthScreen.tsx)).

As páginas de CRUD (`VeiculosPage`, `RotasPage`, `ManutencoesPage`, `AbastecimentosPage`, `TiposManutencaoPage`, `DespesasPage`, `TiposDespesaPage`, `TiposCombustivelPage`, `PostosPage`, `ConvitesPage`, `MinhasRotasPage`) seguem o mesmo formato — botão "Novo X" no `PageHeader`, tabela paginada, e o cadastro/edição em **modal** — e reaproveitam `src/components/Table.tsx` (`MotoristasPage`, `AuditoriaPage` e `CustosPage` são somente leitura — usam só `TableStates`, e as duas últimas mais o `Paginacao`):

- `TableStates` — renderização compartilhada das linhas de carregando/erro/vazio.
- `RowActions` / `ConfirmDialog` — ícones de editar/excluir por linha e confirmação de ação consequente. Os defaults do `ConfirmDialog` são os da exclusão; `textoConfirmar`/`textoPendente`/`variante` cobrem os outros casos (a troca de permissão em `/usuarios` usa `variante="padrao"`, porque derrubar a sessão de alguém não é destrutivo como apagar um registro).
- `FormDialog` — **todo formulário de criação/edição do painel**, mais as transições de estado que pedem campos ("concluir manutenção", "encerrar rota"). É um `<dialog>` nativo: `showModal()` dá trava de foco, Escape e `::backdrop` de graça, e a tabela continua visível ao fundo. `largura` separa os dois usos — **760** para os cadastros (o `.dialog-grid` cabe em três colunas) e o default **520** para as transições, de dois ou três campos. Não há mais formulário acima da tabela: o antigo `InlineForm` foi removido, e com ele o `window.scrollTo` que a edição fazia.
- `SecaoCampos` — bloco de campos dentro de um `FormDialog`, com título opcional em caixa alta. **Formulário com mais de uns cinco campos agrupa por categoria** ("Dados do posto", "Veículo e motorista", "Vencimento"), em vez de uma fileira sem hierarquia; os diálogos curtos usam uma `SecaoCampos` sem título, que é só o grid. Quem manda na largura de cada campo é o `.dialog-grid` (`auto-fit`/`minmax(190px, 1fr)`) — **não ponha largura fixa no wrapper do campo**; o que precisa da linha inteira (observação, aviso, nota explicativa) recebe a classe `campo-largo`.
- `Paginacao` — rodapé com o seletor de itens por página (10/15/20) e o "X–Y de Z" + anterior/próxima. Some quando o total cabe na menor opção — ver a regra completa em **Paginação**, acima.
- `PainelDialog` — o diálogo que só mostra conteúdo, com "Fechar" como única ação. É o irmão do `ConfirmDialog` (confirma) e do `FormDialog` (submete): serve detalhe sob demanda, como os lançamentos de um veículo em `/custos`.
- `FiltroPeriodo` — select de período pronto (`Hoje`, `Últimos 7/30 dias`, `Este mês`, `Mês passado`), usado por `/manutencoes`, `/abastecimentos`, `/despesas` e `/custos`. **Filtro de data novo usa este componente**, não dois campos `date` soltos; a conversão para `de`/`ate` vive em `lib/periodo.ts` e acontece no cliente — a API só conhece intervalo.

A visibilidade dos botões de novo/editar/excluir é controlada por `pode.*` de `auth/permissions.ts`, não escondendo a página inteira. É assim que `/veiculos` e `/manutencoes` ficam read-only para o motorista sem código condicional novo.

### Design system

`src/styles/design-system.css` define o visual "Modernist" do app autenticado: fundo `#fdfaf6`, superfície `#f2ede4`, texto `#201e1d`, destaque `#1f3a5f` (rampa 100–900), perigo `#a03123`, tipografia Archivo, **`border-radius: 0` em tudo** — arestas retas, sem sombras. Classes compartilhadas: `.btn` (`.btn-primary`/`.btn-secondary`/`.btn-icon`/`.btn-danger`), `.field`+`.input`, `.tag` (abaixo), `.nav`, `.table`, `.dialog*`.

Os três tokens `--radius-*` já valem `0px`: **não escreva `style={{ borderRadius: 0 }}`** em botão ou campo novo — é ruído que não muda nada.

O diálogo tem quatro classes próprias além de `.dialog`/`.dialog-title`/`.dialog-body`/`.dialog-actions`: `.dialog-corpo` é o **único trecho rolável** (o `.dialog` para em `85vh`, e título e ações ficam fixos); `.dialog-secao-titulo` é o cabeçalho de cada `SecaoCampos`, na mesma linguagem do `<th>` da `.table`; `.dialog-grid` é o grid `auto-fit`/`minmax(190px, 1fr)` que decide a largura dos campos — três colunas no modal de 760px, uma no celular, sem media query; e `.campo-largo` é o `grid-column: 1 / -1` de quem precisa da linha inteira.

⚠️ **Não tire o `inset: 0` + `margin: auto` do `.dialog`.** É o par que centra um `<dialog>` modal, e ele vem da folha do navegador — mas o **preflight do Tailwind v4 zera `margin` em `*`**, `<dialog>` incluído. Sem essas duas linhas todo diálogo do app cola no canto superior esquerdo. Pelo mesmo motivo o `useAbrirModalAoMontar` reposiciona o foco: com o corpo rolando, o Chrome torna o contêiner de rolagem focável e o `showModal()` pousa o foco nele, deixando o anel de foco em volta do formulário inteiro. O hook só intervém nesse caso — `autoFocus` declarado continua tendo a palavra final.

**Cor de situação — leia a tabela normativa em [docs/contexto-web.md §8.1](../../docs/contexto-web.md) antes de colorir qualquer estado novo.** Em resumo:

- **Situação se sinaliza pela classe `.tag`, nunca por `style` inline.** A `.tag` tem barra de 3px na cor do estado (`border-left: 3px solid currentColor`), fundo tonal, caixa alta e peso 600 — a mesma forma da etiqueta da landing.
- São **cinco** tokens de estado em `:root`, cada um com seu `-bg`: `--color-accent` (acontecendo agora), `--color-success` (concluído/saudável), `--color-warning` (atenção em breve), `--color-danger` (falha/destrutivo) e a rampa neutra (sem estado). **Não invente um hex novo nem um sexto tom** — `#7a5312` e `#a03123` já viveram crus e espalhados pelo código, e é o que a tokenização veio resolver.
- A cor diz a **consequência, não a entidade**: um tom por entidade transforma a tabela em arco-íris. Cinza é ausência de estado, não "qualquer coisa que terminou"; azul é reservado ao que está em curso, nunca ênfase decorativa num dado que não é situação.
- Quando a cor precisar aparecer fora de uma tag (texto de andamento, borda de alerta), use os mesmos `var(--color-*)`, na mesma escala da tag daquela linha.
- Os helpers que decidem rótulo e classe vivem em `lib/`, não dentro da página: `lib/rota.ts` (`statusDaRota`) e `lib/manutencao.ts` (`badgeDaManutencao`, `estaVencendo`, `FAIXA_AVISO`).

A `LandingPage` ([src/pages/LandingPage.tsx](src/pages/LandingPage.tsx)) tem **visual próprio, diferente do painel** — cantos arredondados, sombra, nav flutuante em pílula — mas continua tirando cor, sombra e tipografia de `design-system.css` (só o raio arredondado é exclusivo dela). Ela mantém folha própria, `src/styles/landing.css`, escopada sob a raiz `.lp` e importada só por ela, pela **escala** (display grande) e pelo visual divergente. A **exceção**: os mocks de UI (painel de veículos, rotas, manutenções) ficam dentro de um `Dispositivo` (moldura arredondada) mas por dentro são **retos e reaproveitam `.table`/`.tag`/`.btn` do design system direto** — precisam parecer com o produto de verdade, inclusive a **espessura das réguas do painel real (2px), afinada pra 1px só dentro do `Dispositivo`** — em escala reduzida a régua original pesa mais do que no tamanho de verdade; a cor não muda. Regras ao editá-la:

- O reset de `.lp` usa **`:where()`**, que não soma especificidade — escrever `.lp p { margin: 0 }` em vez de `.lp :where(p)` engole silenciosamente as margens de `.lp-lead`/`.lp-hero-sub` e o sublinhado de `.lp-cta-mail`.
- Tamanho de fonte vem dos tokens `--t-*`. Sem fonte mono — nem o painel nem a landing usam uma hoje.
- `var(--color-danger)` (vermelho) e `var(--color-warning)` (âmbar) são **estado de manutenção**, nunca decoração — mesmos tokens dentro do `Dispositivo`, via `.tag-danger`/`.tag-warning`.
- Texto usa `--tinta-forte`/`--tinta-media`/`--tinta-fraca` (derivados de `var(--color-text)` por `color-mix()`) — não invente um cinza mais claro para "dar hierarquia"; use tamanho, peso e caixa. `--regua`/`--regua-forte` são contorno, não cor de texto.
- `overflow: hidden` só no `Dispositivo` (precisa recortar o mock reto pelo raio da moldura) — nunca nos cartões que envolvem uma `Rolagem` (`.lp-compara`, `.lp-matriz-cartao`), ou a rolagem horizontal quebra no celular.
- Sem peça de assinatura no hero — ele é só texto centralizado; um odômetro decorativo que ficava ao lado foi removido por complexidade desproporcional ao que agregava.

Não renderiza dado de API: todos os números exibidos são mocks ilustrativos. O detalhamento tela a tela está em [docs/contexto-web.md §3.1](../../docs/contexto-web.md).

### Roteamento

`BrowserRouter` único em [src/App.tsx](src/App.tsx); rotas desconhecidas redirecionam para `/`. Rota autenticada nova entra dentro do wrapper `RequireAuth`, aninhada num `<RequirePode permitido={pode.X} />` com o predicado da própria tela, e precisa ser adicionada também à sidebar em `AppLayout.tsx` e à matriz em `auth/permissions.ts`.

A sidebar tem **três** categorias para a gestão — **Dashboard** (o dia a dia), **Parametrização** (os catálogos: tipos de manutenção, de despesa, de combustível e postos) e **Controle** (Admin: usuários, convites, auditoria) —, e duas para o motorista (**Operação** e **Visualização**). Tela de catálogo nova entra em `ITENS_PARAMETRIZACAO`, não na lista do dashboard.

**Não há guarda por bloco de papéis** (os antigos `RequireGestao`/`RequireGestor`/`RequireAdmin`/`RequireMotorista` não existem mais): desde que o motorista passou a enxergar parte do painel, quem manda é a tela, não o papel. Por isso as entradas `pode.ver*` são **por tela** — um booleano único de "é gestão" seria mentira.

### Configuração de ambiente

`VITE_API_URL` é a única variável de ambiente, definida por ambiente em `.env.development`/`.env.production` (a de produção está intencionalmente em branco e precisa ser preenchida no deploy).