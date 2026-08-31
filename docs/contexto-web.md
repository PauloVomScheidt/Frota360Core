# Frota360 Web — Contexto do Front-end

> Documento **único** de referência do front-end (React + Vite): arquitetura, rotas, endpoints consumidos, o que cada tela faz e as armadilhas conhecidas.
> Complementa [`contexto-api.md`](contexto-api.md): lá está o contrato do servidor, aqui está o que a aplicação faz com ele.
> **Caminhos**: relativos à raiz do monorepo — o código do front vive em `apps/web/`, e os comandos `npm` rodam de lá.
> Última atualização: 2026-08-28 — tela `/perfil` (§5.11): o próprio usuário corrige nome, CPF e nascimento, em qualquer papel. Também nesta rodada: o 422 da RN08 ao excluir veículo com rota (§5.3) e a placa aceita nos dois formatos e normalizada em maiúsculas pelo servidor.

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
| `/minhas-rotas` | `MinhasRotasPage` | **Motorista** |
| `/tipos-manutencao` | `TiposManutencaoPage` | **Admin / Supervisor** |
| `/usuarios` | `UsuariosPage` | **Admin** |
| `/convites` | `ConvitesPage` | **Admin** |
| `/auditoria` | `AuditoriaPage` | **Admin** |
| `/perfil` | `PerfilPage` | **Qualquer autenticado** — única rota interna sem `RequirePode` |

"Gestão" significa **todos os papéis menos `Motorista`**. Ele tem `/minhas-rotas` como home e mais duas telas em leitura: veículos e manutenções — saber o estado do caminhão faz parte do trabalho.

Os guardas estão em [`apps/web/src/components/RequireAuth.tsx`](apps/web/src/components/RequireAuth.tsx) e são **dois**: `RequireAuth`, que redireciona para `/login` quando não há token (guardando a origem em `location.state.from`), e `RequirePode`, que recebe um predicado de `auth/permissions.ts`.

Cada rota declara a própria permissão (`<RequirePode permitido={pode.verVeiculos} />`). Um guarda por bloco de papéis deixou de fazer sentido quando o motorista passou a enxergar parte do painel: quem manda é a tela, não o papel.

**`/perfil` é a exceção**: fica dentro de `RequireAuth` e fora de qualquer `RequirePode`. Editar o próprio cadastro é direito de quem está autenticado, seja qual for o papel — e envolvê-la num predicado seria criar um `pode.verPerfil` que devolve `true` para todo mundo.

**Todo redirecionamento de guarda usa `rotaInicial(role)`** de `auth/permissions.ts` (`/minhas-rotas` para motorista, `/dashboard` para o resto), nunca `/dashboard` fixo — para o motorista o dashboard é justamente uma tela bloqueada, e o par de guardas entraria em pingue-pongue. Os destinos pós-login (`LoginPage`) e pós-aceite de convite (`AcceptInvitePage`) usam a mesma função, a partir do `role` que vem no `AuthResponse`.

O servidor continua sendo a autoridade — os guardas só evitam telas que resultariam em 401/403.

---

## 3. Telas públicas

### 3.1 `/` — Landing page

Página de apresentação do produto (v3 — "ficha de controle"). Não consome a API.

**Mesma linguagem visual do painel.** Até a v2 a landing era deliberadamente o oposto do painel (cantos arredondados, cartões brancos, botões-pílula, sombras difusas). Isso foi revertido: a landing agora é reta, bege e sem sombra como o resto do sistema, e **todas as cores saem de [`design-system.css`](apps/web/src/styles/design-system.css)** — `#fdfaf6` de fundo, `#201e1d` de texto, `#1f3a5f` de acento. Motivo: o mock do painel é o principal argumento da página, e ele mentia sobre o produto quando desenhado noutra língua.

Ela mantém folha própria, [`apps/web/src/styles/landing.css`](apps/web/src/styles/landing.css), escopada em `.lp` e importada só por esta tela — mas agora por causa da **escala** (display grande, faixas de ponta a ponta, odômetro), não por discordar do sistema.

Convenções da folha, que valem ao editar:

- **Escala tipográfica em tokens** (`--t-h1`, `--t-h2`, `--t-corpo`, `--t-cap`…) no bloco `.lp`. Não use tamanho solto no JSX.
- **Três níveis de tinta para texto**, com nome de intenção e não de opacidade: `--tinta-forte` (.86), `--tinta-media` (.74) e `--tinta-fraca` (.66). Os valores foram **calibrados por contraste** — todos passam de 4,5:1 sobre `--papel`. Os antigos `--tinta-35`/`--tinta-50` reprovavam (2,15:1 e 3,13:1) em 23 pontos da página. Hierarquia aqui se faz com tamanho, peso e caixa; para régua e contorno use `--regua`/`--regua-fraca`, que **não são cor de texto**.
- **`min-width: 0` nos filhos de todo grid que contenha tabela** (`.lp-mock > *`, `.lp-split > *`…). Sem isso o filho cresce até o `min-width` da tabela, o `overflow-x` de `.lp-rolagem` nunca entra e cabeçalho e rodapé do mock são cortados fora da tela no celular.
- **O odômetro dimensiona por `cqw`**, não `vw` — ele tem que caber na coluna dele, que muda de largura por breakpoint; `.lp-instrumento` é o `container-type: inline-size`. A fita de algarismos tem `height: 1000%` (dez células de uma linha); com `inset: 0` cada célula vira um décimo de linha e o glifo estoura para fora da janela.
- **`.lp-odo-digito` precisa de `contain: paint`, não só `overflow: hidden`.** Enquanto a fita anima ela vira camada composta própria e escapa do recorte do pai: os algarismos derramam por cima e por baixo da janela e chegam a encostar nas réguas do instrumento. O sintoma é característico — **só os dígitos em movimento vazam**, os parados recortam certo. Pelo mesmo motivo a fita **não** leva `will-change: transform`, que era justamente o que forçava a promoção da camada.
- **Mono para número.** Placa, quilometragem, data, rótulo de campo e cabeçalho de tabela usam **IBM Plex Mono** (`--mono`); título e corpo seguem em Archivo. A fonte é carregada em [`index.html`](apps/web/index.html).
- **Vermelho (`--alerta`) e âmbar (`--vencendo`) são estado de manutenção, nunca enfeite.** Só aparecem em "Atrasada" e "Vencendo".
- **O reset de `.lp` usa `:where()`** — `:where()` não soma especificidade, então `.lp :where(p) { margin: 0 }` vale (0,1,0) e qualquer classe abaixo o vence. Escrever `.lp p` em vez disso quebra silenciosamente as margens de `.lp-lead`, `.lp-hero-sub` e o sublinhado de `.lp-cta-mail`.
- `.lp-wrap.lp-hero` usa classe dupla de propósito: precisa vencer `.lp-wrap` inclusive dentro do media query de 600px, que vem depois no arquivo.

Seções, na ordem: barra fixa reta (âncoras, "Entrar", CTA de WhatsApp, menu `<details>` de seções no celular) → **hero com o odômetro** → mock do painel (sidebar + tabela de veículos) → "O que a planilha perde" (4 rótulos de campo) → comparativo planilha × Frota360 → "Recursos" (Motoristas, Veículos, Rotas, Manutenções) → **Manutenção** (mock da lista + catálogo de tipos no rodapé) → "Permissões" (matriz + três garantias de isolamento) → "Implantação" (3 passos) → "Dúvidas" (9 perguntas em `<details>`) → CTA azul com formulário de demonstração → rodapé.

- **O odômetro é o elemento de assinatura.** No hero, a quilometragem do veículo `MJU-5F71` conta de `KM_INICIAL` (51.780) até `KM_FINAL` (51.988) — cada algarismo é uma fita de 0–9 que desliza (`.lp-odo-fita`, `--digito`). Ao chegar perto de `KM_PREVISTO` (52.000) a manutenção abaixo acende como **Vencendo**, dentro da faixa `FAIXA_AVISO` (500 km). É a mecânica real do produto encenada; o selo "Demonstração" no alto do instrumento marca que os números são ilustrativos.
- **Animação**: só o odômetro e o "+" do FAQ. Os *reveals* por `IntersectionObserver` da v2 foram removidos — eram 13 seções com a mesma transição. `prefers-reduced-motion` já entrega o odômetro no valor final (estado inicial do `useState`, sem salto depois do primeiro render).
- **Numeração**: só "Implantação" é numerada, porque só ela é uma sequência de verdade. As falhas da planilha são identificadas por **rótulo de campo** (`Quilometragem`, `Responsável`, `Permissão`, `Cadastro`), não por `01/02/03`.
- **Acessibilidade**: toda área rolável passa pelo componente `Rolagem` (`tabIndex=0` + `role="region"` + `aria-label`), sem o que ninguém alcança as tabelas só pelo teclado. O odômetro é `role="img"` com `aria-label`, e **sem** `aria-live`: o texto muda a cada tique e viraria dezenas de anúncios.
- **Matriz de permissões**: mostra ✓ (`CheckIcon`) e ✗ (`XIcon`) — mas a palavra "Sim"/"Não" continua no DOM em `.lp-oculto`, porque um ✓ sozinho não se lê em leitor de tela. `.lp-matriz td` é `position: relative` de propósito: sem bloco contentor, o `.lp-oculto` absoluto se posiciona pelo bloco inicial, escapa do `.lp-rolagem` **e** do `overflow-x: clip` do `.lp`, e cria rolagem horizontal na página inteira abaixo de 480px.
- **Formulário de demonstração**: não existe endpoint público na API, então o envio monta um `mailto:` já preenchido com nome, empresa, e-mail e tamanho da frota. Como não há como saber se o cliente de e-mail abriu, a confirmação **não afirma que abriu** — diz o que era para acontecer e oferece WhatsApp e e-mail como saída. Trocar por um endpoint real é uma alteração local em `FormularioDemonstracao`.
- Os contatos são as constantes `WHATSAPP` e `EMAIL` no topo de [`LandingPage.tsx`](apps/web/src/pages/LandingPage.tsx) — trocar ali muda todos os links da página.
- Todos os dados dos mocks (placas, quilometragens, manutenções) são **ilustrativos**; nada vem da API.
- `index.html` carrega `<meta name="description">` e as tags Open Graph — sem elas o link colado no WhatsApp, que é o CTA principal, aparece sem prévia. Falta ainda uma `og:image`.

### 3.2 `/login` — Entrar

Divide a tela em painel de marca (escondido abaixo de `md`) e formulário.

- Campos: e-mail e senha, com botão de mostrar/ocultar senha.
- `POST /auth/login` → grava token, refreshToken e identidade; navega para `location.state.from` ou `/dashboard`.
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

- **Sidebar** recolhível (preferência guardada no `localStorage`), com as categorias "Dashboard" (Visão geral, Motoristas, Veículos, Rotas, Manutenções e — só para Admin/Supervisor — Tipos de manutenção) e "Controle" (Usuários, Convites, Auditoria) — esta só aparece para Admin.
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

- Formulário sempre visível: e-mail + permissão. A descrição do papel selecionado é mostrada abaixo, junto do aviso de que reenviar invalida o convite pendente anterior.
- **`Motorista` é só mais uma opção do select de permissão** — o formulário não muda, não pede nada a mais, e o convite segue o mesmo caminho das outras roles.
- Após criar, o **link em claro** retornado pela API aparece num painel destacado com botão "Copiar link" — em dev o e-mail só vai para o log da API, então esse é o caminho prático.
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

O apontamento é curto de propósito — **veículo, motorista, valor, data e observação**. A versão anterior pedia litros e odômetro para calcular consumo; era precisão que não se paga no posto e que fazia o lançamento ser evitado. O que sobrou serve ao que a tela existe para responder: **quanto se gastou**, por veículo, por motorista e por período.

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
- **O abastecimento não mexe no odômetro do veículo.** Quem o avança são a rota e a manutenção — a tela não participa da cadeia, e por isso a mutation invalida só `['abastecimentos']` (ver §6.4).
- **Veículo e motorista não são editáveis na correção** — trocar qualquer um reatribuiria o gasto. Para isso, exclua e lance de novo; a tela diz isso no formulário. Só valor, data e observação são corrigíveis.
- A rota é **contexto derivado**: a API vincula sozinha quando há rota aberta do motorista naquele veículo, e a tabela mostra "Origem → Destino" no lugar do modelo. Ninguém escolhe rota na tela.
- Rodapé com o total **do que está filtrado** (quantidade e valor), não da frota inteira.

Cache: `['abastecimentos', filtro]`, com `filtro` incluindo `motoristaId`.

### 5.10 `/auditoria` (Admin)

Trilha do que a equipe alterou. **Somente leitura** — não há `InlineForm` nem `RowActions`, porque a API não expõe caminho para alterar ou apagar uma linha (nem para o Admin).

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

- `baseURL` = `VITE_API_URL`; interceptor de request injeta `Authorization: Bearer`.
- Interceptor de response: em **401**, dispara um único refresh (`refreshInFlight` como lock — a rotação do refresh token invalida o anterior, então dois refreshes paralelos quebrariam o segundo), refaz a requisição original uma vez e, se o refresh falhar, limpa a sessão e força `/login`.
- Rotas anônimas (`/auth/login`, `/auth/refresh`, `/auth/esqueci-senha`, `/auth/redefinir-senha`, `/convite/aceitar`) são isentas: 401 ali é credencial inválida, não sessão expirada.
- `unwrap()` desembrulha o envelope `{ sucesso, mensagem, dados, erros }` e lança `ApiError` quando `sucesso: false`.

### 6.2 Erros

[`mensagensDeErro`](apps/web/src/api/errors.ts) transforma qualquer falha numa lista de strings: usa `erros` do envelope quando existe (alimenta formulários), cai para `mensagem` (em português, serve de resumo) e detecta API fora do ar. Toda tela renderiza isso pelo componente `ErrorList`.

### 6.3 Sessão

- [`tokenStorage`](apps/web/src/api/tokenStorage.ts) guarda `frota360.token`, `frota360.refreshToken` e `frota360.user` (nome, e-mail, role) no `localStorage`.
- [`useSession`](apps/web/src/auth/useSession.ts) expõe o usuário logado de forma reativa via `useSyncExternalStore`, ouvindo o evento `storage` (outras abas) e um evento próprio `frota360:sessao` (esta aba — `localStorage` não notifica quem escreveu).
- O papel usado pela UI vem desse cache local; ele só é atualizado quando o token renova. Mudança de papel pode levar até 1 h para refletir na interface — o servidor, porém, já recusa a ação antes disso.

### 6.4 Chaves do React Query

`['motoristas']`, `['veiculos']`, `['rotas']`, `['rotas', 'minhas']`, `['usuarios']`, `['convites']`, `['perfil']`, `['manutencoes', filtro]`, `['abastecimentos', filtro]`, `['auditoria', filtro]`, `['tiposManutencao']` e `['tiposManutencao', 'ativos']` — invalidadas após cada mutação da respectiva tela (e cruzadas quando uma exclusão afeta outra lista). `staleTime` de 30 s e sem retry em erro < 500 ([`apps/web/src/lib/queryClient.ts`](apps/web/src/lib/queryClient.ts)).

⚠️ `['rotas']` e `['rotas','minhas']` são **listas diferentes**, não pai e filho: a segunda vem de outro endpoint e traz só as rotas do motorista logado. Invalidar pelo prefixo `['rotas']` alcançaria as duas, o que é inofensivo apenas porque nenhuma sessão usa as duas telas. Ao mexer nisso, invalide a chave exata.

Cruzamentos que não são óbvios, conferidos no código:

- **Excluir veículo** invalida `['rotas']` ([VeiculosPage.tsx:56](apps/web/src/pages/VeiculosPage.tsx#L56)) — a tabela de rotas exibe a placa dele. Motorista não tem exclusão: é um usuário, e usuário só é desativado.
- **Concluir uma manutenção** invalida também `['veiculos']` ([ManutencoesPage.tsx:165](apps/web/src/pages/ManutencoesPage.tsx#L165)) — o odômetro pode ter avançado.
- **Abrir** ([RotasPage.tsx:118-119](apps/web/src/pages/RotasPage.tsx#L118-L119)) e **encerrar** ([RotasPage.tsx:139-140](apps/web/src/pages/RotasPage.tsx#L139-L140)) uma rota invalidam `['rotas']`, `['veiculos']` e `['manutencoes']`. É a cadeia mais longa do app: rota → veículo → manutenção. Os dois momentos mexem no odômetro (a abertura quando `kmInicial` é maior que o atual; o encerramento quando `kmFinal` é), e é do odômetro que `atrasada` e `kmRestantes` dependem. Sem invalidar a ponta da cadeia, o alerta de atraso só apareceria no próximo `staleTime`.
- **Excluir uma rota** invalida `['rotas']` **e `['veiculos']`** — se a rota estava aberta, o veículo volta a `Disponível` na coluna Situação de `/veiculos` (§5.3). Não invalida `['manutencoes']`: excluir não mexe no odômetro.
- Em `/minhas-rotas`, **abrir** e **encerrar** invalidam `['rotas','minhas']`, `['veiculos']` **e** `['manutencoes']` — a mesma cadeia da tela de gestão, agora que o motorista também lê manutenções e a tela mostra a pendência do veículo escolhido.
- **Salvar o perfil** invalida `['perfil']`, `['motoristas']` **e** `['usuarios']` ([PerfilPage.tsx](apps/web/src/pages/PerfilPage.tsx)) — as duas listas exibem nome e CPF de quem acabou de se corrigir. É o único cruzamento que parte de uma tela sem tabela.
- **Lançar, corrigir ou excluir abastecimento** invalida **só** `['abastecimentos']` — o lançamento é só o gasto e não toca no odômetro do veículo, então não entra na cadeia rota → veículo → manutenção. (Ele já entrou: enquanto o formulário pedia odômetro, invalidava também `['veiculos']` e `['manutencoes']`.)
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
| **auditoria** | `GET /auditoria?pagina=&tamanhoPagina=&entidade=&acao=&usuarioId=&de=&ate=` (Admin) | AuditoriaPage — **único endpoint paginado**: `dados` é um `ResultadoPaginado<T>`, não um array |
| **usuario** | `GET /usuario` (Admin) | UsuariosPage, **AuditoriaPage** (select "Quem") |
| | `PUT /usuario/{id}/role` | muda permissão — revoga a sessão do alvo |
| | `PUT /usuario/{id}/ativo` | ativa/desativa — idem; último admin ativo → 422 |
| | `GET /usuario/perfil` (**qualquer autenticado**) | PerfilPage — o próprio cadastro; `GET /usuario` é Admin e não serve ao Motorista |
| | `PUT /usuario/perfil` (**qualquer autenticado**) | PerfilPage — nome/CPF/nascimento; alvo pelo token, CPF duplicado na empresa → 422 |
| **motorista** | `GET /motorista`, `GET /motorista/{id}` | MotoristasPage, RotasPage (select) — **somente leitura**: são os usuários com a role Motorista |
| **manutencao** | `GET /manutencao?status=Pendente` | MinhasRotasPage — alimenta o aviso de pendência do veículo escolhido |
| **abastecimento** | `GET /abastecimento?veiculoId=&motoristaId=&de=&ate=` | AbastecimentosPage — `motoristaId` serve à gestão; para o Motorista a API o sobrescreve com o do token |
| | `POST /abastecimento` | lançamento — a API resolve motorista (token, para a role Motorista) e rota; veículo fora da rota aberta → **422** |
| | `PUT /abastecimento/{id}` | correção (só valor, data e observação); lançamento de outro motorista → 404 para ele |
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
| Excluir qualquer registro | ✅ | — | — | — |
| Usuários e convites | ✅ | — | — | — |
| Ver a trilha de auditoria | ✅ | — | — | — |
| Editar o **próprio** cadastro (`/perfil`) | ✅ | ✅ | ✅ | ✅ |

A última linha é a única em que as quatro colunas são ✅ — e por isso `/perfil` não tem entrada em `pode.*`: um predicado que devolve `true` para todo mundo é ruído, não permissão. Corrigir o cadastro **de outra pessoa** não aparece na matriz porque não existe em papel nenhum, o Admin incluído.

Na prática: sem permissão de edição, o botão "Novo…" e o ícone de lápis somem; sem permissão de exclusão, some a lixeira; sem nenhuma das duas, a coluna "Ações" inteira desaparece.

O `Motorista` combina os dois mecanismos: nas telas que ele alcança (veículos, manutenções) valem os `pode.*` de sempre, e as que ele não alcança são barradas por `RequirePode` na rota. As entradas `ver*` de `permissions.ts` são **por tela** justamente por isso — um booleano único de "é gestão" seria mentira. `rotaInicial(role)` é o destino de todo redirecionamento.

---

## 8. Design system e componentes compartilhados

Tokens e classes em [`apps/web/src/styles/design-system.css`](apps/web/src/styles/design-system.css): fundo `#fdfaf6`, superfície `#f2ede4`, texto `#201e1d`, acento `#1f3a5f` (com rampa 100–900), perigo `#a03123`, tipografia Archivo e **raio 0 em tudo** — o visual é de réguas retas, não de cartões arredondados. **A landing pública segue esse mesmo visual** desde a v3 (§3.1); não há mais duas linguagens no produto.

`index.html` carrega Archivo (400/600/800) e **IBM Plex Mono** (400/600). O mono é usado hoje só pela landing, para placa, quilometragem, data e rótulo de campo — se o painel passar a usá-lo em coluna numérica, promova-o a token do design system.

Classes: `.btn` (`.btn-primary`, `.btn-secondary`, `.btn-icon`, `.btn-danger`), `.field` + `.input` (`.input-underline` no login), `.tag` (ver abaixo), `.nav`, `.table`, `.dialog*`.

### 8.1 Cor de situação — a tabela normativa

**Situação se sinaliza pela classe `.tag`, nunca por `style` inline.** A `.tag` tem a forma da etiqueta da landing: barra de 3px na cor do estado (`border-left: 3px solid currentColor`), fundo tonal, caixa alta e peso 600. A barra é o que chama o olho numa tabela longa sem que o fundo precise gritar.

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

Componentes reutilizados pelas telas:

| Componente | Onde | O que faz |
|---|---|---|
| `AppLayout`, `PageHeader`, `ErrorList` | `components/AppLayout.tsx` | Casca das telas internas, cabeçalho e lista de erros |
| `AuthScreen`, `AuthHeading` | `components/AuthScreen.tsx` | Casca das telas de autenticação |
| `InlineForm`, `TableStates` | `components/Table.tsx` | Formulário acima da tabela e as linhas de carregando/erro/vazio |
| `Paginacao` | `components/Table.tsx` | Rodapé "X–Y de Z" + anterior/próxima. Só `/auditoria` usa (é a única lista que a API pagina) e some quando cabe tudo numa página |
| `FiltroPeriodo` | `components/Table.tsx` | Select de período pronto, usado por `/manutencoes` e `/abastecimentos`; a conversão para `de`/`ate` está em `lib/periodo.ts` |
| `RowActions`, `ConfirmDialog` | `components/Table.tsx` | Ícones de editar/excluir na linha e confirmação de ação consequente (exclusão ou troca de permissão — `variante="padrao"` tira o vermelho quando não é destrutiva) |
| `FormDialog` | `components/Table.tsx` | Diálogo com campos (concluir uma manutenção, encerrar uma rota) |
| `LogoMark`, `Wordmark` | `components/Logo.tsx` | Marca (versões clara e escura) |
| `icons.tsx` | — | Ícones SVG traçados, 24×24, `currentColor` |
| `lib/format.ts` | — | Datas, CPF, quilometragem, moeda, iniciais, `paraInputDate` e `hojeInputDate` para `<input type="date">` |
| `lib/rota.ts` | `/rotas`, `/minhas-rotas` | `statusDaRota` — o status derivado de `ativo` + `dataFim`, igual nas duas telas |
| `lib/periodo.ts` | `/manutencoes`, `/abastecimentos` | `PERIODOS` e `intervaloDoPeriodo` — converte o período escolhido em `de`/`ate` (hora local, `ate` inclusivo) |

---

## 9. O que ainda não existe

- **Paginação e ordenação** nas demais listas — tudo vem de uma vez. A exceção é `/auditoria`, o único endpoint paginado da API (§5.10); `ResultadoPaginado<T>` e o componente `Paginacao` já nascem genéricos para as próximas listas que precisarem.
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
