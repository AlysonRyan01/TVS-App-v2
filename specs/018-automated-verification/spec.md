# Feature Specification: Verificação automática do sistema (fluxos e WhatsApp)

**Feature Branch**: `018-automated-verification`

**Created**: 2026-11-06

**Status**: Draft

**Input**: User description: "atualmente eu preciso fazer testes manuais para ver se algo funciona, como por exemplo mandar uma mensagem via whatsapp para um cliente no meu nome com meu telefone para ver se esta funcionando, tem como automatizar isso?"

## Contexto

Hoje, para saber se o sistema funciona depois de uma mudança ou de uma atualização na VPS, o dono
cadastra um cliente com o próprio nome e telefone, abre uma OS, avança as etapas (orçamento,
aprovação, conserto, entrega, pagamento) e confere no próprio celular se as mensagens de WhatsApp
chegaram. Isso é lento, fácil de esquecer, deixa dados de teste no banco real e só descobre
problemas depois que já aconteceram. Os testes automáticos existentes cobrem regras isoladas, mas
não o fluxo inteiro funcionando de ponta a ponta, nem a conexão real com o WhatsApp.

## Clarifications

### Session 2026-11-06

- Q: Onde a verificação automática de fluxo deve rodar? → A: Só quando o dono rodar o comando (sob demanda); sem pipeline nem execução na VPS.
- Q: Frequência do envio real de teste? → A: Somente sob demanda; sem agendamento.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Fluxo completo verificado automaticamente, sem mandar WhatsApp de verdade (Priority: P1)

Antes de publicar uma mudança, o dono roda, com um único comando, uma verificação que
percorre o ciclo inteiro de uma OS como um atendente faria — login, cadastro de cliente, abertura
da OS com PDF, orçamento, aprovação (pela loja e pelo link do cliente), compra de peça, conserto,
entrega com PDF, pagamentos com comprovante, venda, busca e métricas — e confirma, para cada
etapa, que o resultado está certo **e** que a mensagem de WhatsApp correta (tipo, destinatário,
número da OS e conteúdo principal) teria sido enviada, sem enviar nada a ninguém e sem tocar no
banco real.

**Why this priority**: substitui o teste manual que mais consome tempo e pega a maioria dos
problemas (rota quebrada, regra alterada, mensagem errada ou não disparada) antes de chegarem aos
clientes.

**Independent Test**: rodar a verificação num repositório sem nenhuma configuração de produção e
ver o relatório de todas as etapas aprovadas; quebrar de propósito uma etapa (ex.: o aviso de
"aparelho pronto") e ver a verificação apontar exatamente essa etapa.

**Acceptance Scenarios**:

1. **Given** o código atual, **When** a verificação é executada, **Then** ela percorre todas as
   etapas do ciclo da OS e da venda e termina com um resumo "aprovado" em poucos minutos.
2. **Given** uma etapa que deveria enviar WhatsApp (boas-vindas, orçamento, aprovação, reprovação,
   aparelho pronto, garantia, venda, contato do site), **When** ela é executada, **Then** a
   verificação confirma que exatamente a mensagem esperada foi gerada para o telefone do cliente de
   teste, com o número da OS certo.
3. **Given** uma mudança que quebra uma etapa, **When** a verificação roda, **Then** ela falha,
   indica qual etapa e o que era esperado × o que aconteceu.
4. **Given** a verificação rodando, **When** termina, **Then** nenhuma mensagem real foi enviada e
   nenhum dado foi gravado no banco de produção.

---

### User Story 2 - Saber se o WhatsApp da loja está conectado (Priority: P2)

O dono quer saber, sem testar manualmente, se o número de WhatsApp da loja continua conectado e
apto a enviar — e ser avisado no painel quando cair (ex.: sessão desconectada no celular,
serviço de envio fora do ar, chave inválida).

**Why this priority**: a falha mais comum na prática não é o código, é a conexão do WhatsApp cair
silenciosamente; hoje só se descobre quando um cliente reclama.

**Independent Test**: desconectar o aparelho da sessão do WhatsApp e ver o painel indicar
"desconectado" e o alerta disparar; reconectar e ver voltar ao normal.

**Acceptance Scenarios**:

1. **Given** o WhatsApp conectado, **When** o dono abre o painel de observabilidade, **Then** vê
   o estado "conectado" e quando foi a última verificação.
2. **Given** a sessão desconectada ou o serviço de envio fora do ar, **When** passam alguns
   minutos, **Then** o painel mostra "desconectado"/"indisponível" e um alerta aparece.
3. **Given** a verificação de conexão, **When** ela roda, **Then** não envia nenhuma mensagem a
   clientes.

---

### User Story 3 - Teste real de envio para o próprio dono (Priority: P3)

Quando quiser ter certeza absoluta de que uma mensagem chega de verdade, o dono dispara (ou agenda)
um envio real de teste para um número dele, configurado no sistema — no lugar de cadastrar um
cliente falso com o próprio nome.

**Why this priority**: complementa as anteriores com a prova final (a mensagem chegou no
celular), mas é usada com menos frequência.

**Independent Test**: acionar o teste real e receber no celular configurado uma mensagem
identificada como teste, com data/hora; ver o resultado (enviada/falhou + motivo) registrado.

**Acceptance Scenarios**:

1. **Given** um número de teste configurado, **When** o dono aciona o teste real, **Then** recebe
   uma mensagem claramente marcada como teste e o resultado fica registrado nos logs/painel.
2. **Given** nenhum número de teste configurado, **When** o teste é acionado, **Then** nada é
   enviado e é informado que falta configurar o número.
3. **Given** o teste real, **When** executado, **Then** ele nunca usa telefones de clientes reais
   e não cria clientes nem OS no banco.
4. **Given** o teste real, **When** o dono não o aciona, **Then** nenhuma mensagem de teste é
   enviada: o envio real é **somente sob demanda** (sem agendamento).

---

### Edge Cases

- Serviço de WhatsApp lento ou fora do ar durante a verificação de fluxo (US1): não deve afetar o
  resultado, pois nada é enviado de verdade.
- Mensagem enfileirada mas não processada a tempo: a verificação aguarda um tempo limite e
  reporta "não gerada" em vez de travar.
- Empresas que não recebem WhatsApp em algumas etapas (Philco/Britania): a verificação confirma
  que **nenhuma** mensagem foi gerada nesses casos.
- Código de segurança errado na aprovação pelo cliente: a verificação confirma a recusa e que
  nenhuma mensagem foi gerada.
- Ambiente de desenvolvimento, que hoje redireciona mensagens para um número de teste: a
  verificação não depende desse comportamento.
- Execução repetida: cada execução começa de um estado limpo e não depende da anterior.
- Teste real com o número de teste igual ao de um cliente: permitido (é o número do dono), mas
  nunca a partir de dados de clientes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST oferecer uma verificação automática de ponta a ponta que percorra o
  ciclo de uma OS de conserto (login, cliente, abertura, orçamento, aprovação pela loja e pelo
  cliente, reprovação, liberação, peça, conserto, entrega, pagamentos, comprovante, reimpressão) e
  de uma venda, além de busca e métricas.
- **FR-002**: Para cada etapa que dispara WhatsApp, a verificação MUST confirmar o tipo da
  mensagem, o telefone de destino, o número da OS e o trecho principal do conteúdo; e, nas etapas
  que não devem disparar, MUST confirmar que nada foi gerado.
- **FR-003**: A verificação de fluxo MUST NOT enviar mensagens reais nem usar o banco de produção;
  MUST rodar num ambiente isolado criado e descartado a cada execução.
- **FR-004**: A verificação MUST produzir um resumo legível em português com cada etapa
  aprovada/reprovada e, em caso de falha, o esperado × obtido.
- **FR-005**: A verificação de fluxo MUST ser executada com um único comando, sob demanda, quando o
  dono quiser (ex.: antes de publicar uma atualização). Não há execução automática por pipeline
  nem na VPS nesta feature.
- **FR-006**: O sistema MUST verificar periodicamente (a cada poucos minutos) o estado da conexão
  do WhatsApp da loja sem enviar mensagens, e expor esse estado no painel de observabilidade.
- **FR-007**: O sistema MUST gerar um alerta visível no painel quando o WhatsApp estiver
  desconectado ou indisponível por mais de alguns minutos, e normalizá-lo quando voltar.
- **FR-008**: O sistema MUST permitir, somente sob demanda, um envio real de teste para um número configurado pelo dono,
  com mensagem identificada como teste, registrando o resultado (enviada/falhou + motivo).
- **FR-009**: O envio real de teste MUST NOT criar clientes ou OS, nem usar telefones de clientes.
- **FR-010**: Sem número de teste configurado, o envio real MUST ser recusado com aviso claro.
- **FR-011**: Mensagens e relatórios voltados ao dono MUST estar em português.

### Key Entities

- **Execução da verificação**: data/hora, etapas executadas, resultado de cada uma, duração,
  resultado geral.
- **Mensagem esperada**: etapa, tipo, telefone de destino, número da OS, trecho de conteúdo.
- **Estado da conexão do WhatsApp**: conectado/desconectado/indisponível, horário da última
  verificação, motivo.
- **Teste real de envio**: número de destino (configuração), horário, resultado, motivo.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: O dono deixa de precisar do teste manual com cliente falso para validar uma
  atualização: a verificação completa roda com um único comando, sob demanda, em até 5 minutos.
- **SC-002**: 100% das etapas que enviam WhatsApp têm a mensagem esperada conferida a cada
  execução.
- **SC-003**: Uma etapa quebrada de propósito é detectada em 100% das execuções, com a etapa
  identificada no resumo.
- **SC-004**: Uma queda da conexão do WhatsApp aparece no painel e gera alerta em até 10 minutos.
- **SC-005**: Zero mensagens enviadas a clientes reais e zero registros criados no banco de
  produção pelas verificações.
- **SC-006**: Execuções repetidas dão o mesmo resultado (sem falhas intermitentes) em pelo menos
  95% das vezes sobre o mesmo código.

## Assumptions

- "Automatizar" cobre três níveis complementares: fluxo completo sem envio real (principal),
  monitoramento da conexão do WhatsApp e envio real só para o número do dono.
- O ambiente isolado da verificação usa um banco descartável próprio, não o Azure SQL.
- O serviço de WhatsApp em uso (Evolution API) informa o estado da conexão da instância.
- O número de teste do dono é configurado por variável de ambiente, como as demais.
- O teste de interface visual (cliques nas telas do Angular) fica fora desta feature; o foco é o
  fluxo pela API, que é por onde todas as telas passam.
- Os testes automáticos atuais (regras de negócio) continuam e não são substituídos.
