# ADR-004 — Sessão em cookies HttpOnly: JWT de 1 hora e refresh de 7 dias

**Situação:** Aceita · **Data:** 2026

## Contexto

A proposta inicial emitia um JWT de 8 horas, que o front guardava no `localStorage` e mandava no cabeçalho `Authorization`. Uma análise estática do front apontou o problema (`auth-token-in-web-storage`): qualquer script injetado na página — por uma dependência comprometida ou por XSS — lê o `localStorage` e leva um token válido por um dia inteiro.

## Decisão

* **JWT de 1 hora + refresh token de 7 dias**, rotacionado a cada renovação e guardado no banco só como hash.
* **Os dois vão em cookies `HttpOnly; Secure; SameSite=None`**, com validade igual à do próprio token. O corpo das respostas de login, renovação e aceite de convite traz só a identidade (nome, e-mail, papel, empresa) — nunca o token.
* A API lê o JWT do cookie quando não há cabeçalho `Authorization`, que continua aceito para o Scalar, o `curl` e clientes externos.
* `POST /auth/refresh` não recebe corpo: o navegador anexa o cookie sozinho.
* O refresh é revogado no logout, na redefinição de senha, na troca de papel e na desativação do usuário.

## Consequências

* O JavaScript do front não consegue ler a sessão — e, portanto, um script malicioso também não.
* O guarda de rotas do front decide pela identidade em cache, não pelo token; o 401 da API cobre o caso de cookie ausente ou expirado.
* **`SameSite=None` é obrigatório** porque front e API vivem em origens diferentes (portas em desenvolvimento, subdomínios em produção), e exige `Secure`: todo ambiente fala HTTPS.
* **CSRF:** com `SameSite=None`, o navegador envia o cookie também em requisições vindas de outros sites. A proteção vem do CORS restrito à origem do front e de os endpoints de escrita exigirem corpo JSON, que um formulário de outro site não consegue enviar sem o *preflight* do CORS. Os endpoints sem corpo (`/auth/refresh` e `/auth/logout`) podem ser disparados por outro site, mas o efeito se limita a renovar ou encerrar a sessão da própria vítima, sem acesso à resposta.
* Token curto significa uma renovação por hora, transparente para o usuário (o interceptor do Axios repete a requisição depois de renovar).

## Alternativas consideradas

* **Manter o token no `localStorage`**: descartado pela exposição a XSS.
* **Sessão no servidor, com cookie de sessão opaco**: exigiria estado compartilhado na API; o JWT mantém a API sem estado, com só o refresh token persistido.
* **`SameSite=Strict`/`Lax`**: bloquearia o cookie entre o domínio do front e o da API.
