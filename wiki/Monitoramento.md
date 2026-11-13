# Monitoramento

> **Situação em 13/11/2026:** os sinais básicos existem (logs estruturados, health checks, trilha de auditoria); a ferramenta externa de monitoramento, os painéis e os alertas são a próxima etapa. Esta página descreve os dois.

## O que já existe

| Sinal | Como | Onde ver |
| :---- | :---- | :---- |
| **Logs estruturados** | Serilog: cada requisição, erro e evento de negócio com propriedades pesquisáveis (`{VeiculoId}`, `{Placa}`) | Produção: `docker compose logs api` (rotação de 5 × 10 MB). Desenvolvimento: console e arquivo diário, 7 dias |
| **Saúde da aplicação** | `GET /health` responde 200 com a API e o banco de pé | Público, sem autenticação |
| **Saúde detalhada** | `GET /health/detail` — situação de cada verificação, sem expor mensagem de exceção | Público |
| **Fuso do processo** | A segunda linha do log mostra o fuso efetivo; `UTC` ali indica deploy errado ([ADR-009](ADR-009-Hora-local-de-Brasilia)) | Log de inicialização |
| **Configuração** | A API não inicia sem a configuração obrigatória e lista o que falta | Log de inicialização |
| **Falha de integração** | Timeout ou erro da Google Routes API vai para o log com o motivo técnico; o usuário recebe mensagem genérica | Log |
| **Trilha de negócio** | Toda escrita fica registrada com autor, IP e diff ([ADR-006](ADR-006-Auditoria-explicita-no-handler)) | Tela `/auditoria` (Admin) |
| **Abuso** | Rate limit responde 429 (5/min nas rotas de autenticação, 200/min no geral) | Log |

Isso ainda não é monitoramento: ninguém é avisado quando algo cai, e não há métricas nem painel.

## Estratégia planejada

**Grafana Cloud** (plano gratuito) com **OpenTelemetry**, para não disputar memória com a API na EC2.

| Peça | Como | Responde |
| :---- | :---- | :---- |
| Métricas e traces | `OpenTelemetry.Extensions.Hosting` com instrumentação de ASP.NET Core, HttpClient e Npgsql, exportando por OTLP | Quantas requisições, quanto demoram, onde o tempo é gasto |
| Logs | Os logs do Serilog enviados ao Loki | Busca por propriedade em vez de `grep` no servidor |
| Disponibilidade | Synthetic Monitoring consultando `/health` a cada minuto, de fora da AWS | O sistema está no ar? |
| Alertas | E-mail quando `/health` falhar ou a taxa de respostas 5xx subir | Quem é avisado, e quando |
| Painel | Requisições/s, latência p95, taxa de erro e saúde do banco | Visão do dia a dia |

### Indicadores e alertas

| Indicador | Meta | Alerta quando |
| :---- | :---- | :---- |
| Disponibilidade de `/health` | 99% em horário comercial (RNF09) | 2 falhas seguidas |
| Latência p95 | < 500 ms (RNF01) | > 1 s por 5 minutos |
| Taxa de 5xx | < 1% | > 5% por 5 minutos |
| Respostas 429 | — | Pico fora do padrão (possível força bruta) |
| Erros da Google Routes API | — | Taxa de falha alta (cota ou chave) |

### Alternativas avaliadas

* **Prometheus + Grafana na própria EC2**: mais controle, mas disputam RAM com a API e o banco na mesma instância.
* **New Relic ou Datadog (plano gratuito)**: equivalentes, com limites de retenção menores no plano gratuito.
* **Erros do front**: Grafana Faro ou Sentry para capturar exceções de JavaScript — evolução desejável depois do básico.

## Como verificar a saúde à mão

```bash
curl -s -o /dev/null -w "%{http_code}\n" https://api.frota360app.com.br/health
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs --since 15m api | grep -E "ERR|FTL"
```
