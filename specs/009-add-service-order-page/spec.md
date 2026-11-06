# Feature Specification: Tela "Adicionar OS" (cliente + nova ordem de serviço)

**Feature Branch**: `009-add-service-order-page`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "vamos criar a rota adicionar e implementar a tela de adicionar ordens e clientes, deve ser uma tela apenas que adiciona clientes e cria ordens. Deve ter um campo search que busca clientes, caso o cliente nao exista ele cria um cliente novo, caso o cliente exista vai abrir um modal central com as informacoes do cliente existente (modal para confirmar dados ou atualizar, como telefone, endereco, etc), apos confirmar esse cliente deverá ser utilizado na nova ordem que iremos criar nessa mesma pagina. siga com rotas REST, olhe os handlers antigo que existiam e siga o fluxo, tambem siga o padrao de design existente do angular. Nao tenho certeza de como essa tela vai ser desenhada, me de opcoes"

## Contexto

Hoje a entrada de um aparelho na assistência não tem tela: o menu lateral já mostra **Adicionar OS**,
mas o item não leva a lugar nenhum. As rotas da API para isso existem, mas:

- `create-service-order` está **quebrada** (MediatR sem handler, mesmo defeito das features 001 a
  006). O handler antigo (`old-handlers/CreateServiceOrderAndReturnPdfHandler.cs`) cria a OS, gera o
  **termo de entrada em PDF** e envia ao cliente o **WhatsApp de boas-vindas**;
- as rotas de cliente (`create-customer`, `update-customer`, `get-customer-by-name`) funcionam, mas
  estão no padrão antigo (verbo no nome).

Esta feature cria a tela **Adicionar OS** (rota `/adicionar`), onde a pessoa do balcão, numa página
só: encontra ou cadastra o cliente, confirma ou atualiza os dados dele e registra o aparelho que
está entrando, gerando a OS e o termo de entrada para imprimir.

**Atores**: atendente da assistência, logado, com o cliente e o aparelho no balcão.

## Clarifications

### Session 2026-11-05

- Q: Como organizar a tela? → A: Duas colunas — cliente à esquerda, aparelho à direita; em telas
  estreitas as colunas se empilham. *(Revisado abaixo: colunas 50/50 e botões embaixo de cada formulário.)*
- Q: A busca encontra clientes só pelo nome ou também por telefone e CPF? → A: Só pelo nome (busca
  atual da API).
- Q: Depois de criar a OS, a página volta ao início ou mantém o cliente? → A: Volta ao início (sem
  cliente e sem aparelho).
- Q: Ajustes de layout depois da primeira versão? → A: (1) cadastrar cliente **não** usa modal: o
  formulário de novo cliente fica sempre visível embaixo da busca; o modal fica só para confirmar ou
  atualizar um cliente existente; (2) colunas com **50%** cada (cliente | ordem de serviço); (3) os
  botões ficam **embaixo de cada formulário**: "Cadastrar cliente" e "Criar OS" (sem rodapé fixo).
- Q: O CPF continua obrigatório? → A: Não. O documento do cliente passa a ser opcional (com
  migration).
- Q: Como aceitar CNPJ: campo novo ou renomear o CPF? → A: Renomear o CPF para **Documento**, que
  guarda CPF ou CNPJ; o tipo é deduzido pelo tamanho (11 = CPF, 14 = CNPJ). A coluna é renomeada por
  migration, mantendo os dados atuais, e passa a aceitar vazio.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Abrir OS para cliente já cadastrado (Priority: P1)

O cliente já veio antes. A pessoa digita parte do nome na busca, escolhe o cliente na lista, confere
os dados num modal no centro da tela (telefone, endereço etc.), corrige o que mudou e confirma. O
cliente fica selecionado na página; ela preenche os dados do aparelho e clica em **Criar OS**. A OS é
criada, o termo de entrada abre para impressão e o cliente recebe o WhatsApp de boas-vindas.

**Why this priority**: é o caso mais comum no balcão e o que mais se beneficia da busca.

**Independent Test**: buscar um cliente existente, confirmar no modal, preencher o aparelho, criar a
OS e conferir que ela aparece em "Orçamentos pendentes" na Home, com o termo impresso.

**Acceptance Scenarios**:

1. **Given** a tela Adicionar OS, **When** a pessoa digita pelo menos 3 letras do nome, **Then**
   aparece a lista de clientes cujo nome contém o texto (nome, telefone e CPF/CNPJ de cada um).
2. **Given** a lista de resultados, **When** a pessoa escolhe um cliente, **Then** abre um modal no
   centro da tela com os dados dele, todos editáveis: nome, CPF/CNPJ, telefone, telefone 2, e-mail,
   CEP, rua, número, bairro, cidade e UF.
3. **Given** o modal aberto, **When** a pessoa clica em **Confirmar** sem mudar nada, **Then** o modal
   fecha e o cliente fica selecionado na página (cartão com nome, telefone e endereço resumido).
4. **Given** o modal aberto, **When** a pessoa altera dados e clica em **Salvar e usar**, **Then** os
   dados do cliente são atualizados, aparece "Cliente atualizado." e o cliente fica selecionado.
5. **Given** um cliente selecionado e os dados do aparelho preenchidos (empresa, tipo, marca,
   modelo, número de série, defeito, acessórios), **When** a pessoa clica em **Criar OS**, **Then** a OS é
   criada, o termo de entrada em PDF abre numa nova aba com a impressão automática (mesmo
   comportamento do termo de entrega), aparece "OS {número} criada." e a página volta ao início: sem cliente selecionado, busca vazia
   e campos do aparelho limpos (empresa e tipo voltam aos padrões).
6. **Given** uma OS criada, **Then** o cliente recebe o WhatsApp de boas-vindas (número da OS e nome) e
   a OS aparece na aba "Orçamentos pendentes" da Home (as telas abertas são avisadas para atualizar).

---

### User Story 2 - Abrir OS para cliente novo (Priority: P1)

O cliente nunca veio. A busca não encontra ninguém, e a pessoa cadastra o cliente na mesma página,
sem sair dela, e segue para o aparelho.

**Why this priority**: tem a mesma importância; todo cliente novo passa por aqui.

**Independent Test**: buscar um nome que não existe, cadastrar o cliente, criar a OS e conferir o
cliente e a OS criados.

**Acceptance Scenarios**:

1. **Given** a tela sem cliente escolhido, **Then** o formulário **Novo cliente** está sempre visível
   embaixo da busca. **Given** uma busca sem resultados, **Then** aparece "Nenhum cliente encontrado.
   Cadastre abaixo." e o botão **Usar "{texto}" no cadastro**, que preenche o nome no formulário.
2. **Given** o formulário de cliente novo, **When** a pessoa informa o CEP, **Then** rua, bairro, cidade
   e UF são preenchidos automaticamente (a pessoa ainda pode corrigir).
3. **Given** o formulário preenchido, **When** a pessoa clica em **Cadastrar cliente** (embaixo do
   formulário), **Then** o cliente é criado, aparece "Cliente cadastrado." e ele fica selecionado para
   a OS.
4. **Given** um cliente novo selecionado, **Then** o fluxo segue igual ao da US1 (cenários 5 e 6).

---

### User Story 3 - Correções e erros (Priority: P2)

A pessoa escolheu o cliente errado, ou algo falhou. Ela consegue trocar o cliente e vê mensagens
claras.

**Why this priority**: evita OS no nome errado e deixa claro quando algo não foi salvo.

**Independent Test**: selecionar um cliente, clicar em Trocar cliente, selecionar outro; forçar erros
de validação e de rede.

**Acceptance Scenarios**:

1. **Given** um cliente selecionado, **When** a pessoa clica em **Trocar cliente**, **Then** a seleção é
   desfeita e a busca volta, sem perder os dados do aparelho já digitados.
2. **Given** dados obrigatórios faltando (cliente: nome e telefone; aparelho: modelo e número de
   série), **Then** o botão de salvar fica bloqueado e cada campo mostra a mensagem do que falta.
3. **Given** um CPF/CNPJ informado que não seja um CPF (11 dígitos) nem um CNPJ (14 caracteres:
   12 letras ou números e 2 dígitos), **Then** aparece "Informe um CPF (11 dígitos) ou um CNPJ (14
   caracteres)." e nada é salvo. Deixar o campo vazio é permitido.
4. **Given** uma falha ao salvar cliente ou OS, **Then** aparece a mensagem do sistema, nada é
   perdido do que foi digitado e a pessoa pode tentar de novo.
5. **Given** a criação da OS em andamento, **Then** o botão Criar OS fica bloqueado com indicação de
   carregamento, evitando OS duplicada por clique duplo.
6. **Given** o PDF do termo de entrada falhar depois da OS criada, **Then** aparece a mensagem de erro
   e a OS continua criada (comportamento do handler antigo); o termo pode ser regerado depois.

---

### Edge Cases

- **Busca com menos de 3 letras**: não busca; mostra "Digite pelo menos 3 letras.".
- **Muitos resultados**: mostra os primeiros 20, mais recentes primeiro, com o aviso "Refine a busca
  para ver mais.".
- **Mesmo nome para clientes diferentes**: a lista mostra telefone e CPF/CNPJ para diferenciar.
- **Documento com pontuação**: pontos, traços e barra são removidos antes de validar e salvar
  (ex.: `123.456.789-09` → `12345678909`; `12.ABC.345/01DE-35` → `12ABC34501DE35`); letras são
  guardadas em maiúsculas. Na tela, o documento é exibido com a máscara do tipo.
- **CNPJ alfanumérico**: aceito (formato em vigor desde julho de 2026: 12 posições com letras ou
  números e 2 dígitos verificadores numéricos).
- **Clientes antigos**: todos têm CPF hoje; depois da migration, aparecem com o mesmo número como
  documento do tipo CPF.
- **CEP não encontrado**: os campos de endereço ficam livres para digitar, com o aviso
  "CEP não encontrado.".
- **Sair da página com dados digitados**: não há rascunho; o que não foi salvo é perdido.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O item **Adicionar OS** do menu lateral MUST levar à tela nova, na rota `/adicionar`,
  acessível só para usuário logado.
- **FR-002**: A tela MUST ter um campo de busca de clientes por nome (a partir de 3 letras, com a
  busca disparando sozinha enquanto se digita), listando nome, telefone e CPF.
- **FR-003**: Ao escolher um cliente da lista, MUST abrir um modal centralizado com todos os dados do
  cliente editáveis e os botões **Cancelar**, **Confirmar** (usa sem salvar) e **Salvar e usar** (só
  habilitado se algo mudou).
- **FR-004**: Enquanto não houver cliente escolhido, a tela MUST mostrar, embaixo da busca, o
  formulário **Novo cliente** (mesmos campos do modal) com o botão **Cadastrar cliente** embaixo dele.
  Quando a busca não encontrar ninguém, a tela MUST oferecer usar o texto digitado como nome no
  formulário. Cadastrar cliente **não** abre modal.
- **FR-005**: Informar o CEP MUST preencher rua, bairro, cidade e UF automaticamente, usando a busca
  de CEP que já existe no sistema.
- **FR-006**: O cliente confirmado ou cadastrado MUST ficar selecionado na página, num cartão com
  nome, telefone e endereço resumido, e com a ação **Trocar cliente**.
- **FR-007**: A tela MUST ter os campos do aparelho: empresa (padrão "Particular"), tipo de produto
  (padrão "TV"), marca, modelo (obrigatório), número de série (obrigatório), defeito e acessórios.
- **FR-008**: **Criar OS** MUST seguir exatamente o handler antigo
  `old-handlers/CreateServiceOrderAndReturnPdfHandler.cs`: validar (cliente, modelo, número de série,
  tipo e empresa válidos); normalizar marca, modelo, série, defeito e acessórios (sem espaços nas
  pontas e em maiúsculas); criar a OS; se falhar, responder com erro; gerar o termo de entrada em PDF
  (se falhar, responder com erro, com a OS já criada); enfileirar o WhatsApp de boas-vindas (número da
  OS, nome e telefone do cliente) para todas as empresas; devolver o PDF.
- **FR-009**: O PDF devolvido MUST abrir numa nova aba com a impressão automática; se a aba for
  bloqueada, MUST ser baixado (mesmo comportamento da feature 006).
- **FR-010**: Cadastrar e atualizar cliente MUST manter as regras atuais de nome e telefone
  (obrigatórios) e as normalizações, com as mesmas mensagens. O documento (CPF/CNPJ) é **opcional**;
  quando informado, MUST ser um CPF (11 dígitos) ou um CNPJ (14 caracteres, as 12 primeiras letras ou
  números e as 2 últimas dígitos). Não há bloqueio de documento repetido (igual a hoje).
- **FR-011**: As rotas usadas por esta tela MUST estar no padrão REST:
  `GET /customers?name=`, `POST /customers`, `PUT /customers/{id}`, `GET /addresses/{cep}` e
  `POST /service-orders`. As rotas antigas correspondentes (`get-customer-by-name`,
  `create-customer`, `update-customer`, `get-address-by-zip-code`, `create-service-order`) MUST ser
  removidas.
- **FR-012**: Após criar a OS, o sistema MUST avisar as outras telas para atualizar.
- **FR-013**: A tela MUST seguir o padrão visual do sistema (cores, tipografia, campos e botões dos
  modais da Home) com o layout em **duas colunas de 50%**: cliente (busca, cadastro e cartão do
  cliente escolhido) à esquerda e ordem de serviço à direita. O botão **Criar OS** fica embaixo do
  formulário da ordem de serviço (sem barra fixa). Em telas estreitas, as colunas se empilham.
- **FR-014**: As regras de criação da OS, de cliente e do documento (CPF/CNPJ vazio, CPF, CNPJ numérico, CNPJ
  alfanumérico e formatos inválidos) MUST ter testes automatizados.
- **FR-015**: O campo do cliente hoje chamado CPF MUST passar a se chamar **Documento** em todo o
  sistema (cadastro, edição, respostas da API e tela), com o rótulo "CPF/CNPJ" na tela. O tipo
  (CPF ou CNPJ) é deduzido pelo tamanho, sem campo separado.
- **FR-016**: A mudança no banco MUST ser feita por migration: renomear a coluna `Cpf` para
  `Document`, mantendo todos os dados, e torná-la opcional. A migration MUST poder ser revertida
  (voltar a coluna para `Cpf`, obrigatória); ao reverter, clientes sem documento ficam com o valor
  vazio (`""`).
### Key Entities *(include if feature involves data)*

- **Cliente**: nome, documento (CPF ou CNPJ, opcional), telefone, telefone 2, e-mail e endereço
  (CEP, rua, número, bairro, cidade, UF). Já existe; o campo CPF passa a se chamar Documento.
- **Documento**: número normalizado (sem pontuação, letras em maiúsculas) e tipo deduzido do
  tamanho: 11 = CPF, 14 = CNPJ.
- **Ordem de serviço**: cliente, empresa, aparelho (tipo, marca, modelo, número de série, defeito,
  acessórios). Nasce com a situação "Entrada" e o orçamento "pendente" (aparece em "Orçamentos
  pendentes").
- **Termo de entrada (PDF)**: documento que já existe, gerado na criação da OS.
- **Mensagem de WhatsApp de boas-vindas**: mensagem que já existe (número da OS, nome, telefone).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Uma OS para cliente já cadastrado é aberta em menos de 1 minuto, do início da busca
  ao termo impresso.
- **SC-002**: Uma OS para cliente novo é aberta em menos de 3 minutos, incluindo o cadastro.
- **SC-003**: 100% das OS criadas aparecem em "Orçamentos pendentes" e geram o termo de entrada.
- **SC-004**: 0 OS duplicadas por clique duplo.
- **SC-005**: Toda falha mostra uma mensagem em português e mantém o que foi digitado.

## Assumptions

- **Rota da tela**: `/adicionar`, como pedido; o item "Adicionar OS" do menu passa a apontar para ela.
- **Busca só por nome** (ver Clarifications): é o que a API oferece hoje; busca por CPF ou telefone
  fica para depois. Como não há bloqueio de documento repetido, a pessoa deve conferir a lista (que
  mostra telefone e CPF/CNPJ) antes de cadastrar um cliente novo.
- **Sem validação de dígitos verificadores**: hoje o CPF só é conferido pelo tamanho; o documento
  segue assim (tamanho e formato). Validar dígitos verificadores fica para depois.
- **Edição de OS** (`EditServiceOrderCommand`): também passa a usar Documento opcional, para não
  recusar clientes sem documento.
- **Venda (`create-sales-service-order`)**: fora do escopo (é o item "Vender" do menu).
- **Handlers de cliente**: os atuais (criar, atualizar, buscar por nome) funcionam; passam para
  handlers comuns com interface (sem MediatR), mantendo as regras, junto com a troca para REST.
- **Sem rascunho** e **sem histórico de OS do cliente** nesta tela.
- **Padrão técnico**: igual às features anteriores (REST, handlers comuns, resposta padronizada,
  textos em português, estilos do sistema).
