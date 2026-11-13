# ADR-001 — PostgreSQL no lugar do SQL Server, sem Redis

**Situação:** Aceita · **Data:** 30/08/2026

## Contexto

A proposta inicial usava SQL Server, com as tabelas criadas à mão no SSMS, e Redis como cache de leitura. Com o deploy se aproximando, três limites do SQL Server Express apareceram: teto de 10 GB por banco, exigência de 2 GB de RAM e nenhuma imagem para arm64, o que impedia usar uma instância Graviton (mais barata) na AWS. Ainda não havia banco em produção, então a troca não exigia migrar dados.

O Redis nunca chegou a ser necessário: o volume de uma frota de pequena e média empresa é atendido pela consulta direta ao banco, e o cache acrescentaria uma peça para operar e um problema de invalidação.

## Decisão

Adotar **PostgreSQL 17** com o provider Npgsql do EF Core. As 12 migrations do SQL Server foram apagadas e regeradas como uma `Initial` única — o histórico antigo continua no git. O schema passou a ser versionado só por migrations, aplicadas no boot em produção. O Redis saiu do escopo.

## Consequências

* **Sem custo de licença**, imagem leve e multiarquitetura; o mesmo `postgres:17` roda em desenvolvimento, nos testes (Testcontainers) e em produção.
* **Comparação de texto passou a diferenciar maiúsculas.** A collation do SQL Server fazia `Fulano@X.com` casar com `fulano@x.com` sem ninguém pedir. Sem tratamento, quem digitasse outra caixa não logaria. Todo e-mail agora passa por `EmailNormalizado.De` na gravação e na consulta, e há teste de integração para isso.
* **O Npgsql é estrito com `DateTimeKind`** e recusa misturar datas locais e UTC na mesma coluna. Isso levou ao [ADR-009](ADR-009-Hora-local-de-Brasilia).
* Defaults e índices filtrados mudaram de sintaxe (`GETDATE()` → `CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'`, colchetes → aspas duplas).
* Sem cache, o RNF01 (500 ms) depende de índices e de consultas paginadas — por isso as listas que crescem paginam no servidor.

## Alternativas consideradas

* **Manter o SQL Server**: descartado pelos limites do Express e pela falta de arm64.
* **Banco gerenciado (RDS)**: descartado pelo custo para o volume atual; ver [ADR-007](ADR-007-EC2-com-Docker-Compose).
* **Manter o Redis**: descartado por não haver gargalo de leitura que o justifique.
