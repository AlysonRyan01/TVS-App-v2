# Feature Specification: Modal "Entregue" na aba "Prontos"

**Feature Branch**: `006-delivery-modal`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "agora vamos para o tab \"Prontos\", vamos criar a coluna \"acao\" com o botao \"ENTREGUE\", vai abrir um Modal que vai chamar o endpoint REST de entrega de ordem, veja como o handler antigo funcionava e faca o mesmo fluxo. O modal deve seguir o mesmo design dos outros"

## Contexto

A aba **Prontos** lista as OS consertadas e as OS liberadas para retirada (reprovadas, sem
conserto ou sem defeito). Quando o cliente busca o aparelho, a OS deve ser marcada como
**entregue** e sai da aba.

O handler antigo (`old-handlers/AddServiceOrderDeliveryAndReturnPdfHandler.cs`) tem uma diferença
importante em relação aos das features anteriores: quando a OS foi **consertada** (orçamento
aprovado e resultado "Tem conserto"), ele gera o **termo de entrega em PDF** (com a garantia),
devolve esse PDF e envia ao cliente o WhatsApp com a garantia. Nos outros casos, só registra a
entrega.

Hoje não há como fazer isso pela tela. A rota que existe (`add-service-order-delivery`) está
quebrada, porque depende de um handler removido (mesmo defeito das features 001 a 005).

Esta feature adiciona à aba "Prontos" a coluna **Ação** com o botão **ENTREGUE**. O botão abre um
modal no mesmo design dos outros, com o botão **Entregue**.

**Atores**: atendente da assistência, logado no sistema, no momento em que o cliente retira o
aparelho.

## Clarifications

### Session 2026-11-05

- Q: O que a tela faz com o termo de entrega em PDF? → A: Abre numa nova aba do navegador, pronto
  para imprimir; se a nova aba for bloqueada, baixa o arquivo.
- Q: A janela de impressão pode abrir sozinha? → A: Sim. Assim que o PDF carrega na nova aba, a
  janela de impressão do navegador abre automaticamente (no download, não).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Entregar aparelho consertado com termo de entrega (Priority: P1)

O cliente veio buscar um aparelho consertado. A pessoa abre a aba "Prontos", clica em
**ENTREGUE**, confere os dados e clica em **Entregue**. A OS é marcada como entregue, o termo de
entrega em PDF é gerado e fica disponível para impressão, e o cliente recebe a garantia pelo
WhatsApp.

**Why this priority**: é o fim do fluxo da OS e o caso mais comum. O termo de entrega com a
garantia é o comprovante que o cliente leva.

**Independent Test**: com uma OS consertada em "Prontos", abrir o modal, clicar em Entregue e
conferir que a OS sai da aba, que o PDF do termo aparece e que o WhatsApp da garantia foi
enfileirado.

**Acceptance Scenarios**:

1. **Given** uma OS na aba "Prontos", **When** a pessoa clica em **ENTREGUE**, **Then** abre o
   modal com o número da OS, os dados do cliente e do aparelho e o resumo (resultado do reparo,
   solução, valor total, garantia e data do conserto).
2. **Given** uma OS consertada (orçamento aprovado, "Tem conserto"), **When** a pessoa clica em
   **Entregue**, **Then** a OS é marcada como entregue com a data atual, o termo de entrega em
   PDF é gerado e aberto numa nova aba do navegador, pronto para imprimir (se o navegador
   bloquear a nova aba, o arquivo é baixado), aparece a mensagem "Ordem de serviço entregue.", o modal fecha e a lista de "Prontos" é
   recarregada sem essa OS.
3. **Given** uma OS consertada entregue (de qualquer empresa), **Then** o cliente recebe pelo
   WhatsApp a mensagem com a garantia (número da OS, nome, garantia) e as outras telas são
   avisadas para atualizar.

---

### User Story 2 - Entregar aparelho sem conserto (Priority: P1)

O cliente veio buscar um aparelho que não foi consertado (orçamento reprovado, sem conserto ou
sem defeito). A pessoa clica em **ENTREGUE** e depois em **Entregue**. A OS é marcada como
entregue, sem termo em PDF e sem WhatsApp.

**Why this priority**: tem a mesma importância; essas OS também ficam em "Prontos" até a retirada.

**Independent Test**: com uma OS reprovada liberada em "Prontos", clicar em Entregue e conferir
que a OS sai da aba, sem PDF e sem WhatsApp.

**Acceptance Scenarios**:

1. **Given** uma OS em "Prontos" que não foi consertada, **When** a pessoa clica em **Entregue**,
   **Then** a OS é marcada como entregue, aparece "Ordem de serviço entregue.", o modal fecha e a
   lista é recarregada; nenhum PDF é gerado e nenhum WhatsApp é enviado.

---

### User Story 3 - Fechar sem entregar e tratar erros (Priority: P2)

A pessoa abre o modal por engano. Ela fecha e nada muda. Se algo der errado, vê a mensagem.

**Why this priority**: evita marcar como entregue um aparelho que ainda está na loja.

**Independent Test**: abrir e fechar o modal (Cancelar, X ou Esc) e conferir que a OS continua em
"Prontos"; derrubar a API, clicar em Entregue e conferir a mensagem com o modal aberto.

**Acceptance Scenarios**:

1. **Given** o modal aberto, **When** a pessoa clica em **Cancelar**, no **X** ou aperta **Esc**,
   **Then** o modal fecha e nada é registrado.
2. **Given** a pessoa clicou em Entregue, **Then** os botões ficam desabilitados e mostram que a
   ação está em andamento, e o modal não pode ser fechado até a resposta.
3. **Given** uma falha ao entregar, **Then** aparece a mensagem do sistema ou "Não foi possível
   registrar a entrega. Tente de novo.", o modal continua aberto e os botões voltam a ficar
   habilitados.
4. **Given** uma falha ao carregar a OS, **Then** o modal mostra o mesmo estado de erro dos outros
   modais (Fechar e Tentar de novo).

---

### Edge Cases

Todos seguem o handler antigo:

- **OS inexistente** → recusado com "Essa ordem de serviço não existe".
- **Falha ao gerar o PDF** (OS consertada): a entrega **já foi registrada** quando o PDF falha
  (o handler antigo salva antes de gerar o PDF). O sistema responde com erro e a mensagem do
  gerador, e nenhum WhatsApp é enviado. A OS sai de "Prontos" mesmo assim, e o termo pode ser
  regerado depois pela função de regerar PDF que já existe (fora do escopo).
- **Entregar duas vezes**: o handler antigo não impede; a data de entrega é gravada de novo e,
  para OS consertada, o PDF e o WhatsApp são gerados de novo. O botão só aparece em "Prontos", e
  a OS sai dessa aba após a primeira entrega.
- **Sem validação de situação**: o handler antigo não verifica se a OS está pronta; o botão só
  existe na aba "Prontos".

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A tabela da aba "Prontos" MUST ter uma nova última coluna **Ação** com o botão
  **ENTREGUE** em cada linha, no mesmo estilo dos botões das outras abas.
- **FR-002**: O botão MUST abrir, em tela cheia, o modal com o mesmo design dos outros: cabeçalho
  com título **Entrega** e botão de fechar; painel lateral com OS, cliente, telefones, aparelho e
  defeito; estados de carregamento e de erro iguais.
- **FR-003**: O modal MUST exibir, somente para leitura, o resumo: resultado do reparo, solução,
  valor total, garantia e "Consertado em" (data do conserto, ou "—").
- **FR-004**: O rodapé MUST ter o texto "O cliente já retirou o aparelho?" e os botões
  **Cancelar** e **Entregue** (ação principal).
- **FR-005**: Ao clicar em **Entregue**, o sistema MUST reproduzir exatamente o handler antigo
  `old-handlers/AddServiceOrderDeliveryAndReturnPdfHandler.cs`:
  1. OS não encontrada → recusar com "Essa ordem de serviço não existe";
  2. registrar a entrega (situação "Entregue", data da entrega = agora) e salvar;
  3. se o orçamento estiver **aprovado** e o resultado for **"Tem conserto"**: gerar o termo de
     entrega em PDF; se falhar, responder com erro e a mensagem do gerador; se der certo,
     enfileirar o WhatsApp de garantia (número da OS, nome, garantia, telefone principal) para
     **todas as empresas** e devolver o PDF;
  4. nos outros casos: responder com sucesso "Ordem de serviço entregue com sucesso!", sem PDF e
     sem WhatsApp.
- **FR-006**: A ação MUST ser exposta como `PUT /service-orders/{id}/delivery` (sem corpo), com
  login obrigatório. Com o PDF, a resposta é o arquivo (`application/pdf`); sem o PDF, a resposta
  padronizada com a mensagem. A rota antiga `add-service-order-delivery` e o command dela MUST ser
  removidos.
- **FR-007**: Após a entrega, o sistema MUST avisar as outras telas para atualizar, mostrar
  "Ordem de serviço entregue.", fechar o modal e recarregar a lista de "Prontos".
- **FR-008**: Quando a resposta trouxer o termo em PDF, a tela MUST abri-lo numa nova aba do
  navegador e abrir automaticamente a janela de impressão assim que o PDF carregar. Se o navegador bloquear a nova aba, MUST baixar o arquivo
  como `ordem_servico_{número}.pdf`.
- **FR-009**: Enquanto a ação é enviada, os botões MUST ficar desabilitados e indicar carregamento,
  e o modal não pode ser fechado.
- **FR-010**: Fechar o modal sem clicar em Entregue MUST NOT registrar nada.
- **FR-011**: O fluxo do FR-005 MUST ter testes automatizados (OS inexistente, OS consertada com
  PDF e WhatsApp, falha do PDF, OS não consertada sem PDF e sem WhatsApp).

### Key Entities *(include if feature involves data)*

- **Ordem de serviço (OS)**: entidade existente. Campos usados: número, cliente, aparelho,
  resultado do reparo, status do orçamento, solução, valor total, garantia, data do conserto,
  situação e **data da entrega**.
- **Termo de entrega (PDF)**: documento que já existe no sistema, gerado na entrega de OS
  consertada.
- **Mensagem de WhatsApp de garantia**: mensagem que já existe (número da OS, nome, garantia,
  telefone).

**Transição de estado**:

| De | Ação | Para | PDF / WhatsApp |
|----|------|------|----------------|
| Pronto, consertado (Aprovado + Tem conserto) | Entregue | Entregue | Termo PDF + WhatsApp de garantia |
| Pronto, sem conserto (reprovado, sem conserto, sem defeito) | Entregue | Entregue | Nenhum |

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A pessoa registra a entrega em até 2 cliques a partir da aba "Prontos".
- **SC-002**: 100% das OS entregues saem de "Prontos" logo após a recarga da lista.
- **SC-003**: 100% das entregas de OS consertadas disponibilizam o termo em PDF e enviam o
  WhatsApp de garantia; 0 PDFs e 0 WhatsApps para OS não consertadas.
- **SC-004**: 0 entregas ao fechar o modal sem clicar em Entregue.
- **SC-005**: Toda falha mostra uma mensagem em português e deixa o modal aberto.

## Assumptions

- **Rota**: `PUT /service-orders/{id}/delivery`, no estilo REST das features 002 a 005.
- **Resposta com arquivo**: diferente das outras rotas, quando há PDF a resposta é o próprio
  arquivo (como na rota antiga), não a resposta padronizada.
- **Nome do botão**: "ENTREGUE" na tabela e "Entregue" no modal; título do modal "Entrega".
- **Sem notificação interna**: o handler antigo não criava.
- **Sem desfazer** e **fora do escopo**: regerar o PDF, histórico de entregadas e pagamentos.
- **Padrão técnico**: igual às features anteriores (handler comum com interface, sem MediatR;
  modal no design dos existentes).
