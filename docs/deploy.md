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
curl -X POST https://api.SEU-DOMINIO.com.br/api/v1/backoffice/empresa \
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
| `ARQUIVO_ENV` | só no ensaio local, para apontar ao `.env.local` |

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

## TODO — dependem do domínio

Nada aqui bloqueia o ensaio local.

- Registrar o domínio e apontar um registro A do subdomínio da API para o IP elástico da EC2.
  **Sem o DNS resolvendo, a validação do Let's Encrypt falha** e o Caddy fica tentando em loop.
- `.env`: `DOMINIO_API`, `EMAIL_ACME`, `Cors__AllowedOrigins__0`, `Frontend__BaseUrl`.
- `apps/web/.env.production`: `VITE_API_URL`.
- Resend: verificar o domínio antes de definir `Resend__From`.
- HSTS: o `Caddyfile` começa com `max-age=86400` de propósito. Suba para `31536000` **depois**
  de algumas semanas estável — um max-age longo com o domínio ainda instável tranca os
  navegadores em HTTPS pelo período inteiro.

## TODO — backup (pendência mais séria)

Não existe backup do Postgres. Ver [contexto-api.md](contexto-api.md) (§ Dívida técnica).
Precisa estar resolvido **antes do primeiro dado real do cliente**.
