# Deploy — EC2 única com Docker Compose

Panorama operacional: como o Frota360 chega à produção, o que precisa estar configurado e
como ensaiar tudo localmente antes.

A arquitetura e o **porquê** de cada decisão estão em [contexto-api.md](contexto-api.md)
(§ Deploy) e no [ADR-012](../wiki/ADR-012-Pipeline-de-CD.md). Aqui é o passo a passo.

## O que roda onde

| Peça | Onde | Observação |
|---|---|---|
| API + Postgres + Caddy | EC2 t3.medium, sa-east-1, Docker Compose | única superfície pública: 80 e 443 |
| Front (Vite, estático) | S3 + CloudFront | bucket privado, acesso só pelo CloudFront (OAC) |
| Imagem da API | GHCR (`ghcr.io/paulovomscheidt/frota360-api`) | marcada com o SHA do commit |

Sem ALB, sem RDS e sem NAT Gateway: o Caddy faz o papel do balanceador com TLS, e o Postgres
roda em container. É a configuração que cabe no orçamento de créditos.

### Recursos na AWS (conta `frota360`, criados em 16/11/2026)

| Recurso | Identificador |
|---|---|
| EC2 `frota360-prod` (Ubuntu 24.04) | `i-052a7f3a8d62ae6e2`, repositório clonado em `/opt/frota360` |
| IP elástico | `18.229.152.249` |
| Security group `frota360-web` | `sg-03055f205baaebbd6` — só 80 e 443, **sem 22** |
| Instance profile | `frota360-ec2` (`AmazonSSMManagedInstanceCore`) |
| Bucket do front | `frota360-web-655503101397` |
| CloudFront | `E4G8OKLS3JMPD` (`dasgc9msn858p.cloudfront.net`), plano flat-rate **Free** |
| Certificado do front | ACM em **us-east-1** (o CloudFront só aceita de lá) |
| Role do pipeline | `frota360-deploy`, assumida por OIDC; política inline `deploy-minimo` |

Não há porta SSH. O shell da instância é pelo **Session Manager** (EC2 → Connect → *SSM
Session Manager*); o *EC2 Instance Connect* falha de propósito.

## Pipeline de CD

Todo push na `main` cujos checks `api`, `web` e `docker` passam vai para produção: o job
`deploy` do `ci.yml` chama o `.github/workflows/deploy.yml` com o SHA do commit.

| Job | O que faz |
|---|---|
| `versao` | resolve o SHA e recusa commit fora do histórico da `main` |
| `imagem` | builda e publica `frota360-api:<sha>` no GHCR — pulado se a imagem já existe |
| `api` | assume a role por OIDC, manda o SSM Run Command (`git checkout <sha>` + `scripts/deploy-ec2.sh`) e confere `https://api.../health` de fora |
| `web` | `npm run build`, `s3 sync` (assets com cache de 1 ano, o resto `no-cache`), invalidação do CloudFront e confere `/` e `/veiculos` |

O `web` só roda depois do `api`: front novo com API velha é o caso que quebra.

O `scripts/deploy-ec2.sh`, na instância:

1. falha se não houver `.env`;
2. `docker compose pull api` com `API_TAG=<sha>`;
3. `up -d --wait`, que espera o healthcheck da API (e as migrations, que rodam no boot);
4. **se a nova versão não ficar saudável, volta a API para a versão anterior** e falha o job;
5. reinicia o Caddy só se o `Caddyfile` mudou (montado como arquivo, ele não enxerga o checkout);
6. grava o SHA em `/opt/frota360/.versao-implantada`.

### Configuração no GitHub (uma vez)

**Settings → Environments → New environment → `production`**, sem regras de proteção (o deploy
é automático). Em *Environment variables*:

| Variável | Valor |
|---|---|
| `AWS_ROLE_ARN` | `arn:aws:iam::655503101397:role/frota360-deploy` |
| `EC2_INSTANCE_ID` | `i-052a7f3a8d62ae6e2` |
| `S3_BUCKET` | `frota360-web-655503101397` |
| `CLOUDFRONT_ID` | `E4G8OKLS3JMPD` |
| `VITE_GOOGLE_MAPS_KEY` | a chave de browser do Maps (a mesma do `.env.local`) |

São variáveis, não secrets: nenhum desses valores dá acesso a nada sozinho. A role só é
assumível por um job deste repositório rodando no environment `production`, e a chave do Maps
vai no bundle de qualquer forma — a proteção dela é a restrição por referenciador no Google.

O nome `production` não é decorativo: a trust policy da role exige o `sub`
`repo:PauloVomScheidt@171389553/Frota360Core@1348478208:environment:production`. Renomear o
environment derruba o deploy com *Not authorized to perform sts:AssumeRoleWithWebIdentity*.

⚠️ **O `sub` carrega os IDs numéricos do dono e do repositório**, não só os nomes — é o formato
que o GitHub envia hoje. Uma trust policy com o formato antigo
(`repo:PauloVomScheidt/Frota360Core:...`) é recusada com o mesmo erro. Os IDs são imutáveis:
renomear o repositório não quebra o deploy, e outro repositório recriado com o mesmo nome não
consegue assumir a role. Para descobrir o `sub` que chegou, procure no CloudTrail (sa-east-1) o
evento `AssumeRoleWithWebIdentity` — o `userName` dele é o `sub`.

### Primeira subida

1. **`.env` na instância**, pelo Session Manager:
   ```bash
   sudo -i
   cd /opt/frota360
   cp .env.example .env && chmod 600 .env
   nano .env          # preencher tudo; ver a tabela de variáveis abaixo
   ```
2. **Disparar o pipeline**: merge na `main`, ou *Actions → deploy → Run workflow* com o SHA
   da `main`.
3. **O primeiro `api` falha no pull**, e é esperado: o pacote nasce **privado** no GHCR. Em
   *github.com/PauloVomScheidt → Packages → frota360-api → Package settings → Change
   visibility → Public*, e depois *Re-run failed jobs*. Público, a EC2 baixa a imagem sem
   token — o repositório já é público, a imagem não expõe nada além do código.
4. **Provisionar a primeira empresa** (não há usuário semeado):
   ```bash
   curl -X POST https://api.frota360app.com.br/api/v1/backoffice/empresa \
     -H "Content-Type: application/json" \
     -H "X-Backoffice-Key: <Backoffice__ApiKey do .env>" \
     -d '{"nomeEmpresa":"...","cnpj":"...","emailAdmin":"..."}'
   ```
   e abrir o `linkConvite` devolvido.

A aplicação **derruba o boot com mensagem explícita** se faltar configuração obrigatória — ver
`ValidarConfiguracaoDeProducao` em `InfrastructureExtensions.cs`. O `--wait` do script então
estoura e o job mostra as últimas linhas do log da API.

### Rollback

*Actions → deploy → Run workflow*, com o **SHA completo** do commit anterior (está no histórico
de runs, ou em `git log main`). A imagem dele já está no GHCR, então o job `imagem` é pulado e
o deploy leva o tempo de um `pull` + `up`. O front é rebuildado a partir do mesmo commit.

⚠️ **Migrations não voltam.** Elas rodam no boot e não têm `Down` automático. Voltar para antes
de uma migration aditiva (coluna ou tabela nova) funciona — o código antigo ignora o que não
conhece. Voltar para antes de uma migration **destrutiva** (coluna removida ou renomeada)
quebra. Para essas, o caminho é corrigir para a frente.

### Operar à mão na instância

O compose exige `API_TAG` e falha sem ele, para nunca subir uma versão desconhecida:

```bash
sudo -i && cd /opt/frota360
export API_TAG=$(cat .versao-implantada)
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f api
```

## Ensaio local (faça antes de mexer em compose, Dockerfile ou middleware)

Sobe a **mesma** stack de produção, trocando apenas o Caddyfile por um que serve HTTP em
`localhost` e buildando a imagem localmente em vez de baixá-la do GHCR. Todo o resto é
idêntico: rede, ausência de portas publicadas, migrations no boot,
`ASPNETCORE_ENVIRONMENT=Production`.

```powershell
# 1. Um .env.local com valores descartáveis (o .env.example lista todas as chaves)
#    Precisa conter ARQUIVO_ENV=.env.local e API_TAG=local

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

## Variáveis

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
| `ARQUIVO_ENV` / `API_TAG` | só no ensaio local (`.env.local` e `local`) — na EC2, o `API_TAG` vem do script de deploy |

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
3. Recriar a API para ler o `.env` novo (`export API_TAG=$(cat .versao-implantada)` e
   `docker compose -f docker-compose.prod.yml up -d --force-recreate api`, ou só rodar o deploy de
   novo) e conferir no log de boot a linha `Telemetria exportada por OTLP para ...`. Em até 1
   minuto as métricas aparecem em **Explore → Prometheus** com `service_name="frota360-api"`.
4. **Painel e alertas** (uma vez por stack; depois, só quando `infra/grafana/` mudar): criar uma
   service account com papel **Admin** (a API de provisionamento de alertas não aceita Editor) (*Administration → Users and access → Service accounts*),
   gravar o token em `GRAFANA_TOKEN` e rodar
   `./scripts/grafana-provisionar.ps1 -Url https://<stack>.grafana.net -EmailAlertas <e-mail>`.
5. **Synthetic Monitoring** → check HTTP em `https://api.frota360app.com.br/health`, a cada
   1 minuto, da sonda mais próxima do Brasil. O alerta dispara com 2 falhas seguidas.
6. Testar o alerta de verdade: `docker compose -f docker-compose.prod.yml stop api`, esperar o
   e-mail, subir de novo.

## Front no S3 + CloudFront

Publicado pelo job `web` do pipeline. `VITE_API_URL` vem do `.env.production` versionado e
`VITE_GOOGLE_MAPS_KEY` da variável do environment `production`: o Vite embute as duas no bundle
em tempo de build, então trocar qualquer uma exige um novo deploy.

**O fallback de SPA não é opcional.** O front usa `BrowserRouter`, então uma recarga em
`/veiculos` faz o navegador pedir esse caminho ao S3, que não tem o arquivo. No CloudFront,
**403 e 404 → `/index.html` com status 200** (403 porque um bucket privado com OAC devolve 403,
não 404). Está configurado, e o pipeline o confere a cada deploy pedindo `/veiculos`.

## DNS — `frota360app.com.br`

Registrado no Registro.br, zona no Cloudflare.

| Nome | Tipo | Destino | Proxy |
|---|---|---|---|
| `api` | A | `18.229.152.249` (IP elástico) | **DNS-only (cinza)** |
| `app` | CNAME | `dasgc9msn858p.cloudfront.net` | **DNS-only (cinza)** |
| `_90c4….app` | CNAME | validação do ACM | DNS-only |
| `@` e `www` | A | `192.0.2.1` (fictício) | **Proxied (laranja)** |
| `send` | conforme o Resend | registros de verificação | DNS-only |

**`api.` precisa ficar em DNS-only.** Proxiado, o Cloudflare intercepta as portas 80 e 443 e o
desafio HTTP-01 do Let's Encrypt nunca chega à instância: o Caddy fica em loop e o TLS nunca
sobe. Uma API REST autenticada não é cacheável, então o proxy agregaria pouco ali.

**`app.` também fica em DNS-only**, para não empilhar dois CDNs e o TLS ser só o do CloudFront.

**A raiz e o `www` são um redirect 3xx de verdade**, não servem o front: a Redirect Rule
`raiz para app` do Cloudflare manda os dois para `https://app.frota360app.com.br` com 301. O IP
`192.0.2.1` é reservado para documentação e só existe para o Cloudflare ter o que proxiar. Se a
raiz passar a servir o mesmo conteúdo, a origem das requisições vira o raiz e o CORS bloqueia
tudo: só `https://app.frota360app.com.br` está em `Cors__AllowedOrigins__0`.

O **security group libera 80 e 443** para `0.0.0.0/0`. A porta 80 não é opcional — é por onde
passa a validação do certificado.

## TODO

- Resend: concluir a verificação de `send.frota360app.com.br` — `Resend__From` só entrega
  depois disso.
- HSTS: o `Caddyfile` começa com `max-age=86400` de propósito. Suba para `31536000` **depois**
  de algumas semanas estável — um max-age longo com o domínio ainda instável tranca os
  navegadores em HTTPS pelo período inteiro.
- **Backup do Postgres (pendência mais séria).** Não existe. Ver [contexto-api.md](contexto-api.md)
  (§ Dívida técnica). Precisa estar resolvido **antes do primeiro dado real do cliente**.
