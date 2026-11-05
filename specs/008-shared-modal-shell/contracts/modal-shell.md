# Contract: `app-service-order-modal` (UI)

## Inputs

| Input | Tipo | Obrigatório | Uso |
|-------|------|-------------|-----|
| `serviceOrderId` | `number` | sim | número da OS a carregar e exibir no painel |
| `title` | `string` | sim | título da barra (`mat-dialog-title`) |
| `closeLabel` | `string` | sim | `aria-label` do X (ex.: "Fechar sem responder") |
| `sending` | `boolean` | não (false) | desabilita o X enquanto o modal envia |
| `closeResult` | `unknown` | não (undefined) | valor passado ao fechar pelo X (`[mat-dialog-close]`) |

## Outputs

| Output | Payload | Quando |
|--------|---------|--------|
| `loaded` | `ServiceOrder` | a OS foi carregada com sucesso (inclusive após "Tentar de novo") |

## Slots (`ng-content select`)

| Seletor | Onde | Exemplo |
|---------|------|---------|
| `[modalBody]` | coluna da direita | `<section modalBody class="estimate">…</section>` |
| `[modalFooterInfo]` | esquerda do rodapé | `<p modalFooterInfo class="question">…</p>` |
| `[modalActions]` | direita do rodapé (dentro de `.actions`) | `<ng-container modalActions>` com Cancelar + ação |

## Uso típico

```html
<app-service-order-modal
  [serviceOrderId]="id" title="Conserto" closeLabel="Fechar sem registrar"
  [sending]="isSending()" (loaded)="serviceOrder.set($event)">

  @if (serviceOrder(); as order) {
    <section modalBody class="estimate">…resumo…</section>
  }
  <p modalFooterInfo class="question">O conserto já foi concluído?</p>
  <ng-container modalActions>
    <button matButton type="button" mat-dialog-close [disabled]="isSending()">Cancelar</button>
    <button matButton="filled" type="button" class="send" …>Consertado</button>
  </ng-container>
</app-service-order-modal>
```

## `ServiceOrderLookupService`

| Método | Chamada |
|--------|---------|
| `getById(id)` | `GET ${apiUrl}/service-orders/${id}` → `Observable<BaseResponse<ServiceOrder>>` |
