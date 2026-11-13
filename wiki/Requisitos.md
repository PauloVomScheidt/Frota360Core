# Requisitos

Requisitos funcionais, não funcionais e regras de negócio do Frota360. A fonte é o [RFC v2.0](https://github.com/PauloVomScheidt/Frota360Core/blob/main/docs/RFC_Gerenciador_Frotas_V1.md) (seção 2); aqui eles aparecem com a situação atual e com o ponto do código que garante cada regra.

Legenda: ✅ implementado · 🟡 parcial · ⏳ depende de produção.

## Papéis

Todo requisito abaixo respeita os papéis da **RN01**:

| Papel | Alcance |
| :---- | :---- |
| **Admin** | Tudo. É o único que administra usuários, convites e auditoria. |
| **Supervisor** | Cadastra, edita e exclui veículos, rotas, manutenções, lançamentos e catálogos. |
| **Operador** | Visualiza a frota, abre, edita e encerra rotas, lança abastecimentos e despesas. |
| **Motorista** | Abre e encerra as próprias rotas, lança os próprios abastecimentos, consulta veículos e manutenções (sem custo). |

"Gestão", no resto da Wiki, quer dizer Admin, Supervisor e Operador.

## Requisitos funcionais

### Acesso e sessão

| ID | Requisito | Situação |
| :---- | :---- | :---- |
| RF01 | Login com e-mail e senha, abrindo uma sessão autenticada | ✅ |
| RF02 | Renovação automática da sessão e logout com revogação do refresh token | ✅ |
| RF03 | Redefinição de senha por link enviado ao e-mail, sem revelar se o e-mail existe | ✅ |
| RF04 | Senha com no mínimo 6 caracteres, uma maiúscula e um número | ✅ |
| RF05 | Rejeição de acesso não autenticado (401) e de ação fora do papel (403) | ✅ |

### Empresa e pessoas

| ID | Requisito | Situação |
| :---- | :---- | :---- |
| RF06 | Provisionamento de empresa pelo backoffice (chave própria), com catálogos semeados e convite do primeiro Admin | ✅ |
| RF07 | Admin convida pessoas por e-mail com papel definido e cancela convites pendentes | ✅ |
| RF08 | Convidado consulta o convite e o aceita com nome, senha e, opcionalmente, CPF e nascimento; o aceite abre a sessão | ✅ |
| RF09 | Admin lista usuários, altera o papel e ativa ou desativa cada um | ✅ |
| RF10 | Todo usuário consulta e corrige o próprio nome, CPF e nascimento | ✅ |

### Veículos e motoristas

| ID | Requisito | Situação |
| :---- | :---- | :---- |
| RF11 | Cadastro de veículo com nome, marca, placa e quilometragem | ✅ |
| RF12 | Listagem, edição e exclusão de veículos | ✅ |
| RF13 | Indicação de veículo em rota ou disponível | ✅ |
| RF14 | Listagem dos motoristas (usuários com papel Motorista) | ✅ |

### Rotas

| ID | Requisito | Situação |
| :---- | :---- | :---- |
| RF15 | Abertura de rota com motorista, veículo, odômetro inicial e endereços | ✅ |
| RF16 | Cálculo de distância, duração e traçado, exibido em mapa | ✅ |
| RF17 | Encerramento com odômetro final e data de fim opcional | ✅ |
| RF18 | Gestão lista, filtra, corrige e remove rotas | ✅ |
| RF19 | Motorista abre, consulta e encerra apenas as próprias rotas | ✅ |

### Manutenção

| ID | Requisito | Situação |
| :---- | :---- | :---- |
| RF20 | Catálogo de tipos de manutenção por empresa, com intervalo de km opcional | ✅ |
| RF21 | Agendamento por quilometragem prevista e data prevista opcional | ✅ |
| RF22 | Conclusão com quilometragem, data e custo | ✅ |
| RF23 | Sinalização de manutenções atrasadas e das que vencem em até 500 km | ✅ |
| RF24 | Motorista consulta as manutenções sem ver o custo | ✅ |

### Abastecimento e despesas

| ID | Requisito | Situação |
| :---- | :---- | :---- |
| RF25 | Lançamento de abastecimento (combustível, posto, litros, R$/l, odômetro, nota fiscal, frentista), com valor total calculado | ✅ |
| RF26 | Catálogos de tipos de combustível e de postos credenciados | ✅ |
| RF27 | Estimativa de consumo (km/l) a partir dos abastecimentos do veículo | ✅ |
| RF28 | Lançamento de despesas avulsas por veículo, com motorista opcional, e catálogo de tipos de despesa | ✅ |

### Indicadores e auditoria

| ID | Requisito | Situação |
| :---- | :---- | :---- |
| RF29 | Custo consolidado (abastecimento, manutenção, despesa) com filtros e R$/km e km/l por veículo e por mês | ✅ |
| RF30 | Dashboard com frota em rota, custo e km do mês, manutenções que precisam de atenção, rotas em andamento e evolução de 6 meses | ✅ |
| RF31 | Trilha de auditoria de toda escrita, com autor, data, IP e alterações | ✅ |
| RF32 | Admin consulta a trilha por entidade, ação, usuário e período | ✅ |

### Transversais

| ID | Requisito | Situação |
| :---- | :---- | :---- |
| RF33 | Listas que crescem com o tempo paginadas no servidor, com totais do filtro inteiro | ✅ |
| RF34 | Página pública de apresentação do produto | ✅ |

## Requisitos não funcionais

| ID | Categoria | Requisito | Como é verificado | Situação |
| :---- | :---- | :---- | :---- | :---- |
| RNF01 | Desempenho | Resposta abaixo de 500 ms em uso normal | Latência p95 no [Monitoramento](Monitoramento) | ⏳ |
| RNF02 | Desempenho | 50 usuários simultâneos | Teste de carga após o deploy | ⏳ |
| RNF03 | Segurança | JWT de 1 h + refresh de 7 dias rotacionado, só em cookies `HttpOnly; Secure; SameSite=None` | `SessaoCookiesTests`, `AuthControllerTests` | ✅ |
| RNF04 | Segurança | Senhas em BCrypt; tokens de convite, redefinição e refresh só como hash | Revisão de código; `AuthServiceTests` | ✅ |
| RNF05 | Segurança | Apenas login, sessão, senha, convite e health checks são anônimos | Atributos `[Authorize]`/`[AllowAnonymous]` nos controllers | ✅ |
| RNF06 | Segurança | Chave do JWT com 32+ caracteres, fora do repositório; sem ela a API não inicia | `InjecaoDeDependenciaTests` | ✅ |
| RNF07 | Segurança | Rate limit de 5/min nas rotas de autenticação e 200/min no geral, por IP | Configuração em `Program.cs` | ✅ |
| RNF08 | Segurança | Isolamento total entre empresas | Testes de escopo em todo handler; [ADR-002](ADR-002-Multi-tenant-por-claim) | ✅ |
| RNF09 | Disponibilidade | 99% em horário comercial, com `/health` e `/health/detail` | Monitor de disponibilidade | ⏳ |
| RNF10 | Implantação | Containerizado; stack de produção ensaiável localmente | Job `docker` do CI; [Deploy](Deploy#ensaio-local) | ✅ |
| RNF11 | Usabilidade | REST com transições de estado em endpoints próprios | [Referência da API](Referencia-da-API) | ✅ |
| RNF12 | Usabilidade | Envelope único de resposta; erros por campo e mensagens para o usuário final | `ExceptionMiddlewareTests` | ✅ |
| RNF13 | Usabilidade | Documentação interativa (Scalar) em desenvolvimento | `/scalar/v1` | ✅ |
| RNF14 | Usabilidade | Interface em português, funcional no computador e no celular | Verificação visual | 🟡 sem layout dedicado ao celular |
| RNF15 | Manutenibilidade | Clean Architecture, CQRS e SOLID | [Arquitetura](Arquitetura) | ✅ |
| RNF16 | Manutenibilidade | Nenhum segredo versionado | Job `docker` (iscas no `.dockerignore`) | ✅ |
| RNF17 | Qualidade | Cobertura mínima de 75% no backend e 50% no frontend | CI reprova abaixo | ✅ 82% / 93% |
| RNF18 | Qualidade | Análise estática com quality gate | [SonarCloud](https://sonarcloud.io/summary/new_code?id=PauloVomScheidt_Frota360Core) | ✅ |
| RNF19 | Observabilidade | Logs estruturados e monitoramento externo com alerta | [Monitoramento](Monitoramento) | 🟡 logs sim, monitor externo pendente |
| RNF20 | Privacidade | Auditoria com retenção de 12 meses; logs com volume limitado | [Segurança e LGPD](Seguranca-e-LGPD) | 🟡 expurgo não automatizado |

## Regras de negócio

RN01 a RN11 mantêm a numeração da primeira versão do RFC, porque o código e os testes as citam pelo número.

| ID | Regra | Onde é garantida |
| :---- | :---- | :---- |
| RN01 | Todo acesso exige sessão, e as ações dependem do papel (tabela acima) | `[Authorize(Roles = ...)]` por ação; `pode.*` no front só esconde botões |
| RN02 | Placa única por empresa (pode repetir entre empresas) | `Create`/`UpdateVeiculoHandler` (422) + índice único `(EmpresaId, Placa)` |
| RN03 | CPF único por empresa; opcional, e vários vazios não colidem | Índice único filtrado `(EmpresaId, CPF)` + `ExisteCpfNaEmpresaAsync` |
| RN04 | E-mail único em todo o sistema, sem distinção de maiúsculas | Índice único em `Usuario.Email` + `EmailNormalizado` |
| RN05 | Quilometragem do veículo não pode ser negativa | `CreateVeiculoValidator`/`UpdateVeiculoValidator` |
| RN06 | Data de fim da rota não pode ser anterior à de início | `EncerrarRotaHandler` |
| RN07 | Rota referencia motorista e veículo existentes **na mesma empresa** | `Create`/`UpdateRotaHandler` resolvem os ids pela empresa do token |
| RN08 | Veículo com rota, abastecimento ou despesa não é excluído; usuário só é desativado; catálogo em uso é inativado | `DeleteVeiculoHandler` + chaves estrangeiras `Restrict` |
| RN09 | Placa no formato Mercosul (ABC1D23) ou antigo (ABC1234), gravada em maiúsculas | Validators de veículo + normalização nos handlers |
| RN10 | Encerrar a rota calcula o km percorrido e atualiza odômetro, último motorista e data da última viagem | `EncerrarRotaHandler` |
| RN11 | Motorista com rota aberta só abastece o veículo dessa rota, e o lançamento é vinculado a ela | `CreateAbastecimentoHandler.ResolverRotaAsync` |
| RN12 | O odômetro do veículo só avança; o km inicial da rota não pode ser menor que ele | Handlers de rota, manutenção e abastecimento |
| RN13 | O valor do abastecimento é calculado no servidor (litros × R$/l) | `AbastecimentoMappings.CalcularValor` |
| RN14 | Manutenção vence no que vier primeiro (km ou data); "atrasada" e "vencendo" (≤ 500 km) são calculadas na leitura; sem duplicata pendente | `ManutencaoMappings`, `RegrasDeManutencao.FaixaAvisoKm` |
| RN15 | A empresa nunca fica sem Admin ativo; mudar papel ou desativar encerra a sessão | `UsuarioService` |
| RN16 | Motorista só vê as próprias rotas e os abastecimentos lançados para ele; o resto responde 404 | `GetMinhasRotasHandler`, `GetAllAbastecimentosHandler` (recorte pelo token) |
| RN17 | Item de catálogo inativo não recebe lançamento novo | Handlers de criação de manutenção, abastecimento e despesa |
| RN18 | Auditoria é somente inclusão, sem edição ou exclusão | `ILogAuditoriaRepository` não expõe update/delete |
| RN19 | Distância e duração do trajeto recusadas fora de 5–180 km/h de média | `RegrasDeTrajeto` |
| RN20 | Convite vale 7 dias e uma única vez; um novo invalida os pendentes do mesmo e-mail | `ConviteService` |

## Fora do escopo

* Rastreamento por GPS ou telemetria (IoT).
* Notificações por e-mail ou push: os alertas de manutenção são visuais.
* Exportação de relatórios e BI.
* Integração com ERP, pedágio ou plataformas de logística.
* Aplicativo móvel nativo e layout dedicado ao celular.
* Contratação e cobrança online: a empresa é provisionada pela equipe.
* Expurgo automático da auditoria (a política de 12 meses está declarada).
* Operação fora do fuso de Brasília.

## Evolução do escopo

O escopo cresceu durante o desenvolvimento. As principais mudanças em relação à proposta de junho/2026:

| Na proposta | Hoje | Motivo |
| :---- | :---- | :---- |
| Cadastro aberto | Entrada por convite; empresa provisionada pelo backoffice | Com várias empresas, cadastro aberto não diria a qual a conta pertence |
| Sem noção de empresa | Multiempresa, dados isolados pelo token | Atender várias empresas com uma instância |
| Motorista como cadastro à parte | Motorista é um usuário com papel | Ele precisa entrar no sistema para operar a própria rota |
| Perfis de acesso fora do escopo | Quatro papéis | Sem eles o motorista alteraria a frota inteira |
| Auditoria fora do escopo | Trilha de toda escrita | "Quem mudou isto?" virou pergunta frequente |
| Alertas de manutenção no futuro | Atrasada/vencendo no dashboard | Era a principal dor do parceiro |
| Gestão financeira fora do escopo | Abastecimento, despesas e custo por km | Pedido do parceiro: medir km/l e R$/l |
| SQL Server e Redis | PostgreSQL, sem cache | Ver [ADR-001](ADR-001-PostgreSQL) |

A tabela completa, com todas as mudanças, está na seção 2.7 do RFC.
