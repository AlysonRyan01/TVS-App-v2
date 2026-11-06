# Feature Specification: Limpeza das rotas da API (REST + comandos/consultas)

**Feature Branch**: `014-rest-routes-cleanup`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "vamos apagar as rotas que nao estao sendo utilizadas no angular nem no /home/alyson/Documentos/github/tvseletronica, vamos transformar todas em REST e tambem vamos separar em commandendpoints e query endpoints"

## Contexto

A API tem hoje **44 rotas**. Elas são usadas por dois clientes:

- **Sistema interno (Angular, `web/`)**: telas da Home, Adicionar OS, Vender, Buscar OS, Clientes,
  Métricas, login.
- **Site público (`tvseletronica`, `js/api.js`)**: o cliente consulta a OS pelo QR Code
  (`get-service-order-for-customer/{id}/{code}`), **aprova/reprova o orçamento**
  (`add-service-order-approve-estimate`, `add-service-order-reject-estimate`, com número e código)
  e manda mensagem pelo formulário de contato (`v1/whatsapp/site-contact`).

Inventário (levantado no código dos dois clientes):

| Situação | Rotas |
|----------|-------|
| **Usadas e já REST** | `GET/POST /customers`, `PUT /customers/{id}`, `GET /addresses/{cep}`, `GET /metrics`, `POST /service-orders`, `POST /service-orders/sales`, `GET /service-orders/search`, `GET/PUT /service-orders/{id}`, `GET /service-orders/{id}/pdf`, as 6 filas da Home (`GET /service-orders/pending-estimate` …), as 8 ações da Home (`PUT /service-orders/{id}/estimate/approve` …, `/delivery`, `/repair`, `/part/purchase`, `/part/purchase-cost`, `/product/location`, `/ready-for-pickup`) |
| **Usadas, fora do padrão REST** | `PUT /add-service-order-estimate` (Angular), `POST /login`, `/logout`, `/refresh-token` (Angular), `GET /get-service-order-for-customer/{id}/{code}` e `POST /v1/whatsapp/site-contact` (site) |
| **Usadas pelo site, mas já não existem na v2** | `PUT /add-service-order-approve-estimate` e `/add-service-order-reject-estimate` (o site do cliente aprova/reprova o orçamento; hoje daria 404 na v2) |
| **Não usadas, mas mantidas** | pagamentos (`GET /get-payments`, `POST /add-payment`, `PUT /update-payment`, `DELETE /remove-payment`: a tela ainda vai ser feita) e `POST /register` (criar usuário) |
| **Não usadas por ninguém (saem)** | `GET /get-all-customers/{pageSize}/{pageNumber}`, `GET /get-customer-by-id/{id}`, `GET /get-delivered-service-orders/{pageNumber}/{pageSize}`, `GET /get-service-orders-by-enterprise`, `GET /get-abandoned-orders`, `GET /notifications/unread`, `POST /notifications`, `PUT /notifications/{id}/read`, `POST /v1/email/send` |

**Atores**: equipe da loja (sistema interno) e clientes da loja (site público).

## Clarifications

### Session 2026-11-05

- Pagamentos: **manter** as 4 rotas (a tela de pagamentos ainda vai ser feita), passando para REST
  e sem MediatR, seguindo os handlers antigos.
- Q: Cadastro de usuário? → A: **Manter**, como `POST /auth/register` protegida (só usuário logado).
- Q: Site público? → A: **Manter os caminhos que o site usa** (aprovar/reprovar antigos, mesmo não
  sendo REST); o `js/api.js` do site não muda nesta feature.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sistema interno continua funcionando (Priority: P1)

Depois da limpeza, todas as telas do sistema interno funcionam como antes: login, Home (filas e
ações), Adicionar OS, Vender, Buscar OS (com painel, alterar, reimprimir, prateleira), Clientes e
Métricas.

**Why this priority**: a limpeza não pode quebrar o uso diário da loja.

**Independent Test**: percorrer todas as telas e ações com a API nova.

**Acceptance Scenarios**:

1. **Given** o sistema interno atualizado, **When** a pessoa usa cada tela e cada ação, **Then**
   tudo funciona igual a antes (mesmos dados, mesmas mensagens, mesmo PDF).
2. **Given** a sessão expirando, **Then** o login é renovado automaticamente como hoje.

---

### User Story 2 - Site público continua funcionando (Priority: P1)

O cliente abre o link/QR Code da OS no site, vê a situação e o orçamento, aprova ou reprova, e
pode mandar mensagem pelo formulário de contato.

**Why this priority**: é o canal do cliente; hoje a aprovação/reprovação nem existe na v2.

**Independent Test**: no site, consultar uma OS com número e código, aprovar (outra: reprovar) e
enviar o formulário de contato.

**Acceptance Scenarios**:

1. **Given** número e código de segurança corretos, **When** o cliente consulta, **Then** vê a OS.
   Código errado → "Essa ordem de serviço não existe" (sem revelar se o número existe).
2. **Given** uma OS aguardando resposta, **When** o cliente aprova (ou reprova) com número e
   código, **Then** o orçamento fica aprovado (reprovado) com as mesmas consequências da ação da
   Home (WhatsApp, aviso ao dono, notificação, atualização das telas).
3. **Given** um código errado na aprovação/reprovação, **Then** nada muda e aparece o mesmo erro do
   item 1.
4. **Given** o formulário de contato, **When** o cliente envia, **Then** a mensagem chega como hoje.
5. Essas rotas do site continuam **sem login** (o cliente não tem conta).

---

### User Story 3 - API enxuta e organizada (Priority: P2)

Quem mantém o sistema encontra só rotas usadas, todas no padrão REST, separadas entre as que
**mudam** dados (comandos) e as que só **leem** (consultas).

**Independent Test**: listar as rotas da API e conferir que cada uma é usada por um dos clientes,
segue o padrão e está no grupo certo.

**Acceptance Scenarios**:

1. **Given** a lista de rotas, **Then** nenhuma rota da tabela "Não usadas por ninguém (saem)"
   existe.
2. **Given** cada recurso (ordens de serviço, clientes, autenticação, métricas, site público),
   **Then** as rotas de leitura ficam no grupo de consultas e as de escrita no de comandos.
3. **Given** qualquer rota (exceto as do site público, mantidas como estão), **Then** o caminho
   segue o padrão REST (recurso no plural, ação como sub-recurso, verbo HTTP coerente) e responde no
   formato padrão de resposta da API.

### Edge Cases

- **Usuários novos**: criados por `POST /auth/register`, só por quem já está logado (hoje a rota é
  aberta a qualquer um).
- **Notificações**: só as rotas saem; a tabela e os dados continuam (as notificações ainda são
  gravadas ao aprovar/reprovar).
- **Pagamentos**: rotas mantidas (sem tela por enquanto), agora REST.
- **Código de segurança**: o handler antigo de aprovação **não conferia** o código (só o número);
  aqui o código passa a ser obrigatório e conferido, para um número de OS chutado não aprovar o
  orçamento de outra pessoa.
- **Site público**: os 4 caminhos que o site usa ficam **iguais** (exceção ao padrão REST, por
  decisão do usuário), para o site não precisar mudar: `GET /get-service-order-for-customer/{id}/{code}`,
  `PUT /add-service-order-approve-estimate`, `PUT /add-service-order-reject-estimate` (corpo
  `{ id, code }`) e `POST /v1/whatsapp/site-contact`.
- **Hub em tempo real** (`/osHub`): não é rota REST; continua como está.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: As 9 rotas da tabela "Não usadas por ninguém (saem)" MUST ser removidas (e o código
  que só elas usavam), sem mexer no banco.
- **FR-002**: Todas as rotas restantes MUST seguir o padrão REST. Nomes novos:
  - `PUT /add-service-order-estimate` → `PUT /service-orders/{id}/estimate`;
  - `POST /login`, `/logout`, `/refresh-token`, `/register` → `POST /auth/login`, `/auth/logout`,
    `/auth/refresh`, `/auth/register` (este só para usuário logado);
  - pagamentos → `GET /service-orders/{id}/payments`, `POST /service-orders/{id}/payments`,
    `PUT /service-orders/{id}/payments/{paymentId}`, `DELETE /service-orders/{id}/payments/{paymentId}`
    (mesmas regras de `old-handlers/{Get,Add,Update,Remove}Payment*Handler.cs`);
  - **exceção**: as 4 rotas do site público mantêm o caminho atual (ver Edge Cases).
- **FR-003**: Para cada recurso, as rotas MUST ficar separadas em **comandos** (escrita) e
  **consultas** (leitura).
- **FR-004**: O sistema interno (Angular) MUST passar a usar os caminhos novos e funcionar igual
  (US1).
- **FR-005**: O site público MUST funcionar sem mudanças, com os mesmos caminhos (US2), sem login,
  conferindo o código de segurança.
- **FR-006**: Aprovar/reprovar pelo site MUST ter o mesmo efeito da ação da Home (mesma regra de
  negócio), com testes automatizados, inclusive do código errado.
- **FR-007**: Nenhuma rota restante MUST depender do padrão antigo (MediatR).

### Key Entities

- **Rota da API**: caminho + verbo + cliente que a usa + grupo (comando/consulta).
- Nada muda no banco.

## Success Criteria *(mandatory)*

- **SC-001**: As 9 rotas sem uso removidas (de 44 para 37, contando as 2 de aprovar/reprovar do
  site que voltam).
- **SC-002**: 100% das rotas no padrão REST (exceto as 4 do site) e no grupo certo
  (comando/consulta).
- **SC-003**: 100% das telas do sistema interno e das 3 funções do site funcionando após a troca.
- **SC-004**: Aprovar/reprovar pelo site com código errado é recusado em 100% dos casos.

## Assumptions

- **Fonte do "usado"**: busca no código do Angular (`web/src`) e do site (`tvseletronica/js/api.js`);
  nenhum outro cliente consome a API.
- **Padrão técnico**: igual às features anteriores (handlers sem MediatR, `BaseResponse.ToResult()`,
  textos em pt-BR).
