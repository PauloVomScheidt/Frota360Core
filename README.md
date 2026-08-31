# Frota360

Monorepo do **Frota360** — gestão de frotas multi-tenant. Cada empresa cliente enxerga apenas os seus veículos, motoristas, rotas e manutenções; o isolamento é aplicado em cada acesso a dados, a partir da claim `empresaId` do JWT.

| App | Stack | Documentação |
|---|---|---|
| [`apps/api`](apps/api) — API REST | .NET 10, EF Core, PostgreSQL, JWT | [README](apps/api/README.md) · [CLAUDE.md](apps/api/CLAUDE.md) · [contexto-api.md](docs/contexto-api.md) |
| [`apps/web`](apps/web) — front-end | React 19, Vite, TanStack Query, Tailwind | [README](apps/web/README.md) · [CLAUDE.md](apps/web/CLAUDE.md) · [contexto-web.md](docs/contexto-web.md) |

Cada app é **autocontido**: tem sua própria solution/`package.json`, seus comandos e seu `CLAUDE.md`. O que é transversal aos dois — a regra do português, a documentação de contexto e o **contrato entre API e front** — está em [CLAUDE.md](CLAUDE.md) na raiz.

---

## Estrutura

```
apps/
├── api/                   backend .NET 10
│   ├── Frota360.slnx
│   ├── Dockerfile         contexto de build = apps/api/
│   ├── src/{Domain,Application,Infrastructure,Api}
│   └── tests/Frota360.Tests
└── web/                   front React + Vite
    ├── package.json
    └── src/{api,auth,components,pages,styles,lib}
docs/
├── contexto-api.md        contexto profundo do backend
└── contexto-web.md        contexto profundo do front
```

Este repositório substitui os antigos `Frota360`/`Rota360` (API) e `Frota360Web` (front) — o histórico de commits dos dois foi preservado aqui.

## Deploy

Produção é uma EC2 única com Docker Compose (API + PostgreSQL + Caddy com TLS automático) e o
front estático em S3/CloudFront. O roteiro completo, incluindo o ensaio local da mesma stack
sem precisar de domínio, está em [docs/deploy.md](docs/deploy.md).

```powershell
# ensaio local da stack de produção
docker compose -f docker-compose.prod.yml -f docker-compose.local.yml --env-file .env.local up -d --build
```

---

## Como rodar

Pré-requisitos: **.NET 10 SDK**, **Node 20+** e uma instância de **PostgreSQL 17**.

O jeito mais rápido de ter o banco é o compose da raiz — ele já cria o banco `frota360` com as
credenciais que os `appsettings` de desenvolvimento esperam:

```powershell
docker compose up -d
```

**Terminal 1 — API:**

```powershell
cd apps/api
copy src/Api/appsettings.example.json src/Api/appsettings.json
copy src/Api/appsettings.example.json src/Api/appsettings.Development.json
dotnet build Frota360.slnx
dotnet user-secrets set "Jwt:Key" "uma-chave-secreta-com-pelo-menos-32-caracteres" --project src/Api
dotnet ef database update --project src/Infrastructure --startup-project src/Api
dotnet run --project src/Api          # http://localhost:5062 → /scalar/v1
```

**Terminal 2 — front:**

```powershell
cd apps/web
npm install
npm run dev                           # http://localhost:5173
```

A porta 5173 é fixa: é a origem liberada no CORS da API. A URL da API vem de `VITE_API_URL` (`apps/web/.env.development`).

> Num banco zerado não há usuários. Provisione uma empresa pelo backoffice da API (`POST /backoffice/empresa`) e abra o `linkConvite` retornado — ele cai em `/convite?token=...`.

Detalhes de configuração, endpoints, papéis e regras de negócio: [`apps/api/README.md`](apps/api/README.md). Telas, rotas e scripts do front: [`apps/web/README.md`](apps/web/README.md).

---

## Comandos por app

| | `apps/api` | `apps/web` |
|---|---|---|
| Build | `dotnet build Frota360.slnx` | `npm run build` |
| Testes | `dotnet test` | — (sem suíte) |
| Lint | — | `npm run lint` |
| Dev | `dotnet run --project src/Api` | `npm run dev` |

---

## Convenções

O projeto é escrito **inteiramente em português** — classes, métodos, DTOs, comentários, logs, textos de UI e mensagens de resposta.

Toda alteração estrutural, regra de negócio ou endpoint novo exige atualizar a documentação de contexto correspondente em `docs/` — e **os dois lados** quando a mudança atravessa a fronteira entre API e front.
