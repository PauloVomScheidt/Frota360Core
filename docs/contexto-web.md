# Frota360 Web — Contexto do Front-end

> Documento **único** de referência do front-end (React + Vite): arquitetura, rotas, endpoints consumidos, o que cada tela faz e as armadilhas conhecidas.
> Complementa [`contexto-api.md`](contexto-api.md): lá está o contrato do servidor, aqui está o que a aplicação faz com ele.
> **Caminhos**: relativos à raiz do monorepo — o código do front vive em `apps/web/`, e os comandos `npm` rodam de lá.
> Última atualização: 2026-09-04 — duas rodadas no mesmo dia. (1) **Todo cadastro/edição virou modal** (§8): o `InlineForm` acima da tabela saiu, o `FormDialog` passou a servir as onze telas de escrita e os campos agora se agrupam em `SecaoCampos` por categoria. (2) **Toda listagem pagina** (§8.2), com seletor de 10/15/20 lembrado no navegador — e `/custos` (§5.12) perdeu a tabela de lançamentos da tela principal: agora é um botão por linha de "Por veículo" abrindo o detalhe daquele veículo em modal.

---

## 1. Visão geral

SPA React 19 + Vite 8 + TypeScript, front-end da API Frota360 — gestão de frota **multi-tenant** (motoristas, veículos, rotas, manutenção preventiva). Sem suíte de testes.

| Aspecto | Escolha |
|---|---|
| Build | Vite 8 + React 19 + TypeScript |
| Rotas | react-router-dom 7 (`BrowserRouter`) |
| Dados | TanStack Query 5 |
| HTTP | axios com interceptors (Bearer + refresh) |
| Estilo | Tailwind 4 + design system próprio (`apps/web/src/styles/design-system.css`) |
| Lint | oxlint |
| Sessão | `localStorage` (token, refreshToken, identidade) |

```bash
npm run dev      # Vite em http://localhost:5173
npm run build    # tsc -b + vite build
npm run lint     # oxlint
npm run gen:api  # regenera tipos do OpenAPI (API precisa estar no ar)
```

Base da API por ambiente: `VITE_API_URL` (`.env.development` → `https://localhost:7271/api/v1`; `.env.production` → `https://api.frota360app.com.br/api/v1`). **O `/api/v1` faz parte do valor**: os módulos de `src/api` chamam caminhos relativos (`/veiculo`, `/auth/login`) sobre o `baseURL`. Se a variável estiver vazia, o `http.ts` lança na carga do módulo em vez de deixar o axios cair em URLs relativas à origem do front. É a única variável de ambiente do projeto. A porta 5173 do `npm run dev` é fixa — é a origem liberada no CORS da API.

O `empresaId` **nunca** é enviado pelo cliente — vem do JWT. A multi-tenancy é transparente para o front.

Em base nova não existe usuário: é preciso provisionar uma empresa pelo backoffice da API (`POST /backoffice/empresa`) e abrir o `linkConvite` devolvido, que cai em `/convite?token=…`.

O app tem **duas faces**: o painel de gestão da frota (Admin, Supervisor, Operador) e a operação do motorista, cuja home é `/minhas-rotas`. Um usuário nunca vê as duas — o motorista alcança ainda `/veiculos` e `/manutencoes` em leitura (precisa saber o estado do caminhão) e `/perfil`, que não é de face nenhuma: vale para todos os papéis.

Não existe cadastro de motorista separado: **um motorista é um usuário com a role `Motorista`**, convidado, promovido e rebaixado exatamente como Supervisor e Operador.

---

## 2. Mapa de rotas

Definido em [`apps/web/src/App.tsx`](apps/web/src/App.tsx). Qualquer rota desconhecida cai em `/`.

| Rota | Tela | Acesso |
|---|---|---|
| `/` | `LandingPage` | Público |
| `/login` | `LoginPage` | Público |
| `/esqueci-senha` | `ForgotPasswordPage` | Público |
| `/redefinir-senha?token=…` | `ResetPasswordPage` | Público (link do e-mail) |
| `/convite?token=…` | `AcceptInvitePage` | Público (link do e-mail) |
| `/dashboard` | `DashboardPage` | Gestão (Admin / Supervisor / Operador) |
| `/motoristas` | `MotoristasPage` | Gestão |
| `/veiculos` | `VeiculosPage` | Todos (Motorista: leitura) |
| `/rotas` | `RotasPage` | Gestão |
| `/manutencoes` | `ManutencoesPage` | Todos (Motorista: leitura, sem custo) |
| `/abastecimentos` | `AbastecimentosPage` | Todos — **leitura e escrita**; Motorista vê só o que é dele |
| `/despesas` | `DespesasPage` | Gestão — leitura e escrita |
| `/custos` | `CustosPage` | Gestão — somente leitura |
| `/minhas-rotas` | `MinhasRotasPage` | **Motorista** |
| `/tipos-manutencao` | `TiposManutencaoPage` | **Admin / Supervisor** |
| `/tipos-despesa` | `TiposDespesaPage` | **Admin / Supervisor** |
| `/tipos-combustivel` | `TiposCombustivelPage` | **Admin / Supervisor** — mas a **leitura** do catálogo na API é aberta a todos, para o Motorista lançar |
| `/postos` | `PostosPage` | **Admin / Supervisor** — mesma nota da anterior |
| `/usuarios` | `UsuariosPage` | **Admin** |
| `/convites` | `ConvitesPage` | **Admin** |
| `/auditoria` | `AuditoriaPage` | **Admin** |
| `/perfil` | `PerfilPage` | **Qualquer autenticado** — única rota interna sem `RequirePode` |

"Gestão" significa **todos os papéis menos `Motorista`**. Ele tem `/minhas-rotas` como home e mais duas telas em leitura: veículos e manutenções — saber o estado do caminhão faz parte do trabalho.

Os guardas estão em [`apps/web/src/components/RequireAuth.tsx`](apps/web/src/components/RequireAuth.tsx) e são **dois**: `RequireAuth`, que redireciona para `/login` quando não há sessão (guardando a origem em `location.state.from`), e `RequirePode`, que recebe um predicado de `auth/permissions.ts`. Como o JWT vive num cookie `HttpOnly` invisível ao JS, `RequireAuth` decide pela identidade em cache (`useSession`), não por um token lido do storage — o 401 do servidor cobre o caso de cookie ausente/expirado que o guard não vê.

Cada rota declara a própria permissão (`<RequirePode permitido={pode.verVeiculos} />`). Um guarda por bloco de papéis deixou de fazer sentido quando o motorista passou a enxergar parte do painel: quem manda é a tela, não o papel.

**`/perfil` é a exceção**: fica dentro de `RequireAuth` e fora de qualquer `RequirePode`. Editar o próprio cadastro é direito de quem está autenticado, seja qual for o papel — e envolvê-la num predicado seria criar um `pode.verPerfil` que devolve `true` para todo mundo.

**Todo redirecionamento de guarda usa `rotaInicial(role)`** de `auth/permissions.ts` (`/minhas-rotas` para motorista, `/dashboard` para o resto), nunca `/dashboard` fixo — para o motorista o dashboard é justamente uma tela bloqueada, e o par de guardas entraria em pingue-pongue. Os destinos pós-login (`LoginPage`) e pós-aceite de convite (`AcceptInvitePage`) usam a mesma função, a partir do `role` que vem na `SessaoResponse`.

O servidor continua sendo a autoridade — os guardas só evitam telas que resultariam em 401/403.

---

## 3. Telas públicas

### 3.1 `/` — Landing page

Página de apresentação do produto (v4). Não consome a API.

**Volta a ter linguagem visual própria.** A v3 tinha revertido a landing para a mesma língua reta/sem sombra do painel — essa decisão foi desfeita: a landing é de novo cantos arredondados, cartões com sombra e nav flutuante em pílula, o oposto do painel. A **exceção** é o que fica dentro de um `Dispositivo` (moldura arredondada + sombra) — os mocks que fingem ser telas de verdade (painel de veículos, rotas, manutenções): esses continuam **retos e reaproveitam as classes globais** `.table`/`.tag`/`.btn` de [`design-system.css`](apps/web/src/styles/design-system.css), porque representam o produto real, e desenhá-los arredondados mentiria sobre como ele é. Cor, sombra (`--shadow-*`) e tipografia (`--font-*`) continuam saindo do design system em toda a página — só o raio arredondado e as sombras maiores da vitrine (`--lp-radius-*`, `--lp-shadow-*`) são tokens exclusivos da landing.

Ela mantém folha própria, [`apps/web/src/styles/landing.css`](apps/web/src/styles/landing.css), escopada em `.lp` e importada só por esta tela — pela **escala** (display grande) e pelo visual divergente do painel.

Convenções da folha, que valem ao editar:

- **Escala tipográfica em tokens** (`--t-h1`, `--t-h2`, `--t-corpo`, `--t-cap`…) no bloco `.lp`. Não use tamanho solto no JSX.
- **Três níveis de tinta para texto**, com nome de intenção e não de opacidade: `--tinta-forte` (78%), `--tinta-media` (62%) e `--tinta-fraca` (45%) — todos derivados de `var(--color-text)` por `color-mix()`, não um hex novo. Hierarquia aqui se faz com tamanho, peso e caixa; para régua use `--regua`/`--regua-forte`, que **não são cor de texto**.
- **`min-width: 0` nos filhos de todo grid que contenha tabela** (`.lp-painel > *`, `.lp-split > *`, `.lp-cta-grade > *`). Sem isso o filho cresce até o `min-width` da tabela, o `overflow-x` de `.lp-rolagem` nunca entra e cabeçalho e rodapé do mock são cortados fora da tela no celular.
- **`Dispositivo` (`comMenu?`) é a moldura de vitrine** — arredondada, com sombra, `overflow: hidden` (necessário aqui: os filhos retos têm fundo próprio e precisam ser recortados pelo raio da moldura). Por dentro fica um `.lp-painel`, que só abre a segunda coluna (206px de sidebar) com o modificador `.lp-painel-com-menu` — rotas e manutenções são um card só, sem sidebar.
- **Dentro do `Dispositivo` as réguas do painel real (2px) afinam para 1px** — `.lp-painel-aside`, `.lp-painel-cab` e o cabeçalho de `.lp-painel .table` sobrescrevem só a espessura, nunca a cor (`var(--color-divider)` continua vindo do design system). Em escala reduzida a régua de 2px pesa mais do que no painel de verdade; a cor idêntica é o que garante que o mock ainda "é" a UI real, só menor.
- **As demais rolagens horizontais (`.lp-compara`, `.lp-matriz-cartao`) NÃO levam `overflow: hidden`.** Elas não têm fundo próprio nas linhas, então não há nada pra recortar — e recortar quebraria a rolagem: o `.lp-rolagem` em volta só enxerga overflow que realmente transborda por um ancestral sem `overflow: hidden` no meio do caminho.
- **Sem fonte mono.** A v3 usava IBM Plex Mono para placa/km/data — removida (o painel de verdade também não usa mono, então a mock não precisava). Números tabulares vêm de `font-variant-numeric: tabular-nums` em Archivo. `index.html` carrega só Archivo agora.
- **Vermelho (`var(--color-danger)`) e âmbar (`var(--color-warning)`) são estado de manutenção, nunca enfeite** — os mesmos tokens do design system, via `.tag-danger`/`.tag-warning` dentro do `.lp-painel`. Não inventa um sexto tom nem apaga um existente (§8.1).
- **O reset de `.lp` usa `:where()`** — `:where()` não soma especificidade, então `.lp :where(p) { margin: 0 }` vale (0,1,0) e qualquer classe abaixo o vence. Escrever `.lp p` em vez disso quebra silenciosamente as margens de `.lp-lead`, `.lp-hero-sub` e o sublinhado de `.lp-cta-mail`.
- **Espaço entre faixas via `--topo`.** `Faixa` recebe `espaco` (px, padrão 120) e escreve `--topo` inline; o CSS lê `margin-top: var(--topo, 120px)` e reduz pela metade abaixo de 700px. Não há mais régua entre faixas (era a identidade "ficha" da v3) — o espaço é a própria divisão.

Seções, na ordem: nav flutuante em pílula (âncoras, "Entrar", CTA de WhatsApp, menu `<details>` no celular) → **hero** (texto centralizado, sem imagem) → mock do painel de veículos → barra "Construído sobre" → estatísticas → "O que a planilha perde" (4 dores numeradas) → comparativo planilha × Frota360 → **Recursos** (Motoristas, Veículos, Rotas, Manutenções) → **Como funciona** (3 passos) → Rotas (texto + mock) → **Manutenção** (mock da lista + catálogo de tipos + destaque) → **Permissões** (matriz) → Segurança (3 cards) → Objeções (3 citações) → **Dúvidas** (7 perguntas em `<details>`) → CTA azul com formulário de demonstração → rodapé. `ANCORAS` segue essa mesma ordem de leitura.

- **Sem peça de assinatura no hero.** A v4 original tinha um odômetro decorativo (fita de dígitos contando até a manutenção "vencer") ao lado do texto — removido: complexidade (dois `useEffect` com `setTimeout`/`setInterval` encadeados, `contain: paint` para não vazar a animação) para um elemento que não era nem o mock do painel nem parte do produto real. O hero agora é só texto centralizado.
- **Animação**: o "+" do FAQ e um *reveal* por `IntersectionObserver` (`useRevelarFaixas`) — cada `.lp-faixa` a partir da segunda (a primeira é o mock do painel, sempre visível) nasce oculta e aparece ao entrar na viewport. Quem prefere menos movimento **nunca recebe a classe que começa oculta** — não é a transição que se pula, é o esconder que não acontece.
- **Numeração**: "Como funciona" (passos) e "O que a planilha perde" (dores) são as duas sequências numeradas da página — a primeira por serem passos de verdade, a segunda porque o layout de vitrine do canvas de origem pede o número como elemento visual do card.
- **Acessibilidade**: toda área rolável passa pelo componente `Rolagem` (`tabIndex=0` + `role="region"` + `aria-label`), sem o que ninguém alcança as tabelas só pelo teclado.
- **Matriz de permissões**: mostra ✓ (`CheckIcon`) e ✗ (`XIcon`) — mas a palavra "Sim"/"Não" continua no DOM em `.lp-oculto`, porque um ✓ sozinho não se lê em leitor de tela. `.lp-matriz td` é `position: relative` de propósito: sem bloco contentor, o `.lp-oculto` absoluto se posiciona pelo bloco inicial, escapa do `.lp-rolagem` **e** do `overflow-x: clip` do `.lp`, e cria rolagem horizontal na página inteira abaixo de 480px.
- **Formulário de demonstração**: não existe endpoint público na API, então o envio monta um `mailto:` já preenchido com nome, empresa, e-mail e tamanho da frota. Como não há como saber se o cliente de e-mail abriu, a confirmação **não afirma que abriu** — diz o que era para acontecer e oferece WhatsApp e e-mail como saída. Trocar por um endpoint real é uma alteração local em `FormularioDemonstracao`.
- Os contatos são as constantes `WHATSAPP` e `EMAIL` no topo de [`LandingPage.tsx`](apps/web/src/pages/LandingPage.tsx) — trocar ali muda todos os links da página.
- Todos os dados dos mocks (placas, quilometragens, manutenções) são **ilustrativos**; nada vem da API.
- `index.html` carrega `<meta name="description">` e as tags Open Graph — sem elas o link colado no WhatsApp, que é o CTA principal, aparece sem prévia. Falta ainda uma `og:image`.

### 3.2 `/login` — Entrar

Divide a tela em painel de marca (escondido abaixo de `md`) e formulário.

- Campos: e-mail e senha, com botão de mostrar/ocultar senha.
- `POST /auth/login` → token e refreshToken chegam em cookie `HttpOnly` (o front nunca os lê); `tokenStorage.setSession` grava só a identidade; navega para `location.state.from` ou `/dashboard`.
- Erros da API aparecem acima do botão; link para "Esqueci minha senha".
- A logo do painel esquerdo é um link para a landing page.
- Não existe link de cadastro: contas nascem por convite.

### 3.3 `/esqueci-senha`

Formulário de um campo. `POST /auth/esqueci-senha` responde **sempre 200 neutro**, e a tela reflete isso: após enviar, mostra a mensagem da própria API (ou o texto padrão de 30 minutos de validade) sem confirmar se o e-mail existe.

### 3.4 `/redefinir-senha?token=…`

Três estados:

1. **Sem token na URL** → tela "Link inválido" com botão para pedir outro.
2. **Formulário** → nova senha + confirmação, validadas no cliente por [`validarSenha`](apps/web/src/auth/senha.ts) (≥ 6 caracteres, 1 maiúscula, 1 número, iguais) antes de chamar a API.
3. **Sucesso** → avisa que as sessões antigas foram encerradas e redireciona para `/login` em 2,5 s.

### 3.5 `/convite?token=…`

Além de nome e senha, o formulário tem **CPF e data de nascimento opcionais** — hoje é o único ponto de entrada desses dados, já que não existe tela de perfil (§9). Em branco viram `undefined` no corpo, para o back gravar nulo em vez de string vazia. O CPF usa `mascaraCpf` na digitação e vai só com os 11 dígitos.

Destino do link enviado pelo admin. Sem token, mostra "Convite inválido".

Formulário: nome, senha, confirmação e checkbox de termos (obrigatório, validado só no cliente). `POST /convite/aceitar` já devolve a sessão autenticada — o usuário cai direto em `/dashboard`, sem passar pelo login. Empresa e permissão vêm do convite, não do formulário.

---

## 4. Layout interno

Todas as telas autenticadas são embrulhadas por `AppLayout` ([`apps/web/src/components/AppLayout.tsx`](apps/web/src/components/AppLayout.tsx)):

- **Sidebar** recolhível (preferência guardada no `localStorage`), com **três** categorias para a gestão:
  - **"Dashboard"** — Visão geral, Motoristas, Veículos, Rotas, Manutenções, Abastecimentos, Despesas, Custos: o dia a dia.
  - **"Parametrização"** (Admin/Supervisor) — Tipos de manutenção, Tipos de despesa, Tipos de combustível e Postos. São os catálogos que alimentam os seletores das telas de lançamento; ficam juntos por serem a mesma **natureza de trabalho** (configurar antes de operar), não por compartilharem assunto. `/postos` entra apesar de não se chamar "tipo": é catálogo como os outros, mantido pelas mesmas pessoas e consumido do mesmo jeito pelo formulário de abastecimento.
  - **"Controle"** (só Admin) — Usuários, Convites, Auditoria.

  ⚠️ A categoria de parametrização é gated por **um** predicado (`pode.editarTiposManutencao`), e não por um por item, porque os quatro coincidem hoje em Admin/Supervisor. Se algum divergir, ele passa a precisar de guarda própria na lista e o gate vira um OR — está anotado no código.
- Para a role **Motorista** a sidebar tem **duas** categorias: "Operação" (Minhas rotas, Abastecimentos — o que ele **faz**) e "Visualização" (Veículos, Manutenções — o que ele só **consulta**). A separação é por escrita vs. leitura, não por assunto: misturar as quatro sugeria que ele pudesse editar veículo e manutenção. Ele não tem painel de frota: esconder os itens acompanha o guarda `RequireGestao`, não o substitui.
- **Header** com o avatar de iniciais, nome e papel do usuário — o bloco inteiro é o link para `/perfil` — e o botão de sair (`POST /auth/logout` → limpa tokens, limpa o cache do React Query, vai para `/login`).
- `PageHeader` padroniza título, subtítulo e o botão de ação da página.

O sino de notificações é decorativo — não há funcionalidade por trás dele ainda.

---

## 5. Telas internas

### 5.1 `/dashboard` — Visão geral

Somente leitura. Busca as três listas (`veiculos`, `motoristas`, `rotas`) e monta:

- **5 KPIs**: total de veículos, total de motoristas, rotas ativas (`de N no total`), quilometragem acumulada da frota (soma de `quilometragem`) e **km rodado no mês** (soma de `kmPercorrido` das rotas cujo `dataFim` cai no mês corrente, com a contagem de rotas encerradas no detalhe). Enquanto carrega, mostram `—`.
- **Tabela de veículos** com busca client-side (filtra por placa, nome, marca e último motorista) e uma tag para a última viagem (`Sem viagens` quando nula).
- O subtítulo mostra o horário da última atualização do cache.

Todos os cálculos são feitos no cliente — a API não tem endpoint de agregação.

### 5.2 `/motoristas`

**Somente leitura.** A tela é a projeção dos usuários com a role `Motorista` (`GET /motorista`) — não há formulário, edição nem exclusão, porque não existe cadastro separado a manter.

| Ação | Onde acontece |
|---|---|
| Ver a lista | aqui (Gestão) |
| Conceder acesso | `/convites` — role `Motorista`, como qualquer outra |
| Trocar perfil / desativar | `/usuarios` |

- Colunas: nome, e-mail, CPF, nascimento, **status** (Ativo/Inativo, que é o do usuário) e desde quando.
- **CPF e nascimento são opcionais** e mostram `—` quando vazios: a pessoa os informa ao aceitar o convite, e não há outro ponto de entrada hoje (§9).
- Cadastrar aqui recriaria a duplicação que o modelo eliminou — uma pessoa existindo como `Motorista` **e** como `Usuario`, com um vínculo frágil entre os dois.

### 5.3 `/veiculos`

Cadastro completo para a gestão; **leitura para o motorista**, que chega aqui pela sidebar para conferir odômetro, placa e quem levou o veículo por último. Os botões somem sozinhos: são controlados por `pode.editarCadastros`/`pode.excluir`, ambos falsos para ele.

- **Coluna "Situação": `Em rota` (azul) ou `Disponível` (verde)**, a partir de `emRota` — derivado pela API, não cruzado aqui. Cruzar no cliente não era opção: `GET /rota` é restrito à gestão e esta tela é visível ao motorista, que tomaria 403. A coluna acompanha abrir, encerrar **e excluir** rota, pelas invalidações de `['veiculos']` (§6.4).
- Campos do formulário: nome, marca, placa (maiúsculas automáticas) e quilometragem.
- **Detalhe importante**: `ultimoMotorista` e `dataUltimaViagem` não estão no formulário — são preenchidos pela operação de rotas. Como o `PUT` substitui o registro inteiro, a edição reenvia esses dois campos intactos, vindos do registro carregado. Alterar isso sem cuidado apaga o histórico do veículo.
- A placa é aceita nos dois formatos (`ABC1234` e `ABC1D23`) e o servidor a grava sempre em maiúsculas — o `text-transform` da tela é conveniência, não a regra.
- **Excluir veículo com rota associada é 422** (RN08): a mensagem *"Não é possível excluir um veículo com rotas associadas…"* aparece dentro do próprio `ConfirmDialog`, que recebe `erros={errosExclusao}`. É o caso mais comum de exclusão recusada nesta tela.

### 5.4 `/rotas`

| Ação | Quem |
|---|---|
| Ver / criar / editar / encerrar | Gestão (Admin / Supervisor / Operador) |
| Excluir | Admin |

Esta é a tela de **toda a frota**. O motorista não a alcança — ele tem `/minhas-rotas` (§5.9).

A rota tem um ciclo de vida: nasce **ativa** com o hodômetro de abertura e é **encerrada** por uma ação própria, que apura a quilometragem percorrida e avança o odômetro do veículo.

- Formulário: origem, destino, motorista (select), veículo (select), início e **quilometragem inicial**. Não há mais campo de "fim" nem de "situação" — a API os removeu dos requests justamente para que encerrar seja a única transição de estado (por `PUT` dava para "encerrar" uma rota sem calcular km nem tocar no odômetro).
- A **quilometragem inicial só aparece na criação**: o `PUT` não altera esse número, então exibi-lo na edição sugeriria um poder que a tela não tem.
- O formulário **sugere a quilometragem inicial** como o odômetro atual do veículo selecionado, reaplicando a sugestão quando o veículo muda, mas nunca sobrescrevendo um número digitado à mão (mesma mecânica de `/manutencoes`, comparando com a última sugestão emitida). O select de veículos mostra o km atual de cada um.
- Regras lembradas na própria tela: a quilometragem inicial não pode ser menor que o odômetro do veículo (422 com o km atual na mensagem) e, quando é maior, o odômetro é atualizado **já na abertura** — o veículo rodou fora do sistema, e o número mais recente vence. Por isso o cadastro invalida também `['veiculos']` e `['manutencoes']`.
- **Encerrar** aparece só em linha ativa e abre um `FormDialog` com km final (pré-preenchido com o odômetro atual do veículo, `min` no km de abertura) e data de fim opcional (limitada entre a data de início e hoje; em branco, a API assume "agora"). No sucesso, invalida `['rotas']`, `['veiculos']` **e `['manutencoes']`** — ver §6.4.
- Os 422 do encerramento (rota já encerrada, km final menor que o inicial, data de fim anterior à de início) caem no `ErrorList` do próprio diálogo.
- A `RotaResponse` é flat (só traz as FKs), então a tela cruza `codigoMotorista`/`codigoVeiculo` com as listas de motoristas e veículos para exibir nome e placa; sem correspondência, mostra `#id`. O mapa de veículos serve também à sugestão de km.
- A coluna **Quilometragem** mostra `kmPercorrido` (que vem persistido da API — não é recalculado aqui) e, abaixo, o intervalo `kmInicial → kmFinal`; nas rotas ativas, só o km de abertura.
- O status é **derivado**, não vem da API: `ativo` → "Ativa"; senão com `dataFim` → "Encerrada"; senão → "Inativa". A função vive em `src/lib/rota.ts` (`statusDaRota`), compartilhada com `/minhas-rotas` para que as duas telas nomeiem o mesmo estado do mesmo jeito.
- Excluir um veículo invalida também o cache de rotas, porque a lista exibe a placa dele. O **nome do motorista não precisa de cruzamento**: vem desnormalizado em `rota.nomeMotorista`, o que mantém a rota identificável mesmo depois que a pessoa é rebaixada e some da lista de motoristas.

### 5.5 `/manutencoes`

**Leitura para o motorista**, com a coluna **Custo escondida** — a API já devolve `custo: null` para essa role, então exibir a coluna renderizaria uma fileira de traços. Agendar, replanejar e concluir seguem restritos a Admin/Supervisor.


| Ação | Quem |
|---|---|
| Ver a lista | Todos |
| Agendar / editar / concluir | Admin, Supervisor |
| Excluir | Admin |

A tela de manutenção preventiva. Um registro nasce **planejado** (veículo + tipo + km previsto) e recebe os dados de execução ao ser concluído — é o mesmo registro nos dois momentos.

- **Os filtros** de veículo, situação e **período** disparam a query no servidor (`GET /manutencao?veiculoId=&status=&de=&ate=`), então entram na chave do cache: `['manutencoes', { veiculoId, status, de, ate }]`. "Cancelada" **não** é oferecida no filtro: o status existe no enum, mas nenhum endpoint o produz ainda.
- **O período é um select de opções prontas** (`Todo o período`, `Hoje`, `Últimos 7/30 dias`, `Este mês`, `Mês passado`), não dois campos de data. Dois campos soltos exigiam do usuário o que o sistema já sabe fazer, e ainda obrigavam as duas datas a baterem entre si. A conversão para `de`/`ate` é do **cliente** ([`lib/periodo.ts`](apps/web/src/lib/periodo.ts)) — a API não conhece "últimos 7 dias" e não mudou.
- **O período olha a data relevante do status**, não um campo fixo: pendência é situada pela `dataPrevista`, concluída pela `dataRealizacao`. Filtrar por uma só das duas deixaria metade da tela de fora. A tela **avisa em texto** quando há período ativo que pendência agendada só por quilometragem, sem `dataPrevista`, não aparece — ela não está em data nenhuma.
- **A lista não é reordenada no cliente** — a API já devolve pendentes primeiro e, dentro de cada grupo, o que vence antes no topo.
- **`atrasada` e `kmRestantes` vêm prontos do servidor** (recalculados a cada leitura, comparando o km previsto com a quilometragem atual do veículo). A tela só formata: `atrasada` tem precedência sobre `status` na badge, e `kmRestantes` negativo vira "3.200 km em atraso". Nada disso é recalculado aqui.
- **Cinco situações, cinco cores** (§8.1): `Atrasada` (vermelho), `Vencendo` (âmbar), `Pendente` (azul), `Concluída` (verde), `Cancelada` (cinza). **`Vencendo` é do cliente**: pendente, ainda não vencida e com `kmRestantes <= FAIXA_AVISO` (500 km, em [`lib/manutencao.ts`](apps/web/src/lib/manutencao.ts)) — é corte de leitura, não regra do servidor, porque `kmRestantes` já chega calculado. A coluna "Andamento" acompanha a cor da tag da mesma linha.
- O formulário **sugere a quilometragem prevista** como `km atual do veículo + intervaloKm do tipo`. A sugestão é reaplicada quando o veículo ou o tipo mudam, mas nunca sobrescreve um número digitado à mão (a comparação é com a última sugestão emitida).
- O select de tipos usa `apenasAtivos=true`: agendar com tipo inativo resulta em 422. `dataPrevista` ganha `min` de hoje no cadastro e nenhum limite na edição, espelhando a regra da API (o PUT permite replanejar um agendamento antigo).
- **Editar e Concluir só aparecem em linha pendente** — `PUT`/`concluir` em registro realizado retorna 422; o resto é histórico somente leitura.
- **Concluir** abre um `FormDialog` com km realizado (pré-preenchido com a quilometragem atual do veículo), data (hoje, limitada a não-futura), custo e observação. No sucesso, invalida `['manutencoes']` **e `['veiculos']`** — a conclusão pode ter avançado o odômetro do veículo, o que muda `atrasada`/`kmRestantes` das outras manutenções dele.
- **Estado vazio explícito**: empresas provisionadas antes da manutenção preventiva não receberam o catálogo padrão. Sem nenhum tipo ativo, a tela mostra um painel com link para `/tipos-manutencao` (ou o pedido para procurar um gestor) e desabilita "Nova manutenção".
- A resposta é desnormalizada (`veiculoNome`, `veiculoPlaca`, `tipoManutencaoNome`), então — ao contrário de `/rotas` — **não há cruzamento com outras listas** para montar a tabela. Veículos e tipos são buscados só para os selects do formulário e dos filtros.

### 5.6 `/tipos-manutencao` (Admin / Supervisor)

Catálogo da empresa que alimenta o seletor de agendamento.

- A lista vem **sem** `apenasAtivos` — os inativos aparecem esmaecidos, para poderem ser reativados.
- Formulário: nome (único por empresa, ≤ 100 caracteres) e intervalo em km (opcional; em branco vira `null`, não `0`). O campo "Situação" só aparece na edição, porque o POST não aceita `ativo`.
- **Inativar em vez de excluir**: cada linha tem um botão Inativar/Ativar (um `PUT` com o `ativo` invertido). O DELETE fica só para Admin e responde 422 quando o tipo já é referenciado por alguma manutenção — o diálogo de confirmação avisa disso antes.
- O intervalo é **informativo**: serve para sugerir a quilometragem no agendamento. A recorrência automática não existe na API.

### 5.7 `/usuarios` (Admin)

Gestão da equipe, tudo editado direto na linha:

- **Permissão**: `select` com as quatro roles do sistema — **`Motorista` inclusive**. Promover e rebaixar um motorista funciona igual a qualquer outro papel; é aqui que se tira o acesso de alguém que deixou de dirigir.
- **A troca passa por confirmação** (`ConfirmDialog`), não vai direto no `onChange`: o `PUT /usuario/{id}/role` derruba a sessão de outra pessoa, e esbarrar no select era fácil demais. O diálogo diz o que muda de fato — `de X para Y`, que a sessão cai, e o que a pessoa ganha ou perde quando o papel envolvido é `Motorista`. Cancelar não precisa desfazer nada: o select é controlado por `usuario.role` e volta sozinho ao valor atual.
- Os erros das duas ações confirmadas aparecem **dentro do diálogo**, junto do que os provocou — é onde cai o 422 do "último admin ativo", tanto ao rebaixar quanto ao desativar. O `ErrorList` do topo sobrou para o único caminho sem diálogo: reativar.
- **Status**: botão Ativar/Desativar → `PUT /usuario/{id}/ativo`. **Desativar passa pela mesma confirmação** — tira o acesso e derruba a sessão; o diálogo lembra que nada é apagado e, no caso de um motorista, que rotas em andamento continuam abertas para a gestão encerrar. **Ativar vai direto**: devolve acesso e não revoga nada.
- As duas confirmações compartilham um estado só (`Confirmacao`, união discriminada), então nunca há dois diálogos disputando a atenção.
- A própria conta do usuário logado aparece marcada com "(você)" e tem os dois controles desabilitados.
- A tela avisa que alterar permissão ou desativar **encerra a sessão** do alvo (a API revoga o refresh token).

### 5.8 `/convites` (Admin)

- Botão "Novo convite" abre o modal: e-mail + permissão. A descrição do papel selecionado é mostrada abaixo, junto do aviso de que reenviar invalida o convite pendente anterior.
- **`Motorista` é só mais uma opção do select de permissão** — o formulário não muda, não pede nada a mais, e o convite segue o mesmo caminho das outras roles.
- Após criar, o modal fecha e o **link em claro** retornado pela API aparece num painel destacado **na página**, com botão "Copiar link" — em dev o e-mail só vai para o log da API, então esse é o caminho prático. O link fica fora do modal de propósito: quem copia precisa dele depois de enviar, não durante o preenchimento.
- Tabela com status **derivado no cliente**: `utilizadoEm` → "Utilizado" (verde); `expiraEm` no passado → "Expirado" (âmbar); senão "Pendente" (azul). Os três têm cor própria: expirado é uma falha que pede reenvio e antes ficava idêntico a um aceito — os dois eram neutros (§8.1).
- Convites não utilizados podem ser cancelados; os utilizados mostram a data do aceite no lugar do botão.

### 5.9 `/minhas-rotas` (Motorista)

A tela do motorista, e a única que ele enxerga. O recorte é **do servidor** (`GET /rota/minhas` usa o `sub` do token): não há filtro no cliente, e nem poderia haver — filtro de cliente não é isolamento.

| Ação | Quem |
|---|---|
| Ver as próprias rotas / abrir / encerrar | Motorista |
| Editar / excluir | ninguém aqui — é da gestão, em `/rotas` |

Layout pensado para uso rápido, não para auditoria:

1. **Rota em andamento** em destaque no topo (contorno grosso): origem → destino, veículo, data de abertura, km de saída, e um botão único **Encerrar rota**.
2. **Abrir rota** no cabeçalho, desabilitado enquanto houver rota ativa (com o motivo no `title`) ou se não houver veículo cadastrado. O formulário **não tem seletor de motorista**: o `POST /rota` sai sem `codigoMotorista` (`AbrirMinhaRotaRequest`), porque a API grava o id do usuário logado e ignora o corpo. A sugestão de quilometragem pelo veículo escolhido é a mesma de `/rotas`; `dataInicio` já vem com hoje.
3. **Histórico** — tabela das rotas encerradas, com km percorrido e o intervalo de odômetro.

**Aviso de manutenção.** Ao escolher o veículo no formulário de abrir rota, as pendências daquele veículo aparecem logo abaixo do seletor — contorno vermelho quando há alguma atrasada. Vem de `GET /manutencao?status=Pendente` numa consulta só para a frota (a lista é curta e o cruzamento é local, em vez de uma consulta por veículo selecionado). **Não bloqueia a abertura**: informa no momento em que a informação decide algo, e quem decide é o motorista.

Cache: chave própria **`['rotas', 'minhas']`** — o conteúdo é um recorte de `['rotas']`, e as duas telas nunca convivem na mesma sessão. Abrir invalida `['rotas','minhas']` e `['veiculos']`; encerrar invalida as mesmas duas. Não invalida `['manutencoes']`: o motorista não tem acesso a esse endpoint, então a chave nunca está povoada na sessão dele (a cadeia rota → veículo → manutenção continua valendo do lado da gestão — §6.4).

Os 422 do encerramento caem no `ErrorList` do próprio `FormDialog`, igual a `/rotas`.

### 5.9.1 `/abastecimentos` (todos os papéis)

A **única tela que todo mundo lê e escreve**: quem abastece na estrada é o motorista, no pátio é o operador.

O apontamento é **fiscal** desde 03/09/2026 — veículo, motorista, **combustível, posto, litros, valor do litro, odômetro e nota fiscal**, mais frentista (opcional), data e observação. A versão anterior era deliberadamente curta, apostando que precisão não se paga no posto; a aposta foi revista com o stakeholder, porque sem litros nem odômetro **km/l e R$/l eram impossíveis de apurar** — e é o que a gestão de frota precisa medir.

São treze campos, o maior formulário do app — e por isso o modal os divide em **quatro `SecaoCampos`**: **Veículo e motorista** (só no cadastro), **Abastecimento** (combustível, litros, R$/litro, valor total, odômetro, data, mais os avisos de odômetro retroativo e de consumo estimado), **Dados do posto** (posto, nota fiscal, frentista) e **Observação**. É a tela que justifica o agrupamento: numa fileira única os treze campos não têm hierarquia nenhuma.

| Ação | Quem |
|---|---|
| Ver a frota inteira | Admin, Supervisor, Operador |
| Ver **o que é dele** | Motorista |
| Lançar e corrigir | Todos |
| Excluir | Admin |

- **O recorte do motorista é do servidor** (sai do `sub` do token), como em `/minhas-rotas`: um `motoristaId` enviado por ele é sobrescrito — filtro de cliente não é isolamento. Consequência prática na tela: **toda linha que o motorista enxerga é dele**, então o botão de corrigir não precisa de condicional por dono. Para ele a coluna "Quem lançou" some (seria sempre alguém da gestão ou ele mesmo) e o filtro por motorista também.
- **"Motorista" e "Quem lançou" são pessoas diferentes** quando a gestão lança em nome de alguém: o gasto é do motorista, o registro é de quem digitou. O recorte do motorista é pelo **primeiro** — ele enxerga o que o supervisor lançou **para** ele.
- **O campo motorista muda de natureza pelo papel**: para a gestão é um `<select required>` alimentado por `['motoristas']`; para a role Motorista é um `input disabled` com o próprio nome, e o corpo nem leva o campo — a API o resolve pelo token. O `useQuery` de motoristas é `enabled: !motorista`, porque `GET /motorista` é restrito à gestão e devolveria **403** para ele.
- **Trava de veículo por rota aberta**: tendo o motorista uma rota ativa (`['rotas','minhas']`, `r.ativo` — a mesma derivação de `/minhas-rotas`), o select mostra **só o veículo da rota**, já pré-selecionado, e o formulário diz com qual carro ele está. Sem rota aberta, a lista completa. **A trava não é só visual**: mandar outro veículo devolve **422** do servidor.
- O filtro de período usa o mesmo select de `/manutencoes` (`lib/periodo.ts`); ao lado dele, a gestão filtra por veículo e por motorista.
- ⚠️ **O valor total é readonly e não vai no corpo.** A tela calcula `litros × valor do litro` para o usuário conferir na hora, mas quem grava é o servidor, que recalcula e ignora qualquer `valor` recebido. O input é `readOnly` com `tabIndex={-1}` — espelho, não entrada.
- ⚠️ **O abastecimento passou a mexer no odômetro do veículo.** É o **terceiro** caminho que o avança, ao lado de rota e manutenção, e como eles só anda para frente: um lançamento retroativo com km menor é aceito e não retrocede a ficha. Por isso a mutation invalida **quatro** chaves — `['abastecimentos']`, `['custos']`, `['veiculos']` e `['manutencoes']` (§6.4).
- **Odômetro menor que a ficha do veículo dispara um aviso, não um erro.** O servidor aceita (pode ser lançamento retroativo — o caminhão rodou desde a data do abastecimento) e apenas não retrocede a quilometragem. Mas o mesmo formato tem cara de erro de digitação, e um número furado envenena o km/l daquele veículo depois — então a tela compara com `['veiculos']` e avisa em `var(--color-warning)`, **sem bloquear o envio**. Bloquear com 422 no servidor foi descartado: mataria o lançamento retroativo, que é caso comum e não exceção.
- **A tela estima o km/l enquanto a pessoa digita.** Assim que odômetro e litros fecham, aparece uma linha dizendo desde qual abastecimento a conta parte: `Desde o abastecimento de 28/08 (152.340 km): 500 km ÷ 48,5 L ≈ 10,3 km/l`. É o método tanque a tanque — os litros de agora repõem o que foi queimado no trecho. A referência é o abastecimento de **maior odômetro abaixo do digitado** (ordenar por odômetro e não por data é o que impede lançamento retroativo de virar km negativo), e em modo de correção o próprio registro fica de fora. Some no primeiro abastecimento do veículo, onde não há de onde partir.
- A prévia usa uma query própria — `['abastecimentos', 'doVeiculo', id]`, **sem recorte de data**, porque o abastecimento anterior pode ser de qualquer época e o filtro da listagem (que abre no mês corrente) esconderia justamente a referência. Só busca com o formulário aberto; o prefixo `['abastecimentos']` faz o `invalidar()` já existente alcançá-la.
- ⚠️ **Para a role Motorista a prévia pode sair inflada, e é por isso que a referência aparece nomeada.** A lista dele vem recortada pelo servidor: se o abastecimento anterior daquele caminhão foi de outra pessoa, a conta vai pegar um lançamento mais antigo dele mesmo — km maior, litros do tanque errado. Mostrar a data e o odômetro da referência deixa isso visível em vez de silencioso; corrigir exigiria afrouxar o recorte, o que não vale a troca.
- **Combustível e posto vêm dos catálogos** (`/tipos-combustivel` e `/postos`), carregados com `apenasAtivos: true` e **sem `enabled`**, ao contrário de `['motoristas']`: a API abre a leitura dos dois a todos os papéis justamente para o motorista conseguir lançar. Sem catálogo cadastrado o botão de novo lançamento fica desabilitado, com o motivo no `title` — mesma mecânica de "sem veículos"/"sem motoristas".
- **Veículo e motorista não são editáveis na correção** — trocar qualquer um reatribuiria o gasto. Para isso, exclua e lance de novo; na correção a seção inteira "Veículo e motorista" some, e a `descricao` do modal diz o motivo. Todo o resto do apontamento é corrigível.
- A rota é **contexto derivado**: a API vincula sozinha quando há rota aberta do motorista naquele veículo, e a tabela mostra "Origem → Destino" no lugar do modelo. Ninguém escolhe rota na tela.
- Litros e R$/litro dividem uma célula na tabela (`48,5 L` com `R$ 6,19/L · 152.340 km` embaixo), no mesmo formato de duas linhas da célula de veículo — a tabela já tem nove colunas.
- Rodapé com o total **do que está filtrado** (quantidade e valor), não da frota inteira.

Cache: `['abastecimentos', filtro]`, com `filtro` incluindo `motoristaId`.

### 5.9.2 `/custos` (gestão)

Onde o gasto da frota fica visível numa tela só. Antes dela, a resposta para "quanto esse veículo custou em agosto" exigia abrir duas telas e somar à mão: `/abastecimentos` totalizava no cliente, `/manutencoes` exibia a coluna `Custo` e nem isso. Hoje são **três** origens — abastecimento, manutenção concluída e despesa avulsa (§5.9.3).

**Somente leitura, e de propósito.** Não há botão "Novo", `FormDialog` nem `RowActions`: o lançamento continua acontecendo nas telas de origem. Do lado da API não existe tabela de custos — as duas origens são unidas na leitura (§ 8.2 do contexto-api), e é isso que faz corrigir um valor em `/abastecimentos` corrigir o total aqui.

**Só gestão.** O endpoint devolve totais da frota inteira, que é justamente o que o motorista não pode ver — a API já esconde dele o custo da manutenção. Sendo a tela fechada na porta (`Roles.Gestao` → 403), nenhum valor precisa ser escondido condicionalmente aqui dentro.

A tela tem **três** blocos, nesta ordem — a tabela de lançamentos era o quarto e saiu: uma lista longa embaixo do resumo competia com ele pela atenção, e quem abre `/custos` quer o total, descendo ao lançamento só quando um número não fecha.

1. **Faixa de KPIs** no formato do dashboard, em **seis** cartões: custo total (com a contagem de lançamentos), combustível, manutenção e despesas (cada um com sua participação no total), **custo por km** e **consumo médio**. Os números vêm somados do banco, não de `reduce` no cliente.
2. **Gráfico de evolução mensal** — barras empilhadas por origem, some quando o período cabe num mês só. Feito com divs e altura em porcentagem: uma biblioteca de gráfico seria a primeira dependência de front do projeto para desenhar três retângulos. As três séries usam degraus da rampa do acento (`--color-accent-700`, `-400` e `-200`), **não** as classes `.tag-*`: aquelas são reservadas a situação (§8.1), e série de gráfico é categoria.
3. **Tabela por veículo** — combustível, manutenção, despesas, total, km rodado e R$/km, do maior total para o menor. A última coluna traz um botão **"Ver"** por linha, e é por ele que se chega aos lançamentos.

**Os lançamentos são detalhe sob demanda.** O botão da linha abre o `PainelDialog` `LancamentosDoVeiculoDialog`, que consulta `GET /custo` com o **recorte da tela** (período, origem, motorista) mais o `veiculoId` daquela linha — então o que se vê ali explica exatamente o número que foi clicado, nunca um recorte diferente. Paginação do servidor, com o mesmo seletor de 10/15/20 do resto do painel (teto de 100 no servidor). Mudar qualquer filtro **fecha o modal**: mantê-lo aberto sobre um recorte que mudou embaixo dele diria uma coisa e mostraria outra.

⚠️ **Não há mais uma visão de "todos os lançamentos" sem escolher veículo.** É a troca consciente por uma tela de resumo limpa; o caminho para a lista crua de um período é o filtro da própria tela de origem (`/abastecimentos`, `/despesas`, `/manutencoes`).

**Dois avisos em `tag-warning`, e ambos existem porque sem eles o número mente:**

- **`N manutenções concluídas sem custo informado não entram neste total`** — `custo` é opcional em `ConcluirManutencaoRequest`, e quem concluiu sem preencher some da soma. A contagem vem do servidor (`manutencoesSemCustoInformado`).
- **`Manutenção não é atribuída a motorista`**, quando há filtro de motorista ativo. O recorte por pessoa devolve **abastecimentos e despesas** (multa tem dono), mas nunca manutenção — é a única das três origens que some inteira nesse filtro. Sem o aviso, "custo do motorista X = R$ 800" seria lido como se incluísse oficina.

Outros detalhes que decidem se o número é confiável:

- **O período começa em "Este mês"**, não em "Todo o período": um total de todos os tempos não responde pergunta nenhuma e ainda faz a primeira carga ser a mais cara possível. É a única tela cujo `FiltroPeriodo` não abre em `todos`.
- **Custo por km é `—`, nunca zero, quando não houve rota encerrada no período.** Sem denominador não existe métrica, e zero afirmaria que a frota rodou de graça. Rota ainda aberta não tem `kmPercorrido`, então o mês corrente subestima o km e **superestima** o R$/km.
- ⚠️ **Os dois KPIs de km medem coisas diferentes, e o detalhe de cada um diz qual.** O R$/km divide pelo km das **rotas encerradas** (`"2.000 km em rotas encerradas"`); o consumo médio divide pelo **odômetro dos abastecimentos** (`"3.100 km pelo odômetro"`), porque combustível é queimado dentro e fora de rota e o km das rotas subestimaria o consumo. Ver os dois números lado a lado e não entender por que divergem seria pior do que a divergência — daí o detalhe ser obrigatório nos dois cartões.
- **Consumo médio é `—` quando o veículo teve menos de dois abastecimentos no período.** Sem intervalo entre dois pontos não existe consumo. Os litros do primeiro abastecimento do período são descontados do denominador: eles pagaram o trecho *anterior* a ele (§ 8.2 do contexto-api).
- ⚠️ **Filtrar por motorista superestima o consumo**, e a tela mostra uma segunda `tag tag-warning` dizendo isso: o recorte deixa só os abastecimentos daquela pessoa, mas o odômetro continua saltando os dos outros, então o km cobre trechos que ela não abasteceu.
- **Veículo que rodou sem custo lançado aparece com total zero.** É o caso que mais merece ser visto — ninguém lançou o abastecimento —, e mantê-lo faz as colunas fecharem com o km total.
- Filtros no servidor (veículo, motorista, origem, período); qualquer mudança **volta para a página 1**. Botão **Atualizar** no cabeçalho, como em `/auditoria`.
- `formatCustoPorKm` (`lib/custo.ts`) usa até 4 casas: o valor costuma ficar abaixo de um real, e duas casas transformariam a diferença entre veículos em "R$ 0,50" para todo mundo.

Cache: **`['custos', filtro]`** para a lista e **`['custos', 'resumo', recorte]`** para os totais — o recorte do resumo não carrega paginação, senão trocar de página refaria as somas. Ao contrário de `['auditoria']`, esta chave **é** invalidada de fora: ver a segunda cadeia longa em §6.4.

### 5.9.3 `/despesas` (gestão)

Onde entra o custo que não tinha lugar nenhum: pedágio, multa, IPVA, seguro, licenciamento. É a **terceira origem** de `/custos`, e a única cuja tabela é fonte de verdade — as outras duas são lidas das telas de abastecimento e manutenção.

**Só gestão**, Operador incluído. O Motorista não vê a tela nem o endpoint (403): o lançamento é administrativo e ele não enxerga valor de frota.

| Ação | Quem |
|---|---|
| Ver e lançar | Admin, Supervisor, Operador |
| **Excluir** | **Admin e Supervisor** ⚠️ |

⚠️ **A exclusão pelo Supervisor é a única do app que não é exclusiva do Admin** (§7). Na tela isso é `pode.excluirDespesa`, entrada separada de `pode.excluir` de propósito — afrouxar aquela afetaria todas as outras telas.

Mesmo formato de `/abastecimentos`: cadastro em `FormDialog`, filtros, e rodapé com o total **do que está filtrado**. Duas seções no modal — **Despesa** (veículo, tipo, motorista) e **Lançamento** (valor, data, observação). As diferenças:

- **O motorista é opcional** e o select abre em "Não atribuída". Multa tem dono; IPVA e seguro não. É esse campo que faz o filtro por motorista de `/custos` alcançar a despesa.
- **O veículo é obrigatório** — sem ele o resumo por veículo não fecharia com os totais.
- **A correção alcança todos os campos**, inclusive veículo, tipo e motorista. Em `/abastecimentos` a tela diz "exclua e lance de novo" porque a troca reatribuiria um gasto sujeito a recorte por dono; aqui não há recorte, e a auditoria grava o diff campo a campo.
- **O select de tipo mostra só os ativos** (`['tiposDespesa', 'ativos']`) — lançar em tipo aposentado devolve 422. Quando não há nenhum tipo ativo, um aviso `tag-warning` aparece e o botão "Nova despesa" fica desabilitado: é melhor do que deixar abrir um formulário que não tem como ser enviado.
- Período padrão "Últimos 30 dias" — é uma tela de lançamento, não de análise como `/custos`.

Cache: `['despesas', filtro]`. Toda mutação invalida também `['custos']` (§6.4).

### 5.9.4 `/tipos-despesa` (Admin / Supervisor)

Gêmeo de `/tipos-manutencao`, sem o campo de intervalo em km. Catálogo por empresa, com nome único, semeado no provisionamento (Pedágio, Multa de trânsito, IPVA, Licenciamento, Seguro, Lavagem, Estacionamento) — as empresas que já existiam foram semeadas pela migration, então a tela nunca abre vazia num cliente antigo.

- **Inativar é o caminho, não excluir**: o atalho na linha tira o tipo do seletor de lançamento sem apagar o histórico. O DELETE de um tipo em uso devolve 422 dizendo exatamente isso, exibido dentro do `ConfirmDialog`.
- Exclusão é **só Admin** aqui — a exceção do Supervisor vale para a despesa, não para o catálogo.
- ⚠️ Renomear um tipo muda o que **três** telas exibem: o catálogo, a lista de despesas (que desnormaliza o nome) e a coluna Categoria de `/custos`. Por isso a mutação invalida as três chaves (§6.4).

### 5.9.5 `/tipos-combustivel` e `/postos` (Admin / Supervisor)

Os dois catálogos que sustentam o apontamento de abastecimento, no mesmo formato de `/tipos-despesa`.

| | `/tipos-combustivel` | `/postos` |
|---|---|---|
| Campos | Nome | Nome, CNPJ (opcional), Cidade (opcional) |
| Seed no provisionamento | Diesel S10/S500, Gasolina comum/aditivada, Etanol, GNV, ARLA 32 | **nenhum** — cada empresa credencia a sua rede |
| Atalho na linha | Inativar / Ativar | Descredenciar / Credenciar |

- ⚠️ **A tela é de Admin/Supervisor, mas o endpoint de leitura é aberto a todos os papéis.** É a diferença em relação a `/tipos-despesa`: o motorista lança abastecimento e precisa dos dois catálogos para preencher o formulário. `pode.editarTiposCombustivel`/`pode.editarPostos` escondem só a tela e o item de menu.
- **Inativar é o caminho, não excluir**, como no catálogo de despesa: o DELETE de um item em uso devolve 422 pedindo exatamente isso, exibido dentro do `ConfirmDialog`.
- ⚠️ **Renomear muda o que duas telas exibem** — o catálogo e a lista de abastecimentos, que desnormaliza os dois nomes. **Não** muda `/custos`: a categoria da linha de custo do abastecimento é a constante "Combustível", não o nome do tipo. Por isso a mutação invalida duas chaves, e não três como a de tipo de despesa (§6.4).

### 5.10 `/auditoria` (Admin)

Trilha do que a equipe alterou. **Somente leitura** — não há botão "Novo" nem `RowActions`, porque a API não expõe caminho para alterar ou apagar uma linha (nem para o Admin).

- **Filtros no servidor**, como em `/manutencoes`: o quê (entidade), ação, quem (select alimentado por `['usuarios']` — a tela é Admin, a query já existe) e período de/até. Qualquer mudança de filtro **volta para a página 1**; sem isso a tela abriria vazia ao filtrar estando na página 4.
- Colunas: **Quando** (`formatDateTime`), **Quem** (nome + o papel *do momento da ação*, que vem gravado na linha e não é o papel atual), **Ação** (`tag`), **Registro** (`Entidade #id`) e **O que aconteceu** (a `descricao` pronta que vem do servidor — nunca montada no cliente).
- A cor da tag sinaliza **consequência, não entidade** (§8.1): `tag-danger` para Excluiu/Desativou, `tag-warning` para AlterouPermissao/Cancelou, `tag-accent` para Criou, `tag-success` para Concluiu/Encerrou/Ativou/Aceitou. `Atualizou` fica neutro **de propósito** — é a ação mais comum da trilha, e colori-la afogaria o resto. Numa tabela longa, colorir por entidade viraria arco-íris.
- **Linha expansível** quando há diff: clicar abre uma sublinha com `campo · de → para`, mais o IP de origem. Sem diff (criação, exclusão) a seta nem aparece — não há o que abrir.
- Os valores do diff chegam em cultura invariante, de propósito: o histórico não depende de quem o escreveu. A tela converte datas ISO para pt-BR na leitura; o resto passa direto.
- Rodapé com o componente `Paginacao` (25 por página, teto de 100 no servidor) e botão **Atualizar** no cabeçalho.

Cache: **`['auditoria', filtro]`**, no padrão de `['manutencoes', filtro]`. **Sem cross-invalidation** — ver §6.4.

### 5.11 `/perfil` (qualquer autenticado)

Formulário único, sem tabela: **nome, CPF e data de nascimento** do próprio usuário. É o caminho do direito de correção da LGPD (Art. 18, III) e a única tela alcançável por todas as roles — inclusive o Motorista, que é justamente quem tem CPF. Chega-se a ela pelo bloco do avatar no header.

- Carrega por `GET /usuario/perfil` (`['perfil']`) e salva por `PUT /usuario/perfil`. Nenhum id trafega: o alvo é o dono do token.
- **E-mail e papel aparecem como texto, não como campo**, com a razão escrita ao lado: o e-mail é a chave de acesso e o papel é concedido pelo administrador. Mostrá-los evita a pergunta de onde alterar; deixá-los editáveis prometeria o que a API não faz.
- CPF com `mascaraCpf` na digitação e só os 11 dígitos no envio (mesmo par `mascaraCpf`/`somenteDigitos` de `/convite`). Em branco vira `undefined` → a API grava `null`, e a tela avisa que apagar o campo remove o CPF do cadastro.
- CPF já usado por outro usuário da mesma empresa → **422**, exibido no `ErrorList`.
- O formulário é preenchido a partir da query com um **ajuste de estado durante o render** guardado pelo `id` já carregado, e não num `useEffect`: a fonte é o próprio estado do componente, e a guarda impede que um refetch descarte o que está sendo digitado.
- No sucesso, invalida `['perfil']`, `['motoristas']` e `['usuarios']` (as duas listas exibem nome e CPF) **e** corrige o nome guardado na sessão via `tokenStorage.atualizarNome` + `notificarMudancaDeSessao` — sem isso o header exibiria o nome antigo até o token girar, porque o claim `name` do JWT não é reemitido na hora.

---

## 6. Camada de API e sessão

### 6.1 `apps/web/src/api/http.ts`

- `baseURL` = `VITE_API_URL`; `withCredentials: true` — o JWT e o refresh token viajam em cookie `HttpOnly; Secure; SameSite=None` setado pelo servidor, nunca em `localStorage`/header `Authorization`. Sem `withCredentials` o navegador não anexaria o cookie numa chamada cross-origin (front e API vivem em portas/domínios diferentes).
- Interceptor de response: em **401**, dispara um único refresh (`refreshInFlight` como lock — a rotação do refresh token invalida o anterior, então dois refreshes paralelos quebrariam o segundo), refaz a requisição original uma vez e, se o refresh falhar, limpa a sessão e força `/login`. `POST /auth/refresh` não leva corpo: o refresh token vai no cookie, anexado sozinho pelo navegador — o refresh renova esse mesmo cookie, e a requisição original é só repetida.
- Rotas anônimas (`/auth/login`, `/auth/refresh`, `/auth/esqueci-senha`, `/auth/redefinir-senha`, `/convite/aceitar`) são isentas: 401 ali é credencial inválida, não sessão expirada.
- `unwrap()` desembrulha o envelope `{ sucesso, mensagem, dados, erros }` e lança `ApiError` quando `sucesso: false`.

### 6.2 Erros

[`mensagensDeErro`](apps/web/src/api/errors.ts) transforma qualquer falha numa lista de strings: usa `erros` do envelope quando existe (alimenta formulários), cai para `mensagem` (em português, serve de resumo) e detecta API fora do ar. Toda tela renderiza isso pelo componente `ErrorList`.

### 6.3 Sessão

- [`tokenStorage`](apps/web/src/api/tokenStorage.ts) guarda só `frota360.user` (nome, e-mail, role) no `localStorage` — puramente para a UI exibir quem está logado. Token e refresh token não passam por aqui: chegam do servidor em cookie `HttpOnly`, invisível a JavaScript (mitiga exfiltração por XSS — era o achado do React Doctor `auth-token-in-web-storage`), e nenhum código do front os lê ou escreve.
- [`useSession`](apps/web/src/auth/useSession.ts) expõe o usuário logado de forma reativa via `useSyncExternalStore`, ouvindo o evento `storage` (outras abas) e um evento próprio `frota360:sessao` (esta aba — `localStorage` não notifica quem escreveu).
- O papel usado pela UI vem desse cache local; ele só é atualizado quando o token renova. Mudança de papel pode levar até 1 h para refletir na interface — o servidor, porém, já recusa a ação antes disso.

### 6.4 Chaves do React Query

`['motoristas']`, `['veiculos']`, `['rotas']`, `['rotas', 'minhas']`, `['usuarios']`, `['convites']`, `['perfil']`, `['manutencoes', filtro]`, `['abastecimentos', filtro]`, `['auditoria', filtro]`, `['custos', filtro]`, `['custos', 'resumo', recorte]`, `['despesas', filtro]`, `['tiposDespesa']`, `['tiposDespesa', 'ativos']`, `['tiposCombustivel']`, `['tiposCombustivel', 'ativos']`, `['postos']`, `['postos', 'ativos']`, `['tiposManutencao']` e `['tiposManutencao', 'ativos']` — invalidadas após cada mutação da respectiva tela (e cruzadas quando uma exclusão afeta outra lista). `staleTime` de 30 s e sem retry em erro < 500 ([`apps/web/src/lib/queryClient.ts`](apps/web/src/lib/queryClient.ts)).

⚠️ `['rotas']` e `['rotas','minhas']` são **listas diferentes**, não pai e filho: a segunda vem de outro endpoint e traz só as rotas do motorista logado. Invalidar pelo prefixo `['rotas']` alcançaria as duas, o que é inofensivo apenas porque nenhuma sessão usa as duas telas. Ao mexer nisso, invalide a chave exata.

Cruzamentos que não são óbvios, conferidos no código:

- **Excluir veículo** invalida `['rotas']` ([VeiculosPage.tsx:56](apps/web/src/pages/VeiculosPage.tsx#L56)) — a tabela de rotas exibe a placa dele. Motorista não tem exclusão: é um usuário, e usuário só é desativado.
- **Concluir uma manutenção** invalida também `['veiculos']` ([ManutencoesPage.tsx:165](apps/web/src/pages/ManutencoesPage.tsx#L165)) — o odômetro pode ter avançado.
- **Abrir** ([RotasPage.tsx:118-119](apps/web/src/pages/RotasPage.tsx#L118-L119)) e **encerrar** ([RotasPage.tsx:139-140](apps/web/src/pages/RotasPage.tsx#L139-L140)) uma rota invalidam `['rotas']`, `['veiculos']` e `['manutencoes']`. É a cadeia mais longa do app: rota → veículo → manutenção. Os dois momentos mexem no odômetro (a abertura quando `kmInicial` é maior que o atual; o encerramento quando `kmFinal` é), e é do odômetro que `atrasada` e `kmRestantes` dependem. Sem invalidar a ponta da cadeia, o alerta de atraso só apareceria no próximo `staleTime`.
- **Excluir uma rota** invalida `['rotas']` **e `['veiculos']`** — se a rota estava aberta, o veículo volta a `Disponível` na coluna Situação de `/veiculos` (§5.3). Não invalida `['manutencoes']`: excluir não mexe no odômetro.
- Em `/minhas-rotas`, **abrir** e **encerrar** invalidam `['rotas','minhas']`, `['veiculos']` **e** `['manutencoes']` — a mesma cadeia da tela de gestão, agora que o motorista também lê manutenções e a tela mostra a pendência do veículo escolhido.
- **Salvar o perfil** invalida `['perfil']`, `['motoristas']` **e** `['usuarios']` ([PerfilPage.tsx](apps/web/src/pages/PerfilPage.tsx)) — as duas listas exibem nome e CPF de quem acabou de se corrigir. É o único cruzamento que parte de uma tela sem tabela.
- ⚠️ **Lançar, corrigir ou excluir abastecimento invalida quatro chaves**: `['abastecimentos']`, `['custos']`, `['veiculos']` **e** `['manutencoes']`. Desde 03/09/2026 o apontamento carrega odômetro e **avança a ficha do veículo** — o abastecimento voltou para a cadeia rota → veículo → manutenção, agora como o **terceiro** caminho que move o odômetro. `['custos']` continua porque o valor é metade do que `/custos` soma. (Este bullet já disse o contrário: enquanto o formulário não pedia odômetro, eram só duas chaves.)
- ⚠️ **`['custos']` é a segunda cadeia longa do app, e a menos óbvia.** Ela é alimentada por **quatro** telas: abastecimento (criar/corrigir/excluir), manutenção (**concluir** — é onde o custo entra —, editar, que pode trocar o veículo a que o custo é atribuído, e excluir), **despesa** (criar/corrigir/excluir) e **encerrar rota**, que apura o `kmPercorrido`: o denominador do R$/km. Encerrar rota invalida **quatro** chaves. As duas chaves de custo (lista e resumo) compartilham o prefixo `['custos']`, então uma invalidação alcança as duas de propósito — nenhuma tela usa uma sem a outra.
- **Mutação de tipo de combustível ou de posto invalida duas chaves**: a própria (`['tiposCombustivel']` / `['postos']`, prefixo que cobre o catálogo completo e a lista de ativos) e `['abastecimentos']`, porque os dois nomes são desnormalizados na listagem. **`['custos']` fica de fora de propósito**: a categoria da linha de custo do abastecimento é a constante `"Combustível"`, não o nome do tipo — ao contrário da despesa, logo abaixo.
- **Mutação de tipo de despesa invalida três chaves**: `['tiposDespesa']`, `['despesas']` e `['custos']`. O nome do tipo é desnormalizado na resposta da despesa **e** é a `categoria` da linha de custo — renomear um tipo muda o que as três telas exibem. É o cruzamento mais fácil de esquecer, porque a tela do catálogo não mostra despesa nenhuma.
- Qualquer mutação no catálogo invalida o prefixo `['tiposManutencao']`, que cobre de uma vez o catálogo completo e a lista de ativos usada no agendamento.
- ⚠️ **`['auditoria']` é a exceção deliberada: ninguém a invalida.** Praticamente toda mutação do app cria uma linha de trilha, então invalidar de dentro de cada tela espalharia acoplamento pelo front inteiro — cada mutation passaria a conhecer uma tela que ela não afeta. O `staleTime` de 30 s cobre o uso normal, e a tela tem botão "Atualizar" para quem quer ver agora.

### 6.5 Endpoints consumidos

| Módulo | Método + rota | Onde é chamado |
|---|---|---|
| **auth** | `POST /auth/login` | LoginPage |
| | `POST /auth/logout` | AppLayout (botão sair) |
| | `POST /auth/refresh` | interceptor de 401 (axios cru) |
| | `POST /auth/esqueci-senha` | ForgotPasswordPage |
| | `POST /auth/redefinir-senha` | ResetPasswordPage |
| **convite** | `POST /convite` (Admin) | ConvitesPage — a resposta traz `linkConvite` em claro |
| | `GET /convite` (Admin) | ConvitesPage |
| | `DELETE /convite/{id}` (Admin) | cancelar pendente (utilizado → 422) |
| | `POST /convite/aceitar` (anônimo) | AcceptInvitePage — **já devolve sessão autenticada**; leva `cpf`/`dataNascimento` opcionais |
| **auditoria** | `GET /auditoria?pagina=&tamanhoPagina=&entidade=&acao=&usuarioId=&de=&ate=` (Admin) | AuditoriaPage — paginado: `dados` é um `ResultadoPaginado<T>`, não um array |
| **custo** | `GET /custo?pagina=&tamanhoPagina=&veiculoId=&motoristaId=&origem=&de=&ate=` · `GET /custo/resumo?veiculoId=&motoristaId=&origem=&de=&ate=` (gestão) | CustosPage — a lista é paginada (`ResultadoPaginado<T>`); o resumo é a **única agregação servida pela API** |
| **despesa** | `GET /despesa?veiculoId=&motoristaId=&tipoDespesaId=&de=&ate=`, `POST`, `PUT /{id}`, `DELETE /{id}` (gestão; **DELETE também pelo Supervisor**) | DespesasPage |
| **tipodespesa** | `GET /tipodespesa?apenasAtivos=`, `POST`, `PUT /{id}`, `DELETE /{id}` (gestão; escrita Admin+Supervisor, DELETE só Admin) | TiposDespesaPage e o seletor de DespesasPage |
| **tipocombustivel** | `GET /tipocombustivel?apenasAtivos=` (**qualquer autenticado**, Motorista incluído), `POST`, `PUT /{id}` (Admin+Supervisor), `DELETE /{id}` (Admin) | TiposCombustivelPage e o seletor de AbastecimentosPage — a query **não** usa `enabled`, ao contrário de `['motoristas']` |
| **posto** | `GET /posto?apenasAtivos=` (**qualquer autenticado**), `POST`, `PUT /{id}` (Admin+Supervisor), `DELETE /{id}` (Admin) | PostosPage e o seletor de AbastecimentosPage; item em uso no DELETE → **422** pedindo para inativar |
| **usuario** | `GET /usuario` (Admin) | UsuariosPage, **AuditoriaPage** (select "Quem") |
| | `PUT /usuario/{id}/role` | muda permissão — revoga a sessão do alvo |
| | `PUT /usuario/{id}/ativo` | ativa/desativa — idem; último admin ativo → 422 |
| | `GET /usuario/perfil` (**qualquer autenticado**) | PerfilPage — o próprio cadastro; `GET /usuario` é Admin e não serve ao Motorista |
| | `PUT /usuario/perfil` (**qualquer autenticado**) | PerfilPage — nome/CPF/nascimento; alvo pelo token, CPF duplicado na empresa → 422 |
| **motorista** | `GET /motorista`, `GET /motorista/{id}` | MotoristasPage, RotasPage (select) — **somente leitura**: são os usuários com a role Motorista |
| **manutencao** | `GET /manutencao?status=Pendente` | MinhasRotasPage — alimenta o aviso de pendência do veículo escolhido |
| **abastecimento** | `GET /abastecimento?veiculoId=&motoristaId=&de=&ate=` | AbastecimentosPage — `motoristaId` serve à gestão; para o Motorista a API o sobrescreve com o do token |
| | `POST /abastecimento` | lançamento — a API resolve motorista (token, para a role Motorista) e rota, **calcula o `valor`** (litros × R$/l; o corpo não o envia) e **avança o odômetro do veículo**; veículo fora da rota aberta, combustível/posto inativo ou de outra empresa → **422** |
| | `PUT /abastecimento/{id}` | correção de todo o apontamento **menos** veículo e motorista; lançamento de outro motorista → 404 para ele |
| | `DELETE /abastecimento/{id}` (Admin) | exclusão |
| **veiculo** | `GET/POST /veiculo`, `GET/PUT/DELETE /veiculo/{id}` | VeiculosPage, Dashboard — a resposta traz `emRota` derivado (existe rota aberta com o veículo); placa nos dois formatos, normalizada em maiúsculas pelo servidor; DELETE com rota **ou abastecimento** associado → **422** (RN08) |
| **rota** | `GET/POST /rota`, `GET/PUT/DELETE /rota/{id}` | RotasPage, Dashboard — o POST leva `kmInicial` e **pode avançar o odômetro do veículo**; o PUT não mexe em `kmInicial`, `ativo` nem `dataFim` |
| | `GET /rota/minhas` (Motorista) | MinhasRotasPage — sem parâmetro: o motorista vem da claim |
| | `POST /rota` sem `codigoMotorista` | MinhasRotasPage (`abrirMinha`) — a API grava o id do usuário logado |
| | `POST /rota/{id}/encerrar` | encerramento — apura `kmPercorrido` e **pode avançar o odômetro do veículo**; para o motorista, rota alheia → 404 |
| **tipomanutencao** | `GET /tipomanutencao?apenasAtivos=` | catálogo (sem filtro) / select de agendamento (`true`) |
| | `POST`, `PUT /{id}`, `DELETE /{id}` | TiposManutencaoPage |
| **manutencao** | `GET /manutencao?veiculoId=&status=&de=&ate=` | ManutencoesPage — todos os filtros vão para o servidor, período incluído |
| | `POST /manutencao`, `PUT /manutencao/{id}` | agendar / replanejar (só pendente) |
| | `POST /manutencao/{id}/concluir` | conclusão — **pode avançar o odômetro do veículo** |
| | `DELETE /manutencao/{id}` (Admin) | descarte (não há endpoint de cancelar) |

---

## 7. Permissões na interface

[`apps/web/src/auth/permissions.ts`](apps/web/src/auth/permissions.ts) espelha a matriz do §5 do `CONTEXTO.md`. A UI só esconde o que resultaria em 403 — quem decide é a API.

| Ação | Admin | Supervisor | Operador | Motorista |
|---|---|---|---|---|
| Ver visão geral, motoristas e rotas da frota | ✅ | ✅ | ✅ | — |
| Ver veículos e manutenções | ✅ | ✅ | ✅ | ✅ (leitura, sem custo) |
| Criar/editar rotas da frota | ✅ | ✅ | ✅ | — |
| Ver/abrir/encerrar **as próprias** rotas | — | — | — | ✅ |
| Criar/editar veículos | ✅ | ✅ | — | — |
| Criar/editar/concluir manutenções e tipos | ✅ | ✅ | — | — |
| Lançar e corrigir abastecimento | ✅ | ✅ | ✅ | ✅ (só o que é dele) |
| Ver os custos consolidados (`/custos`) | ✅ | ✅ | ✅ | — |
| Lançar e corrigir despesa (`/despesas`) | ✅ | ✅ | ✅ | — |
| **Excluir despesa** ⚠️ | ✅ | ✅ | — | — |
| Manter o catálogo de tipos de despesa | ✅ | ✅ | — | — |
| Manter os catálogos de combustível e postos ⚠️ | ✅ | ✅ | — | — |
| Excluir qualquer registro | ✅ | — | — | — |
| Usuários e convites | ✅ | — | — | — |
| Ver a trilha de auditoria | ✅ | — | — | — |
| Editar o **próprio** cadastro (`/perfil`) | ✅ | ✅ | ✅ | ✅ |

⚠️ **Os catálogos de combustível e posto são o único caso em que a tela é mais restrita que o endpoint.** A **leitura** de `/tipocombustivel` e `/posto` é aberta a todos os papéis na API — o motorista precisa dela para lançar abastecimento —, mas as telas `/tipos-combustivel` e `/postos` são de Admin/Supervisor. A linha acima descreve a tela, não o endpoint.

⚠️ **Excluir despesa é a única exclusão que não é exclusiva do Admin.** É decisão de produto, e por isso `permissions.ts` tem uma entrada separada (`pode.excluirDespesa`) em vez de afrouxar `pode.excluir`, que continua Admin-only e serve todas as outras telas.

A linha do perfil é a única em que as quatro colunas são ✅ — e por isso `/perfil` não tem entrada em `pode.*`: um predicado que devolve `true` para todo mundo é ruído, não permissão. Corrigir o cadastro **de outra pessoa** não aparece na matriz porque não existe em papel nenhum, o Admin incluído.

Na prática: sem permissão de edição, o botão "Novo…" e o ícone de lápis somem; sem permissão de exclusão, some a lixeira; sem nenhuma das duas, a coluna "Ações" inteira desaparece.

O `Motorista` combina os dois mecanismos: nas telas que ele alcança (veículos, manutenções) valem os `pode.*` de sempre, e as que ele não alcança são barradas por `RequirePode` na rota. As entradas `ver*` de `permissions.ts` são **por tela** justamente por isso — um booleano único de "é gestão" seria mentira. `rotaInicial(role)` é o destino de todo redirecionamento.

---

## 8. Design system e componentes compartilhados

Tokens e classes em [`apps/web/src/styles/design-system.css`](apps/web/src/styles/design-system.css): fundo `#fdfaf6`, superfície `#f2ede4`, texto `#201e1d`, acento `#1f3a5f` (com rampa 100–900), perigo `#a03123`, tipografia Archivo e **raio 0 em tudo** — o visual é de réguas retas, não de cartões arredondados. **A landing pública (§3.1) tem visual próprio** (cantos arredondados, sombra) desde a v4 — a única exceção são os mocks de UI dentro de um `Dispositivo`, que reaproveitam estas mesmas classes (`.table`/`.tag`/`.btn`) porque precisam parecer com o produto real.

`index.html` carrega só Archivo (400/600/800) — o mono (IBM Plex Mono, usado pela landing até a v3) foi removido; nada no produto usa fonte monoespaçada hoje.

Classes: `.btn` (`.btn-primary`, `.btn-secondary`, `.btn-icon`, `.btn-danger`), `.field` + `.input` (`.input-underline` no login), `.tag` (ver abaixo), `.nav`, `.table`, `.dialog*`.

Os três tokens `--radius-*` já valem `0px`, então **`style={{ borderRadius: 0 }}` num botão ou campo novo é ruído** — não escreva.

O diálogo tem quatro classes além de `.dialog`/`.dialog-title`/`.dialog-body`/`.dialog-actions`, todas nascidas com a migração dos formulários para modal:

| Classe | O que faz |
|---|---|
| `.dialog-corpo` | O **único trecho rolável** do diálogo. O `.dialog` para em `85vh`; título e botões ficam fixos e só os campos rolam. O `min-height: 0` é o que permite o overflow acontecer dentro do flex |
| `.dialog-secao-titulo` | Cabeçalho de cada `SecaoCampos` — 11px, caixa alta, régua de 1px embaixo: a mesma linguagem do `<th>` da `.table` |
| `.dialog-grid` | `repeat(auto-fit, minmax(190px, 1fr))` — três colunas no modal de cadastro (760px), uma no celular, sem media query. **É ele quem decide a largura dos campos**; largura fixa no wrapper do campo quebra o grid |
| `.campo-largo` | `grid-column: 1 / -1`, para observação, aviso e nota explicativa |

⚠️ **`.dialog` precisa declarar `inset: 0` e `margin: auto` à mão.** O centramento de um `<dialog>` modal é do navegador e sai desse par na folha do UA — mas o **preflight do Tailwind v4 zera `margin` em `*`**, `<dialog>` incluído, e sem as duas linhas o diálogo cola no canto superior esquerdo da tela. Vale para os três (`ConfirmDialog`, `FormDialog` e o que vier depois), já que todos usam a mesma classe. A largura é `calc(100vw - 2rem)` no limite inferior, e não `100%`, para o backdrop continuar visível no celular.

⚠️ **O `useAbrirModalAoMontar` empurra o foco para o primeiro campo — e isso não é enfeite.** Quando o formulário é longo o bastante para `.dialog-corpo` rolar, o Chrome torna o **contêiner de rolagem** focável (é ele quem responde a PageUp/PageDown) e o `showModal()` pousa o foco ali: o anel de `:focus-visible` contorna o formulário inteiro e quem abriu o diálogo começa sem cursor em campo nenhum. O hook só age nesse caso exato — testa `document.activeElement === corpo` antes —, então o `autoFocus` do Cancelar no `ConfirmDialog` e os declarados nas telas continuam valendo.

### 8.1 Cor de situação — a tabela normativa

**Situação se sinaliza pela classe `.tag`, nunca por `style` inline.** Barra de 3px na cor do estado (`border-left: 3px solid currentColor`), fundo tonal, caixa alta e peso 600 — a mesma classe usada nos mocks de UI da landing (§3.1), não uma imitação. A barra é o que chama o olho numa tabela longa sem que o fundo precise gritar.

Cinco tokens de estado, em `:root`: `--color-accent`, `--color-success` (`#2e5c42`), `--color-warning` (`#7a5312`), `--color-danger` (`#a03123`) e a rampa neutra — cada um com seu `-bg`. **Não invente um hex novo nem um sexto tom**: a cor diz a consequência, não a entidade, e um tom por entidade transformaria a tabela em arco-íris.

| Classe | Significado | Onde aparece |
|---|---|---|
| `.tag-accent` | **acontecendo agora** | Rota `Ativa`, Manutenção `Pendente`, Convite `Pendente`, Veículo `Em rota`, Auditoria `Criou` |
| `.tag-success` | **concluído / saudável** | Rota `Encerrada`, Manutenção `Concluída`, Convite `Utilizado`, `Ativo` (motorista/usuário/tipo), Veículo `Disponível`, Auditoria `Concluiu`/`Encerrou`/`Ativou`/`Aceitou` |
| `.tag-warning` | **exige atenção em breve** | Manutenção `Vencendo`, Convite `Expirado`, Auditoria `Alterou permissão`/`Cancelou` |
| `.tag-danger` | **falhou / destrutivo** | Manutenção `Atrasada`, Auditoria `Excluiu`/`Desativou` |
| `.tag-neutral` | **sem estado** | `Inativa`/`Inativo`, Manutenção `Cancelada`, Auditoria `Atualizou` |

Duas regras que a versão anterior violava e explicam o desenho atual:

- **Cinza é ausência de estado, não "qualquer coisa que terminou".** Antes, `Encerrada`/`Concluída`/`Utilizado`/`Expirado`/`Cancelada` eram todos neutros — um convite expirado ficava idêntico a um aceito. Sucesso é verde; falha e vencimento são âmbar ou vermelho.
- **Azul é reservado ao que está em curso.** Não use `.tag-accent` para dar ênfase a um dado que não é situação — a coluna "Última viagem" do dashboard exibia uma **data** como tag azul e diluía o significado do accent no app inteiro.

Onde a cor de estado precisa aparecer **fora** de uma tag (texto de andamento em `/manutencoes`, borda do alerta em `/minhas-rotas`), use os mesmos tokens `var(--color-warning)`/`var(--color-danger)`, na mesma escala da tag da linha — o usuário lê os dois canais juntos.

Os helpers que decidem rótulo e classe vivem em `lib/`, nunca dentro da página: [`lib/rota.ts`](apps/web/src/lib/rota.ts) (`statusDaRota`) e [`lib/manutencao.ts`](apps/web/src/lib/manutencao.ts) (`badgeDaManutencao`, `estaVencendo`, `textoKmRestantes`, `FAIXA_AVISO`) — duas telas nomeiam o mesmo estado e precisam nomeá-lo igual.

### 8.2 Paginação — o corte é no cliente

`lib/paginacao.ts` é o dono da regra. **`usePaginacao(itens)`** recebe a lista já filtrada e devolve a fatia junto com as props do `Paginacao`; **`useTamanhoPagina()`** é só a preferência, para quem pagina no servidor. O tamanho (10/15/20, padrão 15) fica no `localStorage` (`frota360.itensPorPagina`) e vale para o painel inteiro — quem prefere 20 escolhe uma vez.

**Por que no cliente.** Não foi economia de trabalho: `/abastecimentos` e `/despesas` fecham com "N lançamentos · Total: R$ X" somando o **filtro inteiro**, e paginar no servidor reduziria esses números à página visível. Consertar isso exigiria um endpoint de agregação novo em cada recurso, só para desfazer uma regressão. Enquanto a lista couber numa requisição, fatiar no cliente mantém o rodapé honesto de graça. Em `/abastecimentos` a contagem chega ao componente da tabela pela prop **`quantidade`**, separada das linhas, justamente para não haver confusão entre as duas.

⚠️ **O `usePaginacao` clampa a página no render** (`Math.min(pagina, totalPaginas)`), então uma lista que encolhe por um filtro nunca deixa a tela vazia — e **nenhuma tela precisa chamar `resetarPaginacao()` ao filtrar**. Como consequência, página vazia só existe quando a lista inteira é vazia; é por isso que passar a fatia para o `empty` de um `TableStates` continua correto.

**As duas exceções paginam no servidor** e seguem regra própria: `/auditoria` e `/custos` mandam `tamanhoPagina` na consulta e **precisam voltar para a página 1 a cada filtro**, porque o clamp do cliente não alcança o que o servidor recortou. O teto do servidor é 100 nos dois validators.

Componentes reutilizados pelas telas:

| Componente | Onde | O que faz |
|---|---|---|
| `AppLayout`, `PageHeader`, `ErrorList` | `components/AppLayout.tsx` | Casca das telas internas, cabeçalho e lista de erros |
| `AuthScreen`, `AuthHeading` | `components/AuthScreen.tsx` | Casca das telas de autenticação |
| `TableStates` | `components/Table.tsx` | As linhas de carregando/erro/vazio dentro de um `<tbody>` |
| `Paginacao` | `components/Table.tsx` | Rodapé de toda listagem: seletor de itens por página (10/15/20), "X–Y de Z" e anterior/próxima. **Some quando o total cabe na menor opção** — não quando há uma página só, senão esconderia o seletor de quem tem 12 registros e quer ver 10 |
| `PainelDialog` | `components/Table.tsx` | O diálogo que só mostra conteúdo, com "Fechar" como única ação — irmão do `ConfirmDialog` (confirma) e do `FormDialog` (submete). Serve detalhe sob demanda: hoje, os lançamentos de um veículo em `/custos` |
| `FiltroPeriodo` | `components/Table.tsx` | Select de período pronto, usado por `/manutencoes` e `/abastecimentos`; a conversão para `de`/`ate` está em `lib/periodo.ts` |
| `RowActions`, `ConfirmDialog` | `components/Table.tsx` | Ícones de editar/excluir na linha e confirmação de ação consequente (exclusão ou troca de permissão — `variante="padrao"` tira o vermelho quando não é destrutiva) |
| `FormDialog`, `SecaoCampos` | `components/Table.tsx` | **Todo cadastro/edição do painel** e as transições com campos (concluir manutenção, encerrar rota). `<dialog>` nativo — `showModal()` traz trava de foco, Escape e `::backdrop`; a tabela fica visível ao fundo. `largura` é 760 nos cadastros e 520 (o default) nas transições. `SecaoCampos` agrupa os campos por categoria e o `.dialog-grid` decide a largura de cada um — largura fixa no wrapper, não; `campo-largo` para quem ocupa a linha inteira |
| `LogoMark`, `Wordmark` | `components/Logo.tsx` | Marca (versões clara e escura) |
| `icons.tsx` | — | Ícones SVG traçados, 24×24, `currentColor` |
| `lib/format.ts` | — | Datas, CPF, quilometragem, moeda, iniciais, `paraInputDate` e `hojeInputDate` para `<input type="date">` |
| `lib/rota.ts` | `/rotas`, `/minhas-rotas` | `statusDaRota` — o status derivado de `ativo` + `dataFim`, igual nas duas telas |
| `lib/periodo.ts` | `/manutencoes`, `/abastecimentos` | `PERIODOS` e `intervaloDoPeriodo` — converte o período escolhido em `de`/`ate` (hora local, `ate` inclusivo) |

---

## 9. O que ainda não existe

- **Ordenação por coluna** — nenhuma tabela deixa clicar no cabeçalho para reordenar; a ordem é a que o servidor devolve. (**Paginação deixou de ser uma pendência**: toda listagem pagina desde 04/09/2026 — no cliente, via `usePaginacao`, exceto `/auditoria` e `/custos`, que já paginavam no servidor. Ver §8.)
- **Biblioteca de gráfico**: não há nenhuma no `package.json`. O gráfico de `/custos` é feito com divs e altura em porcentagem — uma dependência nova não se paga por três barras empilhadas.
- **Toasts globais**: erros e sucessos são exibidos no local da ação, não há notificação central.
- **Tratamento específico de 429**: a mensagem do rate limit chega como erro comum.
- **Testes**: não há suíte no front.
- Sino de notificações e os links do rodapé/termos de uso são placeholders.
- A landing usa dados fictícios no mock do painel — nada ali reflete a base real.
- **Cancelar manutenção**: a API ainda não expõe `POST /manutencao/{id}/cancelar`, então descartar um agendamento passa pelo DELETE (Admin) e o filtro "Cancelada" nem é oferecido.
- **Atualizar só a quilometragem do veículo**: não há `PATCH` dedicado. O odômetro sobe pelo `PUT /veiculo/{id}` completo, pela conclusão de uma manutenção e — desde a RN10 — pela abertura e pelo encerramento de rotas, que é o caminho do dia a dia e o que finalmente alimenta os alertas de atraso.
- **Reabrir uma rota encerrada**: a API não expõe o caminho inverso do encerramento, e o `PUT` não mexe mais em `ativo`/`dataFim`. Corrigir um encerramento errado passa por excluir a rota (Admin) e recriá-la.
- O dashboard ainda não mostra nada de manutenção (nenhum KPI de atrasadas).
- **Corrigir o cadastro de outra pessoa**: não existe, nem para o Admin. `/perfil` (§5.11) é autoatendimento — se alguém precisa de correção e não consegue entrar, o caminho é reenviar convite ou solicitação formal ao controlador.
- **Trocar o próprio e-mail**: fora do escopo de `/perfil`. É a chave de login e exigiria reverificação, além de mexer em convite e refresh token.
- **Purga da trilha de auditoria**: a política é de **12 meses**, mas a rotina que apaga não existe — hoje nada é expurgado. Limitação declarada, não esquecida.
- **Tela do motorista em celular**: `/minhas-rotas` usa o mesmo `AppLayout` do painel, com sidebar — funciona, mas não é um layout mobile de verdade, que é o contexto natural de uso. É a próxima coisa a fazer pelo motorista.

---

## 10. Inconsistências conhecidas

- `npm run gen:api` aponta para `http://localhost:5062/openapi/v1.json` ([package.json:10](package.json#L10)), enquanto a API roda em `https://localhost:7271` conforme o `.env.development`. O script provavelmente quebra como está.

### Fuso horário — o front exibe verbatim, e isso é a política (30/08/2026)

As datas chegam da API como ISO **sem sufixo `Z`** (`"2026-08-30T00:00:00"`), e `formatDate`/`formatDateTime` ([lib/format.ts](../apps/web/src/lib/format.ts)) as interpretam como hora local. **O front não converte fuso em lugar nenhum, de propósito.**

Isso funciona porque o backend grava e devolve **hora local de Brasília**: o `DataSemFusoConverter` mapeia todo `DateTime` para `timestamp without time zone`, e o `Dockerfile` fixa `TZ=America/Sao_Paulo` para que o container concorde com a máquina de dev (detalhes em [contexto-api.md](contexto-api.md), § Banco: PostgreSQL). O valor que o usuário digita é o valor gravado e o valor exibido, sem intermediários.

Se o backend algum dia passar a mandar `Z`, `new Date("2026-08-30T00:00:00Z").toLocaleDateString('pt-BR')` exibiria **29/08/2026** no Brasil — erro de um dia em toda tela com data. **Este arquivo e `lib/format.ts` mudam junto com aquela decisão, nunca depois dela.**

Isso fechou dois bugs que este documento listava como conhecidos: o status "Expirado" de [ConvitesPage.tsx:17](../apps/web/src/pages/ConvitesPage.tsx#L17), que aparecia ~3 h antes do prazo real, e a `dataHora` de [AuditoriaPage.tsx:276](../apps/web/src/pages/AuditoriaPage.tsx#L276), que era gravada em UTC e exibida como local. Ambos eram a mesma causa — valor UTC lido como local — e sumiram quando o valor gravado passou a ser local de verdade.
