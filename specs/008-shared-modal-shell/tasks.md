---

description: "Task list for 008-shared-modal-shell"
---

# Tasks: Estrutura compartilhada dos modais da Home

**Input**: Design documents from `/specs/008-shared-modal-shell/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/modal-shell.md, quickstart.md

**Tests**: sem testes automatizados (research.md R7). A validação é feita com `ng build` (com os
limites de estilo), contagem de linhas e roteiro manual.

**Organization**: a Fase 2 cria a casca, o painel e o serviço. US1 migra os 6 modais. US2 valida
que nada mudou para quem usa.

## Format: `[ID] [P?] [Story] Description`

## Regras gerais para todas as tarefas

- Base: `web/src/app/features/home/modals/`.
- Não mudar nenhum texto visível, classe de layout ou comportamento (US2). Copiar a marcação e os
  estilos atuais em vez de reescrever.
- Signal inputs/outputs (`input()`, `input.required()`, `output()`), componentes standalone.

---

## Phase 1: Setup

- [X] T001 Rodar `cd web && npx ng build` e anotar os avisos de limite de estilo atuais; rodar `wc -l web/src/app/features/home/modals/*/*.component.* web/src/app/features/home/modals/*/*.service.ts | tail -1` e anotar o total (base ~3.900)
- [X] T002 [P] Criar a pasta `web/src/app/features/home/modals/shared/`

---

## Phase 2: Foundational (casca, painel e serviço)

**⚠️ CRITICAL**: nenhum modal é migrado antes desta fase.

- [X] T003 [P] Criar `shared/service-order-lookup.service.ts`: `@Injectable({ providedIn: 'root' })` `ServiceOrderLookupService` com `getById(id: number): Observable<BaseResponse<ServiceOrder>>` → `this._http.get<BaseResponse<ServiceOrder>>(\`${environment.apiUrl}/service-orders/${id}\`)`
- [X] T004 [P] Criar `shared/service-order-ticket.component.ts` + `.html` (selector `app-service-order-ticket`, `ViewEncapsulation.None`, sem `styleUrl`): inputs `order = input.required<ServiceOrder>()` e `serviceOrderId = input.required<number>()`; template = o `<aside class="ticket" aria-label="Dados da ordem de serviço">…</aside>` copiado exatamente de `repair/repair-modal.component.html` (trocando `order` por `order()` e `id` por `serviceOrderId()`); imports `EnumLabelPipe`
- [X] T005 Criar `shared/service-order-modal.component.ts` (selector `app-service-order-modal`, `ViewEncapsulation.None`): inputs `serviceOrderId = input.required<number>()`, `title = input.required<string>()`, `closeLabel = input.required<string>()`, `sending = input(false)`, `closeResult = input<unknown>(undefined)`; output `loaded = output<ServiceOrder>()`; signals `isLoading`, `serviceOrder`; `ngOnInit` → `loadServiceOrder()`; `loadServiceOrder()` igual ao atual dos modais (usa `ServiceOrderLookupService.getById`, snackbar em erro, mensagens "Ordem de serviço não encontrada." e "Erro ao carregar a ordem de serviço.") e, em sucesso, `this.serviceOrder.set(data)` + `this.loaded.emit(data)`; imports `MatDialogModule`, `MatButtonModule`, `MatProgressSpinnerModule`, `ServiceOrderTicketComponent` (depende de T003, T004)
- [X] T006 Criar `shared/service-order-modal.component.html` conforme `data-model.md`: `header.bar` (`<h1 class="bar-title" mat-dialog-title>{{ title() }}</h1>`, X `type="button"` com `[mat-dialog-close]="closeResult()"`, `[disabled]="sending()"`, `[attr.aria-label]="closeLabel()"`); `@if (isLoading())` spinner; `@else if (serviceOrder(); as order)` → `.body` > `.columns` (`<app-service-order-ticket [order]="order" [serviceOrderId]="serviceOrderId()" />` + `<ng-content select="[modalBody]" />`) + `footer.summary` (`<ng-content select="[modalFooterInfo]" />` + `<div class="actions"><ng-content select="[modalActions]" /></div>`); `@else` estado de erro idêntico ao atual, com "Tentar de novo" chamando `loadServiceOrder()` (botões `type="button"`) (depende de T005)
- [X] T007 Criar `shared/service-order-modal.component.scss` movendo de `repair/repair-modal.component.scss` todas as regras (variáveis, `:host` → `app-service-order-modal`, `.bar`, `.close`, `.state*`, `.body`, `.columns`, `.ticket*`, `.mono`, `.estimate*`, `.question`, `.summary`, `.actions`, `.send mat-spinner`, `.sr-only`, `prefers-reduced-motion`), prefixando **cada** seletor com `app-service-order-modal ` (e o antigo `:host` vira o próprio `app-service-order-modal` com `display: flex; flex-direction: column; height: 100%;`); trocar `:host ::ng-deep *` por `app-service-order-modal *` (depende de T006)
- [X] T008 Rodar `cd web && npx ng build`

**Checkpoint**: a casca compila (ainda sem uso).

---

## Phase 3: User Story 1 - Uma única estrutura de modal (Priority: P1) 🎯 MVP

**Goal**: os 6 modais usam a casca; sem duplicação de painel, estados, estilos ou busca.

**Independent Test**: quickstart.md §1, §2 e §3.

- [X] T009 [US1] Migrar `repair/`: no `.ts`, remover `isLoading`, `loadServiceOrder`, `ngOnInit`, a injeção de `MAT_DIALOG_DATA` continua (`id`), manter `serviceOrder = signal<ServiceOrder | null>(null)`, `isSending`, `registerRepair()`; importar `ServiceOrderModalComponent`; no `.html`, usar `<app-service-order-modal [serviceOrderId]="id" title="Conserto" closeLabel="Fechar sem registrar" [sending]="isSending()" (loaded)="serviceOrder.set($event)">` com `@if (serviceOrder(); as order) { <section modalBody class="estimate" …>…resumo atual…</section> }`, `<p modalFooterInfo class="question">O conserto já foi concluído?</p>` e `<ng-container modalActions>` com Cancelar e Consertado como hoje; apagar `repair-modal.component.scss` e o `styleUrl`; no `repair-modal.service.ts`, remover `getServiceOrderById` (depende de T008)
- [ ] T010 [US1] Conferir visualmente o modal Conserto (quickstart §4, linha "Aguardando peça") antes de seguir para os outros (depende de T009)
- [X] T011 [P] [US1] Migrar `part-purchase/` como T009 (título "Compra da peça", closeLabel "Fechar sem registrar", pergunta "A peça já foi comprada?", botão Comprado); apagar o `.scss` e o `styleUrl`; remover `getServiceOrderById` do serviço (depende de T010)
- [X] T012 [P] [US1] Migrar `rejected-release/` como T009 (título "Liberação", closeLabel "Fechar sem liberar", pergunta "O aparelho já está pronto para retirada?", botão Liberado); apagar o `.scss` e o `styleUrl`; remover `getServiceOrderById` do serviço (depende de T010)
- [X] T013 [P] [US1] Migrar `estimate-response/` como T009 (título "Resposta", closeLabel "Fechar sem responder", `[closeResult]="answeredElsewhere()"`, pergunta "O cliente aprovou o orçamento?", Cancelar com `[mat-dialog-close]="answeredElsewhere()"`, Reprovado e Aprovado); manter no `.scss` só o bloco `.reject` (com os imports de `@use` necessários); remover `getServiceOrderById` do serviço (depende de T010)
- [X] T014 [P] [US1] Migrar `delivery/` como T009 (título "Entrega", closeLabel "Fechar sem entregar"; `modalFooterInfo` = o `<div>` com a pergunta "O cliente já retirou o aparelho?" e a `.hint` condicional; `isRepaired` passa a ler o `serviceOrder` preenchido pelo `(loaded)`; manter todo o fluxo de PDF e impressão); manter no `.scss` só o `.hint`; remover `getServiceOrderById` do serviço (depende de T010)
- [X] T015 [US1] Migrar `estimate/`: envolver a casca com `<form class="modal-form" [formGroup]="form" (ngSubmit)="sendEstimate()">`; `<app-service-order-modal … title="Orçamento" closeLabel="Fechar sem salvar" [sending]="isSending()" (loaded)="onLoaded($event)">`; `onLoaded(order)` faz `serviceOrder.set(order)` + `prefill(order)` (substitui `loadServiceOrder`/`ngOnInit`); `modalBody` = a `div.fields` atual; `modalFooterInfo` = a `div.total` atual; `modalActions` = Cancelar + "Enviar orçamento" (`type="submit"`); no `.scss`, remover tudo o que foi para a casca e manter `.modal-form { display: contents; }`, os overrides de form-field e button-toggle (em `:host` → mover para `.modal-form`) e os estilos de `.fields`, `.result`, `.money`, `.warranty`, `.presets`, `.preset`, `.total*`; remover `getServiceOrderById` do serviço (depende de T010)
- [X] T016 [US1] Rodar `cd web && npx ng build` e quickstart.md §1, §2 e §3 (sem avisos de limite nos 5 modais simples e na casca; total de linhas ≤ ~1.950; `ticket-device` só em `shared/`; nenhum `getServiceOrderById`)

**Checkpoint**: estrutura única; build verde.

---

## Phase 4: User Story 2 - Telas idênticas para quem usa (Priority: P1)

**Goal**: zero diferença visual ou de comportamento.

**Independent Test**: quickstart.md §4.

- [ ] T017 [US2] Executar quickstart.md §4 para os 6 modais (tela larga e estreita; fluxo completo de cada um; X/Cancelar/Esc bloqueados durante o envio; resposta 409 recarregando a lista; PDF com impressão na entrega; Enter enviando o orçamento)
- [ ] T018 [US2] Executar o teste de erro e "Tentar de novo" (API derrubada ao abrir um modal, depois de volta)

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T019 [P] Remover imports não usados nos 6 componentes e serviços (ex.: `finalize`, `HttpErrorResponse`, `OnInit`, `CurrencyPipe` onde não forem mais usados) e rodar `cd web && npx ng build`
- [X] T020 [P] Atualizar `specs/008-shared-modal-shell/quickstart.md` §2 com o total de linhas medido

---

## Dependencies & Execution Order

- Setup → Foundational (T003 ∥ T004 → T005 → T006 → T007 → T008) → US1 (T009 → T010 → T011–T015) → US2 → Polish
- Os modais T011–T015 são independentes entre si (pastas diferentes) depois de validar o primeiro (T010).

### Parallel Opportunities

- T003 ∥ T004
- T011 ∥ T012 ∥ T013 ∥ T014 ∥ T015
- T019 ∥ T020

## Parallel Example: User Story 1

```bash
Task: "T011 migrar part-purchase/"
Task: "T012 migrar rejected-release/"
Task: "T013 migrar estimate-response/"
Task: "T014 migrar delivery/"
Task: "T015 migrar estimate/"
```

## Implementation Strategy

1. Casca + painel + serviço (Fase 2)
2. Migrar o Conserto e **validar visualmente** (T010) antes de replicar
3. Migrar os outros 5
4. Validação completa (US2)
