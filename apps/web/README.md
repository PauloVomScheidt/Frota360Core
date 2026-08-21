# Frota360 Web

Front-end React da API [Frota360](http://localhost:5062/scalar/v1) — gestão de motoristas, veículos e rotas.

## Stack

- React 19 + TypeScript + Vite
- Tailwind CSS 4
- React Router 7
- TanStack Query 5 + Axios (com refresh automático de token)

## Como rodar

Pré-requisitos: Node 20+, API Frota360 rodando em `http://localhost:5062`.

```powershell
npm install
npm run dev
# → http://localhost:5173 (porta fixa — é a origem liberada no CORS da API)
```

A URL da API vem de `VITE_API_URL` (`.env.development` / `.env.production`).

## Scripts

| Script | O que faz |
|---|---|
| `npm run dev` | Servidor de desenvolvimento |
| `npm run build` | Type-check + build de produção |
| `npm run lint` | Lint (oxlint) |
| `npm run gen:api` | Gera `src/api/schema.d.ts` a partir do OpenAPI da API (precisa da API rodando) |

## Estrutura

```
src/
├── api/            # camada de acesso à API
│   ├── http.ts         # axios + Bearer + refresh automático em 401 (single-flight)
│   ├── types.ts        # envelope ApiResponse e DTOs dos recursos
│   ├── tokenStorage.ts # tokens no localStorage
│   ├── auth.ts         # login / register / logout
│   └── motoristas.ts, veiculos.ts, rotas.ts  # CRUD por recurso
├── components/     # componentes compartilhados (RequireAuth, ...)
├── lib/            # queryClient do TanStack Query
└── pages/          # páginas (placeholders — designs virão do Claude Design)
```

## Convenções da API

- Toda resposta vem no envelope `{ sucesso, mensagem, dados, erros }`; a camada `src/api` já desembrulha `dados` e converte falhas em `ApiError` (com a lista `erros` para formulários).
- Em 401, o interceptor tenta `/auth/refresh` uma única vez (lock contra refreshes paralelos, pois a rotação invalida o token anterior) e repete a requisição; se falhar, limpa a sessão e redireciona para `/login`.
- Erros 429 (rate limit) chegam no mesmo envelope — tratar com mensagem amigável.
