# Testes e Qualidade

| | Backend | Frontend |
| :---- | :---- | :---- |
| Testes | 592, em três suítes | 769, em 40 arquivos |
| Cobertura de linhas | **82,0%** | **93,2%** |
| Mínimo exigido no CI | 75% | 50% |
| Ferramentas | xUnit, NSubstitute, Testcontainers, coverlet, ReportGenerator | Vitest, Testing Library, MSW, coverage-v8 |

Análise estática no [SonarCloud](https://sonarcloud.io/summary/new_code?id=PauloVomScheidt_Frota360Core): quality gate verde, nota **A** em segurança, confiabilidade e manutenibilidade, duplicação abaixo de 3%.

## Backend: três suítes

| Suíte | O que testa | Como |
| :---- | :---- | :---- |
| `Frota360.Tests` (404) | Handlers, validators, mapeamentos e serviços — as regras de negócio | xUnit + NSubstitute, sem banco; roda em ~1 s |
| `Frota360.Api.Tests` (139) | Controllers, middlewares, cookies de sessão e a montagem da injeção de dependência | Controller instanciado com dependências substituídas; middleware sobre `DefaultHttpContext` |
| `Frota360.IntegrationTests` (49) | Repositórios, índices, tipos de coluna, collation, tradução de consultas | PostgreSQL 17 real, descartável, via Testcontainers |

Por que três: a suíte unitária não prova nada sobre o banco (um índice único, a precisão de um `numeric`, a tradução de uma consulta do EF), e a de integração seria lenta demais para testar cada regra. A camada HTTP ganhou suíte própria quando o SonarCloud mostrou que nenhuma das outras a alcançava.

Padrões das suítes:

* Nome do teste em português, no formato `Metodo_Cenario_DeveResultado`.
* **Todo handler tem um teste que prova o escopo pela empresa** do usuário ([ADR-002](ADR-002-Multi-tenant-por-claim)).
* Regra de negócio é testada pela exceção que vira 422.

### Gerar o relatório de cobertura

Da raiz do repositório, com o Docker no ar:

```powershell
./scripts/cobertura-api.ps1 -Abrir
```

O script builda em Release, roda as três suítes, junta a cobertura com o ReportGenerator e abre o HTML consolidado. Termina com erro abaixo de 75%. Migrations e `Program.cs` ficam fora da conta.

> O SonarCloud mostra uns 2 pontos a menos em `apps/api`: ele conta como linha descoberta o inicializador de propriedade dos DTOs (`= string.Empty`), que o coverlet não instrumenta.

## Frontend

| Tipo | Exemplo |
| :---- | :---- |
| Função pura | `src/lib/periodo.test.ts` — relógio congelado, virada de mês e de ano |
| Tela com API simulada | `src/pages/DespesasPage.test.tsx` — o MSW intercepta as chamadas; o teste confere que o rodapé vem de `/despesa/resumo`, não da soma da página |
| Permissões | `src/auth/permissions.test.ts` — cada ação testada para cada papel |

```powershell
cd apps/web
npm run test:cov            # tabela por arquivo no terminal
start coverage/index.html   # relatório navegável
```

O Google Maps é substituído por `vi.mock` nos testes; o cálculo de trajeto é testado de verdade, com a API simulada pelo MSW.

## Bugs que os testes encontraram

Os testes de caracterização do front acharam cinco bugs, e a revisão do RFC achou um no backend. Cada um foi corrigido junto com o teste que o prova:

| Bug | Onde |
| :---- | :---- |
| `unwrap()` tratava `dados: null` como erro | Front, `api/http.ts` |
| Arredondamento do total do abastecimento divergia da API | Front |
| Edição de rota de motorista rebaixado ficava bloqueada | Front |
| Data sugerida em "Nova despesa" ficava presa ao carregamento da tela | Front |
| Logout travava com a API fora do ar | Front |
| Placa repetida era aceita na mesma empresa (RN02 nunca implementada) | Backend — teste escrito antes da correção |

## Integração contínua

`.github/workflows/ci.yml` roda em todo pull request e push na `main`. Os três primeiros jobs são checks obrigatórios para o merge:

| Job | O que faz | Reprova quando |
| :---- | :---- | :---- |
| `api` | Build em Release, as três suítes em passos separados e a cobertura consolidada | Algum teste falha ou a cobertura fica abaixo de 75% |
| `web` | `npm ci --ignore-scripts`, build (`tsc` + Vite), lint (oxlint) e testes com cobertura | Build, lint ou teste falha, ou cobertura abaixo de 50% |
| `docker` | Build da imagem da API, sem publicar, e prova de que o `.dockerignore` barra os arquivos com segredo | Um arquivo de configuração sensível entra na imagem |
| `sonar` | Depois de `api` e `web`, envia análise e cobertura ao SonarCloud | Não bloqueia o merge; o quality gate aparece no PR |

Detalhes que importam ao mexer no CI:

* **Sem filtro de caminho nos checks obrigatórios.** Um workflow que não roda não reporta status, e o PR fica travado esperando.
* **O job `docker` planta "iscas"** (um `appsettings.Development.json` e um `.env` com texto marcador) antes do build, porque num clone limpo esses arquivos não existem e a verificação passaria vazia.
* As actions são fixadas por SHA de commit, não por tag.
* O relatório de cobertura da API sai como artefato `relatorio-cobertura-api`, e o resumo aparece no *job summary*.

## TDD daqui em diante

Os testes até 12/11/2026 caracterizaram código que já existia. Daqui em diante, funcionalidade nova nasce com o teste no commit primeiro — o histórico do Git é a evidência. A correção da RN02 seguiu esse caminho: teste vermelho, implementação, verde.
