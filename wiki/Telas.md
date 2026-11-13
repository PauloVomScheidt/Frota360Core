# Telas

O front tem duas faces: o **painel de gestão** (Admin, Supervisor, Operador), que abre no dashboard, e a **operação do motorista**, que abre em "Minhas rotas". Uma pessoa nunca vê as duas.

## Mapa de telas

| Rota | Tela | Quem acessa |
| :---- | :---- | :---- |
| `/` | Página de apresentação | Público |
| `/login` | Entrar | Público |
| `/esqueci-senha` | Pedir redefinição de senha | Público |
| `/redefinir-senha?token=…` | Nova senha (link do e-mail) | Público |
| `/convite?token=…` | Aceite de convite (link do e-mail) | Público |
| `/dashboard` | Visão geral da frota | Gestão |
| `/motoristas` | Motoristas | Gestão |
| `/veiculos` | Veículos | Todos — o motorista só consulta |
| `/rotas` | Rotas | Gestão |
| `/manutencoes` | Manutenções | Todos — o motorista só consulta, sem custo |
| `/abastecimentos` | Abastecimentos | Todos — o motorista vê só os dele |
| `/despesas` | Despesas | Gestão |
| `/custos` | Custos | Gestão |
| `/minhas-rotas` | Minhas rotas | Motorista |
| `/tipos-manutencao`, `/tipos-despesa`, `/tipos-combustivel`, `/postos` | Catálogos | Admin e Supervisor |
| `/usuarios`, `/convites`, `/auditoria` | Administração | Admin |
| `/perfil` | Meu cadastro | Todos |

Os botões de cada tela aparecem conforme o papel, mas quem decide é a API: uma ação fora do papel responde 403 mesmo que alguém a dispare por fora da interface.

```mermaid
flowchart LR
    subgraph Publico[Público]
        L[Landing] --> LG[Login]
        LG --> ES[Esqueci a senha] --> RS[Redefinir senha]
        CV[Aceite de convite]
    end
    subgraph Gestao[Admin · Supervisor · Operador]
        D[Dashboard] --> MO[Motoristas]
        D --> VE[Veículos]
        D --> RO[Rotas]
        D --> MA[Manutenções]
        D --> AB[Abastecimentos]
        D --> DE[Despesas]
        D --> CU[Custos]
        D --> CAT[Catálogos]
        D --> ADM[Usuários · Convites · Auditoria — só Admin]
    end
    subgraph Mot[Motorista]
        MR[Minhas rotas] --> AB2[Abastecimentos]
        MR --> VE2[Veículos — leitura]
        MR --> MA2[Manutenções — leitura]
    end
    LG --> D
    LG --> MR
    CV --> D
    CV --> MR
    PF[Perfil — todos os papéis]
```

## Capturas

As capturas usam a base de desenvolvimento, por isso alguns números (odômetro, consumo) não são realistas.

### Login
A entrada é por convite; não há cadastro aberto.

![Login](imagens/login.png)

### Dashboard
Frota em rota, custo e km do mês, custo por km, consumo, manutenções atrasadas e rotas em andamento — cada item leva à tela de detalhe.

![Dashboard](imagens/dashboard.png)

### Rotas
Partida e chegada, motorista, veículo, odômetro inicial e final, situação e o botão de encerrar.

![Rotas](imagens/rotas.png)

### Manutenções
Pendentes primeiro, com o atraso calculado a partir do odômetro atual do veículo.

![Manutenções](imagens/manutencoes.png)

### Abastecimentos
Apontamento fiscal: combustível, posto, litros, R$/l, odômetro e nota fiscal, com quem é o dono do gasto e quem lançou.

![Abastecimentos](imagens/abastecimentos.png)

### Custos
Combustível, manutenção e despesas consolidados, com R$/km e km/l por veículo.

![Custos](imagens/custos.png)

### Auditoria
Trilha somente leitura de tudo o que foi alterado na empresa.

![Auditoria](imagens/auditoria.png)

### Minhas rotas (motorista)
A rota em andamento com os atalhos de abastecer e encerrar, o resumo do mês e o histórico.

![Minhas rotas](imagens/minhas-rotas.png)
