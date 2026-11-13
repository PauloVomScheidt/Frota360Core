# Referência da API

Base: `https://api.frota360app.com.br/api/v1` (desenvolvimento: `https://localhost:7271/api/v1`). A versão também é aceita pelo cabeçalho `api-version`.

A documentação interativa (Scalar, OpenAPI) fica em **`/scalar/v1`**, disponível só em desenvolvimento.

## Autenticação

O login devolve a identidade no corpo e a sessão em dois cookies `HttpOnly`; o navegador os anexa sozinho às chamadas seguintes. Para clientes fora do navegador (Scalar, `curl`), o cabeçalho `Authorization: Bearer <token>` também é aceito.

O `EmpresaId` nunca é enviado pelo cliente: vem do token.

## Envelope de resposta

Toda resposta, de sucesso ou de erro, usa o mesmo formato:

```json
{
  "sucesso": true,
  "mensagem": "Veículo cadastrado com sucesso.",
  "dados": { "id": 3, "placa": "ABC1D23", "emRota": false },
  "erros": []
}
```

## Erros

| Status | Quando | `mensagem` / `erros` |
| :---- | :---- | :---- |
| 400 | Dados inválidos (validação) | `"Dados inválidos."` + um erro por campo: `"Placa: formato inválido"` |
| 401 | Sem sessão ou sessão expirada | Mensagem fixa; o front tenta renovar a sessão uma vez |
| 403 | Papel sem permissão para a ação | Mensagem fixa |
| 404 | Registro inexistente — **ou de outra empresa, ou de outro motorista** | `"Veículo 99 não encontrado."` |
| 422 | Regra de negócio violada | Texto para o usuário final, exibido literalmente: `"Já existe um veículo com a placa ABC1D23 nesta empresa."` |
| 429 | Rate limit excedido | — |
| 500 | Erro inesperado | Mensagem genérica; o detalhe fica só no log |

## Paginação

Seis recursos paginam no servidor: `/rota` (inclusive `/rota/minhas`), `/manutencao`, `/abastecimento`, `/despesa`, `/custo` e `/auditoria`. Parâmetros `pagina` (a partir de 1) e `tamanhoPagina` (máximo 100). A resposta traz:

```json
{ "itens": [ ... ], "pagina": 1, "tamanhoPagina": 20, "total": 137, "totalPaginas": 7 }
```

Os totais de rodapé ("N lançamentos · R$ X") vêm de endpoints `/resumo` com os mesmos filtros, nunca da soma da página.

As demais listas (veículos, motoristas, usuários, convites, catálogos) vêm inteiras: são limitadas pelo tamanho da frota e da equipe.

## Endpoints

"Gestão" = Admin, Supervisor e Operador. "Todos" = qualquer usuário autenticado.

### Acesso e conta

| Método | Rota | Acesso |
| :---- | :---- | :---- |
| POST | `/auth/login`, `/auth/refresh`, `/auth/esqueci-senha`, `/auth/redefinir-senha` | Anônimo, 5 req/min |
| POST | `/auth/logout` | Todos |
| POST | `/backoffice/empresa` | Cabeçalho `X-Backoffice-Key` |
| POST | `/convite/consultar`, `/convite/aceitar` | Anônimo |
| GET · POST · DELETE | `/convite`, `/convite/{id}` | Admin |
| GET | `/usuario` | Admin |
| PUT | `/usuario/{id}/role`, `/usuario/{id}/ativo` | Admin |
| GET · PUT | `/usuario/perfil` | Todos (o próprio cadastro) |

### Frota e operação

| Método | Rota | Acesso |
| :---- | :---- | :---- |
| GET | `/veiculo`, `/veiculo/{id}` | Todos (resposta traz `emRota`) |
| POST · PUT | `/veiculo` | Admin, Supervisor |
| GET | `/motorista`, `/motorista/{id}` | Gestão (somente leitura) |
| GET | `/rota?ativo=`, `/rota/{id}` | Gestão |
| GET | `/rota/minhas` | Motorista (as próprias) |
| GET | `/rota/resumo?de=&ate=` | Todos (o motorista recebe só o próprio km) |
| POST | `/rota`, `/rota/{id}/encerrar` | Todos (o motorista só as próprias) |
| POST | `/rota/calcular` | Todos — consulta cobrada à Google, não grava nada |
| PUT | `/rota/{id}` | Gestão |

### Manutenção

| Método | Rota | Acesso |
| :---- | :---- | :---- |
| GET | `/manutencao?veiculoId=&status=&de=&ate=`, `/manutencao/{id}` | Todos (o motorista não vê o custo) |
| GET | `/manutencao/resumo` | Gestão — atrasadas, vencendo e as mais urgentes |
| POST · PUT | `/manutencao`, `/manutencao/{id}/concluir` | Admin, Supervisor |
| GET | `/tipomanutencao?apenasAtivos=` | Gestão |
| POST · PUT | `/tipomanutencao` | Admin, Supervisor |

### Abastecimento, despesas e custos

| Método | Rota | Acesso |
| :---- | :---- | :---- |
| GET · POST · PUT | `/abastecimento`, `/abastecimento/{id}` | Todos (o motorista só o dele; o valor é calculado pelo servidor) |
| GET | `/abastecimento/resumo`, `/abastecimento/anterior` | Todos (mesmo recorte) |
| GET | `/tipocombustivel`, `/posto` | Todos (o motorista precisa para lançar) |
| POST · PUT | `/tipocombustivel`, `/posto` | Admin, Supervisor |
| GET · POST · PUT | `/despesa`, `/despesa/{id}`, `/despesa/resumo` | Gestão |
| GET | `/tipodespesa` | Gestão |
| POST · PUT | `/tipodespesa` | Admin, Supervisor |
| GET | `/custo?veiculoId=&motoristaId=&origem=&de=&ate=` | Gestão |
| GET | `/custo/resumo` | Gestão — totais por origem, por veículo (R$/km, km/l) e por mês |

### Exclusão, auditoria e saúde

| Método | Rota | Acesso |
| :---- | :---- | :---- |
| DELETE | `/{recurso}/{id}` | Admin, Supervisor (exclusão acompanha edição). `/convite/{id}` é só Admin. Não existe exclusão de usuário |
| GET | `/auditoria?entidade=&acao=&usuarioId=&de=&ate=` | Admin — somente leitura |
| GET | `/health`, `/health/detail` | Aberto |

## Transições de estado

Mudar o estado de um registro é uma ação própria, não um `PUT`, porque ela tem efeitos colaterais:

| Ação | Efeito |
| :---- | :---- |
| `POST /rota/{id}/encerrar` `{ kmFinal, dataFim? }` | Calcula o km percorrido, avança o odômetro e atualiza último motorista e última viagem do veículo |
| `POST /manutencao/{id}/concluir` `{ quilometragemRealizada, dataRealizacao, custo?, observacao? }` | Marca como realizada e avança o odômetro se for o caso |

O `PUT` dessas entidades não aceita os campos de estado (`ativo`, `dataFim`, `status`).
