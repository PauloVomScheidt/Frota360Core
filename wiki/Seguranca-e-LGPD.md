# Segurança e LGPD

## Autenticação e sessão

* **Senhas com BCrypt.** Tokens de convite, redefinição de senha e refresh são guardados apenas como hash.
* **Sessão em cookies `HttpOnly; Secure; SameSite=None`**: JWT de 1 hora e refresh token de 7 dias, rotacionado a cada uso. O JavaScript do front nunca lê o token ([ADR-004](ADR-004-Sessao-em-cookie-HttpOnly)).
* **Sem cadastro aberto.** A conta nasce de um convite do Admin, válido por 7 dias e de uso único.
* **Revogação** do refresh token no logout, na redefinição de senha, na troca de papel e na desativação.
* **Recuperação de senha neutra**: a resposta é a mesma exista ou não o e-mail.
* **Rate limit por IP**: 5 requisições por minuto nas rotas de autenticação e convite; 200 por minuto no geral.

## Autorização

| Camada | Controle |
| :---- | :---- |
| Papel | `[Authorize(Roles = ...)]` em cada ação: Admin, Supervisor, Operador, Motorista ([Requisitos](Requisitos#papéis)) |
| Empresa | O `EmpresaId` vem só do token e filtra toda consulta; id de outra empresa "não existe" ([ADR-002](ADR-002-Multi-tenant-por-claim)) |
| Dono do registro | O motorista só alcança as próprias rotas e abastecimentos; o resto responde 404 ([ADR-003](ADR-003-Motorista-e-um-Usuario)) |
| Interface | `pode.*` esconde o que o papel não pode usar — só conveniência; quem decide é a API |

## OWASP Top 10

| Risco | Mitigação |
| :---- | :---- |
| A01 Broken Access Control | Papel por ação, escopo por empresa em toda consulta, recorte por dono para o motorista |
| A02 Cryptographic Failures | BCrypt; tokens só como hash; JWT HMAC-SHA256 com chave de 32+ caracteres; TLS em todo tráfego externo |
| A03 Injection | EF Core com consultas parametrizadas; nenhum SQL concatenado |
| A04 Insecure Design | Valores derivados calculados no servidor (valor do abastecimento, km percorrido, situação da manutenção); trava de plausibilidade no traçado vindo do cliente |
| A05 Security Misconfiguration | Segredos só por variável de ambiente; a API não inicia sem a configuração obrigatória; Scalar e OpenAPI desligados em produção; só 80 e 443 expostas |
| A06 Vulnerable Components | Análise do SonarCloud em todo PR; `npm ci --ignore-scripts`; actions fixadas por SHA |
| A07 Authentication Failures | Rate limit na autenticação; JWT curto com `jti`; refresh rotacionado e revogável |
| A08 Integrity Failures | Imagem multi-stage com base oficial da Microsoft; container sem privilégio; o CI prova que segredos não entram na imagem |
| A09 Logging Failures | Serilog estruturado; auditoria de toda escrita com autor e IP; monitoramento planejado ([Monitoramento](Monitoramento)) |
| A10 SSRF | A API só chama hosts fixos (Google, Resend); o cálculo de trajeto recebe apenas coordenadas numéricas validadas |

## Dados em trânsito e infraestrutura

* HTTPS obrigatório: o Caddy termina o TLS com certificado automático do Let's Encrypt.
* Banco e API conversam pela rede interna do Docker; o banco não tem porta exposta.
* Os cabeçalhos `X-Forwarded-*` só são aceitos vindos da sub-rede do Compose, o que impede forjar o IP gravado na auditoria.
* Erros nunca expõem stack trace ou detalhe de infraestrutura: o `ExceptionMiddleware` devolve mensagens fixas para 400, 401 e 500.
* Nenhum segredo é versionado: `appsettings` de ambiente e `.env` ficam fora do git e da imagem.

## LGPD

### Dados pessoais tratados

| Onde | Dados |
| :---- | :---- |
| Usuários (gestão e motoristas) | Nome, e-mail, senha (hash), papel, CPF (opcional), data de nascimento (opcional) |
| Convites | E-mail do convidado |
| Auditoria | Nome, e-mail, papel e IP de quem executou a ação |
| Rotas | Endereços e coordenadas de partida e chegada, ligados ao motorista |
| Abastecimentos e despesas | Vínculo do gasto ao motorista; nome do frentista (opcional) |

**Finalidade e base legal:** operação da gestão de frota, com base no legítimo interesse do controlador — a empresa cliente (Art. 7º, IX). O Frota360 atua como operador.

**Minimização:** CPF e nascimento são opcionais; o motorista não vê valores de custo; o e-mail do convite não carrega id de empresa nem quem convidou.

### Compartilhamento e transferência internacional

| Operador | Recebe | Para quê |
| :---- | :---- | :---- |
| Google (Maps Platform) | Endereços e coordenadas das rotas | Autocomplete, mapa e cálculo de trajeto |
| Resend | E-mail do destinatário e o link | Convite e redefinição de senha |
| AWS (sa-east-1) | Todos os dados | Hospedagem no Brasil |

Google e Resend processam fora do Brasil (Art. 33), com o mínimo necessário: nenhum dos dois recebe CPF, data de nascimento ou dado financeiro.

### Retenção

| Dado | Prazo |
| :---- | :---- |
| Trilha de auditoria | 12 meses — ciclo anual de auditoria e princípio da necessidade (Art. 6º, III). O expurgo ainda não está automatizado |
| Logs da aplicação | Volume limitado pela rotação (5 × 10 MB por container) |
| Cadastros e histórico operacional | Enquanto a empresa for cliente |

### Direitos do titular

| Direito | Como exercer |
| :---- | :---- |
| Acesso | Tela **Perfil**, disponível a todos os papéis |
| Correção | Tela **Perfil**: nome, CPF e nascimento. O e-mail, por ser a chave de login, é corrigido por solicitação ao controlador |
| Eliminação | O usuário é desativado (não excluído, para preservar o histórico de rotas e gastos); os dados pessoais são anonimizados sob solicitação formal |
| Portabilidade | Exportação em JSON pela API, sob solicitação |
