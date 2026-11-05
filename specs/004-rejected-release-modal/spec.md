# Feature Specification: Modal "Liberado" na aba "Reprovados"

**Feature Branch**: `004-rejected-release-modal`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "agora vamos para o tab \"Reprovados\", vamos criar a coluna \"acao\" com o botao \"LIBERADO\", vai abrir um Modal que vai chamar o endpoint REST de liberar a ordem reprovada, veja como o handler antigo funcionava e faca o mesmo fluxo. O modal deve seguir o mesmo design dos outros"

## Contexto

A aba **Reprovados** lista as OS cujo orçamento o cliente reprovou. Depois da reprovação, a
assistência monta o aparelho de volta e o **libera** para o cliente buscar. A OS então passa
para a aba **Prontos**, e o cliente recebe um aviso pelo WhatsApp.

Hoje não há como fazer isso pela tela. A rota que existe (`mark-estimate-rejected-as-ready-to-pick-up`)
está quebrada, porque depende de um handler removido (mesmo defeito das features 001 a 003).

Esta feature adiciona à aba "Reprovados" a coluna **Ação** com o botão **LIBERADO**. O botão abre
um modal no mesmo design dos outros, com o botão **Liberado**. O comportamento é o do handler
antigo `old-handlers/MarkEstimateRejectedAsReadyToPickUpHandler.cs`.

**Atores**: atendente ou técnico da assistência, logado no sistema.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Liberar OS reprovada para retirada (Priority: P1)

O aparelho de uma OS reprovada já foi montado de volta. A pessoa abre a aba "Reprovados", clica
em **LIBERADO**, confere os dados no modal e clica em **Liberado**. A OS sai de "Reprovados",
aparece em "Prontos" e o cliente recebe pelo WhatsApp o aviso de que pode buscar o aparelho.

**Why this priority**: fecha o fluxo da reprovação. Sem isso, as OS reprovadas ficam paradas e o
cliente não é avisado.

**Independent Test**: com uma OS em "Reprovados", abrir o modal, clicar em Liberado e conferir que
a OS some dessa aba, aparece em "Prontos" e que o WhatsApp foi enfileirado.

**Acceptance Scenarios**:

1. **Given** uma OS na aba "Reprovados", **When** a pessoa clica em **LIBERADO**, **Then** abre o
   modal com o número da OS, os dados do cliente e do aparelho e o resumo do orçamento reprovado
   (solução, valor total, garantia e data da reprovação).
2. **Given** o modal aberto, **When** a pessoa clica em **Liberado**, **Then** a OS passa para a
   situação "Pronto para entrega", aparece a mensagem "Ordem de serviço liberada para retirada.",
   o modal fecha e a lista de "Reprovados" é recarregada sem essa OS.
3. **Given** uma OS liberada, **When** se abre a aba "Prontos", **Then** a OS aparece lá.
4. **Given** uma OS liberada (de qualquer empresa), **Then** o cliente recebe pelo WhatsApp a
   mensagem de que o aparelho reprovado está pronto para retirada, e as outras telas abertas
   são avisadas para atualizar.

---

### User Story 2 - Fechar sem liberar e tratar erros (Priority: P2)

A pessoa abre o modal por engano ou o aparelho ainda não foi montado. Ela fecha e nada muda. Se
algo der errado, vê a mensagem e pode tentar de novo.

**Why this priority**: evita liberar por engano, o que dispara o WhatsApp para o cliente.

**Independent Test**: abrir e fechar o modal (Cancelar, X ou Esc) e conferir que a OS continua em
"Reprovados"; derrubar a API, clicar em Liberado e conferir a mensagem com o modal aberto.

**Acceptance Scenarios**:

1. **Given** o modal aberto, **When** a pessoa clica em **Cancelar**, no **X** ou aperta **Esc**,
   **Then** o modal fecha e nada é registrado.
2. **Given** a pessoa clicou em Liberado, **Then** os botões ficam desabilitados e mostram que a
   ação está em andamento, e o modal não pode ser fechado até a resposta.
3. **Given** uma falha ao liberar, **Then** aparece a mensagem do sistema ou "Não foi possível
   liberar a ordem de serviço. Tente de novo.", o modal continua aberto e os botões voltam a
   ficar habilitados.
4. **Given** uma falha ao carregar a OS, **Then** o modal mostra o mesmo estado de erro dos outros
   modais (Fechar e Tentar de novo).

---

### Edge Cases

Todos seguem o handler antigo:

- **Número da OS igual a 0** → recusado com "O número da ordem de serviço não pode ser 0".
- **OS inexistente** → recusado com a mensagem de "não encontrada" do sistema.
- **OS cujo orçamento não está reprovado** (ex.: já liberada em outra tela, ou aprovada) →
  recusado com "O orçamento precisa estar rejeitado para marcar como pronto para entrega".
  Nada é alterado e nenhum WhatsApp é enviado.
- **Falha ao salvar** → a mensagem de erro do sistema é exibida e nenhum WhatsApp é enviado.
- **Liberar duas vezes**: depois da primeira, a OS continua com orçamento reprovado, então a
  segunda liberação é aceita de novo (o handler antigo não impede) e reenvia o WhatsApp. O botão
  só aparece em "Reprovados", e a OS sai dessa aba após a primeira liberação.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A tabela da aba "Reprovados" MUST ter uma nova última coluna **Ação** com o botão
  **LIBERADO** em cada linha, no mesmo estilo dos botões das outras abas.
- **FR-002**: O botão MUST abrir, em tela cheia, o modal com o mesmo design dos outros: cabeçalho
  com título **Liberação** e botão de fechar; painel lateral com OS, cliente, telefones, aparelho
  e defeito; estados de carregamento e de erro iguais.
- **FR-003**: O modal MUST exibir, somente para leitura, o resumo do orçamento reprovado: solução,
  valor total, garantia e "Reprovado em" (data da resposta).
- **FR-004**: O rodapé MUST ter o texto "O aparelho já está pronto para retirada?" e os botões
  **Cancelar** e **Liberado** (ação principal).
- **FR-005**: Ao clicar em **Liberado**, o sistema MUST reproduzir exatamente o handler antigo
  `old-handlers/MarkEstimateRejectedAsReadyToPickUpHandler.cs`:
  1. número da OS igual a 0 → recusar com "O número da ordem de serviço não pode ser 0";
  2. OS não encontrada → recusar com a mensagem de "não encontrada" do sistema;
  3. marcar a OS como "Pronto para entrega" (o domínio recusa se o orçamento não estiver
     reprovado);
  4. salvar; em caso de falha, responder com o erro e não enviar WhatsApp;
  5. enfileirar o WhatsApp de "reprovado pronto para retirada" para o cliente (nome e telefone
     principal), **para todas as empresas**;
  6. responder com sucesso "Ordem de serviço marcada como pronta para entrega".
- **FR-006**: A ação MUST ser exposta como `PUT /service-orders/{id}/ready-for-pickup` (sem corpo),
  com login obrigatório. A rota antiga `mark-estimate-rejected-as-ready-to-pick-up` e o command
  dela MUST ser removidos.
- **FR-007**: Após a liberação, o sistema MUST avisar as outras telas para atualizar (a rota antiga
  já fazia isso), mostrar "Ordem de serviço liberada para retirada.", fechar o modal e
  recarregar a lista de "Reprovados".
- **FR-008**: Enquanto a ação é enviada, os botões MUST ficar desabilitados e indicar carregamento,
  e o modal não pode ser fechado.
- **FR-009**: Fechar o modal sem clicar em Liberado MUST NOT registrar nada.
- **FR-010**: O fluxo do FR-005 MUST ter testes automatizados (cada saída de erro, o sucesso e o
  envio de WhatsApp).

### Key Entities *(include if feature involves data)*

- **Ordem de serviço (OS)**: entidade existente. Campos usados: número, cliente (nome, telefones),
  aparelho, solução, valor total, garantia, data da resposta (reprovação), status do orçamento
  e situação.
- **Mensagem de WhatsApp "reprovado pronto para retirada"**: mensagem que já existe no sistema
  (nome do cliente e telefone).

**Transição de estado**:

| De | Ação | Para | Aba de destino |
|----|------|------|----------------|
| Avaliado + Reprovado | Liberado | Pronto para entrega (orçamento continua Reprovado) | Prontos |
| Orçamento não reprovado | Liberado | (recusado) | — |

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A pessoa libera uma OS reprovada em até 2 cliques a partir da aba "Reprovados".
- **SC-002**: 100% das OS liberadas saem de "Reprovados" e aparecem em "Prontos" logo após a
  recarga da lista.
- **SC-003**: 100% das liberações com sucesso enviam o aviso de WhatsApp ao cliente; 0 avisos
  em liberações recusadas ou com falha ao salvar.
- **SC-004**: 0 liberações ao fechar o modal sem clicar em Liberado.
- **SC-005**: Toda falha mostra uma mensagem em português e deixa o modal aberto.

## Assumptions

- **Rota**: `PUT /service-orders/{id}/ready-for-pickup`, porque "Pronto para entrega" corresponde
  ao termo canônico `ReadyForPickup` da feature 001 e ao estilo REST das features 002 e 003.
- **Nome do botão**: "LIBERADO" na tabela e "Liberado" no modal, como pedido; título do modal
  "Liberação".
- **Sem notificação interna**: o handler antigo e a rota antiga não criavam notificação interna.
- **Sem desfazer**: não é possível desfazer a liberação nesta feature.
- **Padrão técnico**: igual às features anteriores (handler comum com interface, sem MediatR;
  resultado padronizado; modal no design dos existentes).
- **Fora do escopo**: a aba "Prontos" e a entrega do aparelho.
