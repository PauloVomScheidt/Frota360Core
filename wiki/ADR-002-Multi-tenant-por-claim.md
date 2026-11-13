# ADR-002 — Multiempresa em banco compartilhado, com a empresa vinda do token

**Situação:** Aceita · **Data:** 21/08/2026

## Contexto

O Frota360 atende várias empresas com uma única instância. Os dados de uma não podem, em hipótese nenhuma, aparecer para outra — e um vazamento entre tenants é o tipo de falha que não se descobre olhando a tela de quem está certo.

## Decisão

* **Um banco, uma coluna `EmpresaId` em toda entidade de negócio.**
* **O `EmpresaId` vem só da claim `empresaId` do JWT**, lida por `ICurrentUserService` — nunca do corpo, da rota ou da query string. O front nem envia esse dado.
* **Sem filtro global do EF.** Todo método de repositório recebe `empresaId` como parâmetro e filtra por ele: `GetByIdAsync(id, empresaId)`, `ExisteXAsync(empresaId, …)`. Não existe sobrecarga sem ele. Nos filtros de consulta, o `empresaId` fica fora do objeto de filtro, como parâmetro separado, para que nenhum caminho consiga montar uma consulta sem escopo.
* **Todo id de chave estrangeira que chega na requisição é resolvido dentro da empresa** antes de ser gravado. Id de outra empresa simplesmente "não existe" e cai no mesmo 404/422 de um id inexistente, sem revelar que ele existe em outro lugar.
* **Índices únicos compostos com `EmpresaId`**: a mesma placa ou o mesmo CPF podem existir em empresas diferentes. A exceção é `Usuario.Email`, único global, porque identifica a pessoa no login, antes de haver empresa no contexto.

## Consequências

* O escopo fica visível na assinatura de cada método: esquecer o filtro exige escrever uma consulta que não recebe `empresaId`, o que destoa no code review.
* **Todo handler tem um teste que prova o escopo** (o repositório é chamado com o `EmpresaId` do usuário), e os repositórios são testados contra o banco real na suíte de integração.
* Uma varredura em 25/08/2026 procurou ids de chave estrangeira gravados sem resolução pela empresa e achou dois casos, nos handlers de rota; foram corrigidos e viraram a **RN07**.
* O nome da empresa aparece na sessão e no cabeçalho, mas o id nunca sai para o cliente.

## Alternativas consideradas

* **Um banco por empresa** ou **um schema por empresa**: isolamento mais forte, mas multiplica migrations, conexões e custo de operação para um produto que atende empresas pequenas.
* **Filtro global de consulta no EF**: menos código repetido, mas o escopo fica implícito, e um `IgnoreQueryFilters()` esquecido, ou uma consulta fora do `DbContext`, escapa sem aviso.
