# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Navegação de código
Para perguntas estruturais (como X funciona, o que chama Y, o que quebra se eu mudar Z),
use `codegraph_explore` em vez de Grep/Read. O índice está sempre atualizado.

## Project

Frota360 Web — the React front-end for the Frota360 API, a multi-tenant fleet management system (motoristas/veículos/rotas/manutenções preventivas). All UI copy, comments, and docs in this repo are in **Portuguese**; match that when editing existing files.

The authoritative, detailed reference for screen-by-screen behavior, API conventions, and React Query cache keys is [src/CONTEXTO-FRONT.md](src/CONTEXTO-FRONT.md) — the single consolidated context document for this repo (it also carries the endpoint map in §6.5 and known inconsistencies in §10). Read it before making non-trivial changes to a page. This file covers commands and cross-cutting architecture only, to avoid duplicating that document.

## Documentation
Apos todas as alterações realizadas atualizar as documentações de contexto, claude.md e readme para refletir as mudanças. Documentação de contexto é obrigatória para qualquer alteração estrutural, regra de negócio ou endpoint novo.

## Commands

```powershell
npm run dev      # Vite dev server at http://localhost:5173 (fixed port — it's the origin whitelisted in the API's CORS)
npm run build    # tsc -b (type-check) + vite build
npm run lint     # oxlint
npm run gen:api  # regenerates src/api/schema.d.ts from the API's OpenAPI spec — requires the API running locally
```

There is no test suite in this repo. There is no single-file lint/typecheck shortcut beyond running the full `lint`/`build` commands (oxlint and `tsc -b` don't take a useful single-file mode here).

Requires the Frota360 API running locally (default `https://localhost:7271/api/v1` per `.env.development`, overridden by `VITE_API_URL`). On a fresh database there are no users — provision a company via the API's backoffice (`POST /backoffice/empresa`) and open the returned `linkConvite`, which lands on `/convite?token=...`.

## Architecture

### API layer (`src/api/`)

- `http.ts` — a single axios instance (`http`). Request interceptor injects `Authorization: Bearer`. Response interceptor: on **401**, attempts exactly one `/auth/refresh` (guarded by an in-flight promise lock, since refresh-token rotation invalidates the previous token — two parallel refreshes would break the second one), retries the original request once, and on failure clears the session and redirects to `/login`. A fixed list of anonymous routes (`/auth/login`, `/auth/refresh`, `/auth/esqueci-senha`, `/auth/redefinir-senha`, `/convite/aceitar`) is exempt — a 401 there means bad credentials, not an expired session.
- Every API response is wrapped in an envelope `{ sucesso, mensagem, dados, erros }`, including error responses (401/403/422/429). `unwrap()` in `http.ts` unpacks `dados` and throws `ApiError` (with `erros: string[]`) when `sucesso` is false. Per-resource modules (`motoristas.ts`, `veiculos.ts`, `rotas.ts`, `manutencoes.ts`, `tiposManutencao.ts`, `usuarios.ts`, `convites.ts`, `auth.ts`) call `http` and `unwrap()`.
- `errors.ts` — `mensagensDeErro()` turns any failure (API envelope, network error, etc.) into `string[]` for display via the shared `ErrorList` component.
- `types.ts` — the envelope type, `Role`, and DTOs (kept in sync with the API by hand; `gen:api` only regenerates `schema.d.ts`).
- `tokenStorage.ts` — persists `frota360.token`, `frota360.refreshToken`, `frota360.user` (name/email/role) in `localStorage`.
- Multi-tenancy is transparent to the front-end: `empresaId` comes from the JWT; the client never sends a company id.

### Auth & permissions (`src/auth/`)

- `useSession.ts` exposes the logged-in user reactively via `useSyncExternalStore`, listening to the native `storage` event (other tabs) and a custom `frota360:sessao` event (same tab, since `localStorage` doesn't notify its own writer).
- `permissions.ts` (`pode.*`) mirrors the API's role matrix (Admin / Supervisor / Operador) purely to hide actions that would 403 — **the server is always the actual authority**, never trust the client matrix for security-sensitive logic.
- Route guards live in `src/components/RequireAuth.tsx`: `RequireAuth` (redirect to `/login`, preserving `location.state.from`), `RequireAdmin`, `RequireGestor` (Admin or Supervisor). Applied as wrapper `<Route>` elements in [src/App.tsx](src/App.tsx).
- The role used by the UI is cached at login/refresh time — a role change server-side can take up to the token's lifetime to reflect in the UI, even though the server already enforces it immediately.

### Data fetching

TanStack Query 5. Cache keys are simple arrays per resource (`['motoristas']`, `['veiculos']`, `['rotas']`, `['manutencoes', filtro]`, `['tiposManutencao']`, `['tiposManutencao', 'ativos']`, `['usuarios']`, `['convites']`), invalidated after each mutation on the owning page. Watch for **cross-invalidation** where one resource's mutation affects another list's displayed data (e.g. deleting a motorista/veículo also invalidates `['rotas']` because that table denormalizes their name/plate; concluding a manutenção also invalidates `['veiculos']` because it can advance the vehicle's odometer). The longest chain is **rota → veículo → manutenção**: both opening a rota (when `kmInicial` exceeds the current odometer) and closing one (`POST /rota/{id}/encerrar`) advance the vehicle's odometer, which is what `atrasada`/`kmRestantes` are derived from — so those mutations invalidate `['rotas']`, `['veiculos']` **and** `['manutencoes']`. When adding a mutation, check `src/CONTEXTO-FRONT.md` §6.4 for the current cross-invalidation map before assuming a single `invalidateQueries` call is sufficient.

### Page structure & shared components

Authenticated pages are children of `RequireAuth` and wrapped by `AppLayout` ([src/components/AppLayout.tsx](src/components/AppLayout.tsx)), which provides the collapsible sidebar (state in `localStorage`), header, and the `PageHeader`/`ErrorList` building blocks. Public/auth pages use `AuthScreen`/`AuthHeading` instead ([src/components/AuthScreen.tsx](src/components/AuthScreen.tsx)).

CRUD pages (`MotoristasPage`, `VeiculosPage`, `RotasPage`, `ManutencoesPage`, `TiposManutencaoPage`) follow the same shape and reuse `src/components/Table.tsx`:
- `InlineForm` — create/edit form rendered above the table (not a modal); editing reuses the same form pre-filled, with the page scrolling to top.
- `TableStates` — shared loading/error/empty row rendering.
- `RowActions` / `ConfirmDialog` — per-row edit/delete icons and delete confirmation.
- `FormDialog` — a modal form (used for "concluir manutenção" and "encerrar rota" — the state transitions that carry a side effect on the vehicle's odometer, kept out of the plain edit form on purpose).

Visibility of the "new"/edit/delete affordances is gated by `pode.*` from `auth/permissions.ts`, not by hiding the whole page.

### Design system

`src/styles/design-system.css` defines the "Modernist" look used by the authenticated app: background `#fdfaf6`, surface `#f2ede4`, text `#201e1d`, accent `#1f3a5f` (100–900 ramp), danger `#a03123`, Archivo typeface, **`border-radius: 0` everywhere** — straight-edge, no shadows. Shared classes: `.btn` (`.btn-primary`/`.btn-secondary`/`.btn-icon`/`.btn-danger`), `.field`+`.input`, `.tag` (`.tag-accent`/`.tag-neutral`/`.tag-danger`/`.tag-warning`), `.nav`, `.table`, `.dialog*`.

`LandingPage` ([src/pages/LandingPage.tsx](src/pages/LandingPage.tsx)) is the one exception: it has its own visual language (rounded corners, white cards, pill buttons, soft shadows) in `src/styles/landing.css`, scoped under a `.lp` root and imported only by that page — it locally overrides design-system defaults (heading weight, `p` margins, link color) without touching them anywhere else. It renders no API data; all figures shown are illustrative mocks.

### Routing

Single `BrowserRouter` in [src/App.tsx](src/App.tsx); unknown routes redirect to `/`. New authenticated routes go inside the `RequireAuth` wrapper route, additionally nested under `RequireAdmin`/`RequireGestor` if role-gated, and must also be added to the sidebar in `AppLayout.tsx` and the permission matrix in `auth/permissions.ts` if they're not universally visible.

### Env config

`VITE_API_URL` is the only env var, set per environment in `.env.development`/`.env.production` (production's is intentionally blank and must be filled in at deploy time).
