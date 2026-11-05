# Quickstart: validação da padronização da Home

## Pré-requisitos

- .NET 9 SDK, Node + Angular CLI, SQL Server (via `docker-compose.yml`) com dados de OS em
  cada fila.
- Antes de começar: anotar (ou fazer screenshot de) o conteúdo das 6 abas na base de teste,
  para comparar no passo 4.

## 1. Build e testes

```bash
dotnet build TVSApp.sln
dotnet test src/TVSApp.Tests
cd web && npx ng build
```

Esperado: os três terminam sem erros. Os testes de `ServiceOrderCommandHandlerTests` e
`ServiceOrderQueryHandlerTests` passam ([research.md](research.md) R6).

## 2. Sem MediatR no escopo

```bash
grep -n "ISender" src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs | grep -i "estimate-service-order\|add-service-order-estimate"
grep -n "IRequest" src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/AddServiceOrderEstimateCommand.cs
```

Esperado: nenhuma saída.

## 3. Termos antigos eliminados (SC-002)

```bash
grep -rnE "Refused|Reproved|PendingPurchase|pending-purchase|WaitingPickup|waiting-pickup|\bWaitingPart|\bwaiting-part|\bWaitingResponse|\bwaiting-response|-ordens|ApprovedIn|RefusedIn|InspectionIn|PurchasedIn|Quote|/quote|-quote|myTab" \
  web/src/app/features/home src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs \
  src/TVSApp.Application/Handlers src/TVSApp.Domain/Repositories \
  src/TVSApp.Infrastructure/Repositories/ServiceOrders
```

Esperado: nenhuma saída. Exceções aceitas: textos de UI em português e enums de domínio fora do
escopo, como `ERepairStatus.Waiting`.

## 4. Comportamento inalterado (US3 / SC-004)

1. Subir a API (`dotnet run --project src/TVSApp.Api`) e o Angular (`cd web && npx ng serve`).
2. Abrir a Home e passar pelas 6 abas. Conferir se registros, colunas, valores e ordenação
   batem com o que foi anotado antes.
3. Em uma aba com mais de 25 OS, trocar de página e de tamanho de página.
4. Em "Orçamentos pendentes", clicar em **ORÇAMENTO**, preencher (incluindo garantia) e enviar.
   - Esperado: sucesso, a lista recarrega e a OS sai da fila.
   - Esperado: a OS aparece em "Aguardando resposta" (com conserto) ou em "Prontos"
     (sem conserto ou sem defeito).
   - Esperado: no detalhe da OS e no PDF, a garantia informada aparece.
5. Derrubar a API e trocar de aba. Esperado: a mesma mensagem de erro em português de antes.

## 5. Contrato (SC-003)

No DevTools (aba Network), conferir se o JSON de cada fila tem exatamente os campos de
[data-model.md](data-model.md) e se o PUT de orçamento envia `warranty` (e não `guarantee`).
