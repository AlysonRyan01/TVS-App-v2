# Feature Specification: Modal "Consertado" na aba "Aguardando peça"

**Feature Branch**: `005-repair-modal`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "agora vamos para o tab \"Aguardando peca\", vamos criar a coluna \"acao\" com o botao \"CONSERTADO\", vai abrir um Modal que vai chamar o endpoint REST de conserto de ordem, veja como o handler antigo funcionava e faca o mesmo fluxo. O modal deve seguir o mesmo design dos outros"

## Contexto

A aba **Aguardando peça** lista as OS cuja peça já foi comprada. Quando a peça chega e o
técnico conclui o conserto, a OS deve ir para a aba **Prontos** e o cliente é avisado pelo
WhatsApp de que o aparelho está pronto.

Hoje não há como registrar o conserto pela tela. A rota que existe (`add-service-order-repair`)
está quebrada, porque depende de um handler removido (mesmo defeito das features 001 a 004).

Esta feature adiciona à aba "Aguardando peça" a coluna **Ação** com o botão **CONSERTADO**. O botão
abre um modal no mesmo design dos outros, com o botão **Consertado**. O comportamento é o do handler
antigo `old-handlers/AddServiceOrderRepairHandler.cs`.

**Atores**: atendente ou técnico da assistência, logado no sistema.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registrar o conserto (Priority: P1)

O técnico terminou o conserto de uma OS. A pessoa abre a aba "Aguardando peça", clica em
**CONSERTADO**, confere os dados no modal e clica em **Consertado**. A OS sai de "Aguardando peça",
aparece em "Prontos" com a data do conserto e o cliente recebe pelo WhatsApp o aviso de que o
aparelho está pronto.

**Why this priority**: é a etapa que encerra o trabalho técnico e avisa o cliente para buscar o
aparelho e pagar.

**Independent Test**: com uma OS em "Aguardando peça", abrir o modal, clicar em Consertado e
conferir que a OS some dessa aba, aparece em "Prontos" e que o WhatsApp foi enfileirado.

**Acceptance Scenarios**:

1. **Given** uma OS na aba "Aguardando peça", **When** a pessoa clica em **CONSERTADO**, **Then**
   abre o modal com o número da OS, os dados do cliente e do aparelho e o resumo do conserto
   (solução, valor total, garantia e data da compra da peça).
2. **Given** o modal aberto, **When** a pessoa clica em **Consertado**, **Then** a OS passa para a
   situação "Consertado" com a data atual, aparece a mensagem "Conserto registrado.", o modal
   fecha e a lista de "Aguardando peça" é recarregada sem essa OS.
3. **Given** uma OS consertada, **When** se abre a aba "Prontos", **Then** a OS aparece lá com a
   data do conserto.
4. **Given** um conserto registrado (de qualquer empresa), **Then** o cliente recebe pelo WhatsApp
   o aviso de aparelho pronto (número da OS, nome) e as outras telas abertas são avisadas para
   atualizar.

---

### User Story 2 - Fechar sem registrar e tratar erros (Priority: P2)

A pessoa abre o modal por engano ou o conserto ainda não terminou. Ela fecha e nada muda. Se algo
der errado, vê a mensagem e pode tentar de novo.

**Why this priority**: evita registrar conserto por engano, o que avisaria o cliente antes da hora.

**Independent Test**: abrir e fechar o modal (Cancelar, X ou Esc) e conferir que a OS continua em
"Aguardando peça"; derrubar a API, clicar em Consertado e conferir a mensagem com o modal aberto.

**Acceptance Scenarios**:

1. **Given** o modal aberto, **When** a pessoa clica em **Cancelar**, no **X** ou aperta **Esc**,
   **Then** o modal fecha e nada é registrado.
2. **Given** a pessoa clicou em Consertado, **Then** os botões ficam desabilitados e mostram que a
   ação está em andamento, e o modal não pode ser fechado até a resposta.
3. **Given** uma falha ao registrar, **Then** aparece a mensagem do sistema ou "Não foi possível
   registrar o conserto. Tente de novo.", o modal continua aberto e os botões voltam a ficar
   habilitados.
4. **Given** uma falha ao carregar a OS, **Then** o modal mostra o mesmo estado de erro dos outros
   modais (Fechar e Tentar de novo).

---

### Edge Cases

Todos seguem o handler antigo e a regra que já existe no domínio:

- **OS inexistente** → recusado com "Essa ordem de serviço não existe".
- **Orçamento ainda pendente ou aguardando resposta** → recusado com "Não podemos executar o
  conserto pois a ordem de serviço não tem o status de aprovado". Nada é alterado e nenhum
  WhatsApp é enviado.
- **Conserto registrado duas vezes**: o handler antigo não impede; a data do conserto é gravada
  de novo e o WhatsApp é reenviado. O botão só aparece em "Aguardando peça", e a OS sai dessa
  aba após o primeiro registro.
- **Lista vazia**: a coluna Ação aparece normalmente e a mensagem de lista vazia continua igual.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A tabela da aba "Aguardando peça" MUST ter uma nova última coluna **Ação** com o botão
  **CONSERTADO** em cada linha, no mesmo estilo dos botões das outras abas.
- **FR-002**: O botão MUST abrir, em tela cheia, o modal com o mesmo design dos outros: cabeçalho
  com título **Conserto** e botão de fechar; painel lateral com OS, cliente, telefones, aparelho e
  defeito; estados de carregamento e de erro iguais.
- **FR-003**: O modal MUST exibir, somente para leitura, o resumo: solução, valor total, garantia e
  "Peça comprada em" (data da compra da peça).
- **FR-004**: O rodapé MUST ter o texto "O conserto já foi concluído?" e os botões **Cancelar** e
  **Consertado** (ação principal).
- **FR-005**: Ao clicar em **Consertado**, o sistema MUST reproduzir exatamente o handler antigo
  `old-handlers/AddServiceOrderRepairHandler.cs`:
  1. OS não encontrada → recusar com "Essa ordem de serviço não existe";
  2. registrar o conserto: situação "Consertado" e data do conserto = agora (o domínio recusa
     se o orçamento estiver pendente ou aguardando resposta);
  3. salvar;
  4. enfileirar o WhatsApp de "aparelho pronto" para o cliente (número da OS, nome e telefone
     principal), **para todas as empresas**;
  5. responder com sucesso "Conserto adicionado com sucesso".
- **FR-006**: A ação MUST ser exposta como `PUT /service-orders/{id}/repair` (sem corpo), com login
  obrigatório. A rota antiga `add-service-order-repair` e o command dela MUST ser removidos.
- **FR-007**: Após o registro, o sistema MUST avisar as outras telas para atualizar (a rota antiga
  já fazia isso), mostrar "Conserto registrado.", fechar o modal e recarregar a lista de
  "Aguardando peça".
- **FR-008**: Enquanto a ação é enviada, os botões MUST ficar desabilitados e indicar carregamento,
  e o modal não pode ser fechado.
- **FR-009**: Fechar o modal sem clicar em Consertado MUST NOT registrar nada.
- **FR-010**: O fluxo do FR-005 MUST ter testes automatizados (OS inexistente, orçamento não
  aprovado, sucesso e envio do WhatsApp).

### Key Entities *(include if feature involves data)*

- **Ordem de serviço (OS)**: entidade existente. Campos usados: número, cliente (nome, telefones),
  aparelho, solução, valor total, garantia, data da compra da peça, situação, status do
  orçamento e **data do conserto**.
- **Mensagem de WhatsApp "aparelho pronto"**: mensagem que já existe no sistema (número da OS,
  nome do cliente e telefone).

**Transição de estado**:

| De | Ação | Para | Aba de destino |
|----|------|------|----------------|
| Aguardando peça (orçamento aprovado) | Consertado | Consertado (com data do conserto) | Prontos |
| Orçamento pendente ou aguardando resposta | Consertado | (recusado) | — |

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A pessoa registra o conserto em até 2 cliques a partir da aba "Aguardando peça".
- **SC-002**: 100% das OS consertadas saem de "Aguardando peça" e aparecem em "Prontos" logo após a
  recarga da lista.
- **SC-003**: 100% dos consertos registrados com sucesso enviam o aviso de WhatsApp ao cliente;
  0 avisos em registros recusados.
- **SC-004**: 0 registros ao fechar o modal sem clicar em Consertado.
- **SC-005**: Toda falha mostra uma mensagem em português e deixa o modal aberto.

## Assumptions

- **Rota**: `PUT /service-orders/{id}/repair`, no estilo REST das features 002 a 004.
- **Nome do botão**: "CONSERTADO" na tabela e "Consertado" no modal, como pedido; título do modal
  "Conserto".
- **Sem notificação interna**: o handler antigo e a rota antiga não criavam notificação interna (a
  descrição da rota antiga menciona uma notificação, mas o código não cria nenhuma).
- **Sem checagem de salvamento**: o handler antigo não verificava o resultado do salvamento antes de
  enviar o WhatsApp; isso é mantido.
- **Sem desfazer**: não é possível desfazer o conserto nesta feature.
- **Padrão técnico**: igual às features anteriores (handler comum com interface, sem MediatR;
  resultado padronizado; modal no design dos existentes).
- **Fora do escopo**: a aba "Prontos" e a entrega do aparelho.
