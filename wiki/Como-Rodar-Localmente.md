# Como Rodar Localmente

## Pré-requisitos

| Ferramenta | Versão | Para quê |
| :---- | :---- | :---- |
| .NET SDK | 10 | API |
| Node.js | 20 ou superior | Front |
| Docker Desktop | recente | PostgreSQL de desenvolvimento e testes de integração |
| Git | — | — |

Os comandos abaixo são para PowerShell, a partir da raiz do repositório.

## 1. Banco de dados

```powershell
docker compose up -d        # postgres:17 na porta 5432, banco "frota360"
```

O banco usa as credenciais que os `appsettings` de desenvolvimento já esperam.

## 2. API

```powershell
cd apps/api
copy src/Api/appsettings.example.json src/Api/appsettings.json
copy src/Api/appsettings.example.json src/Api/appsettings.Development.json

dotnet user-secrets set "Jwt:Key" "uma-chave-secreta-com-pelo-menos-32-caracteres" --project src/Api
dotnet user-secrets set "Backoffice:ApiKey" "uma-chave-de-backoffice" --project src/Api

dotnet ef database update --project src/Infrastructure --startup-project src/Api
dotnet run --project src/Api --launch-profile https
```

A API sobe em `https://localhost:7271` (e `http://localhost:5062`), com a documentação interativa em **`/scalar/v1`**.

Os `appsettings*.json` não são versionados, e segredo nunca vai para eles: tudo que é chave fica em *user-secrets*. Configurações opcionais:

| Chave (user-secrets) | Sem ela |
| :---- | :---- |
| `Resend:ApiKey` | Os e-mails de convite e senha são impressos no console, em vez de enviados — prático em desenvolvimento |
| `GoogleRoutes:ApiKey` | O cálculo de trajeto responde 422; o resto funciona |

## 3. Front

```powershell
cd apps/web
npm install
npm run dev                 # http://localhost:5173
```

A porta **5173 é fixa**: é a origem liberada no CORS da API. A URL da API vem de `VITE_API_URL` em `apps/web/.env.development` e precisa terminar em `/api/v1`.

Para o mapa e o autocomplete de endereço, crie `apps/web/.env.local` (não versionado) com `VITE_GOOGLE_MAPS_KEY=<chave de navegador>`. Sem ela, os campos de endereço viram texto livre.

## 4. Primeiro acesso

Num banco novo não existe usuário: a conta nasce de um convite. O script de seed faz o caminho inteiro de uma vez:

```powershell
./scripts/seed-dev.ps1
```

Ele cria a empresa **Transportadora Dev** pelo backoffice e as contas abaixo, todas com a senha `SenhaForte123`:

| E-mail | Papel |
| :---- | :---- |
| `admin@dev.com` | Admin |
| `motorista@dev.com` | Motorista |

É seguro rodar de novo: reaproveita a empresa e não toca em outras. Só `-Recriar` é destrutivo (apaga o banco inteiro, com confirmação). Se a API estiver no perfil `http`, passe `-ApiUrl http://localhost:5062/api/v1`.

Sem o script, o caminho manual é `POST /backoffice/empresa` com o cabeçalho `X-Backoffice-Key` e abrir o `linkConvite` devolvido.

## Comandos do dia a dia

| | `apps/api` | `apps/web` |
| :---- | :---- | :---- |
| Build | `dotnet build Frota360.slnx` | `npm run build` |
| Testes | `dotnet test` | `npm run test` |
| Cobertura | `../../scripts/cobertura-api.ps1 -Abrir` | `npm run test:cov` |
| Lint | — | `npm run lint` |
| Nova migration | `dotnet ef migrations add <Nome> --project src/Infrastructure --startup-project src/Api` | — |

Detalhes em [Testes e Qualidade](Testes-e-Qualidade).

## Problemas comuns

| Sintoma | Causa |
| :---- | :---- |
| A API não sobe e reclama de `Jwt:Key` | Chave ausente ou com menos de 32 caracteres nos user-secrets |
| O navegador bloqueia as chamadas (CORS) | O front não está na porta 5173 |
| O front falha ao carregar com erro de `VITE_API_URL` | `.env.development` ausente ou vazio |
| `npm run test` falha com "failed to find the runner" no Git Bash | Rode no PowerShell: o caminho `/c/...` carrega o pacote duas vezes |
| Testes de integração falham ao iniciar | O Docker não está rodando |
