# Frota360 Web

Front-end React da API [Frota360](../api) (`apps/api`, mesmo repositório) — gestão multi-empresa de motoristas, veículos e rotas.

## Stack

- React 19 + TypeScript + Vite
- Tailwind CSS 4 + design system "Modernist" (`src/styles/design-system.css`)
- React Router 7
- TanStack Query 5 + Axios (Bearer + refresh automático com lock)

## Como rodar

Pré-requisitos: Node 20+ e a API Frota360 rodando — ela vive neste mesmo repositório:
`cd apps/api` e `dotnet run --project src/Api` (`http://localhost:5062`).

```powershell
npm install
npm run dev
# → http://localhost:5173 (porta fixa — é a origem liberada no CORS da API)
```

A URL da API vem de `VITE_API_URL` (`.env.development` / `.env.production`).

> Num banco zerado não há usuários: provisione uma empresa pelo backoffice da API
> (`POST /backoffice/empresa`) e abra o `linkConvite` retornado — ele cai em `/convite?token=...`.

## Scripts

| Script | O que faz |
|---|---|
| `npm run dev` | Servidor de desenvolvimento |
| `npm run build` | Type-check + build de produção |
| `npm run lint` | Lint (oxlint) |
| `npm run gen:api` | Gera `src/api/schema.d.ts` a partir do OpenAPI da API (precisa da API rodando) |

## Rotas

| Rota | Acesso | Descrição |
|---|---|---|
| `/` | público | Landing (apresentação do produto) |
| `/login` | anônimo | Entrar (não há cadastro público) |
| `/esqueci-senha` | anônimo | Dispara `POST /auth/esqueci-senha` (resposta neutra) |
| `/redefinir-senha?token=` | anônimo | Destino do e-mail de reset |
| `/convite?token=` | anônimo | Destino do convite: cria a conta e já autentica |
| `/dashboard` | gestão | Visão geral da frota |
| `/motoristas` | gestão | Lista de motoristas (somente leitura) |
| `/veiculos` | autenticado (cadastro: Admin/Supervisor) | Lista e cadastro de veículos |
| `/rotas` | gestão | Lista e cadastro de rotas |
| `/manutencoes` | autenticado (edição: Admin/Supervisor) | Plano de manutenção; o Motorista não vê custo |
| `/abastecimentos` | autenticado | Lançar o gasto; o Motorista vê só o que é dele |
| `/despesas` | gestão | Custos avulsos: pedágio, multa, IPVA, seguro |
| `/custos` | gestão | Abastecimentos, manutenções e despesas consolidados, com totais e R$/km |
| `/minhas-rotas` | Motorista | Abrir e encerrar as próprias rotas |
| `/tipos-manutencao` | Admin/Supervisor | Catálogo de tipos de manutenção |
| `/tipos-despesa` | Admin/Supervisor | Catálogo de tipos de despesa |
| `/usuarios` | Admin | Alterar permissão, ativar/desativar |
| `/convites` | Admin | Criar, listar e cancelar convites |
| `/auditoria` | Admin | Trilha do que a equipe alterou (somente leitura) |
| `/perfil` | qualquer autenticado | O próprio cadastro |

"Gestão" é todo papel menos `Motorista`, que tem `/minhas-rotas` como home.

A navegação interna é uma **sidebar colapsável** (`AppLayout`) com duas categorias: *Dashboard*
(as telas da frota) e *Controle* (Usuários, Convites, Auditoria — só para Admin). Para o
Motorista ela separa o que ele **faz** do que só **consulta**. O estado recolhido/expandido
fica em `localStorage`.

## Estrutura

```
src/
├── api/            # camada de acesso à API
│   ├── http.ts         # axios + cookie HttpOnly (withCredentials) + refresh automático em 401 (single-flight)
│   ├── errors.ts       # mensagensDeErro(): extrai texto do envelope em qualquer status
│   ├── types.ts        # envelope, Role e DTOs
│   ├── tokenStorage.ts # só identidade (nome/email/role) no localStorage — tokens vêm em cookie HttpOnly
│   ├── auth.ts, convites.ts, usuarios.ts
│   └── motoristas.ts, veiculos.ts, rotas.ts
├── auth/           # sessão e permissões
│   ├── useSession.ts   # usuário logado, reativo a login/logout
│   ├── permissions.ts  # matriz de roles (espelho da API — o servidor é a autoridade)
│   └── senha.ts        # regras de senha compartilhadas
├── components/     # AppLayout (sidebar+topbar), Table, AuthScreen, Logo, RequireAuth/RequireAdmin, icons
├── lib/            # queryClient do TanStack Query, format.ts (datas, km, CPF)
└── pages/          # telas
```

## Convenções da API

- Toda resposta vem no envelope `{ sucesso, mensagem, dados, erros }` — inclusive 401/403/422/429.
  `src/api` desembrulha `dados` e converte falhas em `ApiError`; use `mensagensDeErro()` na UI.
- Em 401, o interceptor tenta `/auth/refresh` uma única vez (lock contra refreshes paralelos, pois a
  rotação invalida o token anterior) e repete a requisição; se falhar, limpa a sessão e vai para `/login`.
  O refresh também renova as claims — é quando uma mudança de role passa a valer.
- Em 403, a API recusa por permissão. A matriz em `auth/permissions.ts` evita oferecer a ação,
  mas quem decide é o servidor.
- Multi-tenant é transparente: o `empresaId` vem do token, o front nunca envia id de empresa.
