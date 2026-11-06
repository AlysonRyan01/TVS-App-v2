#!/usr/bin/env bash
# Envia uma mensagem REAL de teste de WhatsApp para o WHATSAPP_TEST_PHONE do .env
# e mostra o estado da conexão. Uso (na VPS, na pasta do projeto): ./scripts/teste-whatsapp.sh
set -euo pipefail
cd "$(dirname "$0")/.."

echo "== Conexão do WhatsApp (último estado registrado) =="
docker compose logs api 2>/dev/null | grep -o 'WhatsApp conexão: [^"]*' | tail -1 || echo "(sem registro ainda)"

echo
echo "== Enviando mensagem de teste =="
docker compose exec -T api curl -s -X POST localhost:8080/internal/whatsapp/test-message
echo
