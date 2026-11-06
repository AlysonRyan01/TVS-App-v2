# HTTP Contracts: rotas que mudam

| Rota nova | Corpo / parâmetros | Resposta |
|-----------|-------------------|----------|
| `PUT /service-orders/{id}/estimate` | `AddServiceOrderEstimateCommand` sem `serviceOrderId` | igual à rota antiga |
| `POST /auth/login` | `{ email, password }` | `BaseResponse<AuthTokensDto>` (igual) |
| `POST /auth/logout` | igual à antiga | igual |
| `POST /auth/refresh` | `{ refreshToken }` | igual |
| `POST /auth/register` | `{ email, password }` + token | igual; 401 sem login |
| `GET /service-orders/{id}/payments` | — | lista de pagamentos (handler antigo) |
| `POST /service-orders/{id}/payments` | `{ amount, paymentDate }` | handler antigo |
| `PUT /service-orders/{id}/payments/{paymentId}` | `{ amount, paymentDate }` | handler antigo |
| `DELETE /service-orders/{id}/payments/{paymentId}` | — | handler antigo |

## Site público (caminho igual; comportamento)

| Rota | Corpo | Resposta |
|------|-------|----------|
| `GET /get-service-order-for-customer/{id}/{code}` | — | 200 `EstimateServiceOrder`; 404 "Essa ordem de serviço não existe" |
| `PUT /add-service-order-approve-estimate` | `{ id, code }` | 200 `ServiceOrderDto`; 404 igual; 409 já respondido |
| `PUT /add-service-order-reject-estimate` | `{ id, code }` | idem |
| `POST /v1/whatsapp/site-contact` | form-data (sem mudança) | sem mudança |

## Removidas
As 9 da spec (clientes paginados/por id, entregues, por empresa, abandonadas, 3 de notificações,
e-mail) e os caminhos antigos renomeados acima.
