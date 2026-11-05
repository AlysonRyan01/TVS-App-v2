# Feature Specification: Modal de resposta do orçamento (aba "Aguardando resposta")

**Feature Branch**: `002-estimate-response-modal`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "agora vamos trabalhar no tab \"Aguardando Resposta\", vamos criar a coluna \"acao\" com o botao \"RESPOSTA\", que vai abrir o modal de resposta, vamos criar esse modal seguindo o mesmo padrao do modal de orcamento. Nesse modal vai ter os botoes de reprovado e aprovado"

## Contexto

Depois que o orçamento é enviado, a OS vai para a aba **Aguardando resposta**. Hoje essa aba
só lista as OS, e não há como registrar pela tela a resposta do cliente (aprovou ou reprovou o
orçamento). A API tem rotas para isso, mas elas estão quebradas: os handlers foram removidos e
as rotas ainda dependem deles (é o mesmo defeito corrigido para o orçamento na feature 001).
Além disso, a rota de aprovação não exige login.

Esta feature cria, na aba "Aguardando resposta", uma coluna **Ação** com o botão **RESPOSTA**.
O botão abre o **modal de resposta**, com o mesmo padrão visual e de comportamento do modal de
orçamento, onde a pessoa da assistência registra se o cliente **aprovou** ou **reprovou** o
orçamento.

**Atores**: atendente ou técnico da assistência, logado no sistema.

## Clarifications

### Session 2026-11-05

- Q: Ao registrar a resposta, o sistema envia WhatsApp ao cliente? → A: Sim, nos dois casos
  (agradecimento na aprovação, confirmação na reprovação), exceto para OS da PhilcoBritania,
  com a mesma regra do orçamento.
- Q: Os efeitos de WhatsApp devem ser iguais aos handlers antigos (`old-handlers/`)? → A: Sim.
  Aprovação: agradecimento ao cliente + aviso ao dono (`NotifyOwnerEstimateDecisionRequest`),
  exceto PhilcoBritania. Reprovação: confirmação ao cliente + aviso ao dono, **para todas as
  empresas**. Mensagem de sucesso da aprovação: "Aprovação adicionada com sucesso!". Esta
  resposta substitui a regra de PhilcoBritania da resposta anterior para a reprovação.
- Q: Como nomear as rotas de aprovação e reprovação? → A: Estilo REST:
  `PUT /service-orders/{id}/estimate/approve` e `PUT /service-orders/{id}/estimate/reject`. Elas
  substituem `add-service-order-approve-estimate` e `add-service-order-reject-estimate`, que são
  removidas.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registrar aprovação do orçamento (Priority: P1)

O cliente avisa que aprovou o orçamento. A pessoa da assistência abre a aba "Aguardando
resposta", clica em **RESPOSTA** na linha da OS, confere os dados no modal e clica em
**Aprovado**. A OS sai da aba "Aguardando resposta" e passa a aparecer em "Aprovados", onde
segue para a compra da peça e o conserto.

**Why this priority**: a aprovação é o caminho que gera receita e libera o conserto. Sem ela,
as OS ficam paradas em "Aguardando resposta".

**Independent Test**: com uma OS em "Aguardando resposta", abrir o modal, clicar em Aprovado e
conferir que a OS some dessa aba e aparece em "Aprovados".

**Acceptance Scenarios**:

1. **Given** uma OS na aba "Aguardando resposta", **When** a pessoa clica em **RESPOSTA**,
   **Then** abre o modal de resposta com o número da OS, os dados do cliente e do aparelho e o
   resumo do orçamento (solução, valor, garantia e mensagem enviada).
2. **Given** o modal de resposta aberto, **When** a pessoa clica em **Aprovado**, **Then** a
   resposta é registrada, aparece a mensagem de sucesso "Orçamento aprovado.", o modal fecha e
   a lista da aba "Aguardando resposta" é recarregada sem essa OS.
3. **Given** uma OS aprovada, **When** se abre a aba "Aprovados", **Then** a OS aparece lá com a
   data da aprovação.
4. **Given** uma OS aprovada, **Then** a equipe recebe a notificação interna "Orçamento
   Aprovado, OS: {número}" / "Favor comprar peça para OS: {número}", e as outras telas abertas
   são avisadas para atualizar.
5. **Given** uma OS aprovada de empresa diferente de PhilcoBritania, **Then** o cliente recebe
   pelo WhatsApp a mensagem de agradecimento pela aprovação (número da OS e valor) e o dono
   recebe o aviso de orçamento aprovado. Na PhilcoBritania nenhum WhatsApp é enviado.

---

### User Story 2 - Registrar reprovação do orçamento (Priority: P1)

O cliente avisa que não quer fazer o conserto. A pessoa abre o mesmo modal e clica em
**Reprovado**. A OS sai de "Aguardando resposta" e passa a aparecer em "Reprovados".

**Why this priority**: tem a mesma importância que a aprovação. Sem ela, as OS reprovadas ficam
presas na fila e o aparelho não é devolvido.

**Independent Test**: com uma OS em "Aguardando resposta", abrir o modal, clicar em Reprovado e
conferir que a OS aparece em "Reprovados".

**Acceptance Scenarios**:

1. **Given** o modal de resposta aberto, **When** a pessoa clica em **Reprovado**, **Then** a
   resposta é registrada, aparece a mensagem "Orçamento reprovado.", o modal fecha e a lista é
   recarregada sem essa OS.
2. **Given** uma OS reprovada, **When** se abre a aba "Reprovados", **Then** a OS aparece lá com
   a data da reprovação.
3. **Given** uma OS reprovada, **Then** a equipe recebe a notificação interna "Orçamento
   Reprovado, OS: {número}" / "Favor montar a OS {número} para entrega", e as outras telas são
   avisadas para atualizar.
4. **Given** uma OS reprovada (de qualquer empresa, inclusive PhilcoBritania), **Then** o cliente
   recebe pelo WhatsApp a confirmação da reprovação e o dono recebe o aviso de orçamento
   reprovado.

---

### User Story 3 - Fechar sem responder e tratar erros (Priority: P2)

A pessoa abre o modal por engano ou o cliente ainda não decidiu. Ela fecha o modal e nada muda.
Se algo der errado (falha de conexão, OS já respondida em outra tela), a pessoa vê uma mensagem
clara em português e pode tentar de novo.

**Why this priority**: evita respostas registradas por engano e deixa claro quando a ação não
foi concluída.

**Independent Test**: abrir o modal e fechar (botão Cancelar, X ou tecla Esc); conferir que a OS
continua em "Aguardando resposta". Derrubar a API e clicar em Aprovado; conferir que aparece a
mensagem de erro e o modal continua aberto.

**Acceptance Scenarios**:

1. **Given** o modal aberto, **When** a pessoa clica em **Cancelar**, no **X** ou aperta **Esc**,
   **Then** o modal fecha, nenhuma resposta é registrada e a lista não muda.
2. **Given** o modal aberto, **When** a pessoa clica em Aprovado ou Reprovado, **Then** os dois
   botões ficam desabilitados e mostram que a ação está em andamento até a resposta do sistema,
   evitando cliques duplos.
3. **Given** uma falha ao registrar a resposta, **When** o sistema retorna erro, **Then** aparece a
   mensagem de erro (a mensagem do sistema, ou "Não foi possível registrar a resposta. Tente de
   novo."), o modal continua aberto e os botões voltam a ficar habilitados.
4. **Given** uma falha ao carregar os dados da OS no modal, **Then** o modal mostra "Não foi
   possível carregar a OS {número}. Confira a conexão e tente de novo." com os botões **Fechar**
   e **Tentar de novo**, como no modal de orçamento.

---

### Edge Cases

- **OS já respondida**: se a OS já foi aprovada ou reprovada (por outra pessoa ou em outra aba),
  o sistema MUST recusar uma nova resposta com a mensagem "Esse orçamento já foi respondido."
  e a lista deve ser recarregada quando o modal fechar.
- **OS sem solução**: o domínio recusa aprovar ou reprovar uma OS sem solução cadastrada; a
  mensagem do sistema ("A solução não pode estar nula…") é exibida no modal.
- **OS inexistente**: resposta "Essa ordem de serviço não existe", exibida no modal.
- **Sem conserto / sem defeito / PhilcoBritania**: essas OS não passam por "Aguardando
  resposta" (vão direto para "Prontos" ou "Aprovados" ao enviar o orçamento), então não
  aparecem nesta aba e não usam este modal.
- **Lista vazia**: a coluna Ação aparece normalmente; a mensagem "Nenhum orçamento esperando
  resposta do cliente." continua igual.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A tabela da aba "Aguardando resposta" MUST ter uma nova última coluna com o título
  **Ação**, contendo em cada linha o botão **RESPOSTA**, no mesmo estilo do botão
  **ORÇAMENTO** da aba "Orçamentos pendentes".
- **FR-002**: O botão RESPOSTA MUST abrir o modal de resposta da OS daquela linha, em tela cheia,
  com o mesmo padrão do modal de orçamento: cabeçalho com título **Resposta** e botão de fechar;
  painel lateral com número da OS, nome e telefones do cliente, dados do aparelho e defeito;
  estados de carregamento e de erro iguais aos do modal de orçamento.
- **FR-003**: O modal MUST exibir, somente para leitura, o resumo do orçamento enviado: resultado
  do reparo, solução, valor total, garantia e mensagem do orçamento.
- **FR-004**: O rodapé do modal MUST ter os botões **Cancelar**, **Reprovado** e **Aprovado**.
  Aprovado é a ação principal (destaque visual). Reprovado deve ser visualmente diferenciado
  (ex.: cor de alerta). Os rótulos ficam em português.
- **FR-005**: Ao clicar em **Aprovado**, o sistema MUST registrar a aprovação: o status do
  orçamento passa para "Aprovado" e a data da resposta é registrada; a OS passa a aparecer na
  aba "Aprovados".
- **FR-006**: Ao clicar em **Reprovado**, o sistema MUST registrar a reprovação: o status do
  orçamento passa para "Reprovado" e a data da resposta é registrada; a OS passa a aparecer na
  aba "Reprovados".
- **FR-007**: Após uma resposta registrada com sucesso, o sistema MUST mostrar a mensagem de
  sucesso ("Orçamento aprovado." ou "Orçamento reprovado."), fechar o modal e recarregar a
  lista da aba "Aguardando resposta".
- **FR-008**: Após registrar a resposta, o sistema MUST manter os efeitos que existem hoje nas
  rotas de aprovação e reprovação: criar a notificação interna correspondente e avisar as
  telas conectadas para atualizar.
- **FR-008a**: Após registrar a resposta, o sistema MUST enfileirar as mensagens de WhatsApp
  exatamente como os handlers antigos (`old-handlers/`): na **aprovação**, se a empresa não for
  PhilcoBritania, o agradecimento ao cliente (número da OS, nome, telefone principal, valor
  total) e o aviso ao dono (número da OS, "Aprovado", nome do cliente); na **reprovação**, para
  todas as empresas, a confirmação ao cliente (número da OS, nome, telefone principal) e o aviso
  ao dono (número da OS, "Reprovado", nome do cliente). Se o envio do WhatsApp
  falhar, a resposta continua registrada, porque o envio é assíncrono e não bloqueia o fluxo.
- **FR-009**: O sistema MUST aceitar a resposta somente para OS que estão aguardando resposta
  (orçamento avaliado e ainda não respondido). Caso contrário, MUST recusar com a mensagem
  "Esse orçamento já foi respondido." e não alterar a OS.
- **FR-010**: Registrar aprovação ou reprovação MUST exigir usuário logado (hoje a rota de
  aprovação não exige).
- **FR-011**: Enquanto a resposta é enviada, os botões Aprovado e Reprovado MUST ficar
  desabilitados e indicar carregamento; Cancelar também fica desabilitado.
- **FR-012**: Fechar o modal sem clicar em Aprovado ou Reprovado MUST NOT registrar nenhuma
  resposta.
- **FR-013**: Os nomes técnicos MUST seguir o vocabulário definido na feature 001 (`Estimate`,
  `Approve`/`Approved`, `Reject`/`Rejected`, `AwaitingResponse`) de forma idêntica no frontend e
  na API.
- **FR-013a**: As ações de resposta MUST ser expostas como
  `PUT /service-orders/{id}/estimate/approve` e `PUT /service-orders/{id}/estimate/reject`, com
  o número da OS na rota e sem corpo. As rotas antigas `add-service-order-approve-estimate` e
  `add-service-order-reject-estimate` MUST ser removidas, sem aliases, porque nenhuma tela as
  usa. As demais rotas da API continuam no padrão atual: só estas duas adotam o estilo REST
  nesta feature.
- **FR-014**: As regras novas (aceitar resposta só para OS aguardando resposta, aprovar,
  reprovar e efeitos colaterais) MUST ter testes automatizados.

### Key Entities *(include if feature involves data)*

- **Ordem de serviço (OS)**: entidade existente. Campos usados: número, cliente (nome e
  telefones), aparelho (marca, modelo, número de série, acessórios, defeito), solução, valor
  total, garantia, mensagem do orçamento, resultado do reparo, status do orçamento e data da
  resposta.
- **Resposta do orçamento**: a decisão do cliente, que pode ser **Aprovado** ou **Reprovado**.
  Não é uma entidade nova: é registrada no status do orçamento e na data da resposta da OS.
- **Notificação interna**: entidade existente, criada a cada resposta (ver FR-008).
- **Mensagem de WhatsApp**: mensagens que já existem (agradecimento pela aprovação,
  confirmação da reprovação e aviso de decisão ao dono), com as regras do FR-008a.

**Transição de estado (status do orçamento)**:

| De | Ação | Para | Aba de destino |
|----|------|------|----------------|
| Aguardando resposta | Aprovado | Aprovado | Aprovados |
| Aguardando resposta | Reprovado | Reprovado | Reprovados |
| Aprovado / Reprovado | Aprovado ou Reprovado | (recusado) | — |

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A pessoa consegue registrar a resposta do cliente em até 3 cliques a partir da aba
  "Aguardando resposta" (RESPOSTA → Aprovado/Reprovado).
- **SC-002**: 100% das OS respondidas saem da aba "Aguardando resposta" e aparecem na aba
  correspondente (Aprovados ou Reprovados) logo após a recarga da lista.
- **SC-003**: 0 respostas registradas ao fechar o modal sem clicar em Aprovado ou Reprovado.
- **SC-004**: 0 respostas duplicadas para a mesma OS, inclusive com cliques duplos ou duas telas
  abertas.
- **SC-005**: Toda falha mostra uma mensagem em português e deixa o modal aberto para nova
  tentativa.

## Assumptions

- **Sem confirmação extra**: o próprio modal é o passo de confirmação (a pessoa vê o resumo antes
  de decidir). Não há segundo diálogo "Tem certeza?".
- **Sem desfazer**: não é possível desfazer uma resposta nesta feature. Correções ficam para
  uma feature futura.
- **Sem campo de observação**: o modal não pede comentário ou motivo da reprovação.
- **WhatsApp**: reaproveita as mensagens que já existem no sistema (agradecimento pela
  aprovação e confirmação da reprovação). Não é criado nenhum texto novo (ver Clarifications).
- **Padrão técnico**: segue o que foi definido na feature 001 (handler comum com interface, sem
  MediatR; endpoint retornando o resultado padronizado). As rotas de aprovação e reprovação são
  refeitas no estilo REST (FR-013a) como parte desta feature. Migrar as outras rotas para REST
  fica para uma feature futura, se for desejado.
- **Fora do escopo**: marcar OS reprovada como pronta para entrega (rota existente
  `mark-estimate-rejected-as-ready-to-pickup`), outras abas e outras telas.
- **Permissão**: qualquer usuário logado pode registrar a resposta, como já acontece com o
  envio do orçamento.
