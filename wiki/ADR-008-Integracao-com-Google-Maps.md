# ADR-008 — Duas chaves do Google; o traçado é calculado uma vez e enviado pelo cliente

**Situação:** Aceita · **Data:** 08/09/2026

## Contexto

Origem e destino das rotas eram texto livre, o que gerava endereços inconsistentes e nenhuma noção de distância. A Google Maps Platform resolve as duas coisas — autocomplete de endereço e cálculo de trajeto —, mas **cada chamada é cobrada**, e uma chave exposta pode ser usada por terceiros na nossa conta.

## Decisão

* **Duas chaves, nunca a mesma:**
  * a da **Routes API** é de servidor, fica só na API (`GoogleRoutes:ApiKey`) e é restrita por IP;
  * a do **Maps JavaScript + Places** é de navegador. O Vite a embute no bundle, então ela é pública por construção, e sua única proteção é a restrição por domínio no console do Google.
* **`POST /rota/calcular` é uma consulta**: recebe só coordenadas, devolve distância, duração e traçado, e não grava nada.
* **O traçado entra no cadastro da rota vindo do cliente.** O front já pagou pelo cálculo ao montar o formulário; recalcular no salvamento seria uma segunda chamada cobrada pelo mesmo trajeto.
* **Em contrapartida, há uma trava de plausibilidade** (RN19): distância e duração que impliquem média fora de 5–180 km/h são recusadas.
* A chamada usa a máscara de campos mínima e roteamento sem trânsito, que a mantêm na faixa de preço mais barata. Timeout de 10 s.
* Cada cálculo bem-sucedido é registrado na auditoria, para que exista de onde apurar o custo por empresa.

## Consequências

* Aceitar o número do cliente é seguro porque **não é dado financeiro**: o R$/km sai do odômetro informado no encerramento, nunca do traçado.
* Falha da Google (timeout, erro, sem trajeto) vira 422 com mensagem genérica, e o motivo técnico fica só no log. A rota pode ser salva sem traçado, e sem a chave do navegador o campo de endereço vira texto livre.
* A chave da Routes API fica fora da validação de configuração obrigatória: o cálculo é um recurso, não um caminho obrigatório, e sua falta não deve impedir o deploy do resto.
* No front, a consulta de cálculo é cacheada pelas quatro coordenadas sem expiração — cada cálculo é cobrado.

## Alternativas consideradas

* **Uma chave só para tudo**: a chave do navegador é pública; usá-la também no servidor exporia a Routes API.
* **Recalcular no servidor ao salvar**: dobraria o custo por rota.
* **Serviços abertos (OSRM, Nominatim)**: sem custo por chamada, mas com qualidade de endereço inferior no Brasil e infraestrutura própria para manter.
