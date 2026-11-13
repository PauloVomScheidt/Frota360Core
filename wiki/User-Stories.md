# User Stories

Histórias de usuário por papel, com critérios de aceite e os requisitos ([RF/RN](Requisitos)) que cada uma cobre. As personas vêm do RFC:

| Persona | Papel |
| :---- | :---- |
| **Ana Paula**, analista administrativa | Admin |
| **Carlos**, gestor de frota | Supervisor |
| **Operador de pátio** | Operador |
| **Roberto**, motorista há 12 anos, usa o celular | Motorista |

---

## Equipe Frota360 (backoffice)

### US01 — Provisionar uma empresa cliente
**Como** equipe Frota360, **quero** cadastrar uma empresa nova **para que** o primeiro administrador dela receba o acesso.

* A chamada exige o cabeçalho `X-Backoffice-Key`; sem ele, ou com a chave errada, responde 401.
* A empresa nasce com os catálogos padrão de tipos de manutenção, despesa e combustível.
* O primeiro Admin recebe um convite por e-mail.

*Cobre:* RF06.

---

## Convidado

### US02 — Aceitar um convite
**Como** pessoa convidada, **quero** criar minha conta pelo link do e-mail **para** começar a usar o sistema.

* Antes de criar a conta, a tela mostra a empresa, o e-mail e o papel do convite.
* Convite expirado (7 dias), já usado ou substituído por outro mostra "inválido, já utilizado ou expirou".
* A senha exige 6 caracteres, uma maiúscula e um número; CPF e nascimento são opcionais.
* Ao aceitar, já entro no sistema, sem precisar fazer login.

*Cobre:* RF04, RF08, RN03, RN20.

---

## Todos os papéis

### US03 — Entrar e continuar conectado
**Como** usuário, **quero** entrar com e-mail e senha e não ser desconectado a cada hora **para** trabalhar sem interrupção.

* E-mail com maiúsculas ou minúsculas funciona igual.
* A sessão se renova sozinha por até 7 dias; depois disso, volto ao login.
* "Sair" encerra a sessão também no servidor.

*Cobre:* RF01, RF02, RN04.

### US04 — Recuperar a senha
**Como** usuário, **quero** redefinir minha senha pelo e-mail **para** voltar a entrar quando a esquecer.

* A mensagem é a mesma exista ou não o e-mail, para não revelar quem tem conta.
* O link vale 30 minutos e, depois de usado, encerra as sessões abertas.

*Cobre:* RF03.

### US05 — Corrigir meu cadastro
**Como** usuário, **quero** ver e corrigir meu nome, CPF e data de nascimento **para** manter meus dados corretos.

* Só altero os meus dados; não existe edição do cadastro de outra pessoa.
* CPF já usado por outra pessoa da empresa é recusado com mensagem.

*Cobre:* RF10, RN03.

---

## Admin (Ana Paula)

### US06 — Convidar a equipe
**Como** Admin, **quero** convidar pessoas por e-mail já com o papel definido **para** dar acesso só ao que cada uma precisa.

* Escolho entre Admin, Supervisor, Operador e Motorista.
* Posso cancelar um convite pendente.
* Reenviar convite para o mesmo e-mail invalida o anterior.

*Cobre:* RF07, RN20.

### US07 — Administrar usuários
**Como** Admin, **quero** mudar o papel de alguém ou desativar a conta **para** acompanhar mudanças na equipe.

* A mudança de papel ou a desativação encerra a sessão da pessoa na hora.
* O sistema não deixa a empresa ficar sem nenhum Admin ativo.
* Ninguém é excluído: a conta desativada preserva o histórico de rotas e gastos.

*Cobre:* RF09, RN08, RN15.

### US08 — Saber quem alterou o quê
**Como** Admin, **quero** consultar a trilha de alterações **para** responder "quem mudou isto e quando".

* Filtro por tipo de registro, ação, pessoa e período.
* Cada linha mostra autor, papel, data, a frase do que aconteceu e, nas edições, o "de → para" de cada campo.
* Ninguém, nem o Admin, edita ou apaga uma linha da trilha.

*Cobre:* RF31, RF32, RN18.

---

## Supervisor (Carlos)

### US09 — Cadastrar a frota
**Como** Supervisor, **quero** cadastrar veículos com nome, marca, placa e odômetro **para** ter a frota no sistema.

* Placa aceita nos formatos Mercosul e antigo, em qualquer caixa, e gravada em maiúsculas.
* Placa já cadastrada na empresa é recusada com mensagem.
* Veículo com rota, abastecimento ou despesa não pode ser excluído.
* A lista mostra se cada veículo está em rota ou disponível.

*Cobre:* RF11, RF12, RF13, RN02, RN05, RN08, RN09.

### US10 — Planejar a manutenção preventiva
**Como** Supervisor, **quero** agendar manutenções por quilometragem e data **para** não deixar revisão vencer.

* Escolho o tipo num catálogo da empresa; tipo inativo não aparece para lançamento novo.
* Não consigo agendar duas manutenções pendentes iguais (mesmo veículo, tipo e km).
* A manutenção aparece como **atrasada** quando o odômetro ou a data passam do previsto, e como **vencendo** quando faltam até 500 km.
* Ao concluir, informo km, data e custo, e o odômetro do veículo avança se for o caso.

*Cobre:* RF20, RF21, RF22, RF23, RN12, RN14, RN17.

### US11 — Ver o custo da frota
**Como** Supervisor, **quero** ver combustível, manutenção e despesas juntos **para** saber quanto custa cada veículo por quilômetro.

* Filtro por veículo, motorista, origem do custo e período.
* Cada veículo mostra total, km rodado, R$/km e km/l.
* Os totais valem para o filtro inteiro, não só para a página exibida.

*Cobre:* RF27, RF29, RF33.

### US12 — Manter os catálogos
**Como** Supervisor, **quero** manter tipos de manutenção, de despesa, de combustível e os postos credenciados **para** padronizar os lançamentos.

* Nome repetido no mesmo catálogo é recusado.
* Item em uso não é excluído: é inativado, e continua aparecendo no histórico.

*Cobre:* RF20, RF26, RF28, RN08, RN17.

---

## Gestão (Admin, Supervisor, Operador)

### US13 — Começar o dia pelo dashboard
**Como** gestão, **quero** ver num só lugar o que precisa de atenção **para** agir antes do problema.

* Mostra quantos veículos estão em rota, o custo e o km do mês, o custo por km e o consumo médio.
* Lista as manutenções atrasadas ou vencendo e as rotas abertas há muito tempo, cada uma com atalho para a tela de detalhe.
* Mostra a evolução do custo nos últimos 6 meses.

*Cobre:* RF30, RN14.

### US14 — Controlar as rotas
**Como** gestão, **quero** abrir, acompanhar, corrigir e encerrar rotas **para** saber quem está com qual veículo e quanto rodou.

* Abrir exige motorista e veículo da empresa e um odômetro inicial maior ou igual ao atual do veículo.
* Os endereços têm autocomplete, e posso calcular distância, duração e trajeto no mapa.
* Encerrar exige odômetro final maior ou igual ao inicial e data de fim não anterior à de início.
* Ao encerrar, o km percorrido é calculado e a ficha do veículo é atualizada.

*Cobre:* RF15, RF16, RF17, RF18, RN06, RN07, RN10, RN12, RN19.

### US15 — Lançar despesas avulsas
**Como** gestão, **quero** lançar pedágio, multa, IPVA e seguro **para** que entrem no custo do veículo.

* Veículo e tipo são obrigatórios; o motorista é opcional (multa tem dono, IPVA não).
* O rodapé mostra quantidade e total do filtro inteiro.

*Cobre:* RF28, RF33.

---

## Motorista (Roberto)

### US16 — Abrir e encerrar a minha rota
**Como** motorista, **quero** abrir a rota ao sair e encerrá-la ao chegar **para** registrar a viagem sem burocracia.

* Vejo só as minhas rotas; a rota de outro motorista não existe para mim.
* Ao abrir, informo veículo, odômetro e endereços; o motorista é sempre eu.
* A tela destaca a rota em andamento, com os botões de abastecer e encerrar.

*Cobre:* RF15, RF17, RF19, RN12, RN16.

### US17 — Lançar o abastecimento na estrada
**Como** motorista, **quero** registrar o abastecimento com a nota fiscal **para** prestar contas do combustível.

* Informo combustível, posto, litros, preço por litro, odômetro e nota fiscal; o total é calculado sozinho.
* Com rota aberta, só consigo lançar no veículo dessa rota, e o lançamento fica vinculado a ela.
* Vejo a estimativa de km/l enquanto preencho.
* Vejo só os abastecimentos lançados para mim, inclusive os que a gestão lançou no meu nome.

*Cobre:* RF25, RF27, RN11, RN13, RN16.

### US18 — Conferir o veículo antes de sair
**Como** motorista, **quero** consultar os veículos e as manutenções **para** saber se o caminhão está em dia.

* Vejo odômetro, placa e situação dos veículos, sem poder alterá-los.
* Vejo as manutenções e se estão atrasadas, sem ver o custo.

*Cobre:* RF24, RN01.
