# Feature Specification: Estrutura compartilhada dos modais da Home

**Feature Branch**: `008-shared-modal-shell`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "extrair o template dos modais num componente compartilhado"

## Contexto

A Home tem 6 modais de ação, um por aba: **Orçamento**, **Resposta**, **Compra da peça**,
**Liberação**, **Conserto** e **Entrega**. As features 001 a 006 criaram esses modais copiando a
mesma estrutura de um para o outro:

- barra superior com título e botão de fechar;
- estado de carregamento e estado de erro ("Não foi possível carregar a OS {número}…", com os
  botões Fechar e Tentar de novo);
- painel lateral com os dados da OS (número, cliente, telefones, aparelho, modelo, série,
  acessórios e defeito);
- rodapé com a pergunta, os botões e o travamento durante o envio;
- a busca da OS pelo número (repetida em 6 serviços);
- cerca de 370 linhas de estilos repetidas em cada modal.

Hoje são cerca de 3.900 linhas nos 6 modais, e qualquer ajuste no painel lateral ou nos estados
precisa ser feito 6 vezes. Além disso, todos os arquivos de estilo dos modais passam do limite de
tamanho configurado no build.

Esta feature extrai essa estrutura comum para um componente compartilhado. Cada modal fica só com
o que é dele: o título, o resumo ou o formulário, os botões e a ação.

**Usuários**: as pessoas que mantêm o sistema. Para quem usa a tela, nada muda.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Uma única estrutura de modal (Priority: P1)

Quem mantém o sistema altera o painel lateral, a barra superior, os estados de carregamento e erro
ou os estilos comuns num lugar só, e a mudança vale para os 6 modais.

**Why this priority**: é o objetivo do pedido. Acaba com a cópia que cresceu a cada feature.

**Independent Test**: mudar um rótulo do painel lateral (ex.: "Nº de série") no componente
compartilhado e conferir que os 6 modais mostram a mudança.

**Acceptance Scenarios**:

1. **Given** o componente compartilhado, **When** se abre qualquer um dos 6 modais, **Then** a barra
   superior, o painel lateral, o carregamento, o erro e o rodapé vêm desse componente.
2. **Given** um modal, **When** se lê o código dele, **Then** ele contém só o título, o conteúdo
   próprio (resumo ou formulário), os botões de ação e a lógica da ação.
3. **Given** a busca da OS pelo número, **When** os modais carregam a OS, **Then** todos usam uma
   única implementação dessa busca.

---

### User Story 2 - Telas idênticas para quem usa (Priority: P1)

Quem usa a Home abre cada um dos 6 modais e vê exatamente o mesmo de antes: textos, layout, cores,
estados de carregamento e erro, botões, travamento durante o envio, mensagens, recarga da lista e,
na entrega, o PDF com impressão automática.

**Why this priority**: é uma refatoração; só tem valor se nada mudar para quem usa.

**Independent Test**: repetir o fluxo de cada modal (quickstart das features 001 a 006) e comparar
visualmente com antes.

**Acceptance Scenarios**:

1. **Given** cada um dos 6 modais, **When** é aberto, **Then** o layout em tela larga e em tela
   estreita é igual ao de antes.
2. **Given** cada modal, **When** a ação é executada (enviar orçamento, aprovar/reprovar, comprado,
   liberado, consertado, entregue), **Then** o resultado, as mensagens e a recarga da lista são
   iguais aos de antes.
3. **Given** uma falha ao carregar a OS, **Then** o estado de erro e o botão Tentar de novo
   funcionam como antes, em todos os modais.
4. **Given** um envio em andamento, **Then** o X, o Cancelar e o Esc continuam bloqueados.

---

### Edge Cases

- **Modal de orçamento**: é o único com formulário (campos, validação e o envio pelo Enter). O
  formulário precisa continuar envolvendo o conteúdo e o rodapé, para o botão "Enviar orçamento"
  continuar enviando o formulário.
- **Rodapé com conteúdo à esquerda**: o orçamento mostra o valor total e a margem; os outros
  mostram uma pergunta e, na entrega, uma dica. A estrutura compartilhada precisa aceitar conteúdo
  livre nesse lugar.
- **Fechar retornando um valor**: o modal de resposta fecha informando se a OS foi respondida em
  outra tela, para a lista recarregar. O botão de fechar compartilhado precisa aceitar esse valor.
- **Textos específicos**: o aria-label do botão de fechar é diferente em cada modal ("Fechar sem
  salvar", "Fechar sem responder" etc.) e precisa continuar assim.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: MUST existir uma estrutura de modal compartilhada, usada pelos 6 modais da Home, com:
  barra superior (título e botão de fechar), estado de carregamento, estado de erro com Fechar e
  Tentar de novo, painel lateral com os dados da OS, área de conteúdo do modal e rodapé com área
  livre à esquerda e área de ações à direita.
- **FR-002**: A estrutura MUST receber de cada modal: o título, o texto acessível do botão de
  fechar, o número da OS, a OS carregada, se está carregando, se está enviando e o valor de
  retorno ao fechar.
- **FR-003**: A estrutura MUST bloquear o botão de fechar enquanto o modal estiver enviando.
- **FR-004**: A estrutura MUST avisar o modal quando a pessoa clicar em Tentar de novo, para ele
  recarregar a OS.
- **FR-005**: O modal de orçamento MUST continuar com o formulário envolvendo o conteúdo e o
  rodapé, com o mesmo envio e as mesmas validações.
- **FR-006**: A busca da OS pelo número MUST ter uma única implementação, usada pelos 6 modais, no
  lugar dos 6 `getServiceOrderById` repetidos.
- **FR-007**: Os estilos comuns (barra, painel lateral, estados, rodapé, botões) MUST ficar em um
  único lugar. Cada modal mantém só os estilos que são dele (ex.: campos do orçamento, botão
  Reprovado, dica da entrega).
- **FR-008**: O comportamento e a aparência dos 6 modais MUST continuar idênticos (US2).
- **FR-009**: O build do frontend MUST passar, e os arquivos de estilo dos 5 modais que não são o
  orçamento MUST ficar abaixo do limite de tamanho configurado.

### Key Entities *(include if feature involves data)*

- **Estrutura de modal**: a moldura comum (barra, estados, painel lateral, rodapé).
- **Painel lateral da OS**: os dados de consulta da OS, iguais em todos os modais.
- **Modal de ação**: cada um dos 6, com título, conteúdo, botões e ação próprios.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 6 de 6 modais usam a estrutura compartilhada.
- **SC-002**: O total de linhas dos 6 modais (templates, estilos, componentes e serviços) cai pelo
  menos 50% em relação às ~3.900 linhas atuais.
- **SC-003**: 0 diferenças visuais ou de comportamento nos 6 fluxos, conferidos manualmente.
- **SC-004**: 5 de 5 arquivos de estilo dos modais simples ficam abaixo do limite de tamanho do
  build (hoje todos passam).
- **SC-005**: Uma mudança no painel lateral exige editar 1 arquivo (hoje são 6).

## Assumptions

- **Escopo**: os 6 modais da Home. A tabela e as abas da Home não mudam.
- **Sem mudança de backend**: só o frontend muda.
- **Sem testes automatizados novos**: o projeto não tem testes de frontend; a validação é feita
  pelo build e pelos roteiros manuais das features 001 a 006 (o Princípio IV da constituição trata
  de regras de negócio, e esta feature não cria nenhuma).
- **Modal de orçamento**: entra no escopo, mas mantém os estilos próprios do formulário; por isso
  o limite de tamanho do estilo dele pode continuar acima do configurado.
