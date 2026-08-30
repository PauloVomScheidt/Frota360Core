# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Monorepo do **Frota360** — sistema de gestão de frotas multi-tenant por empresa. Reúne a API REST .NET 10 e o front-end React que a consome, que antes viviam em repositórios separados (`Frota360`/`Rota360` e `Frota360Web`).

**Escreva tudo em português**: classes, métodos, DTOs, comentários, logs, textos de UI e mensagens de resposta. A única exceção está registrada em [Diagrama de arquitetura](#diagrama-de-arquitetura).

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
└── arquitetura.py         gera docs/arquitetura.png
```

Cada app é independente: comandos rodam de dentro dele, e os caminhos nos seus `CLAUDE.md`, `.slnx`, `.csproj` e `Dockerfile` são relativos à raiz do próprio app.

**Antes de mexer em código, leia o `CLAUDE.md` do app**: [apps/api/CLAUDE.md](apps/api/CLAUDE.md) ou [apps/web/CLAUDE.md](apps/web/CLAUDE.md). Este arquivo cobre só o que é transversal aos dois — e as três seções abaixo (navegação, documentação, contrato) valem para os dois lados, não estão repetidas lá.

## Commits

Nunca commitar algo sem ser explicitamente pedido, somente efetuar as alterações e deixar para o usuario decidir o commit

## Navegação de código

Para perguntas estruturais (como X funciona, o que chama Y, o que quebra se eu mudar Z),
use `codegraph_explore` em vez de Grep/Read. O índice está sempre atualizado.

Vale para os dois apps.

## Documentação

Após todas as alterações realizadas, atualizar as documentações de contexto, `CLAUDE.md` e README para refletir as mudanças. Documentação de contexto é obrigatória para qualquer alteração estrutural, regra de negócio ou endpoint novo.

O aprofundamento vive em `docs/contexto-api.md` e `docs/contexto-web.md` — atualize o lado correspondente, e **os dois** quando a mudança atravessa a fronteira (endpoint, envelope, papel, regra de negócio visível na tela).

### Diagrama de arquitetura

O diagrama vive em `docs/arquitetura.png` e é **gerado** por `docs/arquitetura.py` (Pillow). Não edite o PNG à mão: altere o script e rode `python docs/arquitetura.py` a partir da raiz. Regenere sempre que mudar camada, pipeline de request ou ponto de isolamento por `EmpresaId`.

O diagrama é a **única exceção à regra do português**: seus rótulos são em inglês, para circular fora do time. Paleta monocromática (tons de cinza + preto), sem cor de destaque — os pontos de isolamento por `EmpresaId` são marcados por contorno preto e selo numerado, não por cor. Mantenha isso ao editar o script.

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
| URL da API | `https://localhost:7271` / `http://localhost:5062` (`src/Api/Properties/launchSettings.json`) | `VITE_API_URL` em `.env.development` — hoje aponta para `https://localhost:7271/api/v1` |
| CORS | origem liberada: `http://localhost:5173` | `npm run dev` usa porta fixa 5173 por causa disso |

**Ao criar ou alterar um endpoint, o roteiro completo é:** controller + handler + validator + teste → `docs/contexto-api.md` → `apps/web/src/api/<recurso>.ts` e `types.ts` → `docs/contexto-web.md` (mapa de endpoints §6.5 e cross-invalidation §6.4) → tela.

`npm run gen:api` (em `apps/web/`) regenera só `src/api/schema.d.ts` a partir do OpenAPI e **exige a API rodando** — ele não atualiza `types.ts`.

## Subir o sistema

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

Os demais comandos (build, testes, migrations, lint) estão no `CLAUDE.md` de cada app.