# Frota360 Web — Contexto do Front-end

> Documento **único** de referência do front-end (React + Vite): arquitetura, rotas, endpoints consumidos, o que cada tela faz e as armadilhas conhecidas.
> Complementa [`contexto-api.md`](contexto-api.md): lá está o contrato do servidor, aqui está o que a aplicação faz com ele.
> **Caminhos**: relativos à raiz do monorepo — o código do front vive em `src/Web/`, e os comandos `npm` rodam de lá.
> Última atualização: 2026-08-26 — consolidação do antigo `contexto_web.md` neste documento (endpoints em §6.5, inconsistências em §10).

---

## 1. Visão geral

SPA React 19 + Vite 8 + TypeScript, front-end da API Frota360 — gestão de frota **multi-tenant** (motoristas, veículos, rotas, manutenção preventiva). Sem suíte de testes.

| Aspecto | Escolha |
|---|---|
| Build | Vite 8 + React 19 + TypeScript |
| Rotas | react-router-dom 7 (`BrowserRouter`) |
| Dados | TanStack Query 5 |
| HTTP | axios com interceptors (Bearer + refresh) |
| Estilo | Tailwind 4 + design system próprio (`src/Web/src/styles/design-system.css`) |
| Lint | oxlint |
| Sessão | `localStorage` (token, refreshToken, identidade) |

```bash
npm run dev      # Vite em http://localhost:5173
npm run build    # tsc -b + vite build
npm run lint     # oxlint
npm run gen:api  # regenera tipos do OpenAPI (API precisa estar no ar)
```

Base da API por ambiente: `VITE_API_URL` (`.env.development` aponta para `https://localhost:7271/api/v1`; `.env.production` está vazio e precisa ser preenchido no deploy). É a única variável de ambiente do projeto. A porta 5173 do `npm run dev` é fixa — é a origem liberada no CORS da API.

O `empresaId` **nunca** é enviado pelo cliente — vem do JWT. A multi-tenancy é transparente para o front.

Em base nova não existe usuário: é preciso provisionar uma empresa pelo backoffice da API (`POST /backoffice/empresa`) e abrir o `linkConvite` devolvido, que cai em `/convite?token=…`.

---

## 2. Mapa de rotas

Definido em [`src/Web/src/App.tsx`](src/Web/src/App.tsx). Qualquer rota desconhecida cai em `/`.

| Rota | Tela | Acesso |
|---|---|---|
| `/` | `LandingPage` | Público |
| `/login` | `LoginPage` | Público |
| `/esqueci-senha` | `ForgotPasswordPage` | Público |
| `/redefinir-senha?token=…` | `ResetPasswordPage` | Público (link do e-mail) |
| `/convite?token=…` | `AcceptInvitePage` | Público (link do e-mail) |
| `/dashboard` | `DashboardPage` | Autenticado |
| `/motoristas` | `MotoristasPage` | Autenticado |
| `/veiculos` | `VeiculosPage` | Autenticado |
| `/rotas` | `RotasPage` | Autenticado |
| `/manutencoes` | `ManutencoesPage` | Autenticado |
| `/tipos-manutencao` | `TiposManutencaoPage` | **Admin / Supervisor** |
| `/usuarios` | `UsuariosPage` | **Admin** |
| `/convites` | `ConvitesPage` | **Admin** |

Os guardas estão em [`src/Web/src/components/RequireAuth.tsx`](src/Web/src/components/RequireAuth.tsx): `RequireAuth` redireciona para `/login` quando não há token (guardando a origem em `location.state.from`); `RequireAdmin` devolve para `/dashboard` quem não é Admin; `RequireGestor` faz o mesmo com quem não é Admin nem Supervisor (catálogo de tipos). O servidor continua sendo a autoridade — os guardas só evitam telas que resultariam em 401/403.

---

## 3. Telas públicas

### 3.1 `/` — Landing page

Página de apresentação do produto (v2 do desenho feito no Claude Design). Não consome a API.

**Linguagem visual própria.** Ao contrário do painel — reto, bege, sem sombra —, a landing usa cantos arredondados, cartões brancos sobre off-white, botões-pílula e sombras difusas. Por isso ela tem a própria folha de estilo, [`src/Web/src/styles/landing.css`](src/Web/src/styles/landing.css), escopada em `.lp` e importada só por esta tela; ela neutraliza localmente os padrões do design system (peso de título, margens de `p`, cor de link) sem alterá-los para o resto da aplicação.

Seções, na ordem: barra flutuante fixa (pílula com blur, âncoras, "Entrar", CTA de WhatsApp) → hero centralizado com selo, dois CTAs e letra miúda → mock do painel (sidebar + tabela de veículos) → chips "Construído sobre" → 4 números → "O problema" (4 cartões) → comparativo planilha × Frota360 → "Recursos" (4 cartões: Motoristas, Veículos, Rotas, Manutenções) → "Como funciona" (3 passos) → bloco de Rotas com mock de lista → **Manutenção preventiva** (mock da lista + catálogo de tipos) → "Permissões" com a matriz → 3 cartões de segurança → objeções da primeira conversa → FAQ (7 perguntas) → CTA azul com formulário de demonstração → rodapé.

- **Animação**: as seções abaixo do mock aparecem com fade + deslize conforme entram na tela (`IntersectionObserver`). Sem suporte ao observer, tudo é revelado de imediato; `prefers-reduced-motion` desliga a transição.
- **Formulário de demonstração**: não existe endpoint público na API, então o envio monta um `mailto:` já preenchido com nome, empresa, e-mail e tamanho da frota. Trocar por um endpoint real é uma alteração local em `FormularioDemonstracao`.
- Os contatos são as constantes `WHATSAPP` e `EMAIL` no topo de [`LandingPage.tsx`](src/Web/src/pages/LandingPage.tsx) — trocar ali muda todos os links da página.
- Todos os dados dos mocks (placas, quilometragens, manutenções) são **ilustrativos**; nada vem da API.

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
2. **Formulário** → nova senha + confirmação, validadas no cliente por [`validarSenha`](src/Web/src/auth/senha.ts) (≥ 6 caracteres, 1 maiúscula, 1 número, iguais) antes de chamar a API.
3. **Sucesso** → avisa que as sessões antigas foram encerradas e redireciona para `/login` em 2,5 s.

### 3.5 `/convite?token=…`

Destino do link enviado pelo admin. Sem token, mostra "Convite inválido".

Formulário: nome, senha, confirmação e checkbox de termos (obrigatório, validado só no cliente). `POST /convite/aceitar` já devolve a sessão autenticada — o usuário cai direto em `/dashboard`, sem passar pelo login. Empresa e permissão vêm do convite, não do formulário.

---

## 4. Layout interno

Todas as telas autenticadas são embrulhadas por `AppLayout` ([`src/Web/src/components/AppLayout.tsx`](src/Web/src/components/AppLayout.tsx)):

- **Sidebar** recolhível (preferência guardada no `localStorage`), com as categorias "Dashboard" (Visão geral, Motoristas, Veículos, Rotas, Manutenções e — só para Admin/Supervisor — Tipos de manutenção) e "Controle" (Usuários, Convites) — esta só aparece para Admin.
- **Header** com o avatar de iniciais, nome e papel do usuário, e o botão de sair (`POST /auth/logout` → limpa tokens, limpa o cache do React Query, vai para `/login`).
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

| Ação | Quem |
|---|---|
| Ver a lista | Todos |
| Cadastrar / editar | Admin, Supervisor |
| Excluir | Admin |

- Formulário inline (abre pelo botão do cabeçalho): nome, e-mail, CPF com máscara progressiva e data de nascimento. O CPF é enviado só com os 11 dígitos — a máscara é visual.
- **Editar** reabre o mesmo formulário pré-preenchido; o id decide entre `POST` e `PUT`, e a página rola ao topo porque o formulário fica acima da tabela.
- **Excluir** passa por um diálogo de confirmação; erros da API (ex.: 422 por vínculo com rotas) aparecem dentro do diálogo.
- Regras lembradas na própria tela: 18 anos ou mais, e-mail e CPF únicos por empresa.

### 5.3 `/veiculos`

Mesma estrutura de motoristas (mesmas permissões).

- Campos do formulário: nome, marca, placa (maiúsculas automáticas) e quilometragem.
- **Detalhe importante**: `ultimoMotorista` e `dataUltimaViagem` não estão no formulário — são preenchidos pela operação de rotas. Como o `PUT` substitui o registro inteiro, a edição reenvia esses dois campos intactos, vindos do registro carregado. Alterar isso sem cuidado apaga o histórico do veículo.

### 5.4 `/rotas`

| Ação | Quem |
|---|---|
| Ver / criar / editar / encerrar | Qualquer usuário autenticado |
| Excluir | Admin |

A rota tem um ciclo de vida: nasce **ativa** com o hodômetro de abertura e é **encerrada** por uma ação própria, que apura a quilometragem percorrida e avança o odômetro do veículo.

- Formulário: origem, destino, motorista (select), veículo (select), início e **quilometragem inicial**. Não há mais campo de "fim" nem de "situação" — a API os removeu dos requests justamente para que encerrar seja a única transição de estado (por `PUT` dava para "encerrar" uma rota sem calcular km nem tocar no odômetro).
- A **quilometragem inicial só aparece na criação**: o `PUT` não altera esse número, então exibi-lo na edição sugeriria um poder que a tela não tem.
- O formulário **sugere a quilometragem inicial** como o odômetro atual do veículo selecionado, reaplicando a sugestão quando o veículo muda, mas nunca sobrescrevendo um número digitado à mão (mesma mecânica de `/manutencoes`, comparando com a última sugestão emitida). O select de veículos mostra o km atual de cada um.
- Regras lembradas na própria tela: a quilometragem inicial não pode ser menor que o odômetro do veículo (422 com o km atual na mensagem) e, quando é maior, o odômetro é atualizado **já na abertura** — o veículo rodou fora do sistema, e o número mais recente vence. Por isso o cadastro invalida também `['veiculos']` e `['manutencoes']`.
- **Encerrar** aparece só em linha ativa e abre um `FormDialog` com km final (pré-preenchido com o odômetro atual do veículo, `min` no km de abertura) e data de fim opcional (limitada entre a data de início e hoje; em branco, a API assume "agora"). No sucesso, invalida `['rotas']`, `['veiculos']` **e `['manutencoes']`** — ver §6.4.
- Os 422 do encerramento (rota já encerrada, km final menor que o inicial, data de fim anterior à de início) caem no `ErrorList` do próprio diálogo.
- A `RotaResponse` é flat (só traz as FKs), então a tela cruza `codigoMotorista`/`codigoVeiculo` com as listas de motoristas e veículos para exibir nome e placa; sem correspondência, mostra `#id`. O mapa de veículos serve também à sugestão de km.
- A coluna **Quilometragem** mostra `kmPercorrido` (que vem persistido da API — não é recalculado aqui) e, abaixo, o intervalo `kmInicial → kmFinal`; nas rotas ativas, só o km de abertura.
- O status é **derivado**, não vem da API: `ativo` → "Ativa"; senão com `dataFim` → "Encerrada"; senão → "Inativa".
- Excluir motorista ou veículo invalida também o cache de rotas, porque a lista exibe o nome e a placa deles.

### 5.5 `/manutencoes`

| Ação | Quem |
|---|---|
| Ver a lista | Todos |
| Agendar / editar / concluir | Admin, Supervisor |
| Excluir | Admin |

A tela de manutenção preventiva. Um registro nasce **planejado** (veículo + tipo + km previsto) e recebe os dados de execução ao ser concluído — é o mesmo registro nos dois momentos.

- **Os filtros** de veículo e situação disparam a query no servidor (`GET /manutencao?veiculoId=&status=`), então entram na chave do cache: `['manutencoes', { veiculoId, status }]`. "Cancelada" **não** é oferecida no filtro: o status existe no enum, mas nenhum endpoint o produz ainda.
- **A lista não é reordenada no cliente** — a API já devolve pendentes primeiro e, dentro de cada grupo, o que vence antes no topo.
- **`atrasada` e `kmRestantes` vêm prontos do servidor** (recalculados a cada leitura, comparando o km previsto com a quilometragem atual do veículo). A tela só formata: `atrasada` tem precedência sobre `status` na badge, e `kmRestantes` negativo vira "3.200 km em atraso". Nada disso é recalculado aqui.
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

- **Permissão**: `select` que dispara `PUT /usuario/{id}/role`.
- **Status**: botão Ativar/Desativar → `PUT /usuario/{id}/ativo`.
- A própria conta do usuário logado aparece marcada com "(você)" e tem os dois controles desabilitados.
- Erros aparecem no topo da tabela — é onde cai o 422 do "último admin ativo".
- A tela avisa que alterar permissão ou desativar **encerra a sessão** do alvo (a API revoga o refresh token).

### 5.8 `/convites` (Admin)

- Formulário sempre visível: e-mail + permissão. A descrição do papel selecionado é mostrada abaixo, junto do aviso de que reenviar invalida o convite pendente anterior.
- Após criar, o **link em claro** retornado pela API aparece num painel destacado com botão "Copiar link" — em dev o e-mail só vai para o log da API, então esse é o caminho prático.
- Tabela com status **derivado no cliente**: `utilizadoEm` → "Utilizado"; `expiraEm` no passado → "Expirado"; senão "Pendente".
- Convites não utilizados podem ser cancelados; os utilizados mostram a data do aceite no lugar do botão.

---

## 6. Camada de API e sessão

### 6.1 `src/Web/src/api/http.ts`

- `baseURL` = `VITE_API_URL`; interceptor de request injeta `Authorization: Bearer`.
- Interceptor de response: em **401**, dispara um único refresh (`refreshInFlight` como lock — a rotação do refresh token invalida o anterior, então dois refreshes paralelos quebrariam o segundo), refaz a requisição original uma vez e, se o refresh falhar, limpa a sessão e força `/login`.
- Rotas anônimas (`/auth/login`, `/auth/refresh`, `/auth/esqueci-senha`, `/auth/redefinir-senha`, `/convite/aceitar`) são isentas: 401 ali é credencial inválida, não sessão expirada.
- `unwrap()` desembrulha o envelope `{ sucesso, mensagem, dados, erros }` e lança `ApiError` quando `sucesso: false`.

### 6.2 Erros

[`mensagensDeErro`](src/Web/src/api/errors.ts) transforma qualquer falha numa lista de strings: usa `erros` do envelope quando existe (alimenta formulários), cai para `mensagem` (em português, serve de resumo) e detecta API fora do ar. Toda tela renderiza isso pelo componente `ErrorList`.

### 6.3 Sessão

- [`tokenStorage`](src/Web/src/api/tokenStorage.ts) guarda `frota360.token`, `frota360.refreshToken` e `frota360.user` (nome, e-mail, role) no `localStorage`.
- [`useSession`](src/Web/src/auth/useSession.ts) expõe o usuário logado de forma reativa via `useSyncExternalStore`, ouvindo o evento `storage` (outras abas) e um evento próprio `frota360:sessao` (esta aba — `localStorage` não notifica quem escreveu).
- O papel usado pela UI vem desse cache local; ele só é atualizado quando o token renova. Mudança de papel pode levar até 1 h para refletir na interface — o servidor, porém, já recusa a ação antes disso.

### 6.4 Chaves do React Query

`['motoristas']`, `['veiculos']`, `['rotas']`, `['usuarios']`, `['convites']`, `['manutencoes', filtro]`, `['tiposManutencao']` e `['tiposManutencao', 'ativos']` — invalidadas após cada mutação da respectiva tela (e cruzadas quando uma exclusão afeta outra lista). `staleTime` de 30 s e sem retry em erro < 500 ([`src/Web/src/lib/queryClient.ts`](src/Web/src/lib/queryClient.ts)).

Cruzamentos que não são óbvios, conferidos no código:

- **Excluir motorista** invalida `['rotas']` ([MotoristasPage.tsx:56](src/Web/src/pages/MotoristasPage.tsx#L56)) e **excluir veículo**, idem ([VeiculosPage.tsx:56](src/Web/src/pages/VeiculosPage.tsx#L56)) — a tabela de rotas exibe o nome e a placa deles.
- **Concluir uma manutenção** invalida também `['veiculos']` ([ManutencoesPage.tsx:165](src/Web/src/pages/ManutencoesPage.tsx#L165)) — o odômetro pode ter avançado.
- **Abrir** ([RotasPage.tsx:118-119](src/Web/src/pages/RotasPage.tsx#L118-L119)) e **encerrar** ([RotasPage.tsx:139-140](src/Web/src/pages/RotasPage.tsx#L139-L140)) uma rota invalidam `['rotas']`, `['veiculos']` e `['manutencoes']`. É a cadeia mais longa do app: rota → veículo → manutenção. Os dois momentos mexem no odômetro (a abertura quando `kmInicial` é maior que o atual; o encerramento quando `kmFinal` é), e é do odômetro que `atrasada` e `kmRestantes` dependem. Sem invalidar a ponta da cadeia, o alerta de atraso só apareceria no próximo `staleTime`.
- Qualquer mutação no catálogo invalida o prefixo `['tiposManutencao']`, que cobre de uma vez o catálogo completo e a lista de ativos usada no agendamento.

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
| | `POST /convite/aceitar` (anônimo) | AcceptInvitePage — **já devolve sessão autenticada** |
| **usuario** | `GET /usuario` (Admin) | UsuariosPage |
| | `PUT /usuario/{id}/role` | muda permissão — revoga a sessão do alvo |
| | `PUT /usuario/{id}/ativo` | ativa/desativa — idem; último admin ativo → 422 |
| **motorista** | `GET/POST /motorista`, `GET/PUT/DELETE /motorista/{id}` | MotoristasPage |
| **veiculo** | `GET/POST /veiculo`, `GET/PUT/DELETE /veiculo/{id}` | VeiculosPage, Dashboard |
| **rota** | `GET/POST /rota`, `GET/PUT/DELETE /rota/{id}` | RotasPage, Dashboard — o POST leva `kmInicial` e **pode avançar o odômetro do veículo**; o PUT não mexe em `kmInicial`, `ativo` nem `dataFim` |
| | `POST /rota/{id}/encerrar` | encerramento — apura `kmPercorrido` e **pode avançar o odômetro do veículo** |
| **tipomanutencao** | `GET /tipomanutencao?apenasAtivos=` | catálogo (sem filtro) / select de agendamento (`true`) |
| | `POST`, `PUT /{id}`, `DELETE /{id}` | TiposManutencaoPage |
| **manutencao** | `GET /manutencao?veiculoId=&status=` | ManutencoesPage (os filtros vão para o servidor) |
| | `POST /manutencao`, `PUT /manutencao/{id}` | agendar / replanejar (só pendente) |
| | `POST /manutencao/{id}/concluir` | conclusão — **pode avançar o odômetro do veículo** |
| | `DELETE /manutencao/{id}` (Admin) | descarte (não há endpoint de cancelar) |

---

## 7. Permissões na interface

[`src/Web/src/auth/permissions.ts`](src/Web/src/auth/permissions.ts) espelha a matriz do §5 do `CONTEXTO.md`. A UI só esconde o que resultaria em 403 — quem decide é a API.

| Ação | Admin | Supervisor | Operador |
|---|---|---|---|
| Ver tudo da empresa | ✅ | ✅ | ✅ |
| Criar/editar rotas | ✅ | ✅ | ✅ |
| Criar/editar motoristas e veículos | ✅ | ✅ | — |
| Criar/editar/concluir manutenções e tipos | ✅ | ✅ | — |
| Excluir qualquer registro | ✅ | — | — |
| Usuários e convites | ✅ | — | — |

Na prática: sem permissão de edição, o botão "Novo…" e o ícone de lápis somem; sem permissão de exclusão, some a lixeira; sem nenhuma das duas, a coluna "Ações" inteira desaparece.

---

## 8. Design system e componentes compartilhados

Tokens e classes em [`src/Web/src/styles/design-system.css`](src/Web/src/styles/design-system.css): fundo `#fdfaf6`, superfície `#f2ede4`, texto `#201e1d`, acento `#1f3a5f` (com rampa 100–900), perigo `#a03123`, tipografia Archivo e **raio 0 em tudo** — o visual é de réguas retas, não de cartões arredondados.

Classes: `.btn` (`.btn-primary`, `.btn-secondary`, `.btn-icon`, `.btn-danger`), `.field` + `.input` (`.input-underline` no login), `.tag` (`.tag-accent`, `.tag-neutral`, `.tag-danger`, `.tag-warning`), `.nav`, `.table`, `.dialog*`.

Componentes reutilizados pelas telas:

| Componente | Onde | O que faz |
|---|---|---|
| `AppLayout`, `PageHeader`, `ErrorList` | `components/AppLayout.tsx` | Casca das telas internas, cabeçalho e lista de erros |
| `AuthScreen`, `AuthHeading` | `components/AuthScreen.tsx` | Casca das telas de autenticação |
| `InlineForm`, `TableStates` | `components/Table.tsx` | Formulário acima da tabela e as linhas de carregando/erro/vazio |
| `RowActions`, `ConfirmDialog` | `components/Table.tsx` | Ícones de editar/excluir na linha e confirmação de exclusão |
| `FormDialog` | `components/Table.tsx` | Diálogo com campos (concluir uma manutenção, encerrar uma rota) |
| `LogoMark`, `Wordmark` | `components/Logo.tsx` | Marca (versões clara e escura) |
| `icons.tsx` | — | Ícones SVG traçados, 24×24, `currentColor` |
| `lib/format.ts` | — | Datas, CPF, quilometragem, moeda, iniciais, `paraInputDate` e `hojeInputDate` para `<input type="date">` |

---

## 9. O que ainda não existe

- **Paginação e ordenação** nas listas — tudo vem de uma vez (a API também não pagina).
- **Toasts globais**: erros e sucessos são exibidos no local da ação, não há notificação central.
- **Tratamento específico de 429**: a mensagem do rate limit chega como erro comum.
- **Testes**: não há suíte no front.
- Sino de notificações e os links do rodapé/termos de uso são placeholders.
- A landing usa dados fictícios no mock do painel — nada ali reflete a base real.
- **Cancelar manutenção**: a API ainda não expõe `POST /manutencao/{id}/cancelar`, então descartar um agendamento passa pelo DELETE (Admin) e o filtro "Cancelada" nem é oferecido.
- **Atualizar só a quilometragem do veículo**: não há `PATCH` dedicado. O odômetro sobe pelo `PUT /veiculo/{id}` completo, pela conclusão de uma manutenção e — desde a RN10 — pela abertura e pelo encerramento de rotas, que é o caminho do dia a dia e o que finalmente alimenta os alertas de atraso.
- **Reabrir uma rota encerrada**: a API não expõe o caminho inverso do encerramento, e o `PUT` não mexe mais em `ativo`/`dataFim`. Corrigir um encerramento errado passa por excluir a rota (Admin) e recriá-la.
- O dashboard ainda não mostra nada de manutenção (nenhum KPI de atrasadas).

---

## 10. Inconsistências conhecidas

- `npm run gen:api` aponta para `http://localhost:5062/openapi/v1.json` ([package.json:10](package.json#L10)), enquanto a API roda em `https://localhost:7271` conforme o `.env.development`. O script provavelmente quebra como está.
