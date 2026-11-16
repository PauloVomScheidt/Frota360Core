#!/usr/bin/env bash
# Atualiza a stack de produção na EC2. Chamado pelo deploy.yml via SSM Run Command, depois do
# checkout do commit a implantar em /opt/frota360:
#   bash scripts/deploy-ec2.sh <sha>
set -euo pipefail

versao="${1:?informe o SHA do commit a implantar}"
cd "$(dirname "$0")/.."

[ -f .env ] || { echo "Falta o .env em $(pwd) - crie a partir do .env.example" >&2; exit 1; }

compose() { docker compose -f docker-compose.prod.yml "$@"; }
anterior=$(cat .versao-implantada 2>/dev/null || true)
export API_TAG="$versao"

if ! compose pull api; then
  echo "Falha ao baixar $versao. Na primeira publicação o pacote nasce privado no GHCR:" \
       "torne-o público em github.com/PauloVomScheidt?tab=packages -> frota360-api -> Package settings." >&2
  exit 1
fi

if ! compose up -d --remove-orphans --wait --wait-timeout 300; then
  echo "A versão $versao não ficou saudável." >&2
  compose logs --tail 80 api >&2 || true
  if [ -n "$anterior" ]; then
    echo "Voltando a API para $anterior." >&2
    API_TAG="$anterior" compose up -d --wait --wait-timeout 300 api || true
  fi
  exit 1
fi

# O Caddyfile é montado como arquivo, e o checkout o recria com outro inode: o container
# continuaria lendo a versão antiga até ser reiniciado.
if ! compose exec -T caddy cat /etc/caddy/Caddyfile | cmp -s - Caddyfile; then
  echo "Caddyfile mudou; reiniciando o Caddy."
  compose restart caddy
fi

echo "$versao" > .versao-implantada
docker image prune -f > /dev/null
echo "Implantado: $versao (anterior: ${anterior:-nenhuma})"
