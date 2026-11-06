# Research: Limpeza das rotas da API

Base: os 7 arquivos de `src/TVSApp.Api/Endpoints/`, `Program.cs`, o uso real em `web/src` e em
`tvseletronica/js/api.js`, e `old-handlers/` (pagamentos, consulta do cliente, aprovar/reprovar).

## R1. Organização dos endpoints (comandos × consultas)

- **Decision**: uma pasta por recurso em `src/TVSApp.Api/Endpoints/`, com dois arquivos —
  `{Recurso}CommandEndpoints.cs` (POST/PUT/DELETE) e `{Recurso}QueryEndpoints.cs` (GET), cada um com
  um método de extensão `Map{Recurso}CommandEndpoints` / `Map{Recurso}QueryEndpoints` chamado em
  `AppExtensions.AddEndpoints`:
  - `ServiceOrders/` (OS, venda, ações da Home, pagamentos),
  - `Customers/` (clientes e CEP),
  - `Auth/` (só comandos),
  - `Metrics/` (só consultas),
  - `Public/` (site: consulta, aprovar/reprovar, contato).
- **Rationale**: espelha a separação que já existe na Application (`*CommandHandler` /
  `*QueryHandler`) e deixa claro o que é anônimo (pasta `Public`).
- **Alternatives**: `MapGroup` por recurso num arquivo só (não separa comando/consulta).

## R2. Rotas removidas e código que sai junto

| Rota | Também sai |
|------|-----------|
| `GET /get-all-customers/...`, `GET /get-customer-by-id/{id}` | `GetAllCustomersHandler`, `GetCustomerByIdHandler`, as 2 queries, `ICustomerQueryRepository.GetAllAsync` (`GetByIdAsync` fica: o command handler usa) |
| `GET /get-delivered-service-orders/...` | `GetDeliveredQuery` |
| `GET /get-service-orders-by-enterprise` | `GetServiceOrdersByEnterpriseQuery`, `IServiceOrderQueryRepository.GetByEnterpriseAsync` |
| `GET /get-abandoned-orders` | `GetAbandonedOrdersHandler`, `GetAbandonedOrdersQuery`, `IMetricsService.GetAbandonedOrdersAsync`, `AbandonedOrder(s)Dto` |
| `GET/POST/PUT /notifications...` | `NotificationEndpoints`, os 3 handlers MediatR e as requests de notificação (o repositório fica: aprovar/reprovar grava notificação) |
| `POST /v1/email/send` | `EmailEndpoints`, `IEmailQueue`/`EmailQueue`, `EmailNotificationWorker`, `IEmailService`/`EmailService`, requests de e-mail e a configuração `EmailServiceSettings` (nada mais usa) |
| — (sem rota) | `UpdateServiceOrderCommand`, `UpdateServiceOrderResponseDto`, `GetServiceOrderByIdQuery` (sem uso) |

Depois disso nenhum código usa MediatR → remover o registro `AddMediatR` e as referências ao pacote.

## R3. Renomeações REST

| Antes | Depois | Quem chama |
|-------|--------|-----------|
| `PUT /add-service-order-estimate` | `PUT /service-orders/{id}/estimate` (corpo sem o id) | Angular |
| `POST /login` | `POST /auth/login` | Angular |
| `POST /logout` | `POST /auth/logout` | Angular |
| `POST /refresh-token` | `POST /auth/refresh` | Angular (interceptor) |
| `POST /register` | `POST /auth/register` + `RequireAuthorization()` | — |
| `GET /get-payments?…` | `GET /service-orders/{id}/payments` | — |
| `POST /add-payment` | `POST /service-orders/{id}/payments` | — |
| `PUT /update-payment` | `PUT /service-orders/{id}/payments/{paymentId}` | — |
| `DELETE /remove-payment` | `DELETE /service-orders/{id}/payments/{paymentId}` | — |

`GET /` ("RUNNING") fica: é a verificação de saúde do servidor.

## R4. Rotas do site (caminho mantido)

- `GET /get-service-order-for-customer/{id}/{code}` → hoje **quebrada** (MediatR sem handler).
  Reescrever como `IServiceOrderQueryHandler.GetForCustomer(id, code)` seguindo
  `old-handlers/GetServiceOrderForCustomerHandler.cs` (mesmo `EstimateServiceOrder`; código com
  trim/maiúsculas). Código errado e OS inexistente respondem igual: 404 "Essa ordem de serviço não
  existe" (o antigo diferenciava as mensagens, o que revelava se o número existe).
- `PUT /add-service-order-approve-estimate` / `-reject-estimate` (corpo `{ id, code }`) → novos
  `ApproveEstimateByCustomer(id, code)` / `RejectEstimateByCustomer(id, code)` no
  `ServiceOrderCommandHandler`: conferem o código (mesma resposta 404 se errado) e chamam
  `ApproveEstimate(id)` / `RejectEstimate(id)` já existentes (mesmo efeito da Home: WhatsApp,
  aviso ao dono, notificação). SignalR "Atualizar" no sucesso.
- `POST /v1/whatsapp/site-contact`: já funciona; só muda de arquivo.
- Todas sem `RequireAuthorization` (como hoje).

## R5. Pagamentos

- Reescrever sem MediatR seguindo `old-handlers/{Get,Add,Update,Remove}PaymentHandler.cs`, em
  `ServiceOrderCommandHandler` (`AddPayment`, `UpdatePayment`, `RemovePayment`) e
  `ServiceOrderQueryHandler` (`GetPayments`), usando os métodos de domínio que já existem
  (`ServiceOrder.AddPayment/UpdatePayment/RemovePayment`). Commands sem `IRequest`; o id da OS e do
  pagamento vêm da rota.

## R6. Frontend

- `add-service-order-estimate-modal`: `PUT /service-orders/{id}/estimate`.
- `core/auth`: `/auth/login`, `/auth/logout`, `/auth/refresh` (e o interceptor que ignora as rotas
  de auth).
- Site público: nada muda.
