# Data Model: conteúdo do README

O "modelo" desta feature é o conteúdo documentado. Cada item abaixo vira linhas/blocos do README.

## Rota da API

| Campo | Exemplo |
|-------|---------|
| grupo | Ordens de serviço — comandos |
| método + caminho | `PUT /service-orders/{id}/delivery` |
| login | Sim (Bearer) / Não (pública) / Interna |
| descrição | Entrega a OS ao cliente e devolve o PDF |
| parâmetros | `id` (rota, long) |
| corpo | link para o objeto de entrada |
| resposta | envelope com DTO / `application/pdf` / sem corpo |
| evento | `service_order.delivered` |

Inventário (41 + extras): Auth 4 · Clientes 4 (inclui `/addresses/{cep}`) · Métricas 1 ·
Públicas 4 · OS comandos 15 · OS consultas 11 · extras: `GET /`, `/osHub`, `/internal/metrics`.

## Objeto de entrada (comando/requisição)

Campos: nome (camelCase), tipo, obrigatório, regra/validação, exemplo. Origem:
`src/TVSApp.Application/Requests/**` (AuthRequests: Login/RefreshToken; CustomerCommands:
Create/Update; ServiceOrderCommands: AddPayment, AddProductLocation, AddServiceOrderEstimate,
CreateSalesServiceOrder, CreateServiceOrder, CustomerEstimateResponse, EditServiceOrder,
UpdatePayment; ServiceOrdersQueries: ServiceOrderSearchQuery) e corpos inline nos endpoints
(ex.: custo da peça, entrega, cadastro).

## DTO de saída

Campos: nome, tipo, descrição. Origem: `src/TVSApp.Application/DTOs/**` (AuthTokensDto,
CustomerDto, CustomerSelectDto, AddressDto, ServiceOrderDto, PaymentDto, CreatedServiceOrderPdf,
EstimateServiceOrder, DashboardMetricsDto, DeliveredProductDto) e paginação, se houver.

## Enumeração

Nome, valor numérico, nome no código, significado em pt-BR. Origem: `src/TVSApp.Domain/Enums`
(EDocumentType, EEnterprise, EProduct, ERepairResult, ERepairStatus, EServiceOrderStatus) e
forma de pagamento, se existir enum/constante.

## Envelope de resposta

`data`, `message`, `code` (+ `isSuccess` se serializado); erros com `data: null`.

## Variável de ambiente

Nome, obrigatória, finalidade, exemplo fictício — todas do `.env.example` (SC-005).

## Evento de negócio / métrica

Nome, quando ocorre, rótulos — catálogo de `BusinessEvents` (25) e métricas `tvs_*`, `http_*`,
`up`, `node_*`, `container_*`.
