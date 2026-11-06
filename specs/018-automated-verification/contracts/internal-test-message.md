# Contract: POST /internal/whatsapp/test-message

Só acessível dentro da rede do Docker (o Caddy responde 404 para `/api/internal/*`). Sem login.

```bash
docker compose exec api curl -s -X POST localhost:8080/internal/whatsapp/test-message
```

| Situação | HTTP | Corpo (envelope) |
|----------|------|------------------|
| Enviada | 200 | `{ "data": "5541999990000", "message": "Mensagem de teste enviada para 55*******0000.", "code": 200 }` |
| Evolution recusou/fora do ar | 502 | `{ "data": null, "message": "Falha ao enviar a mensagem de teste: <motivo>", "code": 502 }` |
| Sem `WHATSAPP_TEST_PHONE` | 400 | `{ "data": null, "message": "Configure WHATSAPP_TEST_PHONE no .env para usar o envio de teste.", "code": 400 }` |

- Texto enviado: `🧪 *Mensagem de teste* do sistema TVS Eletrônica — dd/MM/yyyy HH:mm. Se você recebeu, o envio de WhatsApp está funcionando.`
- Não cria cliente nem OS; não usa telefones de clientes.
- Registra evento `whatsapp.test` (success/failure) e `tvs_whatsapp_messages_total{type="TestMessage"}`.
- Em `Development` o destino é o mesmo `WHATSAPP_TEST_PHONE` (sem redirecionamento).
