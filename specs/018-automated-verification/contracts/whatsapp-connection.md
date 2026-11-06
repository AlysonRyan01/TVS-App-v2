# Contract: monitor de conexão do WhatsApp

## Métrica

`tvs_whatsapp_connected` (gauge) — `1` conectado, `0` desconectado/indisponível; ausente se a
Evolution não estiver configurada. Atualizada a cada `WhatsAppMonitor:IntervalSeconds` (60 s).

## Logs (só na mudança de estado)

- `Warning` — `WhatsApp conexão: desconectado (instância em "close")` / `indisponível (<motivo>)`
- `Information` — `WhatsApp conexão: conectado`
Campos: `Event = whatsapp.connection`, `State`, `Reason`.

## Grafana

- Painel **WhatsApp**: stat "Conexão" (`max(tvs_whatsapp_connected)` → Conectado/Desconectado) e
  linha do tempo do estado.
- Alerta **WhatsApp desconectado**: `max(tvs_whatsapp_connected) < 1` por 5 min; `noDataState: OK`
  (Evolution não configurada não alerta); pasta TVS, grupo Saúde, `severity=critical`, sem envio.
