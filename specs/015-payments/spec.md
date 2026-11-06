# Feature Specification: Pagamentos da OS + comprovante

**Feature Branch**: `015-payments`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "no /home/alyson/Documentos/github/TVS-App veja onde os pagamentos sao utilizados no frontend, use nas mesmas paginas e tambem crie um botao para imprimir um comprovante de pagamento"

## Contexto

No sistema antigo (`TVS-App/Src/TVSApp.Web/Components/PaymentComponent.razor`), cada OS tem uma
caixa **Pagamentos**: lista (valor, data), total pago, **Adicionar**, **Editar** e **Remover**
(com confirmação). Ela aparece em **7 lugares**: os 6 modais de ação da Home (Orçamento, Resposta,
Peça comprada, Conserto, Liberar reprovado, Entrega) e o diálogo de alterar OS.

No sistema novo, as rotas de pagamento já existem (feature 014), mas nenhuma tela as usa. Os
equivalentes dos 7 lugares são: os **6 modais da Home** (que têm a mesma "etiqueta" da OS) e o
**painel da OS na tela Buscar OS** (onde fica o Alterar).

Novo nesta feature: **imprimir um comprovante de pagamento**.

**Atores**: atendente da assistência, logado, recebendo um valor do cliente (sinal, parcial ou
quitação).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registrar e ver os pagamentos de uma OS (Priority: P1)

Em qualquer modal da Home ou no painel da Buscar OS, a pessoa vê a caixa **Pagamentos** da OS:
os pagamentos feitos (valor e data), o total pago, o valor da OS e quanto falta. Pode adicionar,
corrigir e remover pagamentos.

**Why this priority**: é o controle de quanto o cliente já pagou; existia no sistema antigo.

**Independent Test**: abrir uma OS, adicionar dois pagamentos, editar um, remover outro e conferir
lista e totais; fechar e abrir de novo (em outro modal) e ver os mesmos dados.

**Acceptance Scenarios**:

1. **Given** um modal da Home ou o painel da Buscar OS aberto, **Then** aparece a caixa
   **Pagamentos** com a lista (mais recente primeiro), **Total pago**, **Valor da OS** e **Falta**
   (valor da OS − total pago; "Quitado" quando chega a zero).
2. **When** a pessoa clica em **Adicionar**, informa valor e data (padrão: hoje) e salva, **Then** o
   pagamento entra na lista, os totais atualizam e aparece "Pagamento adicionado.".
3. **When** a pessoa edita um pagamento e salva, **Then** os novos valores aparecem e "Pagamento
   atualizado.".
4. **When** a pessoa remove um pagamento e confirma ("Remover este pagamento?"), **Then** ele sai da
   lista e "Pagamento removido.".
5. **Given** uma OS sem pagamentos, **Then** aparece "Nenhum pagamento registrado." e o botão
   Adicionar.

---

### User Story 2 - Imprimir comprovante de pagamento (Priority: P1)

Depois de receber um valor, a pessoa imprime um comprovante para entregar ao cliente.

**Why this priority**: é o pedido novo; dá ao cliente um papel do que pagou.

**Independent Test**: em uma OS com pagamentos, clicar em imprimir num pagamento e conferir o PDF.

**Acceptance Scenarios**:

1. **Given** um pagamento na lista, **When** a pessoa clica em **Imprimir comprovante** na linha,
   **Then** abre numa nova aba, com a impressão automática (ou baixa, se a aba for bloqueada), um
   comprovante com: dados da loja (como nos outros PDFs), número da OS, cliente (nome, telefone,
   documento), aparelho (tipo, marca, modelo, série), **valor deste pagamento por extenso e em
   número**, data do pagamento, número do comprovante, e o resumo: valor da OS, total pago até
   este pagamento e saldo restante.
2. **Given** um erro ao gerar o comprovante, **Then** aparece a mensagem e nada mais muda.

---

### User Story 3 - Validações e erros (Priority: P2)

**Acceptance Scenarios**:

1. **Given** valor vazio, zero ou negativo, ou data vazia, **Then** o campo fica em vermelho e
   Salvar fica bloqueado (o sistema antigo avisava "O valor deve ser maior que zero").
2. **Given** uma falha ao salvar, **Then** aparece a mensagem e o formulário continua aberto com o
   que foi digitado.

### Edge Cases

- **Pagamento maior que o valor da OS** (ou OS ainda sem orçamento): permitido, como no antigo
  (sinal antes do orçamento); "Falta" fica negativo e aparece como **"Crédito de R$ X"**.
- **OS de venda**: também mostra Pagamentos (a venda pode ser parcelada).
- **Data futura**: permitida (pagamento combinado), sem bloqueio.
- **Modais pequenos**: a caixa Pagamentos fica abaixo das informações da OS, sem tirar espaço do
  formulário principal de cada modal.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A caixa **Pagamentos** MUST aparecer nos 6 modais de ação da Home e no painel da OS
  da Buscar OS, com o mesmo comportamento em todos.
- **FR-002**: Listar, adicionar, editar e remover pagamentos MUST usar as regras já existentes
  (feature 014) e mostrar as mensagens da US1.
- **FR-003**: A caixa MUST mostrar Total pago, Valor da OS e Falta / Quitado / Crédito.
- **FR-004**: Cada pagamento MUST ter **Imprimir comprovante**, gerando o PDF da US2 no padrão
  visual dos documentos atuais e abrindo como os outros PDFs (nova aba + impressão).
- **FR-005**: O comprovante MUST ser gerado pelo sistema a partir dos dados gravados (não do que
  está na tela), com número do comprovante = número do pagamento.
- **FR-006**: Valor > 0 e data obrigatória MUST ser validados na tela e no servidor.
- **FR-007**: As regras novas (validação do valor, cálculo de total/saldo do comprovante) MUST ter
  testes automatizados.

### Key Entities

- **Pagamento**: valor, data, OS (já existe; nada muda no banco).
- **Comprovante de pagamento**: documento gerado sob demanda; não é guardado.

## Success Criteria *(mandatory)*

- **SC-001**: Registrar um pagamento e imprimir o comprovante leva menos de 30 segundos.
- **SC-002**: 100% dos 7 lugares mostram a mesma caixa de pagamentos, com os mesmos números.
- **SC-003**: O saldo do comprovante bate com valor da OS − total pago em 100% dos casos.

## Assumptions

- **Padrão técnico**: igual às features anteriores (REST, handlers sem MediatR, textos em pt-BR,
  visual atual, sem mensagens embaixo dos campos).
- **Comprovante por pagamento** (não um extrato da OS inteira); o resumo do saldo já informa o
  total.
- **Fora do escopo**: forma de pagamento (dinheiro, Pix, cartão), estorno, relatório de
  recebimentos.
