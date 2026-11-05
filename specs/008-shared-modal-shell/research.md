# Research: Estrutura compartilhada dos modais

Nenhum `NEEDS CLARIFICATION` em aberto. Angular 22 (signal inputs/outputs, `@if`/`@for`, standalone).

## R1. Componente "casca" com projeção de conteúdo

- **Decision**: `ServiceOrderModalComponent` (selector `app-service-order-modal`) em
  `web/src/app/features/home/modals/shared/`. Ele renderiza a barra superior, os estados de
  carregamento e erro, o painel lateral (via `ServiceOrderTicketComponent`) e o rodapé. Cada modal
  projeta três áreas:
  - `[modalBody]`: resumo ou formulário (coluna da direita);
  - `[modalFooterInfo]`: pergunta, dica ou total (esquerda do rodapé);
  - `[modalActions]`: botões (direita do rodapé).
- **Rationale**: `ng-content` com `select` é o jeito padrão de compor uma moldura no Angular, e
  cada modal continua dono da própria ação e do próprio estado.
- **Alternatives considered**:
  - Um modal genérico configurado por dados (lista de campos e ações): rejeitado, porque o
    orçamento tem formulário e a entrega tem fluxo de PDF; a configuração ficaria mais complexa
    que o código atual.
  - Herança de classe base: rejeitado, porque não resolve o template nem os estilos.

## R2. Carregamento da OS dentro da casca

- **Decision**: a casca recebe `serviceOrderId` e faz a carga sozinha (`isLoading`, `serviceOrder`,
  estado de erro, "Tentar de novo"), usando o serviço único de R4. Quando a OS chega, a casca emite
  `(loaded)` com ela. O modal guarda a OS num `signal` próprio para usar no conteúdo projetado (ex.:
  `isRepaired` da entrega, `prefill` do orçamento).
- **Rationale**: a carga, o erro e o retry eram ~30 linhas idênticas em cada um dos 6 componentes.
  Isso também cobre o FR-004: o "Tentar de novo" é tratado dentro da própria casca, sem o modal
  precisar reagir.
- **Detalhe**: o conteúdo projetado é sempre instanciado pelo Angular (mesmo dentro de `@if` na
  casca). Por isso cada modal envolve o próprio conteúdo em `@if (serviceOrder(); as order)`.

## R3. Estilos compartilhados

- **Problem**: com o encapsulamento padrão, os estilos da casca não alcançam o conteúdo projetado
  (`.estimate-details`, `.question`, `.send`), que pertence ao template do modal.
- **Decision**: a casca usa `ViewEncapsulation.None`, com **todos** os seletores prefixados por
  `app-service-order-modal` (ex.: `app-service-order-modal .ticket`). Assim os estilos valem para o
  conteúdo projetado sem vazar para o resto do app. Os overrides de botão e spinner do Material
  (variáveis CSS) ficam no seletor `app-service-order-modal` e são herdados pelos botões
  projetados.
- **Moves to the shell**: barra, close, state, body/columns, ticket*, `.estimate`, `.estimate-title`,
  `.estimate-details`, `.estimate-amount`, `.question`, `.summary`, `.actions`, `.send mat-spinner`,
  `.sr-only`, `prefers-reduced-motion`.
- **Fica em cada modal**: só o que é dele: `.reject` (resposta), `.hint` (entrega), e campos e
  totais do orçamento (`.fields`, `.result`, `.money`, `.warranty`, `.presets`, `.total*`).
- **Budget**: o limite `anyComponentStyle` (4 kB) passa a valer para a casca (um arquivo só) e para
  cada modal; os 5 modais simples ficam bem abaixo. O orçamento pode continuar acima por causa do
  formulário (aceito na spec).

## R4. Busca única da OS

- **Decision**: `ServiceOrderLookupService` (`providedIn: 'root'`) em
  `web/src/app/features/home/modals/shared/service-order-lookup.service.ts`, com
  `getById(id): Observable<BaseResponse<ServiceOrder>>` → `GET ${apiUrl}/service-orders/${id}`.
  Os 6 serviços de modal perdem o `getServiceOrderById` e ficam só com a ação. O serviço da
  compra da peça, da liberação e do conserto ficam com 1 método cada.

## R5. Formulário do orçamento

- **Decision**: o modal de orçamento envolve a casca inteira com o formulário:
  `<form [formGroup]="form" (ngSubmit)="sendEstimate()" class="modal-form"><app-service-order-modal …>…</app-service-order-modal></form>`,
  com `.modal-form { display: contents; }` no estilo do orçamento, para o layout em flex não
  mudar. O botão "Enviar orçamento" continua `type="submit"`, e os botões de fechar da casca são
  `type="button"`.

## R6. Fechar com valor de retorno

- **Decision**: a casca recebe `closeResult` (padrão `undefined`) e o usa em `[mat-dialog-close]`
  no X. O modal de resposta passa `answeredElsewhere()`. O botão Cancelar fica no `[modalActions]`
  de cada modal, que já controla o próprio valor.

## R7. Testes

- O projeto não tem testes de frontend (Karma/Jest não configurados para esses componentes), e a
  feature não cria regra de negócio. A validação é: `ng build` (incluindo os limites de estilo),
  medição de linhas (SC-002) e o roteiro manual dos 6 modais (quickstart).
