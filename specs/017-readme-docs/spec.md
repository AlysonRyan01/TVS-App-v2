# Feature Specification: README completo do projeto (pt-BR)

**Feature Branch**: `017-readme-docs`

**Created**: 2026-11-06

**Status**: Draft

**Input**: User description: "vamos implementar o readme.me em portugues (brasil), informando rotas, dtos, comandos para subir docker, observalibilidade, etc. completo"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Entender e rodar o projeto do zero (Priority: P1)

Uma pessoa desenvolvedora que nunca viu o repositório abre o README e, só com ele, entende o que
o sistema faz (gestão de ordens de serviço, vendas, orçamentos, pagamentos e avisos por WhatsApp
da assistência técnica), como o código está organizado e consegue rodar o sistema na própria
máquina e executar os testes.

**Why this priority**: hoje o README tem só o título; sem ele, qualquer pessoa nova depende de
quem já conhece o projeto. É a base de toda a documentação.

**Independent Test**: entregar só o README a alguém sem contexto e pedir que suba o sistema
localmente e rode os testes, sem outra ajuda.

**Acceptance Scenarios**:

1. **Given** um clone novo do repositório, **When** a pessoa segue a seção de execução local,
   **Then** consegue subir a API e o sistema web e abrir a tela de login.
2. **Given** o README, **When** a pessoa procura "o que é o projeto" e "como está organizado",
   **Then** encontra a descrição do produto, as tecnologias, as camadas do backend e as pastas
   principais, com o papel de cada uma.
3. **Given** o README, **When** a pessoa procura como testar, **Then** encontra o comando dos
   testes automatizados e o que eles cobrem.

---

### User Story 2 - Consultar rotas e formatos da API (Priority: P1)

Quem integra com a API (o sistema web, o site público ou um script) consulta no README todas as
rotas disponíveis, com método, caminho, se exige login, para que servem e os formatos de dados
de entrada e saída (DTOs/comandos), incluindo o formato padrão de resposta e os códigos de erro.

**Why this priority**: é o pedido explícito do usuário e a dúvida mais frequente de quem integra.

**Independent Test**: escolher qualquer rota exposta pela API e encontrá-la no README com
método, caminho, autenticação, corpo/parâmetros e resposta descritos corretamente.

**Acceptance Scenarios**:

1. **Given** a lista de rotas da API, **When** comparada com o README, **Then** todas as rotas
   aparecem, agrupadas por assunto (autenticação, clientes, ordens de serviço, pagamentos,
   métricas, rotas públicas, tempo real, observabilidade), sem rotas inexistentes.
2. **Given** uma rota que recebe dados, **When** a pessoa a consulta no README, **Then** vê os
   campos do corpo/parâmetros com tipo, obrigatoriedade e um exemplo.
3. **Given** uma rota que devolve dados, **When** a pessoa a consulta, **Then** vê o formato da
   resposta (incluindo o envelope padrão de sucesso/erro) ou que devolve um PDF.
4. **Given** uma rota protegida, **When** a pessoa lê como autenticar, **Then** entende o fluxo
   de login, renovação de sessão e logout e como enviar a credencial.

---

### User Story 3 - Subir em produção com Docker (Priority: P2)

Quem mantém a VPS encontra no README os comandos para subir, atualizar, ver logs e parar o
sistema com Docker, a lista de variáveis de ambiente e o que cada serviço faz.

**Why this priority**: o deploy já está documentado em detalhe em um guia próprio; o README
precisa do resumo prático e do caminho para o guia.

**Independent Test**: seguir só os comandos do README em uma máquina com Docker e ter todos os
serviços rodando.

**Acceptance Scenarios**:

1. **Given** uma máquina com Docker, **When** a pessoa segue a seção de Docker, **Then** sobe
   todos os serviços com os comandos indicados e sabe conferir se estão rodando.
2. **Given** o README, **When** a pessoa procura as variáveis de ambiente, **Then** encontra
   todas, com obrigatoriedade e finalidade, coerentes com o arquivo de exemplo do projeto.
3. **Given** o README, **When** a pessoa precisa de detalhes de deploy (domínio, HTTPS, banco),
   **Then** encontra o link para o guia de deploy.

---

### User Story 4 - Usar a observabilidade (Priority: P2)

Quem investiga um problema descobre pelo README que existem logs, métricas, painéis e alertas,
como acessá-los, como achar uma ordem de serviço ou uma requisição nos logs e quais eventos e
métricas existem.

**Why this priority**: a observabilidade é recente; sem documentação, ela não é usada.

**Independent Test**: com o README, achar nos painéis os registros de uma OS específica.

**Acceptance Scenarios**:

1. **Given** o README, **When** a pessoa procura observabilidade, **Then** encontra os
   componentes, como acessar o painel, os painéis disponíveis, os alertas e a retenção.
2. **Given** o README, **When** a pessoa procura como seguir uma requisição, **Then** entende o
   identificador de correlação e como filtrar por OS.
3. **Given** o README, **When** a pessoa procura o catálogo de eventos e métricas, **Then**
   encontra a lista completa de eventos de negócio e métricas próprias.

---

### User Story 5 - Contribuir seguindo as regras do projeto (Priority: P3)

Quem vai alterar o código encontra as regras do projeto (camadas, testes obrigatórios, textos
em português, migrations) e o fluxo de trabalho com especificações.

**Why this priority**: útil, mas depende das seções anteriores para ter contexto.

**Independent Test**: a pessoa encontra no README como criar uma migration e onde ficam as
especificações das features.

**Acceptance Scenarios**:

1. **Given** o README, **When** a pessoa procura as regras de contribuição, **Then** encontra o
   resumo dos princípios do projeto e o link para o documento completo.

---

### Edge Cases

- Rotas internas (métricas para coleta) devem aparecer como internas e não acessíveis de fora.
- Rotas que devolvem arquivo PDF (criar OS/venda, entregar, reimprimir, comprovante) devem ser
  identificadas como tal, incluindo cabeçalhos relevantes da resposta (ex.: número da OS criada).
- Rotas públicas (sem login), como aprovação/reprovação de orçamento pelo cliente e contato do
  site, devem estar claramente separadas das protegidas.
- Valores enumerados (status, tipo de produto, empresa, forma de pagamento etc.) devem ter os
  valores possíveis listados com o significado em português.
- Segredos nunca aparecem no README: só nomes de variáveis e valores de exemplo fictícios.
- Divergência com o código: se uma rota ou campo do código não estiver no README, a documentação
  está incompleta.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O README MUST estar inteiramente em português do Brasil.
- **FR-002**: O README MUST descrever o produto (para quem é, o que resolve, principais
  funcionalidades) e as tecnologias usadas no backend, no frontend e na infraestrutura.
- **FR-003**: O README MUST apresentar a arquitetura: camadas do backend, regra de dependência
  entre elas, aplicação web, proxy, tempo real e fila de WhatsApp, com um diagrama simples.
- **FR-004**: O README MUST mostrar a estrutura de pastas principal com o papel de cada pasta.
- **FR-005**: O README MUST ensinar a rodar localmente (pré-requisitos, configuração, comandos
  da API e do sistema web, banco local) e a rodar os testes.
- **FR-006**: O README MUST documentar 100% das rotas expostas pela API, cada uma com método,
  caminho, exigência de login, descrição, parâmetros/corpo e resposta, agrupadas por assunto.
- **FR-007**: O README MUST documentar os formatos de dados (comandos de entrada e DTOs de
  saída) usados pelas rotas, com campos, tipos, obrigatoriedade e exemplos, além do envelope
  padrão de resposta e dos códigos de erro.
- **FR-008**: O README MUST listar os valores possíveis de cada enumeração exposta pela API, com
  o significado em português.
- **FR-009**: O README MUST explicar autenticação (login, renovação, logout, envio da
  credencial) e as rotas públicas sem login.
- **FR-010**: O README MUST descrever o canal de tempo real (endereço, evento enviado e quando).
- **FR-011**: O README MUST trazer os comandos de Docker para subir, atualizar, ver estado e
  logs, reiniciar e parar, a lista de serviços e a tabela de variáveis de ambiente, com link
  para o guia de deploy.
- **FR-012**: O README MUST documentar a observabilidade: componentes, acesso, painéis, alertas,
  retenção, identificador de correlação, como achar uma OS, catálogo de eventos de negócio,
  métricas próprias e regras de privacidade dos logs.
- **FR-013**: O README MUST descrever as notificações de WhatsApp (quais mensagens, quando são
  enviadas e como conferir se saíram).
- **FR-014**: O README MUST resumir as regras de contribuição (princípios do projeto, migrations,
  testes, textos em português, fluxo de especificações) com link para o documento completo.
- **FR-015**: O README MUST ter um índice navegável no início.
- **FR-016**: O README MUST NOT conter segredos reais (senhas, chaves, cadeias de conexão).
- **FR-017**: Todo comando e exemplo do README MUST funcionar no estado atual do repositório.

### Key Entities

- **Rota da API**: método, caminho, autenticação, descrição, entrada, saída, grupo.
- **Formato de dados (comando/DTO)**: nome, campos (nome, tipo, obrigatório, descrição), exemplo.
- **Enumeração**: nome, valores e significado.
- **Variável de ambiente**: nome, obrigatoriedade, finalidade, exemplo.
- **Evento de negócio / métrica**: nome, quando ocorre, rótulos.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das rotas expostas pela API aparecem no README, e nenhuma rota documentada
  deixa de existir.
- **SC-002**: 100% dos formatos de entrada usados pelas rotas protegidas e públicas têm seus
  campos documentados.
- **SC-003**: Uma pessoa sem contexto do projeto sobe o sistema localmente seguindo só o README
  em até 30 minutos (excluindo downloads).
- **SC-004**: Uma pessoa encontra a documentação de uma rota qualquer em até 1 minuto a partir
  do índice.
- **SC-005**: 100% das variáveis de ambiente do arquivo de exemplo estão documentadas.
- **SC-006**: Todos os comandos do README executam sem erro no estado atual do repositório.

## Assumptions

- Público: pessoas desenvolvedoras e quem mantém a VPS; não é manual do usuário final da loja.
- O README é a porta de entrada; o guia de deploy (`DEPLOY.md`) continua sendo o detalhe de
  produção e é referenciado, não duplicado por completo.
- A documentação descreve o estado atual do código; nada no sistema muda nesta feature.
- Um único arquivo `README.md` na raiz; se ficar longo demais, partes podem ir para `docs/` com
  links, desde que rotas e DTOs continuem acessíveis a partir do índice.
- A documentação interativa da API (Swagger) existente continua e é mencionada como complemento.
