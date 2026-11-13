# ADR-005 — Custos como read model, sem tabela própria

**Situação:** Aceita · **Data:** 01/09/2026

## Contexto

O custo da frota estava espalhado: `/abastecimentos` somava no navegador e `/manutencoes` nem isso. A gestão queria uma visão só — combustível, manutenção e despesas, por veículo, motorista e período, com custo por km.

O custo já existia em duas colunas editáveis: `Abastecimento.Valor` (corrigível pelo `PUT`) e `Manutencao.Custo` (informado na conclusão).

## Decisão

**Não criar tabela de custos.** `ICustoRepository` é um repositório de leitura que une, na consulta, `Abastecimento.Valor`, `Manutencao.Custo` (só manutenções realizadas, com custo) e `Despesa.Valor`. Cada lançamento chega com um discriminador `Origem`, para que uma origem nova entre sem mudar o contrato.

`Despesa`, por outro lado, **tem** tabela, CRUD e auditoria: ela é a fonte de verdade de um gasto que não existe em nenhum outro lugar (pedágio, multa, IPVA).

## Consequências

* **Corrigir o valor na tela de origem já corrige o relatório.** Não há cópia para sincronizar, e portanto nenhum bug de sincronia virando número errado em silêncio.
* O repositório atravessa quatro tabelas (`Abastecimento`, `Manutencao`, `Despesa` e `Rota`, de onde vem o km do R$/km) — exceção consciente ao "um repositório por agregado" do resto do projeto. Ele não escreve nada.
* O EF Core não traduz a união depois de projetar constantes (`"Combustível"`, a origem), então a união é feita em memória sobre páginas limitadas de cada origem. Há teste de integração para a tradução e para a paginação sem repetir linhas.
* A categoria do abastecimento é a constante "Combustível", não o nome do tipo de combustível.
* O front invalida o cache de custos em toda escrita de abastecimento, manutenção, despesa e no encerramento de rota.

## Alternativas consideradas

* **Tabela de custos alimentada pelas telas de origem**: seria um espelho, exigindo sincronia em criação, edição, exclusão e conclusão. A justificativa de desempenho não existe neste volume.
* **Somar no navegador**: inviável com paginação — o front só tem uma página na mão.
