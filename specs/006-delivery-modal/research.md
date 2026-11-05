# Research: Modal "Entregue" na aba "Prontos"

Nenhum `NEEDS CLARIFICATION` em aberto (o PDF foi resolvido na spec: nova aba, com download como alternativa).

## R1. Comportamento: igual ao handler antigo

- **Decision**: `IServiceOrderCommandHandler.RegisterDelivery(long id)` retorna
  `Task<BaseResponse<byte[]>>` (como o antigo) e reproduz
  `old-handlers/AddServiceOrderDeliveryAndReturnPdfHandler.cs`, na mesma ordem:
  1. `GetByIdForUpdateAsync(id)`; se `Data == null` → `BaseResponse<byte[]>(null, 404, "Essa ordem de serviço não existe")`;
  2. `serviceOrder.AddDelivery()` + `await UpdateAsync(serviceOrder)`, sem checar o resultado;
  3. se `RepairStatus == Approved && RepairResult == Repair`:
     - `var pdf = await _pdfGenerator.GenerateCheckOutDocumentAsync(serviceOrder)`;
     - se `!pdf.IsSuccess` → `BaseResponse<byte[]>(null, 500, pdf.Message)`, sem WhatsApp (a entrega já está salva);
     - `Enqueue(new SendGuaranteeMessageRequest(Id.ToString(), Customer.Name.CustomerName, Guarantee!.ServiceOrderGuarantee, Customer.Phone.CustomerPhone))` para todas as empresas;
     - retornar `pdf` (bytes + código e mensagem do gerador);
  4. senão → `BaseResponse<byte[]>(null, 200, "Ordem de serviço entregue com sucesso!")`.
- Sem notificação interna. `Guarantee!` é mantido como no antigo: o `AddEstimate` sempre cria a
  garantia (vazia se não informada), então toda OS consertada tem uma.
- **Dependência nova do handler**: `IGenerateServiceOrderPdf` (Application/Interfaces/Services,
  já registrado como Transient). O construtor do `ServiceOrderCommandHandler` ganha esse
  parâmetro, e os testes passam a usar um fake.

## R2. Endpoint com arquivo

- **Decision**: `PUT /service-orders/{id:long}/delivery`:
  ```text
  result = await handler.RegisterDelivery(id)
  if (!result.IsSuccess) return result.ToResult();
  await hub "Atualizar"
  if (result.Data != null) return Results.File(result.Data, "application/pdf", $"ordem_servico_{id}.pdf");
  return result.ToResult();
  ```
  Igual à rota antiga, com duas diferenças: os erros saem com o status HTTP certo (via
  `ToResult()`), e o caso sem PDF devolve a resposta padrão com a mensagem (a antiga devolvia 200
  vazio). Esta é a única rota do escopo que não termina sempre em `ToResult()`, porque precisa
  devolver o arquivo.
- Remover `PUT /add-service-order-delivery` e `AddServiceOrderDeliveryAndReturnPdfCommand`.

## R3. Frontend: PDF em nova aba sem bloqueio de pop-up

- **Problem**: `window.open` chamado depois de uma requisição assíncrona costuma ser bloqueado.
- **Decision**:
  1. no clique em **Entregue**, se a OS carregada `isRepaired` (`repairStatus === ERepairStatus.Approved && repairResult === ERepairResult.Repair`),
     abrir **na hora** `const pdfWindow = window.open('', '_blank')` (pode ser `null` se bloqueado);
  2. a requisição usa `HttpClient.put(url, {}, { observe: 'response', responseType: 'blob' })`;
  3. sucesso com `content-type` `application/pdf` → `URL.createObjectURL(blob)`; se `pdfWindow`
     existir, escrever nele uma página com um `<iframe>` em tela cheia apontando para a URL e,
     no `load` do iframe (+300 ms), chamar `iframe.contentWindow.print()` (impressão automática;
     o PDF aberto direto na aba não permite chamar `print()`); senão, download via `<a download="ordem_servico_{id}.pdf">`;
     revogar a URL depois de 60 s;
  4. sucesso sem PDF ou erro → `pdfWindow?.close()`;
  5. erro: o corpo vem como `Blob`; ler com `await blob.text()` e `JSON.parse` para extrair
     `message` (com fallback "Não foi possível registrar a entrega. Tente de novo.").
- **Alternatives considered**: abrir a aba só depois da resposta. Rejeitado por causa do bloqueio
  de pop-up.

## R4. Correção de tipagem no frontend

- **Finding**: `web/src/app/shared/models/service-order.interface.ts` declara
  `repairStatus: ERepairResult`, mas a API devolve `ERepairStatus` (número). O R3 depende desse
  campo.
- **Decision**: corrigir para `repairStatus: ERepairStatus`. Nenhum outro código lê esse campo do
  `ServiceOrder` compartilhado (verificado com grep), então a mudança é segura.

## R5. Modal

- `modals/delivery/` (`DeliveryModalComponent` + `DeliveryModalService`), copiando `modals/repair/`:
  título "Entrega", resumo "Resumo da entrega" (Resultado, Solução, Valor total, Garantia,
  "Consertado em" = `repairDate`), rodapé "O cliente já retirou o aparelho?" com Cancelar e
  **Entregue**. Mensagem de sucesso: "Ordem de serviço entregue.". Quando houver PDF, uma dica
  abaixo da pergunta: "O termo de entrega será aberto para impressão."

## R6. Testes (Princípio IV)

- Fake novo: `FakeServiceOrderPdfGenerator` (`IGenerateServiceOrderPdf`) com `CheckOutResponse`
  configurável e contador de chamadas; os outros métodos lançam `NotImplementedException`.
- `ServiceOrderCommandHandlerTests`:
  - não encontrada → 404 e sem PDF;
  - consertada (Particular e PhilcoBritania) → `Delivered`, `DeliveryDate` preenchida, PDF
    gerado 1 vez, retorno = bytes do fake, 1 `SendGuaranteeMessageRequest("1", "JOÃO DA SILVA", "90 DIAS", "41999999999")`;
  - consertada e o PDF falha → 500 com a mensagem do fake, entrega salva (`LastUpdated` preenchido), 0 WhatsApp;
  - não consertada (reprovada e liberada) → 200 "Ordem de serviço entregue com sucesso!", `Data` nulo, PDF não gerado, 0 WhatsApp.
- Domain (`ServiceOrderDeliveryTests`): `AddDelivery()` → `Delivered` e `DeliveryDate` preenchida.
