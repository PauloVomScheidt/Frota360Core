# Deploy — EC2 única com Docker Compose

Panorama operacional: como subir o Frota360 em produção, o que precisa estar configurado e
como ensaiar tudo localmente antes.

A arquitetura e o **porquê** de cada decisão estão em [contexto-api.md](contexto-api.md)
(§ Deploy). Aqui é o passo a passo.

## O que roda onde

| Peça | Onde | Observação |
|---|---|---|
| API + Postgres + Caddy | EC2 t3.medium, sa-east-1, Docker Compose | única superfície pública: 80 e 443 |
| Front (Vite, estático) | S3 + CloudFront | precisa de fallback de SPA — ver abaixo |

Sem ALB, sem RDS e sem NAT Gateway: o Caddy faz o papel do balanceador com TLS, e o Postgres
roda em container. É a configuração que cabe no orçamento de créditos.

## Ensaio local (faça antes de tocar na EC2)

Sobe a **mesma** stack de produção, trocando apenas o Caddyfile por um que serve HTTP em
`localhost` — sem necessidade de domínio. Todo o resto é idêntico: rede, ausência de portas
publicadas, migrations no boot, `ASPNETCORE_ENVIRONMENT=Production`.

```powershell
# 1. Um .env.local com valores descartáveis (o .env.example lista todas as chaves)
#    Precisa conter ARQUIVO_ENV=.env.local

# 2. A porta 80 e a 5432 precisam estar livres
docker stop pg-frota360

docker compose -f docker-compose.prod.yml -f docker-compose.local.yml --env-file .env.local up -d --build
docker compose -f docker-compose.prod.yml -f docker-compose.local.yml --env-file .env.local logs -f api
```

O que conferir — cada item prova uma correção específica:

| Verificação | Comando | Esperado |
|---|---|---|
| Fuso do processo | log de inicialização | `America/Sao_Paulo (UTC-03)` |
| Migrations no boot | log de inicialização | `Aplicando N migration(s)` |
| Rede confiável do proxy | log de inicialização | `Confiando nos headers de proxy vindos de 172.28.0.0/16` |
| Não roda como root | `docker exec frota360-api whoami` | `app` |
| Imagem sem segredo | `docker exec frota360-api ls /app \| grep appsettings` | só `appsettings.json` |
| Docs fechadas | `curl -o /dev/null -w "%{http_code}" http://localhost/scalar/v1` | `404` |
| Health | `curl http://localhost/health` | `200` |
| `/health/detail` não vaza | `curl http://localhost/health/detail` | sem mensagem de exceção |
| **IP real na auditoria** | criar um veículo e consultar `LogAuditoria` | IP do cliente, **não** o do Caddy |
| **Forja rejeitada** | repetir com `-H "X-Forwarded-For: 1.2.3.4"` | auditoria ignora o valor forjado |
| `Location` público | header da resposta 201 | `http://localhost/...`, não `http://api:8080/...` |
| Retry das migrations | `docker stop frota360-db; docker restart frota360-api` | tenta de novo e recupera ao voltar o banco |
| CORS | preflight `OPTIONS` da origem do front | `Access-Control-Allow-Origin` presente |
| Rotação de log | `docker inspect -f '{{.HostConfig.LogConfig.Config}}' frota360-api` | `max-size:10m max-file:5` |
| Persistência | `down` e `up` de novo | contagens iguais |

Para desfazer o ensaio: `... down -v` (o `-v` apaga o volume descartável) e
`docker start pg-frota360` para recuperar o banco de desenvolvimento.

## Subida na EC2

```bash
# 1. Configuração — o .env NUNCA vai para o git
cp .env.example .env
nano .env          # preencher tudo; ver a tabela abaixo
chmod 600 .env

# 2. Subir
docker compose -f docker-compose.prod.yml up -d --build
docker compose -f docker-compose.prod.yml logs -f api

# 3. Provisionar a primeira empresa (não há usuário semeado)
curl -X POST https://api.frota360app.com.br/api/v1/backoffice/empresa \
  -H "Content-Type: application/json" \
  -H "X-Backoffice-Key: <Backoffice__ApiKey do .env>" \
  -d '{"nomeEmpresa":"...","cnpj":"...","emailAdmin":"..."}'
# e abrir o linkConvite devolvido
```

A aplicação **derruba o boot com mensagem explícita** se faltar configuração obrigatória — ver
`ValidarConfiguracaoDeProducao` em `InfrastructureExtensions.cs`. Se o container reiniciar em
loop, o primeiro lugar a olhar é `docker compose logs api`.

### Variáveis

Todas em `.env.example`, com comentários. As que mais causam problema:

| Variável | Se faltar |
|---|---|
| `Jwt__Key` | boot falha (mínimo 32 caracteres) |
| `ConnectionStrings__DefaultConnection` | boot falha; o host é `db`, o nome do serviço |
| `Cors__AllowedOrigins__0` | boot falha; sem ela o navegador bloquearia todas as chamadas |
| `Frontend__BaseUrl` | boot falha; é a base dos links de convite |
| `Backoffice__ApiKey` | boot falha; sem ela é impossível criar a primeira empresa |
| `Resend__ApiKey` | **não** derruba: cai no log e avisa. Convite nenhum é enviado de verdade |
| `OTEL_EXPORTER_OTLP_ENDPOINT` / `OTEL_EXPORTER_OTLP_HEADERS` | **não** derruba: avisa no boot. Métricas, traces e logs não saem da instância — sem painel e sem alerta |
| `ARQUIVO_ENV` | só no ensaio local, para apontar ao `.env.local` |

## Monitoramento — Grafana Cloud

A API exporta métricas, traces e logs por OTLP ([ADR-011](../wiki/ADR-011-Observabilidade-com-OpenTelemetry.md)).
O destino em produção é a stack gratuita do Grafana Cloud:

1. Na stack, **Connections → Add new connection → OpenTelemetry (OTLP)** → *Create token*. A seção
   *Run the application* mostra o endpoint (a stack do projeto está em São Paulo:
   `https://otlp-gateway-prod-sa-east-1.grafana.net/otlp`) e o header pronto. Ignore os passos de
   instalar o pacote `Grafana.OpenTelemetry` e de *Add instrumentation*: a API já usa o SDK padrão.
2. No `.env` da instância:
   ```
   OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp-gateway-<região>.grafana.net/otlp
   OTEL_EXPORTER_OTLP_HEADERS=Authorization=Basic%20<base64 de instanceId:token>
   ```
   ⚠️ O base64 é de **`<instance ID>:<token>`**, não o token `glc_...` cru. Com o token cru, o
   Grafana responde 401 *"no credentials provided"* e nada aparece, sem erro no console da API.
   Copie o header pronto do bloco *Run the application* (pelo botão: o duplo clique deixa o `==` do
   final para trás) ou monte com `base64("<instance ID>:glc_...")`.
   A tela mostra `Authorization=Basic <base64>`, com espaço: troque-o por **`%20`**: a variável segue o formato W3C Baggage, e tanto
   o SDK quanto o sink do Serilog decodificam. O `ObservabilidadeTests` prova isso.
3. `docker compose -f docker-compose.prod.yml up -d` e conferir no log de boot a linha
   `Telemetria exportada por OTLP para ...`. Em até 1 minuto as métricas aparecem em
   **Explore → Prometheus** com `service_name="frota360-api"`.
4. **Painel e alertas** (uma vez por stack; depois, só quando `infra/grafana/` mudar): criar uma
   service account com papel **Admin** (a API de provisionamento de alertas não aceita Editor) (*Administration → Users and access → Service accounts*),
   gravar o token em `GRAFANA_TOKEN` e rodar
   `./scripts/grafana-provisionar.ps1 -Url https://<stack>.grafana.net -EmailAlertas <e-mail>`.
5. **Synthetic Monitoring** → check HTTP em `https://api.frota360app.com.br/health`, a cada
   1 minuto, da sonda mais próxima do Brasil. O alerta dispara com 2 falhas seguidas.
6. Testar o alerta de verdade: `docker compose -f docker-compose.prod.yml stop api`, esperar o
   e-mail, subir de novo.

## Front no S3 + CloudFront

```powershell
cd apps/web
# VITE_API_URL precisa estar preenchido em .env.production ANTES do build:
# o Vite embute o valor no bundle, não o lê em tempo de execução.
npm run build      # gera dist/
```

**O fallback de SPA não é opcional.** O front usa `BrowserRouter`, então uma recarga em
`/veiculos` faz o navegador pedir esse caminho ao S3, que não tem o arquivo. No CloudFront,
mapeie **403 e 404 → `/index.html` com status 200** (403 porque um bucket privado com OAC
devolve 403, não 404). Sem isso, todo link direto e todo F5 quebram.

## DNS — `frota360app.com.br`

Registrado no Registro.br, zona no Cloudflare.

| Nome | Tipo | Destino | Proxy |
|---|---|---|---|
| `api` | A | IP elástico da EC2 | **DNS-only (cinza)** |
| `app` | CNAME | distribuição do CloudFront | indiferente |
| `send` | conforme o Resend | registros de verificação | DNS-only |
| raiz | redirect | → `https://app.frota360app.com.br` | — |

**`api.` precisa ficar em DNS-only.** Proxiado, o Cloudflare intercepta as portas 80 e 443 e o
desafio HTTP-01 do Let's Encrypt nunca chega à instância: o Caddy fica em loop e o TLS nunca
sobe. Uma API REST autenticada não é cacheável, então o proxy agregaria pouco ali.

O **security group precisa liberar 80 e 443** para `0.0.0.0/0`. A porta 80 não é opcional — é
por onde passa a validação do certificado.

**O raiz precisa ser um redirect 3xx de verdade**, não servir o front. Se ele passar a servir o
mesmo conteúdo, a origem das requisições vira o raiz e o CORS bloqueia tudo: só
`https://app.frota360app.com.br` está em `Cors__AllowedOrigins__0`.

## TODO — dependem do DNS publicado

Nada aqui bloqueia o ensaio local.

- Criar os registros da tabela acima.
- `.env` da instância: `EMAIL_ACME` e os segredos. As URLs já estão preenchidas em `.env.example`.
- Resend: concluir a verificação de `send.frota360app.com.br` — `Resend__From` só entrega
  depois disso.
- HSTS: o `Caddyfile` começa com `max-age=86400` de propósito. Suba para `31536000` **depois**
  de algumas semanas estável — um max-age longo com o domínio ainda instável tranca os
  navegadores em HTTPS pelo período inteiro.

## TODO — backup (pendência mais séria)

Não existe backup do Postgres. Ver [contexto-api.md](contexto-api.md) (§ Dívida técnica).
Precisa estar resolvido **antes do primeiro dado real do cliente**.
