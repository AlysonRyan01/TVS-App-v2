# Data Model: Observabilidade

## Campos dos logs (JSON)

| Campo | Exemplo | Origem |
|-------|---------|--------|
| `@t`, `@l`, `@m` | horário, nível, mensagem | Serilog |
| `service` | `tvs-api` | enriquecimento |
| `CorrelationId` | `7f3c…` | middleware |
| `UserEmail` | `atendente@loja` | usuário logado |
| `RequestMethod`, `RequestPath`, `StatusCode`, `Elapsed` | `PUT`, `/service-orders/{id}/delivery`, `200`, `153.2` | log de requisição |
| `Event`, `Outcome`, `ServiceOrderId`, `CustomerId` | `service_order.delivered`, `success`, `4312` | eventos de negócio |
| `MessageType`, `Phone` (mascarado), `Reason` | `SendGuaranteeMessageRequest`, `41*****0000` | WhatsApp |

Rótulos no Loki (baixa cardinalidade): `service`, `container`, `level`. O resto fica no JSON
(filtro com `| json | ServiceOrderId="4312"`).

## Catálogo de eventos de negócio

`auth.login` · `customer.created` · `customer.updated` · `service_order.created` ·
`service_order.updated` · `sale.created` · `estimate.added` · `estimate.approved` ·
`estimate.rejected` · `estimate.approved_by_customer` · `estimate.rejected_by_customer` ·
`part.purchased` · `part.purchase_cost_updated` · `service_order.ready_for_pickup` ·
`service_order.repaired` · `service_order.delivered` · `service_order.location_updated` ·
`service_order.pdf_reprinted` · `payment.added` · `payment.updated` · `payment.removed` ·
`payment.receipt_printed` · `whatsapp.enqueued` · `whatsapp.sent` · `whatsapp.failed`

## Métricas

| Métrica | Tipo | Rótulos |
|---------|------|---------|
| `http_requests_received_total`, `http_request_duration_seconds` | contador / histograma | `method`, `endpoint`, `code` |
| `tvs_business_events_total` | contador | `event`, `outcome` |
| `tvs_whatsapp_messages_total` | contador | `type`, `outcome` |
| `node_*` (CPU, memória, disco, rede) | — | VPS |
| `container_*` | — | containers |
| `up{job="tvs-api"}` | — | disponibilidade |

## Regras de alerta (sem envio)

| Alerta | Condição | Por |
|--------|----------|-----|
| API fora do ar | `up{job="tvs-api"} == 0` | 2 min |
| Taxa de erros | 5xx / total > 5% | 5 min |
| WhatsApp falhando | `increase(tvs_whatsapp_messages_total{outcome="failure"}[10m]) >= 3` | — |
| Disco quase cheio | uso do `/` > 85% | 5 min |
