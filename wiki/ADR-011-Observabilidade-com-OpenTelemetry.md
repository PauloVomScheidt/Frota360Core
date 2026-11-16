# ADR-011 — Observabilidade com OpenTelemetry e Grafana Cloud

**Situação:** Aceita · **Data:** 13/11/2026

## Contexto

A API tinha logs estruturados (Serilog) e health checks, mas nenhum monitoramento: o log ficava no stdout do container, ninguém era avisado de queda e não havia métricas nem painel. Monitoramento é obrigatório no núcleo comum do TCC.

A produção é uma EC2 única que já divide a memória entre API, Postgres e Caddy ([ADR-007](ADR-007-EC2-com-Docker-Compose)).

## Decisão

* **Instrumentar a API com OpenTelemetry**, o padrão aberto: métricas e traces de ASP.NET Core, HttpClient, Npgsql e runtime .NET, mais os logs do Serilog pelo sink OTLP. O código não conhece o destino.
* **Configuração só pelas variáveis padrão do OpenTelemetry** (`OTEL_EXPORTER_OTLP_*`, `OTEL_SERVICE_NAME`), lidas pelo SDK e pelo sink do Serilog. **Sem endpoint, nada é registrado.** Testes e quem roda o projeto sem Grafana não são afetados.
* **Grafana Cloud (plano gratuito) como destino em produção**: Prometheus, Tempo e Loki gerenciados, mais o Synthetic Monitoring para a disponibilidade, visto de fora da AWS.
* **`grafana/otel-lgtm` em desenvolvimento**, como serviço opcional do `docker-compose.yml`: a mesma stack num container, e o mesmo código apontando para `localhost:4318`.
* **O `/health` fica fora** dos traces, das métricas HTTP e do log de requisições quando saudável. As consultas periódicas distorceriam os números.

## Consequências

* Painel e alertas não consomem memória da EC2; a instrumentação custa algumas dezenas de MB na API.
* Trocar de ferramenta (Datadog, New Relic, Prometheus próprio) é trocar o endpoint.
* **Os logs passam a sair da AWS.** O Grafana Labs vira operador de dados ([Segurança e LGPD](Seguranca-e-LGPD)). Por isso o `MascaraDeEmail` troca todo e-mail das propriedades de log por `p***@dominio.com`, no console e no Grafana. Ele age pelo valor, não pelo nome da propriedade, e cobre também os logs futuros. O log do e-mail de desenvolvimento, que carrega o link com o token de convite, nunca é exportado.
* O plano gratuito tem limites de retenção e volume, suficientes para um cliente.
* O teste `ObservabilidadeTests` sobe um receptor OTLP falso e prova que traces e logs chegam com autenticação, nome do serviço e ambiente.

## Alternativas consideradas

* **Prometheus + Grafana na própria EC2**: mais controle e sem dado saindo da AWS, mas disputam RAM com a API e o banco, e caem junto com a instância — justamente quando o alerta é necessário.
* **New Relic ou Datadog (plano gratuito)**: equivalentes, mas com agente ou SDK próprios; o OpenTelemetry mantém a saída aberta para eles.
* **Só um monitor de disponibilidade (UptimeRobot)**: avisa da queda, mas não explica lentidão nem erro.
