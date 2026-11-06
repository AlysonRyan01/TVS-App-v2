# Feature Specification: Tela Clientes no padrão da tela de Ordens

**Feature Branch**: `020-customers-grid-page`

**Created**: 2026-11-06

**Status**: Draft

**Input**: User description: "vamos deixar a tela Clientes no mesmo padrao da buscar Os, uma header com as pesquisar e embaixo uma grid de itens, tambem altere o buscar os para \"Ordens\""

## Contexto

A tela **Buscar OS** já segue um padrão que funciona bem: no topo, os campos de pesquisa; embaixo, uma
grade com os resultados, que ao abrir já mostra os registros mais recentes, com paginação, e um clique
na linha abre o detalhe. A tela **Clientes**, por outro lado, só tem um campo de busca por nome com uma
lista suspensa — não dá para ver os clientes cadastrados nem pesquisar por telefone ou documento. Esta
feature leva a tela Clientes para o mesmo padrão e renomeia "Buscar OS" para **"Ordens"**.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver e pesquisar clientes numa grade (Priority: P1)

O atendente abre **Clientes** e já vê a lista dos clientes mais recentes numa grade. No topo há campos
para pesquisar por nome, telefone ou CPF/CNPJ; ao pesquisar, a grade mostra só quem corresponde.

**Why this priority**: é o pedido principal; hoje não há como enxergar os clientes cadastrados nem
encontrá-los por telefone ou documento (o caso comum no balcão: "qual o seu telefone?").

**Independent Test**: abrir Clientes → grade com os clientes mais recentes; digitar parte de um
telefone → grade só com os clientes daquele telefone.

**Acceptance Scenarios**:

1. **Given** clientes cadastrados, **When** o atendente abre Clientes, **Then** vê uma grade com os mais
   recentes primeiro, com nome, telefone, CPF/CNPJ e cidade, e o total de clientes.
2. **Given** a grade aberta, **When** pesquisa por parte do nome (mínimo 3 letras), **Then** a grade
   mostra só os clientes cujo nome contém o texto, sem diferenciar maiúsculas/minúsculas e acentos.
3. **Given** a grade aberta, **When** pesquisa por telefone (só os dígitos, com ou sem DDD e
   pontuação), **Then** a grade mostra os clientes com esse telefone (principal ou secundário).
4. **Given** a grade aberta, **When** pesquisa por CPF/CNPJ (com ou sem pontuação), **Then** a grade
   mostra o cliente com esse documento.
5. **Given** mais de 20 resultados, **When** o atendente navega pelas páginas, **Then** vê 20 clientes
   por página e a indicação do total.
6. **Given** uma pesquisa sem resultados, **When** termina, **Then** a grade informa "Nenhum cliente
   encontrado."
7. **Given** filtros preenchidos, **When** clica em **Limpar**, **Then** os campos são esvaziados e a grade
   volta aos clientes mais recentes.

---

### User Story 2 - Editar o cliente a partir da grade (Priority: P1)

Ao clicar (ou tocar, ou pressionar Enter) numa linha da grade, abre-se a edição do cliente, como hoje.
Ao salvar, a grade mostra os dados atualizados.

**Why this priority**: editar é o motivo de a tela existir hoje; não pode se perder na mudança.

**Independent Test**: clicar num cliente → edição abre com os dados dele; alterar o telefone e salvar →
a linha da grade mostra o telefone novo.

**Acceptance Scenarios**:

1. **Given** a grade com clientes, **When** o atendente clica numa linha, **Then** abre a edição daquele
   cliente.
2. **Given** a edição aberta, **When** salva uma alteração, **Then** a grade é atualizada sem perder a
   pesquisa e a página atuais.
3. **Given** a edição aberta, **When** cancela, **Then** nada muda na grade.
4. **Given** outro atendente alterou ou cadastrou um cliente, **When** isso acontece, **Then** a grade é
   atualizada automaticamente (como as demais telas do sistema).

---

### User Story 3 - "Buscar OS" passa a se chamar "Ordens" (Priority: P2)

No menu, no título da tela e no título da aba do navegador, "Buscar OS" passa a ser **"Ordens"**. A tela
continua funcionando igual.

**Why this priority**: ajuste de nome para combinar com "Clientes"; sem mudança de comportamento.

**Independent Test**: abrir o menu → item "Ordens"; abrir a tela → título "Ordens" na tela e na aba.

**Acceptance Scenarios**:

1. **Given** o menu, **When** o atendente o abre, **Then** vê "Ordens" no lugar de "Buscar OS".
2. **Given** a tela de ordens aberta, **When** olha o título da tela e da aba, **Then** ambos dizem
   "Ordens".
3. **Given** um favorito ou link antigo para a tela de busca de OS, **When** é aberto, **Then** leva à tela
   Ordens.

---

### Edge Cases

- Nenhum cliente cadastrado: a grade informa "Nenhum cliente cadastrado."
- Pesquisa por nome com menos de 3 letras: não pesquisa e orienta "Digite pelo menos 3 letras".
- Telefone ou documento digitados com pontuação, espaços ou parênteses: são considerados só os dígitos.
- Cliente sem CPF/CNPJ ou sem cidade: a célula fica vazia (ou "—"), sem erro.
- Nomes longos: quebram em linhas dentro da célula; no celular, a grade vira cartões, como nas outras telas.
- Pesquisa enquanto outra ainda carrega: vale o resultado da última pesquisa.
- Erro de comunicação: mensagem em português e a grade mantém o último resultado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A tela Clientes MUST ter, no topo, campos de pesquisa por **Nome**, **Telefone** e
  **CPF/CNPJ**, com os botões **Limpar** e **Buscar**, no mesmo arranjo visual da tela Ordens.
- **FR-002**: Abaixo dos campos, a tela MUST exibir uma grade de clientes com as colunas **Nome**,
  **Telefone** (principal e, se houver, o secundário), **CPF/CNPJ** formatado, **Cidade/UF** e **E-mail**.
- **FR-003**: Ao abrir a tela, a grade MUST mostrar os clientes mais recentes primeiro, sem precisar pesquisar.
- **FR-004**: A grade MUST ser paginada com 20 clientes por página e mostrar o total ("Últimos clientes ·
  N no total" sem filtro; "N encontrados" com filtro).
- **FR-005**: A pesquisa por nome MUST exigir no mínimo 3 letras e encontrar o texto em qualquer parte do
  nome, sem diferenciar maiúsculas/minúsculas.
- **FR-006**: A pesquisa por telefone MUST considerar só os dígitos e encontrar tanto o telefone principal
  quanto o secundário.
- **FR-007**: A pesquisa por CPF/CNPJ MUST considerar só os dígitos.
- **FR-008**: Os filtros preenchidos MUST ser combinados (todos precisam corresponder).
- **FR-009**: Clicar, tocar ou pressionar Enter numa linha MUST abrir a edição do cliente; salvar MUST
  atualizar a grade mantendo pesquisa e página.
- **FR-010**: A grade MUST se atualizar automaticamente quando um cliente for cadastrado ou alterado.
- **FR-011**: No celular, a grade MUST seguir o mesmo comportamento das outras grades (cada linha vira um
  cartão, sem rolagem lateral).
- **FR-012**: O menu, o título da tela e o título da aba da tela "Buscar OS" MUST passar a dizer
  **"Ordens"**; endereços antigos dessa tela MUST continuar levando a ela.
- **FR-013**: Todos os textos MUST estar em português.

### Key Entities

- **Cliente (na grade)**: nome, telefone principal, telefone secundário, CPF/CNPJ, cidade, UF, e-mail;
  identificador para abrir a edição.
- **Pesquisa de clientes**: nome (parcial), telefone (dígitos), CPF/CNPJ (dígitos), página.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: O atendente encontra um cliente pelo telefone em até 10 segundos a partir da tela Clientes.
- **SC-002**: Ao abrir a tela Clientes, a grade com os clientes recentes aparece em até 2 segundos.
- **SC-003**: 100% dos clientes são alcançáveis navegando pelas páginas sem filtro.
- **SC-004**: A tela Clientes e a tela Ordens têm o mesmo arranjo (filtros no topo, grade embaixo, paginação
  no rodapé), verificável lado a lado.
- **SC-005**: Nenhum ponto do sistema mostra mais "Buscar OS".

## Assumptions

- "Mais recentes" = cadastrados por último (maior número de cliente primeiro).
- A edição usa a mesma janela de edição de cliente que existe hoje.
- Cadastro de cliente novo continua sendo feito nos fluxos atuais (abrir OS/venda); um botão "Novo cliente"
  nesta tela fica fora do escopo.
- Exclusão de clientes fica fora do escopo.
- O endereço da tela de ordens pode mudar para refletir o novo nome, com o endereço antigo redirecionando.
- O sistema antigo, que usa o mesmo banco, não é afetado.
