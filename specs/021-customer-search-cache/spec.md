# Feature Specification: Busca de clientes em memória

**Feature Branch**: `021-customer-search-cache`

**Created**: 2026-11-07

**Status**: Draft

**Input**: User description: "quero carregar todos os clientes na memoria Imemorycache da API, apenas ID e nome, o objetivo é fazer a busca por clientes no search bar (tela criar ordem, criar venda) ficar rapido, por que atualmente estou usando um full table scam no banco. tenho um pouco menos de 30 mil clientes, entao vai ser alguns mb apenas na memoria o que nao faz diferenca e deixa a pesquisa muito mais rapida"

## Contexto

Nas telas **Criar ordem de serviço** e **Criar venda**, o operador encontra o cliente digitando
parte do nome na barra "Buscar cliente". A busca começa com 3 letras e lista até 20 clientes,
dos mais recentes para os mais antigos. Se houver mais, aparece "Refine a busca para ver mais".
Cada resultado mostra o nome e o telefone. Ao clicar, abre a ficha completa do cliente para
confirmar.

Hoje cada letra digitada vira uma consulta que percorre a tabela inteira de clientes, quase
30 mil registros. Por isso a lista demora a aparecer, e a demora piora conforme a base cresce.
A proposta é manter na API uma lista enxuta de todos os clientes, com identificador e nome,
pronta para busca imediata, sem consultar o banco a cada letra.

## Clarifications

### Session 2026-11-07

- Q: O que exibir em cada resultado da busca? → A: Nome + telefone principal, como hoje. A lista em memória guarda ID, nome e telefone principal.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Encontrar o cliente instantaneamente ao digitar (Priority: P1)

O operador está no balcão criando uma ordem ou uma venda. Ele digita parte do nome do cliente,
por exemplo "joao sil", e a lista de clientes que contêm esse trecho aparece praticamente
enquanto ele digita. Ele escolhe o cliente certo e a ficha completa abre para confirmação,
como hoje.

**Why this priority**: é o objetivo da feature. A busca de cliente é o primeiro passo de toda
ordem e venda, e a lentidão atual atrasa o atendimento no balcão.

**Independent Test**: com uma base de cerca de 30 mil clientes, digitar 3 ou mais letras na
barra de busca da tela Criar ordem e medir o tempo até a lista aparecer. Depois, selecionar um
resultado e conferir que a ficha completa do cliente abre com os dados corretos.

**Acceptance Scenarios**:

1. **Given** uma base com cerca de 30 mil clientes, **When** o operador digita "silva" na barra
   de busca, **Then** a lista mostra até 20 clientes cujo nome contém "silva", dos mais recentes
   para os mais antigos, e o aviso "Refine a busca para ver mais" quando houver mais resultados.
2. **Given** o cliente "JOSÉ CONCEIÇÃO" cadastrado, **When** o operador digita "jose conceicao"
   (sem acento, em minúsculas), **Then** o cliente aparece na lista.
3. **Given** uma lista de resultados, **When** o operador clica em um cliente, **Then** a ficha
   completa abre com nome, telefones, documento e endereço atualizados.
4. **Given** um termo que não corresponde a nenhum cliente, **When** a busca termina, **Then**
   aparece "Nenhum cliente encontrado" e, na tela de criação, a opção de cadastrar usando o termo
   digitado, como hoje.
5. **Given** o operador digitou menos de 3 letras, **When** ele para de digitar, **Then** nenhuma
   busca é feita e aparece "Digite pelo menos 3 letras.", como hoje.

---

### User Story 2 - Cliente recém-cadastrado ou editado aparece na busca na hora (Priority: P1)

O operador cadastra um cliente novo, ou corrige o nome de um cliente existente. Na busca
seguinte, em qualquer tela e para qualquer operador, o cliente já aparece com o nome novo. A
lista em memória nunca fica atrasada em relação ao cadastro.

**Why this priority**: uma busca rápida mas desatualizada é pior que a atual. O operador
acharia que o cliente não existe e cadastraria um duplicado.

**Independent Test**: cadastrar um cliente com um nome único, buscar por esse nome logo em
seguida e confirmar que aparece. Depois, editar o nome e confirmar que a busca encontra o nome
novo e não encontra mais o antigo.

**Acceptance Scenarios**:

1. **Given** a busca já foi usada (lista em memória carregada), **When** o operador cadastra o
   cliente "MARIA TESTE ÚNICA" e em seguida busca "teste unica", **Then** o cliente aparece no
   topo da lista, por ser o mais recente.
2. **Given** um cliente "JOAO SILVA", **When** o nome é alterado para "JOAO SILVA SANTOS",
   **Then** a busca por "santos" encontra o cliente e nenhum resultado mostra o nome antigo.
3. **Given** um cadastro ou edição que falhou (por exemplo, validação recusada), **When** o
   operador busca, **Then** a lista em memória não contém a alteração que falhou.

---

### User Story 3 - A busca continua funcionando após reinício da API (Priority: P2)

Depois de um deploy ou reinício da API, a lista em memória está vazia. A primeira busca, ou a
própria inicialização, monta a lista a partir do banco. Nenhuma busca retorna vazia ou errada
porque a lista ainda não estava pronta.

**Why this priority**: o sistema é atualizado com frequência e a API reinicia a cada deploy.
A busca não pode falhar nesse intervalo, mas isso acontece raramente e dura pouco.

**Independent Test**: reiniciar a API e, imediatamente, buscar um cliente existente. Ele deve
aparecer, mesmo que essa primeira busca demore um pouco mais que as seguintes.

**Acceptance Scenarios**:

1. **Given** a API acabou de reiniciar, **When** o primeiro operador busca "silva", **Then** os
   resultados são os mesmos que seriam retornados antes do reinício.
2. **Given** a lista está sendo montada, **When** dois operadores buscam ao mesmo tempo,
   **Then** ambos recebem resultados corretos e a lista é montada uma única vez.

---

### Edge Cases

- **Nomes repetidos**: vários clientes com o mesmo nome, por exemplo "JOSE SILVA". O telefone
  exibido em cada resultado permite distinguir qual é qual (FR-006).
- **Cliente cadastrado por outro caminho**: se existir outra forma de criar ou alterar clientes
  além das telas do sistema, por exemplo um script direto no banco, a lista em memória pode
  ficar desatualizada até ser recarregada (ver FR-009).
- **Termo com espaços extras ou caracteres especiais**: "  joão   silva " deve ser tratado
  como "joao silva". Termos como "%" ou "_" são procurados literalmente, sem efeito de curinga.
- **Falha ao montar a lista**: se o banco estiver indisponível na montagem, a busca devolve um
  erro claro em português, como hoje quando o banco falha, e tenta montar de novo na próxima
  busca. Não guarda uma lista vazia como se fosse válida.
- **Edição simultânea**: duas edições do mesmo cliente quase ao mesmo tempo. A lista em memória
  deve terminar com o mesmo nome que ficou gravado no banco.
- **Crescimento da base**: a solução deve continuar adequada se a base dobrar, chegando a
  cerca de 60 mil clientes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST manter, na API, uma lista com todos os clientes cadastrados,
  contendo apenas identificador, nome e telefone principal (ver FR-006),
  e responder à busca por nome do cliente a partir dessa lista, sem consultar a tabela de
  clientes a cada busca.
- **FR-002**: A busca MUST encontrar clientes cujo nome **contém** o termo digitado, sem
  diferenciar maiúsculas de minúsculas e sem diferenciar acentos ("jose" encontra "JOSÉ").
- **FR-003**: A busca MUST manter as regras atuais da barra "Buscar cliente": mínimo de 3
  letras, mensagem "Digite pelo menos 3 letras." abaixo disso, no máximo 20 resultados
  exibidos, ordem do cliente mais recente para o mais antigo e indicação de que há mais
  resultados.
- **FR-004**: Ao selecionar um resultado, o sistema MUST exibir a ficha completa e atualizada do
  cliente (telefones, documento, endereço), obtida do cadastro e não da lista em memória.
- **FR-005**: Ao cadastrar um cliente ou alterar o nome de um cliente com sucesso, o sistema
  MUST refletir a mudança na lista em memória antes de a próxima busca ser respondida. Mudanças
  que falharem MUST NOT aparecer na lista.
- **FR-006**: Cada resultado da busca MUST exibir o nome e o telefone principal do cliente,
  como hoje. Quando o cliente não tiver telefone, o resultado exibe "Sem telefone". A lista em
  memória guarda, portanto, identificador, nome e telefone principal.
- **FR-007**: Se a lista em memória ainda não existir, por exemplo após um reinício, o sistema
  MUST montá-la a partir do cadastro antes de responder. Buscas simultâneas durante a montagem
  MUST aguardar a mesma montagem, sem disparar várias.
- **FR-008**: Se a montagem da lista falhar, a busca MUST retornar um erro em português ao
  operador, e o sistema MUST tentar montar de novo na busca seguinte, sem guardar uma lista
  vazia ou parcial.
- **FR-009**: O sistema MUST recarregar a lista inteira a partir do cadastro periodicamente,
  como rede de segurança contra alterações feitas fora das telas do sistema. O intervalo
  padrão é de 1 hora (ver Assumptions).
- **FR-010**: A tela "Clientes" (grade com filtros por nome, telefone e CPF/CNPJ) MUST continuar
  funcionando como hoje. Esta feature altera apenas a barra de busca das telas Criar ordem e
  Criar venda.
- **FR-011**: A busca MUST tratar o termo de forma literal: espaços no início e no fim são
  removidos, espaços repetidos viram um só, e caracteres como "%" e "_" não funcionam como
  curinga.

### Key Entities

- **Cliente (resumo para busca)**: representação reduzida de um cliente, usada só para
  localizá-lo. Contém o identificador, o nome e o telefone principal (FR-006), e o
  necessário para ordenar do mais recente para o mais antigo. Existe uma entrada para cada
  cliente cadastrado e ela é derivada do cadastro, que continua sendo a fonte da verdade.
- **Cliente (cadastro completo)**: entidade existente, sem mudanças. É consultada apenas quando o
  operador seleciona um resultado.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Com cerca de 30 mil clientes, 95% das buscas na barra "Buscar cliente" mostram
  os resultados em menos de 300 ms após o operador parar de digitar, medidos na tela.
- **SC-002**: A busca por nome deixa de gerar consultas à tabela de clientes. Em operação
  normal, fora a montagem ou recarga da lista, o número de consultas de busca de cliente por
  nome ao banco cai a zero.
- **SC-003**: 100% dos clientes cadastrados ou renomeados pelas telas do sistema aparecem
  com o nome correto na primeira busca feita logo após a gravação.
- **SC-004**: Para os mesmos termos, a busca nova retorna os mesmos clientes que a busca atual,
  ou mais clientes quando a única diferença for o acento. Isso deve ser verificado com um
  conjunto de pelo menos 10 termos reais.
- **SC-005**: O uso de memória da API aumenta em no máximo 20 MB com 30 mil clientes.
- **SC-006**: Após um reinício da API, a primeira busca retorna resultados corretos em até
  3 segundos.

## Assumptions

- A API roda em **uma única instância**, como no `docker-compose.yml` atual. Com mais de uma
  instância, uma alteração feita em uma não seria vista pela outra até a recarga periódica, e
  isso exigiria rever a solução.
- Não existe exclusão de clientes no sistema hoje, então a feature não precisa tratar remoção.
  Se a exclusão for criada no futuro, ela também deve atualizar a lista (mesma regra do FR-005).
- O intervalo de recarga completa de **1 hora** é suficiente como rede de segurança, já que
  as alterações pelas telas são refletidas imediatamente (FR-005).
- O endpoint de ficha completa do cliente pode ser criado ou reaproveitado para atender o FR-004.
  Hoje a busca devolve o cadastro completo de cada resultado.
- O tamanho estimado da lista, entre 30 mil e 60 mil clientes com poucos campos curtos, cabe com
  folga no limite de memória do container da API.
- A ordem "mais recente primeiro" usa o identificador do cliente, como hoje.
