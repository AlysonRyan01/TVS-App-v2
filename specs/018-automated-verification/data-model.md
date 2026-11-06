# Data Model: Verificação automática

Nada é persistido no banco do sistema. Entidades são de execução/observabilidade.

## Mensagem capturada (verificação, em memória)

| Campo | Exemplo | Origem |
|-------|---------|--------|
| `Kind` | `text` / `media` | rota chamada na Evolution (`sendText`/`sendMedia`) |
| `Instance` | `tvs-teste` | segmento da URL |
| `Number` | `5541900000001` | corpo `number` (já formatado pelo `WhatsappService`) |
| `Text` | `*Olá MARIA…* … nº 12 …` | corpo `text` (ou legenda da mídia) |
| `ReceivedAt` | horário | handler falso |

## Mensagem esperada (por cenário)

| Etapa | Destino | Conteúdo conferido |
|-------|---------|--------------------|
| Abrir OS | cliente | "bem-vindo", nº da OS |
| Orçamento (não Philco/Britania) | cliente | nº da OS, código de segurança |
| Aprovar (loja ou cliente; não Philco/Britania) | cliente + dono | agradecimento com valor; "aprovou" + nº |
| Reprovar | cliente + dono | confirmação; "reprovou" + nº |
| Liberar reprovado | cliente | retirada |
| Conserto | cliente | aparelho pronto, nº |
| Entrega aprovada com conserto | cliente | garantia, nº |
| Venda | cliente | venda, garantia |
| Contato do site | dono | nome e telefone do visitante |
| Philco/Britania (orçamento/aprovação) | — | **nenhuma** mensagem |
| Código de segurança errado | — | **nenhuma** mensagem, resposta 404 |

## Estado da conexão do WhatsApp

| Estado | Condição | `tvs_whatsapp_connected` |
|--------|----------|--------------------------|
| `conectado` | `instance.state == "open"` | 1 |
| `desconectado` | `connecting` ou `close` | 0 |
| `indisponível` | erro HTTP, timeout, 401/403, instância inexistente | 0 |
| (desligado) | `EvolutionApiSettings:BaseUrl` vazio | sem métrica |

Transições geram 1 log cada (`Information` ao voltar a conectado; `Warning` ao cair).

## Teste real de envio

| Campo | Regra |
|-------|-------|
| `WhatsAppTest:Phone` | obrigatório para enviar; só dígitos após normalização; nunca vem de cliente |
| resultado | `sent` / `failed` + motivo (mensagem da Evolution ou exceção) |
| registro | log `WhatsApp enviado|falhou TestMessage …`, evento `whatsapp.test`, contador `type="TestMessage"` |

## Configuração nova

| Chave (.NET) | Variável (`.env`) | Padrão |
|--------------|-------------------|--------|
| `WhatsAppTest:Phone` | `WHATSAPP_TEST_PHONE` | vazio (envio recusado) |
| `WhatsAppMonitor:IntervalSeconds` | — | `60` |
