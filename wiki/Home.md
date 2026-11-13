# Frota360

**Gestão de frotas para pequenas e médias empresas**: rotas, quilometragem, manutenção preventiva, abastecimento, despesas, custo por quilômetro e auditoria, num sistema web multiempresa.

[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=PauloVomScheidt_Frota360Core&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=PauloVomScheidt_Frota360Core)
[![Cobertura](https://sonarcloud.io/api/project_badges/measure?project=PauloVomScheidt_Frota360Core&metric=coverage)](https://sonarcloud.io/summary/new_code?id=PauloVomScheidt_Frota360Core)

![Dashboard do Frota360](imagens/dashboard.png)

## O problema

Transportadoras e indústrias com frota própria ainda controlam veículos em planilhas: o motorista anota o odômetro à mão, o gestor consolida depois, e manutenção e combustível ficam em controles separados. O resultado é revisão esquecida, histórico pouco confiável e nenhuma noção de quanto custa cada quilômetro rodado.

O Frota360 junta esses dados num lugar só. O odômetro informado ao abrir e encerrar rotas, ao abastecer e ao concluir manutenções mantém a ficha do veículo atualizada, sem nenhum hardware instalado. A partir dela o sistema avisa quais manutenções venceram e calcula R$/km e km/l por veículo.

## Parceiro

A demanda veio da **Opa Bier Joinville**, que relatou perda de controle sobre revisões, falta de histórico confiável e dificuldade de saber quem usou cada veículo. O gestor da fábrica acompanhou o desenvolvimento, e parte do escopo mudou a partir do retorno dele (ver [Requisitos](Requisitos#evolução-do-escopo)).

## Links

| | |
| :---- | :---- |
| Aplicação | [app.frota360app.com.br](https://app.frota360app.com.br) *(em implantação)* |
| API | [api.frota360app.com.br](https://api.frota360app.com.br/health) *(em implantação)* |
| Código | [github.com/PauloVomScheidt/Frota360Core](https://github.com/PauloVomScheidt/Frota360Core) |
| Análise estática | [SonarCloud](https://sonarcloud.io/summary/new_code?id=PauloVomScheidt_Frota360Core) |
| RFC completo | [docs/RFC_Gerenciador_Frotas_V1.md](https://github.com/PauloVomScheidt/Frota360Core/blob/main/docs/RFC_Gerenciador_Frotas_V1.md) |

## Guia desta Wiki

| Para saber… | Leia |
| :---- | :---- |
| O que o sistema faz e as regras que ele garante | [Requisitos](Requisitos) |
| O que cada papel consegue fazer | [User Stories](User-Stories) · [Telas](Telas) |
| Como as peças se encaixam | [Arquitetura](Arquitetura) · [Modelo de Dados](Modelo-de-Dados) |
| Por que foi feito assim | [Decisões de Arquitetura (ADRs)](Decisoes-de-Arquitetura) |
| Como rodar, testar e publicar | [Como Rodar Localmente](Como-Rodar-Localmente) · [Testes e Qualidade](Testes-e-Qualidade) · [Deploy](Deploy) |
| Como o sistema é observado em produção | [Monitoramento](Monitoramento) |
| Como os dados são protegidos | [Segurança e LGPD](Seguranca-e-LGPD) |
| Os endpoints da API | [Referência da API](Referencia-da-API) |
| Como contribuir | [Convenções e Fluxo de Trabalho](Convencoes-e-Fluxo-de-Trabalho) |

## Em números

| | |
| :---- | :---- |
| Telas | 22 (5 públicas, 17 internas) |
| Papéis de acesso | 4 — Admin, Supervisor, Operador, Motorista |
| Entidades | 13 |
| Requisitos | 34 funcionais, 20 não funcionais, 20 regras de negócio |
| Testes | 592 no backend (82% de linhas) · 769 no frontend (93% de linhas) |

## Stack

**Backend:** .NET 10, ASP.NET Core, EF Core, PostgreSQL 17, FluentValidation, Serilog.
**Frontend:** React 19, TypeScript, Vite, TanStack Query, Tailwind CSS, Google Maps.
**Infraestrutura:** Docker Compose, Caddy, AWS (EC2, S3, CloudFront), Cloudflare.
**Qualidade:** xUnit, Testcontainers, Vitest, MSW, SonarCloud, GitHub Actions.

---

Autor: Paulo Henrique Scialla Vom Scheidt · Trabalho de Conclusão de Curso, linha Web Apps.
