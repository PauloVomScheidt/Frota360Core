# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Monorepo do **Frota360** — sistema de gestão de frotas multi-tenant por empresa. Reúne a API REST .NET 10 e o front-end React que a consome, que antes viviam em repositórios separados (`Frota360`/`Rota360` e `Frota360Web`).

**Escreva tudo em português**: classes, métodos, DTOs, comentários, logs, textos de UI e mensagens de resposta.

## Mapa do repositório

```
apps/
├── api/                   backend .NET 10 — autocontido
│   ├── CLAUDE.md          ← convenções do backend
│   ├── Frota360.slnx
│   ├── Dockerfile
│   ├── src/{Domain,Application,Infrastructure,Api}
│   └── tests/Frota360.Tests
└── web/                   front React 19 + Vite — autocontido
    ├── CLAUDE.md          ← convenções do front
    ├── package.json
    └── src/
docs/
├── contexto-api.md        contexto profundo do backend
├── contexto-web.md        contexto profundo do front (tela a tela, cache keys, endpoints)
└── deploy.md              roteiro de produção (EC2 + Docker Compose) e ensaio local
docker-compose.yml         banco de desenvolvimento
docker-compose.prod.yml    stack de produção (db + api + caddy)
docker-compose.local.yml   override que ensaia a stack de produção sem domínio
Caddyfile / Caddyfile.local  proxy reverso — produção e ensaio
.env.example               modelo da configuração de produção
```

Cada app é independente: comandos rodam de dentro dele, e os caminhos nos seus `CLAUDE.md`, `.slnx`, `.csproj` e `Dockerfile` são relativos à raiz do próprio app.

**Antes de mexer em código, leia o `CLAUDE.md` do app**: [apps/api/CLAUDE.md](apps/api/CLAUDE.md) ou [apps/web/CLAUDE.md](apps/web/CLAUDE.md). Este arquivo cobre só o que é transversal aos dois — e as três seções abaixo (navegação, documentação, contrato) valem para os dois lados, não estão repetidas lá.

## Convenções de Codigo

Incluir a menor quantidade possivel de comentarios, o codigo de ser autoexplicativo, limpo e facil de entender, somente quando for essencial para o entendimento de alguma configuração ou regra de negocio não explicita

## Commits

Nunca commitar algo sem ser explicitamente pedido, somente efetuar as alterações e deixar para o usuario decidir o commit

## Navegação de código

Para perguntas estruturais (como X funciona, o que chama Y, o que quebra se eu mudar Z),
use `codegraph_explore` em vez de Grep/Read. O índice está sempre atualizado.

Vale para os dois apps.

## Documentação

Após todas as alterações realizadas, atualizar as documentações de contexto, `CLAUDE.md` e README para refletir as mudanças. Documentação de contexto é obrigatória para qualquer alteração estrutural, regra de negócio ou endpoint novo.

O aprofundamento vive em `docs/contexto-api.md` e `docs/contexto-web.md` — atualize o lado correspondente, e **os dois** quando a mudança atravessa a fronteira (endpoint, envelope, papel, regra de negócio visível na tela).

## O contrato entre API e front

O motivo de os dois viverem no mesmo repositório: mudança de um lado quase sempre exige mexer no outro **no mesmo commit**.

| Ponto de contato | Backend (`apps/api`) | Front (`apps/web`) |
|---|---|---|
| Envelope de resposta | `ApiResponse<T>` montado no controller (`Sucesso`/`Mensagem`/`Dados`/`Erros`) | `unwrap()` em `src/api/http.ts` desempacota `dados` e lança `ApiError` |
| Tipos dos DTOs | `src/Application/DTOs/**` | `src/api/types.ts` — **mantido à mão**, não gerado. Campo derivado na leitura (`Atrasada`/`KmRestantes` da manutenção, `EmRota` do veículo) entra só no `*Response`, nunca no `*Request` |
| Papéis | `Roles` em `src/Domain` (`Admin`, `Supervisor`, `Operador`, `Motorista`, + a constante `Roles.Gestao`) + `[Authorize(Roles = ...)]` nos controllers | `pode.*` em `src/auth/permissions.ts` — espelho apenas para esconder ações; **o servidor é a autoridade** |
| Auditoria | `LogAuditoria` (append-only) alimentada por `IAuditoriaService.RegistrarAsync` em cada handler de escrita; vocabulário fechado em `AcoesAuditoria`/`EntidadesAuditadas` | `pode.verAuditoria` (só Admin) + `AuditoriaPage`; as uniões `EntidadeAuditada`/`AcaoAuditoria` em `types.ts` espelham as constantes do Domain — **mexeu numa, mexa na outra** |
| Paginação | `ResultadoPaginado<T>` (`src/Domain/Common`) dentro de `ApiResponse<T>.Dados`; só `GET /auditoria` pagina | `ResultadoPaginado<T>` em `types.ts` + o componente `Paginacao` de `components/Table.tsx` |
| Segundo eixo (Motorista) | além da rota, vale no **abastecimento**, e o eixo é `MotoristaId` (de quem é o gasto), não `UsuarioId` (quem digitou): `GetAllAbastecimentosHandler` sobrescreve o filtro com o token, e lançamento de outro motorista devolve `null` → 404. No create ele lança sempre em si mesmo e, com rota aberta, só no veículo dela (422) | a lista já vem recortada — a tela não filtra por dono e esconde "Quem lançou"; o campo motorista vira `disabled` com o próprio nome, e o select de veículo mostra só o da rota ativa |
| Odômetro | **dois** fluxos o avançam (rota e manutenção) e nenhum o retrocede — o abastecimento não mexe nele | quem mexe no odômetro invalida `['veiculos']` **e** `['manutencoes']`; o abastecimento invalida só a própria lista |
| Motorista | **é o próprio `Usuario`** com `Role = Motorista` (não há entidade `Motorista`); `Rota.CodigoMotorista` referencia `Usuario`, e o escopo de `/rota/minhas` sai do `sub` do token. Lê veículos e manutenções (sem `Custo`) | as entradas `pode.ver*` são **por tela** e o guarda `RequirePode` as aplica na rota; `rotaInicial(role)` é o destino de todo redirecionamento |
| Multi-tenant | `EmpresaId` vem da claim `empresaId` do JWT | transparente — o cliente nunca envia id de empresa |
| Erro de regra de negócio | `throw new InvalidOperationException("texto ao usuário")` → 422 | mensagem exibida literalmente via `mensagensDeErro()` |
| URL da API | dev: `https://localhost:7271` / `http://localhost:5062`; prod: `api.frota360app.com.br` atrás do Caddy | `VITE_API_URL` — **sempre terminando em `/api/v1`**, porque os módulos de `src/api` chamam caminhos relativos sobre o `baseURL`. Vazio, o `http.ts` lança no boot |
| CORS | origem liberada: `http://localhost:5173` | `npm run dev` usa porta fixa 5173 por causa disso |

**Ao criar ou alterar um endpoint, o roteiro completo é:** controller + handler + validator + teste → `docs/contexto-api.md` → `apps/web/src/api/<recurso>.ts` e `types.ts` → `docs/contexto-web.md` (mapa de endpoints §6.5 e cross-invalidation §6.4) → tela.

`npm run gen:api` (em `apps/web/`) regenera só `src/api/schema.d.ts` a partir do OpenAPI e **exige a API rodando** — ele não atualiza `types.ts`.

## Subir o sistema

O banco é **PostgreSQL 17** e roda em container — suba antes dos dois terminais:

```powershell
docker compose up -d             # postgres:17 na 5432, banco `frota360`
```

Dois terminais:

```powershell
cd apps/api
dotnet run --project src/Api     # http://localhost:5062 → /scalar/v1
```

```powershell
cd apps/web
npm install
npm run dev                      # http://localhost:5173 (porta fixa — origem liberada no CORS)
```

Num banco zerado não há usuários: provisione uma empresa pelo backoffice da API (`POST /backoffice/empresa`) e abra o `linkConvite` retornado — ele cai em `/convite?token=...`.

Para poupar esse passo em dev, `./scripts/seed-dev.ps1` faz o bootstrap inteiro (empresa + Admin + Motorista, senha `SenhaForte123`). É re-executável e não toca em outras empresas — só o `-Recriar` é destrutivo.

Os demais comandos (build, testes, migrations, lint) estão no `CLAUDE.md` de cada app.

## Produção

O deploy é uma EC2 única com Docker Compose (API + Postgres + Caddy) e o front estático em
S3/CloudFront. O roteiro está em [docs/deploy.md](docs/deploy.md); o **porquê** de cada decisão,
em [docs/contexto-api.md](docs/contexto-api.md) (§ Deploy).

Três coisas que valem para qualquer mudança:

- **A stack de produção tem ensaio local.** `docker-compose.local.yml` sobe exatamente a mesma
  configuração trocando só o Caddyfile, sem exigir domínio. Mexeu em compose, Dockerfile ou
  pipeline de middleware? Ensaie antes.
- **O `ForwardedHeaders` depende da sub-rede do compose.** Se mudar `172.28.0.0/16` em
  `docker-compose.prod.yml`, mude `ProxyReverso__RedeConfiavel` junto — senão a auditoria volta
  a gravar o IP do proxy, em silêncio.
- **Nada de segredo em arquivo versionado.** `appsettings.json` é a base sem segredo e vai para
  a imagem; os de ambiente são gitignored e excluídos pelo `.dockerignore`. Em produção tudo vem
  do `.env` da instância (modelo em `.env.example`).