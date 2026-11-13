# Decisões de Arquitetura (ADRs)

Cada ADR (*Architecture Decision Record*) registra uma decisão que molda o sistema: o contexto em que foi tomada, o que se decidiu, o que isso custa e o que foi descartado.

Os ADRs foram escritos retroativamente em 13/11/2026, a partir do raciocínio registrado em `docs/contexto-api.md` e `docs/contexto-web.md` ao longo do desenvolvimento. A data de cada decisão é a do código que a introduziu, quando o histórico a preserva.

| ADR | Decisão | Data | Situação |
| :---- | :---- | :---- | :---- |
| [ADR-001](ADR-001-PostgreSQL) | PostgreSQL no lugar do SQL Server, sem Redis | 30/08/2026 | Aceita |
| [ADR-002](ADR-002-Multi-tenant-por-claim) | Multiempresa em banco compartilhado, com a empresa vinda do token | 21/08/2026 | Aceita |
| [ADR-003](ADR-003-Motorista-e-um-Usuario) | O motorista é um usuário com papel, não uma entidade à parte | 2026 | Aceita |
| [ADR-004](ADR-004-Sessao-em-cookie-HttpOnly) | Sessão em cookies `HttpOnly`: JWT de 1 h e refresh de 7 dias | 2026 | Aceita |
| [ADR-005](ADR-005-Custos-como-read-model) | Custos como read model, sem tabela própria | 01/09/2026 | Aceita |
| [ADR-006](ADR-006-Auditoria-explicita-no-handler) | Auditoria registrada explicitamente em cada handler | 28/08/2026 | Aceita |
| [ADR-007](ADR-007-EC2-com-Docker-Compose) | Produção numa EC2 única com Docker Compose e Caddy | 2026 | Aceita |
| [ADR-008](ADR-008-Integracao-com-Google-Maps) | Duas chaves do Google; o traçado é calculado uma vez e enviado pelo cliente | 08/09/2026 | Aceita |
| [ADR-009](ADR-009-Hora-local-de-Brasilia) | Datas gravadas em hora local de Brasília, sem fuso | 30/08/2026 | Aceita |
| [ADR-010](ADR-010-Monorepo) | API e front num monorepo | 27/08/2026 | Aceita |

"2026" sem dia indica decisão cujo commit de origem foi absorvido por um merge squash, e por isso não tem data precisa.

## Modelo para um ADR novo

Arquivo `wiki/ADR-0NN-Titulo-Curto.md`, com uma linha na tabela acima e na `_Sidebar.md` se for central:

```markdown
# ADR-0NN — Título

**Situação:** Proposta | Aceita | Substituída por ADR-0XX · **Data:** dd/mm/aaaa

## Contexto
O problema e as forças em jogo.

## Decisão
O que foi decidido, em uma ou duas frases, e como foi implementado.

## Consequências
O que melhora, o que piora e o que passa a ser obrigatório.

## Alternativas consideradas
O que foi descartado, e por quê.
```
