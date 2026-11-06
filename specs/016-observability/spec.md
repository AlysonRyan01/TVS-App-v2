# Feature Specification: Observabilidade (logs, métricas e painéis)

**Feature Branch**: `016-observability`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "vamos adicionar logs para deixar bem robusto a observabilidade, vamos fazer com loki + prometheus + grafana + grafana alloy"

## Contexto

Hoje a API escreve logs simples no console do container (só 3 lugares registram algo de propósito:
o tratamento de erros, a fila e o envio de WhatsApp). Não há histórico pesquisável, nem números de
uso/saúde, nem aviso quando algo quebra. Quando um cliente diz "não recebi o WhatsApp" ou "deu erro
ao criar a OS", não há onde olhar.

Esta feature adiciona, junto do sistema na VPS:
- **Logs centralizados e pesquisáveis** (Loki), coletados automaticamente (Grafana Alloy);
- **Métricas** da API e do servidor (Prometheus);
- **Painéis e alertas** (Grafana).

O banco continua no Azure (fora desta feature).

**Atores**: o dono/responsável técnico do sistema, investigando problemas e acompanhando a saúde.

## Clarifications

### Session 2026-11-05

- Q: Por onde chegam os alertas? → A: **D** — sem aviso por enquanto; as regras de alerta ficam
  configuradas e visíveis no Grafana (estado disparado/normal), sem canal de envio.
- Q: Como acessar o Grafana? → A: **A** — subdomínio próprio com HTTPS (ex.: `grafana.seudominio`),
  pelo mesmo servidor web do sistema, com o login do Grafana.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Investigar um problema pelos logs (Priority: P1)

Um atendente avisa "deu erro ao entregar a OS 4312". O responsável abre o painel, filtra por "OS
4312" (ou pelo horário) e vê o que aconteceu: a requisição, o erro com detalhes técnicos, quem
estava logado e quanto tempo levou.

**Why this priority**: é o uso principal; hoje não há como saber o que aconteceu depois do fato.

**Independent Test**: provocar um erro numa ação e encontrá-lo no painel pela OS e pelo horário.

**Acceptance Scenarios**:

1. **Given** qualquer requisição à API, **Then** fica registrado: horário, rota, método, status,
   tempo de resposta, usuário (e-mail) e um **identificador de correlação** que liga todos os logs
   daquela requisição.
2. **Given** um erro (exceção ou resposta 5xx), **Then** o log traz a mensagem, o detalhe técnico e
   o identificador de correlação; a resposta ao usuário continua a mesma de hoje.
3. **Given** as ações de negócio (criar OS, venda, orçamento, aprovar/reprovar — inclusive pelo site,
   compra de peça, conserto, entrega, alterar OS, pagamentos, comprovantes, cliente criado/alterado,
   login), **Then** cada uma gera um log de evento com o número da OS/cliente e o resultado.
4. **Given** o painel de logs, **When** o responsável filtra por número de OS, nível (erro/aviso),
   rota ou período, **Then** vê só os logs correspondentes.

---

### User Story 2 - Saber se o WhatsApp foi enviado (Priority: P1)

O cliente diz que não recebeu a mensagem. O responsável procura pela OS e vê se a mensagem foi
enfileirada, enviada ou falhou (e o motivo).

**Why this priority**: o WhatsApp é o principal canal com o cliente; falhas hoje são invisíveis.

**Acceptance Scenarios**:

1. **Given** uma mensagem enfileirada, **Then** há log de "enfileirada", e depois de "enviada" ou
   "falhou" (com o motivo devolvido pelo serviço de WhatsApp), com o tipo de mensagem e a OS.
2. **Given** o painel, **Then** há um gráfico de mensagens enviadas × falhas por tipo e por dia.

---

### User Story 3 - Acompanhar a saúde e o uso (Priority: P2)

O responsável abre um painel pronto e vê: se a API está no ar, requisições por minuto, taxa de erro,
tempo de resposta, uso de CPU/memória/disco da VPS, e números do negócio do dia (OS criadas,
entregas, vendas, mensagens enviadas).

**Acceptance Scenarios**:

1. **Given** o painel "Visão geral", **Then** mostra disponibilidade, requisições/min, % de erros,
   tempo de resposta (mediana e p95) por rota, CPU/memória/disco da VPS e eventos de negócio por dia.
2. **Given** um período (última hora, 24h, 7 dias), **Then** os gráficos acompanham.

---

### User Story 4 - Ver os alertas no painel (Priority: P3)

As condições de problema (API fora do ar, taxa de erros alta, WhatsApp falhando, disco quase cheio)
ficam configuradas como alertas no Grafana. **Por enquanto não há envio de aviso**: o responsável vê
o estado deles no painel.

**Acceptance Scenarios**:

1. **Given** uma das condições de alerta pelo tempo definido, **Then** o alerta aparece como
   **disparado** no Grafana (e na Visão geral).
2. **Given** a condição volta ao normal, **Then** o alerta volta a **normal**.
3. Um canal de envio (WhatsApp, e-mail…) pode ser ligado depois, sem refazer as regras.

### Edge Cases

- **Dados pessoais**: telefones, CPF/CNPJ, e-mails de clientes, senhas, tokens e o corpo das
  requisições **não** aparecem nos logs (telefone mascarado, ex.: `41*****0000`); senhas e tokens
  nunca.
- **Observabilidade fora do ar**: se o Loki/Prometheus/Grafana cair, o sistema continua funcionando
  normalmente (nada do sistema depende deles).
- **Volume e espaço**: logs guardados por **30 dias** e métricas por **15 dias**, depois apagados
  automaticamente, para não lotar o disco da VPS.
- **Ruído**: verificações de saúde e arquivos estáticos não poluem os logs de requisição.
- **Acesso**: o Grafana fica num **subdomínio próprio com HTTPS** (configurável no `.env`), com
  login e senha do Grafana (definidos no `.env`; cadastro aberto desligado). Loki, Prometheus e Alloy
  **não** ficam expostos na internet. Sem domínio configurado, o Grafana não é publicado.
- **Desenvolvimento**: rodando local, os logs continuam aparecendo no console como hoje.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Todos os logs da API MUST ser estruturados (campos pesquisáveis: nível, mensagem,
  rota, status, duração, usuário, número da OS, identificador de correlação) e coletados de forma
  centralizada.
- **FR-002**: Cada requisição MUST gerar um log de resumo (rota, método, status, duração, usuário,
  correlação) e responder com o identificador de correlação num cabeçalho, para cruzar com os logs.
- **FR-003**: As ações de negócio da US1-3 e os eventos de WhatsApp da US2 MUST gerar logs de evento
  com nome padronizado, número da OS/cliente e resultado.
- **FR-004**: A API MUST expor métricas de uso (requisições, erros, tempo de resposta por rota) e de
  negócio (OS criadas, entregas, vendas, orçamentos, mensagens enviadas/falhas por tipo).
- **FR-005**: Métricas do servidor (CPU, memória, disco, rede) e dos containers MUST ser coletadas.
- **FR-006**: MUST existir painéis prontos (provisionados, sem configurar à mão): "Visão geral",
  "Logs", "WhatsApp" e "Servidor".
- **FR-007**: MUST existir regras de alerta, visíveis no Grafana, para: API fora do ar (> 2 min),
  taxa de erros 5xx > 5% (5 min), falhas de WhatsApp ≥ 3 em 10 min, disco da VPS > 85%. Sem canal
  de envio nesta feature.
- **FR-008**: Dados pessoais e segredos MUST ser mascarados/omitidos conforme Edge Cases.
- **FR-009**: Toda a observabilidade MUST subir junto com o sistema pelo mesmo procedimento de deploy
  (um comando), com retenção de 30 dias (logs) e 15 dias (métricas), e senhas no `.env`.
- **FR-012**: O Grafana MUST ser publicado só pelo subdomínio configurado, com HTTPS e login;
  os demais componentes ficam só na rede interna.
- **FR-010**: Nenhum componente de observabilidade pode impedir o sistema de funcionar se estiver fora
  do ar.
- **FR-011**: O mascaramento de dados pessoais e o nome/forma dos eventos de negócio MUST ter testes
  automatizados.

### Key Entities

- **Log**: horário, nível, mensagem, campos (rota, status, duração, usuário, OS, correlação, evento).
- **Métrica**: série numérica no tempo (requisições, erros, latência, eventos de negócio, servidor).
- **Alerta**: condição, duração, canal, estado (disparado/resolvido).

## Success Criteria *(mandatory)*

- **SC-001**: Encontrar o que aconteceu com uma OS específica leva menos de 1 minuto.
- **SC-002**: 100% dos erros da API ficam registrados com o identificador de correlação.
- **SC-003**: 100% das mensagens de WhatsApp têm registro de enviada ou falha.
- **SC-004**: Um problema dos tipos da FR-007 aparece como alerta disparado no Grafana em até 5
  minutos.
- **SC-005**: 0 telefones completos, documentos, senhas ou tokens nos logs (verificado por busca).
- **SC-006**: O sistema continua funcionando normalmente com a observabilidade desligada.

## Assumptions

- **Onde roda**: na mesma VPS, em containers, junto do sistema (a VPS precisa de ~1 GB de RAM livre
  a mais para a pilha de observabilidade).
- **Ferramentas**: as escolhidas pelo usuário (coleta Alloy, logs Loki, métricas Prometheus, painéis
  e alertas Grafana).
- **Frontend**: fora do escopo nesta feature (só a API e o servidor); erros do navegador podem vir
  depois.
- **Rastreamento distribuído (traces)**: fora do escopo; o identificador de correlação cobre o
  cruzamento de logs de uma requisição.
- **Banco no Azure**: métricas do banco ficam no próprio Azure; aqui só o tempo das chamadas.
