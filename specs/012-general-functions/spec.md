# Feature Specification: Funções gerais (prateleira, alterar OS, editar cliente, reimprimir)

**Feature Branch**: `012-general-functions`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "[Imagem da tela 'Funções Gerais' do sistema antigo] no meu servico antigo tinha essa tela com essas opcoes, mas nao sei se essa e a melhor forma de organizar, se deixo tudo numa tela geral assim, se cada um tem uma rota, quais sao as melhores opcoes"

## Contexto

O sistema antigo tinha a tela **Funções Gerais** (item "Geral" do menu) com quatro cartões, cada um
pedindo um número de OS ou nome de cliente e um botão:

| Cartão antigo | O que fazia | Situação hoje no sistema novo |
|---------------|-------------|-------------------------------|
| Adicionar Prateleira | Nº OS + prateleira → grava a localização do aparelho | Já existe (rota REST) e já dá para editar a localização nos modais da Home; falta para OS fora das abas (ex.: entregues) |
| Alterar Ordem de Serviço | Nº OS → abre a OS inteira para editar (`old-handlers/EditServiceOrderHandler.cs`: cliente, aparelho, empresa, datas, solução, garantia, valores) | Rota antiga **quebrada** (MediatR) |
| Editar Cliente | Busca por nome → edita os dados do cliente | Já existe (busca + modal "Confirmar cliente" da tela Adicionar OS), mas só dentro do fluxo de criar OS |
| Reimprimir Ordem de Serviço | Nº OS → gera de novo o PDF certo para a situação (`old-handlers/RegenerateAndReturnPdfHandler.cs`) | Rota antiga **quebrada** (MediatR) |

A tela **Buscar OS** (feature 011) já acha qualquer OS e abre um painel com todos os dados — que é o
primeiro passo de três das quatro funções.

**Atores**: atendente da assistência, logado.

## Clarifications

### Session 2026-11-05

- Q: Como organizar as quatro funções? → A: **B** — Reimprimir, Alterar e Prateleira viram ações
  no painel da OS da tela **Buscar OS**; **Clientes** vira item próprio do menu (busca + editar); o
  item "Geral" sai do menu.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reimprimir uma OS (Priority: P1)

A pessoa precisa de outra via do documento de uma OS (cliente perdeu o termo, comprovante de
entrega/venda). Ela acha a OS em **Buscar OS**, abre o painel e clica em **Reimprimir**; o PDF certo para a
situação da OS abre para impressão.

**Why this priority**: é a função mais usada no balcão e hoje está quebrada.

**Independent Test**: reimprimir uma OS em cada situação (entrada, entregue, venda) e conferir o
documento.

**Acceptance Scenarios**:

1. **Given** o painel de uma OS aberto, **When** a pessoa clica em Reimprimir, **Then** o PDF gerado de novo
   (mesma regra do sistema antigo: termo de entrada, comprovante de entrega ou de venda, conforme a
   situação) abre numa nova aba com a impressão automática (ou é baixado, se a aba for bloqueada).
2. **Given** falha ao gerar o PDF, **Then** aparece a mensagem de erro e o painel continua aberto.

---

### User Story 2 - Alterar uma OS (Priority: P1)

A pessoa precisa corrigir algo numa OS (modelo digitado errado, defeito, empresa, valores, datas).
Ela acha a OS em **Buscar OS**, clica em **Alterar** no painel, edita os campos e salva.

**Why this priority**: sem isso, erros de digitação ficam para sempre; hoje está quebrado.

**Independent Test**: alterar campos de uma OS e conferir que foram gravados e que as abas da Home
refletem a mudança.

**Acceptance Scenarios**:

1. **Given** o painel de uma OS, **When** a pessoa clica em Alterar, **Then** os campos do painel
   ficam editáveis com os dados atuais
   (os mesmos campos do sistema antigo: aparelho, defeito, acessórios, localização, empresa, datas,
   solução, garantia, valores de peça e mão de obra).
2. **When** a pessoa salva, **Then** os dados são gravados, aparece "OS {n} alterada.", o painel
   volta a ser só leitura com os dados novos, a linha da tabela de busca é atualizada e as outras
   telas são avisadas para atualizar.
4. **Given** a edição aberta, **When** a pessoa clica em Cancelar (ou Esc), **Then** os campos
   voltam aos valores gravados e nada é salvo.
3. **Given** um campo obrigatório vazio (modelo, série), **Then** o campo fica em vermelho e Salvar
   fica bloqueado.

---

### User Story 3 - Mudar a prateleira (localização) (Priority: P2)

A pessoa guarda o aparelho numa prateleira e registra onde ele está, para qualquer OS (inclusive as
que não estão nas abas da Home).

**Acceptance Scenarios**:

1. **Given** o painel de uma OS, **When** a pessoa edita o campo Localização (prateleira) e confirma,
   **Then** a localização é gravada sem precisar entrar em "Alterar" e aparece nas abas, nos modais
   e no painel.
2. **Given** prateleira vazia, **Then** o botão fica bloqueado.

---

### User Story 4 - Editar um cliente (Priority: P2)

A pessoa atualiza telefone, endereço ou documento de um cliente sem precisar criar uma OS.

**Acceptance Scenarios**:

1. **Given** o item **Clientes** do menu (rota `/clientes`), **When** a pessoa busca por nome
   (mínimo 3 letras, igual à tela Adicionar OS) e escolhe o cliente, **Then** abre o formulário de
   cliente (o mesmo do modal "Confirmar cliente") e salvar grava os dados ("Cliente atualizado.").
2. **Given** o painel de uma OS, **Then** os dados do cliente são só leitura; a edição do cliente é
   feita em Clientes.

### Edge Cases

- **OS de venda**: Alterar mostra os campos de venda (sem defeito/acessórios); Reimprimir gera o
  comprovante de venda.
- **Alterar com dados do cliente**: o formulário antigo de "Alterar OS" também editava o cliente;
  aqui o cliente é editado pela função própria (US4), para não haver dois lugares que fazem o mesmo.
- **Duas pessoas alterando a mesma OS**: vale o último que salvar (como no sistema antigo).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Organização: o painel da OS da tela **Buscar OS** MUST ter as ações **Reimprimir**,
  **Alterar** e a edição rápida da **Localização**; o menu MUST ganhar o item **Clientes** (rota
  `/clientes`) e perder o item **Geral**.
- **FR-002**: Reimprimir MUST seguir `old-handlers/RegenerateAndReturnPdfHandler.cs` (mesmo
  documento conforme a situação) e abrir o PDF como nas outras telas (nova aba + impressão).
- **FR-003**: Alterar OS MUST seguir `old-handlers/EditServiceOrderHandler.cs` (mesmos campos e
  validações), exceto os dados do cliente (ver Edge Cases).
- **FR-004**: Prateleira MUST usar a gravação de localização que já existe.
- **FR-005**: Editar cliente MUST reaproveitar a busca e o formulário de cliente da tela Adicionar
  OS.
- **FR-006**: As rotas MUST seguir o padrão REST e sem MediatR; as rotas antigas quebradas de editar
  e reimprimir OS MUST ser substituídas.
- **FR-007**: As regras de Alterar e Reimprimir MUST ter testes automatizados.
- **FR-008**: Alterar MUST avisar as outras telas para atualizar.

### Key Entities

- **Ordem de serviço** e **Cliente**: existentes; nada muda no banco.

## Success Criteria *(mandatory)*

- **SC-001**: Reimprimir uma OS conhecida leva menos de 20 segundos, do menu ao PDF.
- **SC-002**: Corrigir um campo de uma OS leva menos de 1 minuto.
- **SC-003**: 100% das quatro funções do sistema antigo disponíveis.

## Assumptions

- **Campos de Alterar**: os do sistema antigo, sem os dados do cliente.
- **Padrão técnico**: igual às features anteriores (REST, handlers sem MediatR, textos em pt-BR,
  visual das telas atuais, sem mensagens embaixo dos campos).
- **Fora do escopo**: pagamentos (outras funções antigas), histórico de alterações.
