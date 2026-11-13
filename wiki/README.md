# Fonte da Wiki

Esta pasta é a fonte da [Wiki do GitHub](https://github.com/PauloVomScheidt/Frota360Core/wiki). O workflow [`.github/workflows/wiki.yml`](../.github/workflows/wiki.yml) a publica a cada push na `main` que mexa aqui ou nas capturas de tela, e também pode ser disparado à mão (Actions → wiki → Run workflow).

- **Uma página por arquivo.** O nome do arquivo é o endereço da página: `Modelo-de-Dados.md` vira `/wiki/Modelo-de-Dados`, com o título "Modelo de Dados".
- **Links entre páginas sem `.md`**: `[Deploy](Deploy)`. É o formato da Wiki; no navegador do repositório esses links não abrem.
- **Imagens em `imagens/`**: o workflow copia `docs/img/rfc/` para lá na publicação. Para uma captura nova, salve-a em `docs/img/rfc/` e referencie como `imagens/<arquivo>.png`.
- **`_Sidebar.md` e `_Footer.md`** são a navegação lateral e o rodapé de todas as páginas. Página nova entra na sidebar.
- **Este README não é publicado.**
- **Não edite pela interface da Wiki.** A publicação espelha esta pasta e apaga o que não estiver aqui.

Diagramas são em Mermaid, que a Wiki renderiza. Antes de publicar um diagrama novo, confira-o no [Mermaid Live](https://mermaid.live).
