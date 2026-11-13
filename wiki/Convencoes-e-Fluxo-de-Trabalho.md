# Convenções e Fluxo de Trabalho

## Branches e pull requests

* **A `main` é a única branch de longa duração.** A `develop` foi aposentada em 13/11/2026.
* Todo trabalho sai de uma branch criada a partir da `main` e volta por pull request.
* O merge exige os checks `api`, `web` e `docker` verdes ([Testes e Qualidade](Testes-e-Qualidade#integração-contínua)); o SonarCloud comenta o quality gate no PR.
* Mudança de contrato entre API e front entra **nos dois lados no mesmo PR** ([ADR-010](ADR-010-Monorepo)).

## Idioma

**Tudo em português**: classes, métodos, DTOs, comentários, logs, textos de interface e mensagens de resposta. Mensagens de erro de regra de negócio são escritas para o usuário final, porque aparecem literalmente na tela.

## Código

* O mínimo de comentários: o código deve se explicar. Comentário só quando há uma regra de negócio ou configuração que o código não deixa evidente.
* **Backend**: uma fatia vertical por caso de uso (`UseCases/<Agregado>/Commands|Queries/<Acao>/`), construtores primários, handlers `sealed`, mapeamento manual com `ToResponse()`, resposta sempre no envelope `ApiResponse<T>` montado no controller.
* **Regra de negócio violada** → `throw new InvalidOperationException("mensagem ao usuário")` → 422.
* **Todo acesso a dados recebe o `EmpresaId` do token**, e todo id vindo da requisição é resolvido dentro da empresa antes de gravar ([ADR-002](ADR-002-Multi-tenant-por-claim)).
* **Toda escrita registra auditoria** no fim do caminho feliz ([ADR-006](ADR-006-Auditoria-explicita-no-handler)).
* **Front**: os tipos de `src/api/types.ts` são mantidos à mão e espelham os DTOs; cada escrita invalida as chaves de cache que afeta.

## Testes

* Funcionalidade nova nasce com o teste primeiro (TDD), e o histórico do Git mostra isso.
* O teste vai na suíte que o código pede: regra de negócio na unitária, contrato HTTP na da camada Api, banco na de integração.
* A cobertura não pode cair abaixo do mínimo do CI (75% no backend, 50% no front).

## Documentação

Toda alteração estrutural, regra de negócio nova ou endpoint novo atualiza, no mesmo PR:

| O quê | Onde |
| :---- | :---- |
| Contexto profundo do backend | `docs/contexto-api.md` |
| Contexto profundo do front | `docs/contexto-web.md` |
| Requisitos e regras numeradas | `docs/RFC_Gerenciador_Frotas_V1.md` e a página [Requisitos](Requisitos) |
| Decisão de arquitetura nova | Um [ADR](Decisoes-de-Arquitetura) |
| Esta Wiki | A pasta `wiki/` do repositório — nunca a interface da Wiki |

O roteiro completo para um endpoint novo: controller + handler + validator + teste → `contexto-api.md` → `apps/web/src/api/<recurso>.ts` e `types.ts` → `contexto-web.md` → tela.

## Commits

Mensagens em português, com prefixo de tipo (`feat:`, `fix:`, `test:`, `refactor:`, `docs:`, `chore:`). O PR é integrado por squash.
