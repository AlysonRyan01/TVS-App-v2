# Implementation Plan: Estrutura compartilhada dos modais da Home

**Branch**: `008-shared-modal-shell` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/008-shared-modal-shell/spec.md`

## Summary

Extrair a moldura comum dos 6 modais da Home para uma casca compartilhada
(`app-service-order-modal`). A casca cuida de: barra superior, carregamento da OS, erro com
"Tentar de novo", painel lateral (`app-service-order-ticket`) e rodapé. Cada modal projeta o próprio
conteúdo, a informação do rodapé e os botões. A busca da OS pelo número passa a ser um serviço
único, e os estilos comuns vão para a casca (sem encapsulamento, com prefixo de seletor) ([research.md](research.md)).

## Technical Context

**Language/Version**: TypeScript / Angular 22 (standalone, signals, `input()`/`output()`, `@if`)

**Primary Dependencies**: Angular Material (dialog, button, progress spinner)

**Storage**: N/A

**Testing**: sem testes de frontend no projeto; validação por `ng build` (inclusive os limites de estilo), contagem de linhas e roteiro manual (R7)

**Target Platform**: SPA no navegador

**Project Type**: web application (só `web/` muda)

**Performance Goals**: N/A (CSS menor por componente)

**Constraints**: zero diferença visual ou de comportamento; estilos dos 5 modais simples < 4 kB

**Scale/Scope**: 1 casca + 1 painel + 1 serviço novos; 6 modais refatorados; 6 serviços reduzidos

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Só frontend; o backend não muda. |
| II. Domain isolado | ✅ | N/A. |
| III. EF Core + migrations | ✅ | N/A. |
| IV. Regras de negócio testadas | ✅ | Sem regra de negócio nova; os testes do backend continuam valendo. |
| V. UI em português | ✅ | Todos os textos continuam iguais (US2). |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/008-shared-modal-shell/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/modal-shell.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
web/src/app/features/home/modals/
├── shared/                                              # NOVO
│   ├── service-order-modal.component.{ts,html,scss}     # casca (ViewEncapsulation.None)
│   ├── service-order-ticket.component.{ts,html}         # painel lateral (estilos na casca)
│   └── service-order-lookup.service.ts                  # getById(id)
├── estimate/                    # usa a casca dentro do <form>; mantém estilos do formulário
├── estimate-response/           # usa a casca; mantém .reject
├── part-purchase/               # usa a casca; sem estilos próprios
├── rejected-release/            # usa a casca; sem estilos próprios
├── repair/                      # usa a casca; sem estilos próprios
└── delivery/                    # usa a casca; mantém .hint
```

Os serviços de cada modal ficam só com a ação (`addServiceOrderEstimate`, `approveEstimate`/`rejectEstimate`,
`registerPartPurchase`, `markAsReadyForPickup`, `registerRepair`, `registerDelivery`).

**Structure Decision**: a pasta `modals/shared/` fica junto dos modais que a usam (todos da Home).

### Ordem de implementação sugerida

1. `shared/`: serviço, painel e casca (template + estilos movidos do `repair-modal.component.scss`).
2. Migrar o modal mais simples (`repair`) e validar visualmente.
3. Migrar `part-purchase`, `rejected-release`, `estimate-response`, `delivery`.
4. Migrar `estimate` (formulário em volta da casca).
5. Build, contagem de linhas e roteiro manual.

## Complexity Tracking

Nenhuma violação. Observação: `ViewEncapsulation.None` na casca é deliberado (R3); o risco de
vazar estilos é contido pelo prefixo `app-service-order-modal` em todos os seletores.
