# Feature Specification: Tela "Buscar OS"

**Feature Branch**: `011-search-service-orders`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "vamos criar a tela de busca de ordem, onde vamos ter filtro por ordem, clientes, modelo, serie e data inicial-final, me de opcoes de telas para isso"

## Contexto

O item "Buscar OS" do menu não leva a lugar nenhum. O sistema antigo tinha uma busca separada para
cada campo (`old-handlers/GetServiceOrderByIdHandler.cs`, `GetServiceOrdersByCustomerNameHandler.cs`,
`GetServiceOrdersByModelHandler.cs`, `GetServiceOrdersBySerialNumberHandler.cs`,
`GetServiceOrdersByDateHandler.cs`), com limitações: a busca por cliente trazia só as 5 OS mais
recentes, a por data trazia tudo sem paginação, e as rotas atuais estão quebradas (MediatR sem
handler), exceto a por número (`/service-orders/{id}`).

**Atores**: atendente da assistência, logado, procurando uma OS (cliente ligou, aparelho na
bancada sem etiqueta, conferência do mês).

## Clarifications

### Session 2026-11-05

- Q: Qual layout? → A: **A** — barra de filtros no topo + tabela paginada embaixo (largura toda).
- Q: Os filtros se combinam? → A: **B** — busca por **um campo de cada vez**, como no sistema antigo.
  O período (data inicial + final) conta como um campo só.
- Q: O que abre ao clicar numa OS? → A: **A** — painel só de leitura com todos os dados da OS.
- Ajuste (pedido do usuário): sem filtro, a tela lista as **últimas OS** (paginadas, mais novas
  primeiro) — abre assim e volta a isso no Limpar. Substitui o antigo `get-all-service-orders`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Achar uma OS pelos filtros (Priority: P1)

A pessoa abre **Buscar OS**, preenche **um** filtro (número da OS, nome do cliente, modelo, número
de série ou período de entrada) e vê a lista de OS que atendem.

**Why this priority**: é o objetivo da tela.

**Independent Test**: buscar por cada filtro e conferir os resultados; conferir que preencher um filtro limpa os outros.

**Acceptance Scenarios**:

1. **Given** a tela aberta, **When** a pessoa digita o número de uma OS existente e busca,
   **Then** aparece só essa OS.
2. **Given** um trecho do nome do cliente (mínimo 3 letras), **When** busca, **Then** aparecem as
   OS de clientes cujo nome contém o trecho, sem diferenciar maiúsculas e minúsculas.
3. **Given** um trecho do modelo ou do número de série, **When** busca, **Then** aparecem as OS
   cujo modelo/série contém o trecho.
4. **Given** data inicial e final, **When** busca, **Then** aparecem as OS com entrada entre as
   duas datas (inclusive o dia final inteiro).
5. **Given** um filtro preenchido, **When** a pessoa começa a digitar em outro, **Then** o
   anterior é limpo: a busca usa sempre um campo só (o período, com data inicial e final, conta
   como um campo).
6. **Given** a lista de resultados, **Then** cada linha mostra número da OS, cliente, aparelho
   (tipo, marca, modelo), número de série, empresa, situação e data de entrada, da OS mais nova
   para a mais antiga, com paginação.

---

### User Story 2 - Abrir a OS encontrada (Priority: P1)

Ao achar a OS, a pessoa clica na linha para ver os detalhes.

**Why this priority**: achar sem poder ver não resolve o atendimento.

**Independent Test**: clicar num resultado e conferir o que abre.

**Acceptance Scenarios**:

1. **Given** um resultado, **When** a pessoa clica na linha (ou Enter com a linha em foco),
   **Then** abre um painel **só de leitura** com: cliente (nome, telefones, documento, endereço),
   aparelho (tipo, marca, modelo, série, defeito, acessórios, localização), empresa, situação,
   orçamento (solução, garantia, valor de peça, mão de obra, total) e datas (entrada, avaliação,
   resposta, compra de peça, conserto, entrega).
2. **Given** o painel aberto, **When** a pessoa fecha (botão Fechar ou Esc), **Then** volta à
   lista com os mesmos filtros, resultados e página.

---

### User Story 3 - Busca sem resultado e erros (Priority: P2)

**Acceptance Scenarios**:

1. **Given** filtros sem nenhuma OS, **Then** aparece "Nenhuma ordem de serviço encontrada." e
   os filtros continuam preenchidos.
2. **Given** data final antes da inicial, **Then** o campo fica em vermelho e a busca não roda.
3. **Given** nome de cliente com menos de 3 letras, **Then** o campo fica em vermelho e a busca
   não roda.
4. **Given** nenhum filtro preenchido, **Then** a lista mostra as últimas OS (ao abrir a tela, ao
   clicar em Buscar e ao clicar em Limpar).
5. **Given** falha no sistema, **Then** aparece a mensagem de erro e os filtros não se perdem.

### Edge Cases

- **OS de venda** (defeito "VENDA") também aparecem, com a situação "Venda".
- **Número da OS** inexistente → lista vazia com a mensagem da US3-1.
- **Muitos resultados** (ex.: um ano inteiro): paginação de 20 por página, com o total encontrado.
- **Espaços e maiúsculas** nos textos são ignorados (" lg " acha "LG").
- **Datas**: só a inicial → da inicial até hoje; só a final → até a final.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A tela **Buscar OS** MUST existir na rota `/buscar` (só para usuário logado), ligada
  ao item "Buscar OS" do menu, seguindo o visual das telas atuais.
- **FR-002**: Filtros: Número da OS, Cliente (nome, ≥ 3 letras), Modelo, Número de Série e
  Período (data inicial e final de entrada). A busca usa **um filtro por vez**: preencher um limpa
  os outros.
- **FR-003**: Layout: barra de filtros no topo (largura toda, quebrando em duas linhas se precisar,
  com **Limpar** e **Buscar** no fim) e a tabela de resultados embaixo, com paginação e o total.
- **FR-004**: Resultados em tabela paginada (20 por página), da OS mais nova para a mais antiga,
  com as colunas da US1-6 e o total encontrado.
- **FR-005**: A busca roda pelo botão **Buscar** e pela tecla Enter em qualquer filtro; **Limpar**
  apaga os filtros e os resultados.
- **FR-006**: As mensagens de "nenhum resultado", validação e erro seguem a US3.
- **FR-007**: As rotas de busca MUST seguir o padrão REST e sem MediatR das features anteriores; as
  rotas antigas de busca por campo que estão quebradas MUST ser substituídas.
- **FR-008**: Clicar num resultado MUST abrir o painel só de leitura da US2.
- **FR-009**: As regras da busca (validações, um filtro por vez, datas, paginação) MUST ter testes
  automatizados.

### Key Entities

- **Ordem de serviço**: existente; nada muda no banco.
- **Filtro de busca**: um de: número, trecho do nome do cliente, trecho do modelo, trecho da
  série, período de entrada.

## Success Criteria *(mandatory)*

- **SC-001**: Uma OS conhecida pelo número, cliente, modelo ou série é encontrada em menos de 15
  segundos.
- **SC-002**: Os resultados aparecem em menos de 2 segundos para um mês inteiro de OS.
- **SC-003**: 100% das buscas sem resultado mostram a mensagem, sem tela vazia sem explicação.

## Assumptions

- **Data** = data de entrada da OS (como no sistema antigo).
- **Todas as situações** entram na busca, inclusive entregues e vendas.
- **Fora do escopo**: editar ou agir sobre a OS pela busca (o painel é só leitura), reimprimir PDF,
  exportar resultados, filtro por empresa/situação, combinar filtros.
- **Padrão técnico**: igual às features anteriores (REST, handlers sem MediatR, textos em pt-BR).
