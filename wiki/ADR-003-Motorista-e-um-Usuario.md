# ADR-003 — O motorista é um usuário com papel, não uma entidade à parte

**Situação:** Aceita · **Data:** 2026

## Contexto

Na proposta inicial, `Motorista` era uma entidade com cadastro próprio (nome, e-mail, CPF, nascimento), mantida pela gestão, enquanto `Usuario` era quem operava o sistema. Quando o motorista passou a usar o sistema — abrir e encerrar a própria rota, lançar o abastecimento na estrada —, a mesma pessoa passaria a ter dois registros, que divergiriam na primeira correção de nome.

## Decisão

* **Não existe entidade `Motorista`.** Motorista é um `Usuario` com `Role = Motorista`, convidado, promovido e rebaixado como qualquer outro papel.
* `Rota.CodigoMotorista`, `Abastecimento.MotoristaId` e `Despesa.MotoristaId` são chaves estrangeiras para `Usuario`.
* **O escopo do motorista é duplo**: empresa e o próprio usuário, os dois tirados do token. `GET /rota/minhas` não recebe parâmetro; no abastecimento, o filtro de motorista vindo do cliente é sobrescrito pelo do token. Registro de outro motorista devolve **404**, não 403 — para quem não é dono, ele não existe.
* No cadastro de rota e de abastecimento, o motorista enviado no corpo é ignorado para esse papel: ele só registra para si mesmo.
* `GET /motorista` virou somente leitura: lista os usuários com o papel.

## Consequências

* Uma pessoa, um cadastro. CPF e nascimento são opcionais e informados pela própria pessoa no aceite do convite ou no perfil.
* **Usuário nunca é excluído, só desativado** (chave estrangeira `Restrict`), para que o histórico de rotas e gastos continue apontando para alguém.
* Um motorista rebaixado some da lista de motoristas, mas as rotas dele continuam identificáveis: o nome vai desnormalizado na resposta da rota.
* O abastecimento guarda **duas pessoas**: o dono do gasto (`MotoristaId`, eixo do recorte) e quem digitou (`UsuarioId`). Assim o motorista vê também o que a gestão lançou em seu nome.

## Alternativas consideradas

* **Manter `Motorista` com vínculo opcional a `Usuario`**: preservaria motoristas sem acesso ao sistema, mas criaria dois cadastros por pessoa e um estado inconsistente possível (motorista vinculado a um usuário de outro papel).
