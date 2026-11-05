# Feature Specification: Modal "Comprado" na aba "Aprovados"

**Feature Branch**: `003-part-purchased-modal`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "agora vamos para o tab \"Aprovados\" da home, vamos adicionar a coluna \"acao\" e dentro dele o botao \"COMPRADO\", esse botao vai abrir o modal com as informacoes padroes como os outros modais e o botao \"Comprado\" que vai chamar o endpoint REST de aprovacao"

## Contexto

A aba **Aprovados** lista as OS com orçamento aprovado (pelo cliente ou automaticamente, no caso
da PhilcoBritania) cuja peça ainda não foi comprada. Depois que a peça é comprada, a OS deve ir
para a aba **Aguardando peça**. Hoje não há como registrar essa compra pela tela.

A API já tem uma rota para registrar a compra da peça (`add-service-order-purchased-part`), mas
ela está quebrada: depende de um handler que foi removido, o mesmo defeito corrigido nas
features 001 e 002.

Esta feature adiciona à aba "Aprovados" uma coluna **Ação** com o botão **COMPRADO**. O botão abre
um modal no mesmo padrão dos modais de orçamento e de resposta, com o botão **Comprado** para
registrar que a peça foi comprada.

**Atores**: atendente ou técnico da assistência, logado no sistema.

## Clarifications

### Session 2026-11-05

- Q: O que o botão Comprado executa? → A: O mesmo que o handler antigo
  `old-handlers/AddPurchasedPartHandler.cs`: buscar a OS (404 "Essa ordem de serviço não
  existe" se não existir), registrar a compra da peça (situação "Aguardando peça" + data da
  compra), salvar e responder "Compra de peça adicionada com sucesso!". O handler antigo não tem
  outras validações, notificação nem WhatsApp. A rota segue o estilo REST pedido:
  `PUT /service-orders/{id}/part/purchase`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registrar a compra da peça (Priority: P1)

A peça de uma OS aprovada foi comprada. A pessoa abre a aba "Aprovados", clica em **COMPRADO** na
linha da OS, confere os dados no modal e clica em **Comprado**. A OS sai de "Aprovados" e passa a
aparecer em "Aguardando peça", com a data da compra.

**Why this priority**: é o próximo passo do fluxo depois da aprovação. Sem ele, as OS aprovadas
ficam paradas e o acompanhamento da chegada da peça não funciona.

**Independent Test**: com uma OS em "Aprovados", abrir o modal, clicar em Comprado e conferir que
a OS some dessa aba e aparece em "Aguardando peça".

**Acceptance Scenarios**:

1. **Given** uma OS na aba "Aprovados", **When** a pessoa clica em **COMPRADO**, **Then** abre o
   modal com o número da OS, os dados do cliente e do aparelho e o resumo do orçamento aprovado
   (solução, valor total, custo da peça, garantia e data da aprovação).
2. **Given** o modal aberto, **When** a pessoa clica em **Comprado**, **Then** a compra é
   registrada com a data atual, aparece a mensagem "Compra da peça registrada.", o modal fecha
   e a lista de "Aprovados" é recarregada sem essa OS.
3. **Given** uma OS com a compra registrada, **When** se abre a aba "Aguardando peça", **Then** a
   OS aparece lá com a data da compra.
4. **Given** uma compra registrada, **Then** as outras telas abertas são avisadas para atualizar.

---

### User Story 2 - Fechar sem registrar e tratar erros (Priority: P2)

A pessoa abre o modal por engano ou a peça ainda não chegou ao fornecedor. Ela fecha o modal e nada
muda. Se algo der errado, vê uma mensagem clara e pode tentar de novo.

**Why this priority**: evita registro de compra por engano e deixa claro quando a ação falhou.

**Independent Test**: abrir e fechar o modal (Cancelar, X ou Esc) e conferir que a OS continua em
"Aprovados"; derrubar a API, clicar em Comprado e conferir a mensagem de erro com o modal aberto.

**Acceptance Scenarios**:

1. **Given** o modal aberto, **When** a pessoa clica em **Cancelar**, no **X** ou aperta **Esc**,
   **Then** o modal fecha e nada é registrado.
2. **Given** a pessoa clicou em Comprado, **Then** os botões ficam desabilitados e mostram que a
   ação está em andamento até a resposta, evitando cliques duplos.
3. **Given** uma falha ao registrar, **Then** aparece a mensagem do sistema ou "Não foi possível
   registrar a compra. Tente de novo.", o modal continua aberto e os botões voltam a ficar
   habilitados.
4. **Given** uma falha ao carregar a OS, **Then** o modal mostra o mesmo estado de erro dos outros
   modais ("Não foi possível carregar a OS {número}…", com os botões Fechar e Tentar de novo).

---

### Edge Cases

- **Compra já registrada** (outra pessoa ou outra aba): como no handler antigo, não há
  validação extra. A compra é registrada de novo com a data atual e a OS continua em
  "Aguardando peça". O modal só abre a partir da aba "Aprovados", onde só aparecem OS sem compra.
- **OS inexistente**: "Essa ordem de serviço não existe", exibida no modal.
- **Lista vazia**: a coluna Ação aparece normalmente e a mensagem de lista vazia continua igual.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A tabela da aba "Aprovados" MUST ter uma nova última coluna **Ação** com o botão
  **COMPRADO** em cada linha, no mesmo estilo dos botões ORÇAMENTO e RESPOSTA.
- **FR-002**: O botão MUST abrir, em tela cheia, o modal com o mesmo padrão dos modais de
  orçamento e de resposta: cabeçalho com título **Compra da peça** e botão de fechar; painel
  lateral com OS, cliente, telefones, aparelho e defeito; estados de carregamento e de erro iguais.
- **FR-003**: O modal MUST exibir, somente para leitura, o resumo do orçamento aprovado: solução,
  valor total, custo da peça, garantia e data da aprovação.
- **FR-004**: O rodapé MUST ter os botões **Cancelar** e **Comprado** (ação principal).
- **FR-005**: Ao clicar em **Comprado**, o sistema MUST registrar a compra da peça: a data da compra
  passa a ser a data atual e a OS passa para a situação "Aguardando peça", aparecendo nessa aba e
  saindo de "Aprovados".
- **FR-006**: A ação MUST reproduzir exatamente o handler antigo
  `old-handlers/AddPurchasedPartHandler.cs`: OS inexistente → "Essa ordem de serviço não existe"
  (404); senão, registrar a compra da peça, salvar e responder "Compra de peça adicionada com
  sucesso!". Sem validações extras, sem notificação interna e sem WhatsApp.
- **FR-007**: A ação MUST ser exposta como `PUT /service-orders/{id}/part/purchase` (sem corpo), no
  mesmo estilo REST das rotas de aprovação e reprovação. A rota antiga
  `add-service-order-purchased-part` e o command dela MUST ser removidos.
- **FR-008**: Após o registro com sucesso, o sistema MUST mostrar "Compra da peça registrada.",
  fechar o modal, recarregar a lista de "Aprovados" e avisar as outras telas para atualizar
  (o aviso às telas já acontece hoje na rota antiga).
- **FR-009**: Registrar a compra MUST exigir usuário logado.
- **FR-010**: Enquanto a ação é enviada, os botões MUST ficar desabilitados e indicar carregamento,
  e o modal não pode ser fechado.
- **FR-011**: Fechar o modal sem clicar em Comprado MUST NOT registrar nada.
- **FR-012**: O comportamento do registro (404, mudança de situação e data da compra, mensagem de
  sucesso, ausência de notificação e WhatsApp) MUST ter testes automatizados.

### Key Entities *(include if feature involves data)*

- **Ordem de serviço (OS)**: entidade existente. Campos usados: número, cliente, aparelho,
  solução, valor total, custo da peça, garantia, data da resposta (aprovação), situação e
  **data da compra da peça**.

**Transição de estado**:

| De | Ação | Para | Aba de destino |
|----|------|------|----------------|
| Aprovado (aba "Aprovados") | Comprado | Aguardando peça (com data da compra) | Aguardando peça |

O handler antigo não valida a situação de origem; o botão só existe na aba "Aprovados".

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A pessoa registra a compra em até 2 cliques a partir da aba "Aprovados" (COMPRADO →
  Comprado).
- **SC-002**: 100% das OS com compra registrada saem de "Aprovados" e aparecem em "Aguardando peça"
  logo após a recarga da lista.
- **SC-003**: 0 registros ao fechar o modal sem clicar em Comprado.
- **SC-004**: Cliques duplos no mesmo modal não geram dois registros, porque os botões ficam
  travados durante o envio.
- **SC-005**: Toda falha mostra uma mensagem em português e deixa o modal aberto para nova
  tentativa.

## Assumptions

- **Sem edição de custo**: o custo da peça é só exibido. Editar o valor de compra continua fora
  desta feature.
- **Sem notificação nem WhatsApp**: igual ao handler antigo, que não envia nada além de salvar;
  a rota antiga só avisava as outras telas.
- **Sem validação de situação**: igual ao handler antigo (ver Edge Cases).
- **Sem desfazer**: não é possível desfazer o registro da compra nesta feature.
- **Padrão técnico**: igual às features 001 e 002: handler comum com interface (sem MediatR),
  resultado padronizado e modal no padrão dos existentes.
- **Fora do escopo**: a aba "Aguardando peça" e o registro do conserto.
