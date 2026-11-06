# Feature Specification: Tela "Vender"

**Feature Branch**: `010-sell-page`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "vamos criar a tela da rota vender, vai ser basicamente a mesma tela da ordem de servico. veja como o fluxo de venda funcionava no handler antigo"

## Contexto

A venda de um aparelho no balcão é registrada como uma **ordem de serviço de venda**, como fazia o
handler antigo (`old-handlers/CreateSalesServiceOrderHandler.cs`). Ele cria uma OS com:

- defeito = `"VENDA"` e acessórios vazios;
- empresa = Particular;
- situação "Entregue", orçamento "Aprovado", resultado "Tem conserto" e data de entrega = agora;
- valor da venda no **custo de peça**;
- a garantia da venda.

Depois gera o **comprovante de venda em PDF** e envia ao cliente o **WhatsApp de venda** (nome,
valor, garantia e tipo do produto). As métricas já separam vendas de reparos pelo defeito
`"VENDA"`, e isso continua igual.

Hoje não há tela para isso: o item "Vender" do menu não leva a lugar nenhum e a rota de venda
(`create-sales-service-order`) está **quebrada** (depende do MediatR, cujos handlers foram
removidos, como nas features anteriores).

**Atores**: atendente da assistência, logado, vendendo um aparelho no balcão.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registrar uma venda no balcão (Priority: P1)

A pessoa abre **Vender** no menu, escolhe ou cria o cliente (igual à tela Adicionar OS), preenche o
aparelho vendido, a garantia e o valor, e clica em **Criar venda**. A venda é registrada, o
comprovante abre para impressão e o cliente recebe o WhatsApp da venda.

**Why this priority**: é o objetivo da tela; hoje não há como registrar venda pelo sistema novo.

**Independent Test**: registrar uma venda para um cliente e conferir o comprovante impresso, o
WhatsApp enfileirado, que a OS de venda **não** aparece nas abas da Home e que as métricas contam a
venda.

**Acceptance Scenarios**:

1. **Given** o menu, **When** a pessoa clica em **Vender**, **Then** abre a tela `/vender`, com a
   coluna **① Cliente** (busca, modal de confirmação e "Criar cliente", iguais aos da tela Adicionar
   OS) e a coluna **② Venda**, desfocada até haver cliente escolhido.
2. **Given** um cliente escolhido, **When** a pessoa preenche tipo de produto, marca, modelo*,
   número de série*, garantia* e valor* e clica em **Criar venda**, **Then** a venda é registrada,
   o comprovante de venda em PDF abre com a impressão automática, aparece "Venda registrada (OS
   {número})." e a tela volta ao início.
3. **Given** uma venda registrada, **Then** o cliente recebe o WhatsApp de venda (nome, valor,
   garantia, tipo do produto), a OS de venda fica como "Entregue" (não aparece nas abas da Home) e
   entra nas métricas como venda.

---

### User Story 2 - Correções e erros (Priority: P2)

Mesmos comportamentos da tela Adicionar OS: trocar cliente, campos obrigatórios em vermelho, sem
clique duplo e mensagem quando algo falha.

**Why this priority**: consistência com a tela já conhecida.

**Independent Test**: tentar criar sem garantia ou com valor 0, trocar cliente, derrubar a API.

**Acceptance Scenarios**:

1. **Given** modelo, série ou garantia vazios, ou valor 0, **Then** o campo fica em vermelho e
   **Criar venda** fica bloqueado.
2. **Given** a pessoa clica em "Trocar cliente", **Then** os dados da venda já digitados continuam.
3. **Given** uma falha ao registrar, **Then** aparece a mensagem do sistema e nada do que foi
   digitado se perde.
4. **Given** o PDF falhar depois da venda registrada, **Then** aparece a mensagem de erro e a venda
   continua registrada (igual ao handler antigo).

---

### Edge Cases

- **Venda sem garantia**: recusada ("A venda precisa ter uma garantia"), como no handler antigo.
- **Valor 0**: recusado ("O valor da venda não pode ser vazio ou 0"). A tela só aceita valores
  maiores que 0; o servidor mantém a regra do handler antigo (recusa só o 0).
- **Marca vazia**: aceita (o handler antigo não exigia marca).
- **Cliente inexistente** (apagado entre a busca e a venda): mensagem "Cliente não encontrado".
- **Aba do PDF bloqueada pelo navegador**: o comprovante é baixado, como nas telas de entrega e
  Adicionar OS.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A tela **Vender** MUST existir na rota `/vender` (só para usuário logado), ligada ao
  item "Vender" do menu, com o mesmo layout da tela Adicionar OS: coluna ① Cliente (mesma busca,
  modal de confirmação e "Criar cliente") e coluna ② Venda, desfocada sem cliente.
- **FR-002**: A coluna ② Venda MUST ter: Nome do cliente (só leitura), Tipo de Produto (padrão TV),
  Marca, Modelo*, Número de Série*, Garantia* e Valor* (em reais), e o botão **Criar venda** com a
  largura toda. Sem mensagens de erro embaixo dos campos (padrão das telas atuais).
- **FR-003**: **Criar venda** MUST seguir exatamente o fluxo do handler antigo: validar (cliente,
  modelo, número de série, garantia, valor ≠ 0, tipo válido); normalizar (marca, modelo, série e
  garantia sem espaços nas pontas e em maiúsculas); criar a OS de venda com os dados do Contexto;
  gerar o comprovante de venda em PDF (se falhar, responder com erro, com a OS já criada);
  enfileirar o WhatsApp de venda; devolver o PDF.
- **FR-004**: A resposta MUST trazer também o número da OS de venda, para a tela mostrar "Venda
  registrada (OS {número})."
- **FR-005**: O PDF MUST abrir numa nova aba com a impressão automática (ou ser baixado, se a aba
  for bloqueada), como nas telas de entrega e Adicionar OS.
- **FR-006**: A rota MUST ser REST e sem MediatR, como nas features anteriores; a rota antiga
  `create-sales-service-order` MUST ser substituída.
- **FR-007**: Registrar uma venda MUST avisar as outras telas para atualizar.
- **FR-008**: As regras de venda MUST ter testes automatizados (validação, normalização, dados da
  OS de venda, falha ao criar, falha no PDF, WhatsApp).
- **FR-009**: Depois de registrar, a tela MUST voltar ao início (sem cliente, busca vazia, venda com
  os padrões), como a tela Adicionar OS.

### Key Entities *(include if feature involves data)*

- **Ordem de serviço de venda**: uma ordem de serviço com defeito "VENDA", empresa Particular, já
  entregue e aprovada, com o valor da venda no custo de peça e a garantia. Sem mudança no banco.
- **Cliente**: existente; mesma busca e cadastro da tela Adicionar OS.
- **Comprovante de venda (PDF)** e **WhatsApp de venda**: já existem; usados como estão.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Uma venda para cliente já cadastrado é registrada em menos de 1 minuto, com o
  comprovante impresso.
- **SC-002**: 100% das vendas registradas pela tela aparecem nas métricas como venda e 0 aparecem
  nas abas da Home.
- **SC-003**: 0 vendas duplicadas por clique duplo.
- **SC-004**: Nenhuma mudança de comportamento na tela Adicionar OS.

## Assumptions

- **Modelo de dados**: sem entidade nova e sem migration; a venda continua sendo uma OS com defeito
  "VENDA" (decisão do usuário, após desistir da entidade própria).
- **Campos**: os mesmos do handler antigo (sem defeito, acessórios ou empresa na tela).
- **Garantia**: texto livre (ex.: "90 DIAS"), como no handler antigo.
- **Sem pagamentos**: esta feature não cria tela de pagamento; o valor da venda é o custo de peça.
- **Fora do escopo**: listagem, edição e cancelamento de vendas; mudanças nas métricas.
- **Padrão técnico**: igual às features anteriores (REST, handlers sem MediatR, textos em pt-BR,
  mesmos componentes visuais da tela Adicionar OS).
