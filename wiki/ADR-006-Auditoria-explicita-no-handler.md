# ADR-006 — Auditoria registrada explicitamente em cada handler

**Situação:** Aceita · **Data:** 28/08/2026

## Contexto

Com várias pessoas e quatro papéis alterando os mesmos registros, "quem mudou isto, e quando?" virou pergunta frequente. A proposta inicial deixava auditoria fora do escopo.

## Decisão

* Tabela `LogAuditoria`, **somente inclusão**: não há endpoint de edição nem de exclusão, e o repositório não os expõe.
* **Cada handler de escrita chama `IAuditoriaService.RegistrarAsync`** ao final do caminho feliz, com a entidade, a ação (de um vocabulário fechado), uma frase em português ("Encerrou a rota Joinville → Curitiba") e, nas edições, o diff campo a campo montado antes de a entidade ser alterada.
* Autor (nome, e-mail, papel) e IP de origem ficam gravados na linha, copiados do token: o histórico não muda de sentido se a pessoa for renomeada ou rebaixada.
* Login e logout ficam de fora: seriam o maior volume e o menor valor, e continuam no log da aplicação.

## Consequências

* A trilha registra **a intenção de negócio**, que um `UPDATE` no banco não expressa, e não registra ruído técnico (o hash do refresh token muda a cada login).
* **Auditoria não derruba negócio.** O registro roda depois de a escrita estar salva e engole a própria falha (com log de erro): perder uma linha de trilha é ruim; devolver 500 numa edição já persistida é pior.
* **Nada de segredo no diff**: ele é montado à mão, e hash de senha e tokens nunca entram.
* O custo é disciplina: todo handler de escrita novo precisa registrar, e os testes de cada handler provam o registro. São 35 pontos de registro hoje.
* Os três índices da tabela (`(EmpresaId, DataHora)`, por registro e por pessoa) correspondem às três consultas da tela.
* Retenção declarada de 12 meses; o expurgo ainda não está automatizado.

## Alternativas consideradas

* **Interceptor do EF Core** (`SaveChanges`): captura tudo sem esforço, mas registra mudança de coluna, não intenção, e incluiria ruído técnico e campos sensíveis.
* **Triggers no banco**: mesma limitação, e sem acesso ao usuário do token.
