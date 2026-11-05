# Feature Specification: Rotas GET das tabelas no padrão REST

**Feature Branch**: `007-rest-get-routes`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "veja todas as rotas de get das tables e transforme todas no padrao REST"

## Contexto

As features 002 a 006 criaram as rotas de **ação** no padrão REST
(`PUT /service-orders/{id}/estimate/approve`, `.../part/purchase`, `.../repair`, `.../delivery`
etc.). As rotas de **leitura** que alimentam as 6 tabelas da Home continuam no padrão antigo, com
verbo no nome e paginação no caminho:

| Aba (tabela) | Rota atual |
|--------------|-----------|
| Orçamentos pendentes | `GET /get-pending-estimate-service-orders/{pageNumber}/{pageSize}` |
| Aguardando resposta | `GET /get-awaiting-response-service-orders/{pageNumber}/{pageSize}` |
| Aprovados | `GET /get-approved-service-orders/{pageNumber}/{pageSize}` |
| Reprovados | `GET /get-rejected-service-orders/{pageNumber}/{pageSize}` |
| Aguardando peça | `GET /get-awaiting-part-service-orders/{pageNumber}/{pageSize}` |
| Prontos | `GET /get-ready-for-pickup-service-orders/{pageNumber}/{pageSize}` |

A busca de uma OS pelo número, usada pelos 6 modais (`GET /get-service-order-by-id/{id}`),
também entra (ver Clarifications).

Esta feature padroniza essas leituras: o recurso (`service-orders`) e a fila no caminho, e a
paginação como parâmetros de consulta, no mesmo estilo das rotas de ação.

**Usuários**: as pessoas que mantêm o sistema. Para quem usa a tela, nada muda.

## Clarifications

### Session 2026-11-05

- Q: Quais rotas de leitura entram, além das 6 tabelas da Home? → A: As 6 tabelas e a busca de uma
  OS pelo número (`GET /service-orders/{id}`), usada pelos 6 modais. `get-all-service-orders` e
  `get-delivered-service-orders` ficam fora.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Leituras das tabelas no padrão REST (Priority: P1)

Quem mantém o sistema encontra as leituras das tabelas no mesmo padrão das ações: o caminho
descreve o recurso e a fila, e a paginação vai nos parâmetros de consulta.

**Why this priority**: é o objetivo do pedido. Deixa a API consistente com as rotas de ação já
criadas.

**Independent Test**: conferir que cada aba da Home chama a rota nova e que as rotas antigas não
existem mais.

**Acceptance Scenarios**:

1. **Given** a aba "Orçamentos pendentes", **When** a lista é carregada, **Then** a chamada é
   `GET /service-orders/pending-estimate?pageNumber=1&pageSize=25`, e o mesmo padrão vale para as
   outras 5 abas (`awaiting-response`, `approved`, `rejected`, `awaiting-part`, `ready-for-pickup`).
2. **Given** a API publicada, **When** se chama uma rota antiga (`/get-...-service-orders/1/25`),
   **Then** ela não existe mais.
3. **Given** uma chamada sem `pageNumber` ou `pageSize`, **Then** são usados os padrões
   `pageNumber=1` e `pageSize=25`.

---

### User Story 2 - Comportamento da tela inalterado (Priority: P1)

Quem usa a Home vê as mesmas abas, os mesmos dados, a mesma paginação e as mesmas mensagens de
antes.

**Why this priority**: a mudança só tem valor se não quebrar nada.

**Independent Test**: com a mesma base, abrir cada aba, trocar de página e de tamanho de página e
comparar com o comportamento anterior.

**Acceptance Scenarios**:

1. **Given** a mesma base de dados, **When** cada aba é aberta, **Then** os registros, valores,
   ordenação e paginação são idênticos aos de antes.
2. **Given** uma página ou tamanho inválido (menor que 1), **Then** a resposta de erro e a
   mensagem continuam as mesmas de hoje.

---

### Edge Cases

- **Rota de uma OS pelo número**: as filas usam nomes fixos (`pending-estimate` etc.) e a busca de
  uma OS por número (`/service-orders/{id}`) aceita só dígitos, então os dois caminhos não se
  confundem.
- **Parâmetros não numéricos** (`pageNumber=abc`) → recusado como requisição inválida.
- **Clientes antigos**: só a Home chama essas rotas; frontend e API são publicados juntos, então
  não há rotas de compatibilidade.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: As 6 leituras das tabelas da Home MUST ser expostas como
  `GET /service-orders/{fila}?pageNumber={n}&pageSize={n}`, com `{fila}` igual a
  `pending-estimate`, `awaiting-response`, `approved`, `rejected`, `awaiting-part` e
  `ready-for-pickup` (os termos canônicos da feature 001).
- **FR-002**: `pageNumber` e `pageSize` MUST ser parâmetros de consulta opcionais, com padrão 1 e 25.
- **FR-003**: As rotas antigas (`/get-…-service-orders/{pageNumber}/{pageSize}`) MUST ser
  removidas, sem aliases.
- **FR-004**: As respostas (campos, paginação, mensagens, códigos de erro) e a exigência de login
  MUST continuar idênticas às de hoje.
- **FR-005**: A Home MUST passar a chamar as rotas novas, sem mudança visível na tela.
- **FR-006**: A busca de uma OS pelo número, usada pelos 6 modais da Home, MUST passar de
  `GET /get-service-order-by-id/{id}` para `GET /service-orders/{id}` (só números), com a mesma
  resposta. A rota antiga MUST ser removida, e os 6 modais MUST passar a usar a nova.
- **FR-007**: A correspondência de cada fila com a consulta certa MUST continuar coberta por
  testes automatizados (os testes de handler da feature 001). Os padrões de paginação (1 e 25)
  são verificados pelo roteiro de validação, porque testar rotas HTTP exige um projeto de testes
  de integração que ainda não existe (ver plan/research R3).

### Key Entities *(include if feature involves data)*

- **Fila de OS**: cada uma das 6 abas, identificada pelo termo canônico (feature 001).
- **Página**: número da página e tamanho, com padrão 1 e 25.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 6 de 6 abas e 6 de 6 modais da Home carregam pelas rotas novas.
- **SC-002**: 0 rotas de leitura das tabelas com verbo no nome ou paginação no caminho.
- **SC-003**: Com a mesma base, 100% das abas mostram os mesmos registros e a mesma paginação de
  antes.
- **SC-004**: Build e testes passam sem falhas.

## Assumptions

- **Formato**: `GET /service-orders/{fila}` com paginação por parâmetros de consulta, seguindo o
  prefixo `/service-orders` das rotas de ação (002 a 006). Os nomes das filas são os termos
  canônicos da feature 001, em kebab-case.
- **Sem mudança de dados**: as consultas, os filtros e os formatos de resposta continuam os mesmos.
- **Fora do escopo**: `get-all-service-orders` e `get-delivered-service-orders` (sem tela e hoje
  quebradas; ver Clarifications), as rotas de busca (`get-service-orders-by-customer-name`, `-by-serial-number`,
  `-by-model`, `-by-enterprise`, `-by-date`), a rota do cliente (`get-service-order-for-customer`)
  e a de pagamentos (`get-payments`), que não alimentam tabelas da Home.
- **Publicação conjunta**: frontend e API vão juntos.
