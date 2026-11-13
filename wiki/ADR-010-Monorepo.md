# ADR-010 — API e front num monorepo

**Situação:** Aceita · **Data:** 27/08/2026

## Contexto

API e front nasceram em repositórios separados (`Frota360`/`Rota360` e `Frota360Web`). Os tipos do front são mantidos à mão e espelham os DTOs da API, e quase toda mudança atravessava a fronteira: campo novo num DTO, papel novo, endpoint paginado, mensagem de regra de negócio. Em dois repositórios, isso virava dois PRs que precisavam entrar juntos e que podiam divergir no meio do caminho.

## Decisão

Unir os dois num monorepo, `Frota360Core`, **preservando o histórico de commits de ambos**:

```
apps/api/   backend .NET — autocontido
apps/web/   front React — autocontido
docs/       documentação compartilhada
wiki/       fonte desta Wiki
```

Cada app continua independente: comandos rodam de dentro dele, e seus caminhos (`.slnx`, `Dockerfile`, `package.json`) são relativos à própria raiz. Uma mudança de contrato entra **nos dois lados no mesmo commit**.

## Consequências

* Uma mudança de contrato é revisada num PR só, com o servidor e a tela lado a lado.
* O CI roda os dois apps a cada PR (jobs `api`, `web` e `docker`), e o SonarCloud analisa o monorepo como um projeto único, com um quality gate e um badge.
* **Workflows não podem ter filtro de caminho** nos checks obrigatórios: um check que nunca reporta trava o PR. Os jobs rodam sempre.
* A documentação de contrato vive num lugar só (o `CLAUDE.md` da raiz tem a tabela de pontos de contato API ↔ front).

## Alternativas consideradas

* **Manter dois repositórios**, sincronizando por versão da API: exigiria versionar o contrato ou gerar os tipos automaticamente, um processo pesado para uma equipe de uma pessoa.
* **Gerar os tipos do front a partir do OpenAPI**: existe um script (`npm run gen:api`), mas ele exige a API no ar e os tipos gerados não substituem a camada escrita à mão, que tem os campos derivados de leitura.
