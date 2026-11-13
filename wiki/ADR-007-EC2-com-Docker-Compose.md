# ADR-007 — Produção numa EC2 única com Docker Compose e Caddy

**Situação:** Aceita · **Data:** 2026

## Contexto

A proposta inicial previa AWS App Runner + RDS + ElastiCache. Para o volume de um primeiro cliente e o orçamento de créditos disponível, três serviços gerenciados com cobrança mínima mensal custariam mais do que a aplicação inteira precisa. As plataformas de hospedagem simplificada (Vercel, Netlify, Render) estão vedadas pelas regras do TCC.

## Decisão

* **API, PostgreSQL e Caddy numa EC2 t3.medium** (região sa-east-1), orquestrados por Docker Compose, com IP elástico.
* **Caddy como proxy reverso**, terminando o TLS com certificado automático do Let's Encrypt, no lugar de um load balancer.
* **Só as portas 80 e 443 ficam expostas.** Banco e API não publicam porta; a rede interna tem sub-rede fixa.
* **Front estático em S3 + CloudFront**, com fallback de SPA (403/404 → `index.html`).
* DNS no Cloudflare, com o subdomínio da API em modo *DNS-only*, para o desafio HTTP do Let's Encrypt chegar à instância.

## Consequências

* Custo previsível e baixo, e a mesma stack roda localmente: `docker-compose.local.yml` ensaia a produção trocando só o Caddyfile.
* **O proxy exige tratar `X-Forwarded-*`.** Sem isso, a auditoria gravaria o IP do Caddy, e o rate limit por IP viraria um limite compartilhado por todos. A API só aceita esses cabeçalhos vindos da sub-rede do Compose — mudar a sub-rede exige mudar a variável junto.
* **Ponto único de falha.** Uma instância só, sem réplica. Aceitável para o volume atual, com monitoramento de disponibilidade ([Monitoramento](Monitoramento)).
* **Backup passa a ser responsabilidade nossa**: sem RDS, não há snapshot automático. `pg_dump` diário para S3 é pendência antes do primeiro dado real.
* Logs só no console do container, com rotação do Docker (5 × 10 MB): log sem teto enche o disco.
* Migrations rodam no boot, com novas tentativas se o banco ainda não respondeu.
* O volume do Caddy guarda os certificados; sem ele, cada redeploy pediria certificado novo e esbarraria no limite do Let's Encrypt.

## Alternativas consideradas

* **App Runner + RDS + ElastiCache** (plano original): descartado pelo custo, e o ElastiCache deixou de fazer sentido com a saída do Redis ([ADR-001](ADR-001-PostgreSQL)).
* **ECS/Fargate com ALB**: mais elástico, mas com custo fixo de load balancer e mais peças para operar.
* **Prometheus/Grafana na mesma instância**: ver [Monitoramento](Monitoramento) — a opção gerenciada evita disputar memória com a API.
