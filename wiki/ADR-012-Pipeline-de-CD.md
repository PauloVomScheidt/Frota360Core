# ADR-012 — Pipeline de CD: cada merge na main vai para produção

**Situação:** Aceita · **Data:** 16/11/2026

## Contexto

O CI (`ci.yml`) buildava, testava e analisava, mas a produção dependia de comandos manuais na EC2: `docker compose up --build` na própria instância. Deploy via CI/CD é obrigatório no TCC, e deploy manual por SSH ou FTP é motivo de reprovação.

A produção é uma EC2 única com Docker Compose e o front em S3 + CloudFront ([ADR-007](ADR-007-EC2-com-Docker-Compose)). O time é pequeno e todo código chega à `main` por PR, com os checks `api`, `web` e `docker` obrigatórios.

## Decisão

* **Todo push na `main` que passa pelos três checks vai para produção.** O job `deploy` do `ci.yml` chama o workflow reutilizável `deploy.yml` com o SHA do commit. Não há tag nem aprovação manual: o PR revisado é o portão.
* **A imagem da API vai para o GHCR**, marcada com o SHA do commit. O compose de produção usa `image:` com a tag obrigatória, em vez de `build:` na instância.
* **Sem chave da AWS no GitHub.** Os jobs assumem a role `frota360-deploy` por **OIDC**. A trust policy só aceita o environment `production` deste repositório, e a política da role libera apenas o SSM na instância, o bucket do front e a invalidação da distribuição.
* **Sem SSH.** A EC2 é atualizada por **SSM Run Command**: `git checkout` do commit e `scripts/deploy-ec2.sh`, que faz `pull` + `up --wait`. A instância não tem porta 22 aberta.
* **Volta sozinho se a versão nova não subir.** Se a API não ficar saudável, o script recoloca a versão anterior e o job falha.
* **O front é publicado depois da API**: `s3 sync` (assets com cache de um ano, `index.html` sem cache) e invalidação do CloudFront.
* **O pipeline confere o resultado de fora da AWS**: `/health` da API e uma rota do front, que prova o fallback de SPA.
* **Rollback** é o mesmo workflow disparado à mão com o SHA anterior. A imagem já existe, então o build é pulado.

## Consequências

* O que está no ar é rastreável até um commit: a tag da imagem é o SHA, gravado em `/opt/frota360/.versao-implantada`.
* **Merge na `main` passa a ser deploy.** Mudança em compose, Dockerfile ou middleware precisa do ensaio local antes do PR.
* **Migration não tem rollback.** Elas rodam no boot da API. Voltar para antes de uma migration aditiva funciona; antes de uma destrutiva (coluna removida ou renomeada), não. Essas se corrigem para a frente.
* Na primeira publicação o pacote do GHCR nasce privado e precisa ser tornado público uma vez, para a EC2 baixar a imagem sem token.
* Entre o `up` e o healthcheck, a API fica alguns segundos indisponível. Com uma instância só, não há troca sem interrupção.

## Alternativas consideradas

* **Deploy por tag de release (`v*`)**: daria um passo de decisão explícito entre o merge e a produção, mas acrescenta uma etapa manual a cada entrega. Com PR revisado e checks obrigatórios, o merge já cumpre esse papel.
* **Amazon ECR no lugar do GHCR**: a EC2 baixaria a imagem pela role da instância, sem tornar o pacote público, mas o repositório já é público e o GHCR não tem custo nem exige mais um recurso na AWS.
* **SSH a partir do Actions**: exigiria abrir a porta 22 e guardar uma chave privada no GitHub — e é exatamente o "deploy manual via SSH" que o TCC veta.
* **Gatilho por `workflow_run`**: o deploy dependeria do resultado do CI inteiro, inclusive do `sonar`, que não é check obrigatório. Como job do `ci.yml`, ele depende só dos três checks que importam.
