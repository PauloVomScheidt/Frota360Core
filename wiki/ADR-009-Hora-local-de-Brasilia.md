# ADR-009 — Datas gravadas em hora local de Brasília, sem fuso

**Situação:** Aceita · **Data:** 30/08/2026

## Contexto

Na migração para o PostgreSQL ([ADR-001](ADR-001-PostgreSQL)), o Npgsql se mostrou estrito: colunas `timestamptz` recusam datas sem fuso, e `timestamp without time zone` recusa datas UTC — ele lança exceção em vez de converter. O sistema gravava dois tipos de data na mesma coluna: `DateTime.Now` (local) nos carimbos de inclusão, e o `aaaa-MM-dd` vindo do formulário (sem fuso) nas datas de negócio. O encerramento de rota mistura os dois na mesma propriedade (`dataFim ?? DateTime.Now`).

Ao mesmo tempo, as telas de convites e auditoria tinham um erro de 3 horas: o valor era gravado em UTC e exibido como local.

## Decisão

* **Todo `DateTime` é gravado como hora local de Brasília, em `timestamp without time zone`.** Um conversor aplicado a todas as datas do `DbContext` descarta o tipo de fuso na escrita.
* A API devolve as datas sem o sufixo `Z`, e **o front as exibe como chegam**, sem converter.
* **O fuso do processo faz parte dos dados.** O Dockerfile fixa `TZ=America/Sao_Paulo`, e a API registra o fuso efetivo na segunda linha do log. Os defaults SQL usam `CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'`, porque o container do banco roda em UTC.
* A única data em UTC é a expiração do JWT, que é UTC por definição do protocolo.

## Consequências

* O valor digitado é o gravado e o exibido, sem intermediários; os dois erros de 3 horas desapareceram.
* **Container sem `TZ` gravaria UTC em silêncio**, sem erro nenhum. Por isso o log de inicialização mostra o fuso, e o ensaio local confere esse valor.
* **Não serve para operação em vários fusos.** Se houver cliente fora do Brasil, será preciso separar instantes (UTC, `timestamptz`) de datas de calendário (`date`), o que atinge entidades, DTOs, validators e os tipos do front. O Brasil não tem horário de verão desde 2019, então não há transição a tratar hoje.
* Se a API algum dia passar a mandar `Z`, o front exibiria um dia a menos em toda tela com data: os dois lados mudam juntos.

## Alternativas consideradas

* **Tudo em UTC (`timestamptz`) com conversão no front**: correto para vários fusos, mas exige converter datas de calendário (vencimento, abastecimento) que não são instantes, e reintroduz o risco de erro de um dia.
