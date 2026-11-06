# Feature Specification: Tela "Métricas"

**Feature Branch**: `013-metrics-dashboard`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "vamos criar a tela de metricas, as metricas devem ser essas [Imagem: 'Dashboard de Métricas' do sistema antigo]"

## Contexto

O item **Métricas** do menu não leva a lugar nenhum. O sistema antigo tinha o **Dashboard de
Métricas** (imagem enviada), com:

- **Período**: Mês + Ano, ou um intervalo De/Até, e o botão **Consultar**; subtítulo com o período
  (ex.: "Novembro/2026").
- **11 indicadores** (cartões): Total de ordens, Ordens ativas, Receita de reparos, Taxa de
  aprovação, Tempo médio de ciclo, Total de vendas, Receita de vendas, A receber (prontos ainda não
  entregues), Gasto com peças, Valor total e Total líquido.
- **2 gráficos de rosca**: Ordens por status e Resultado dos reparos.
- **Tabela "Produtos entregues no período (N)"**: Ordem, Cliente, Produto, Tipo, Status
  (Reparo/Venda), Empresa, Entregue em, Valor total e **Valor da peça** (editável na própria linha).

O cálculo já existe na API (serviço de métricas por mês/ano e por intervalo), mas as rotas usam o
padrão antigo (MediatR) e a gravação do **valor da peça** (`update-service-order-part-purchase-cost`,
`old-handlers/UpdatePartPurchaseCostHandler.cs`) está **quebrada**.

**Atores**: dono/gerente da assistência, logado, acompanhando o mês.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver os indicadores do mês (Priority: P1)

A pessoa abre **Métricas** e vê os indicadores, os gráficos e os produtos entregues do mês atual.
Pode trocar o mês/ano ou escolher um intervalo e consultar.

**Why this priority**: é o objetivo da tela.

**Independent Test**: abrir a tela e conferir os números do mês atual com os dados do banco; trocar
para outro mês e para um intervalo.

**Acceptance Scenarios**:

1. **Given** a tela aberta, **Then** o período é o mês atual e aparecem os 11 indicadores, os 2
   gráficos e a tabela de entregues, com o subtítulo "Mês/Ano".
2. **When** a pessoa escolhe outro Mês/Ano e clica em **Consultar**, **Then** tudo é recalculado
   para aquele mês.
3. **When** a pessoa preenche De e Até e clica em **Consultar**, **Then** tudo é recalculado para o
   intervalo (dias inteiros, inclusive o último) e o subtítulo mostra "dd/MM/aaaa a dd/MM/aaaa".
4. **Given** os indicadores, **Then** valem as definições abaixo (as mesmas do sistema antigo):

| Indicador | Definição |
|-----------|-----------|
| Total de ordens | OS de reparo com entrada no período |
| Ordens ativas | dessas, as não entregues |
| Receita de reparos | soma das OS de reparo aprovadas, com conserto e entregues no período |
| Taxa de aprovação | aprovados ÷ (aprovados + reprovados) das OS de reparo do período, em % |
| Tempo médio de ciclo | média, em dias, da entrada até a entrega das OS de reparo do período já entregues |
| Total de vendas | OS de venda com entrada no período |
| Receita de vendas | soma das vendas entregues no período |
| A receber | soma das OS de reparo aprovadas, com conserto, ainda não entregues |
| Gasto com peças | soma do valor da peça dos produtos entregues no período |
| Valor total | receita de reparos + receita de vendas |
| Total líquido | valor total − gasto com peças |

5. **Given** os gráficos, **Then** "Ordens por status" mostra a quantidade por situação (Entrada,
   Avaliado, Aguardando peça, Consertado, Pronto p/ retirada, Entregue) e "Resultado dos reparos" a
   quantidade por resultado (Tem conserto, Não tem conserto, Não apresentou defeito), com legenda.

---

### User Story 2 - Informar o valor pago na peça (Priority: P1)

Na tabela de entregues, a pessoa digita o **valor da peça** de cada produto; o Gasto com peças e o
Total líquido se atualizam.

**Why this priority**: sem isso o Total líquido fica sempre igual ao Valor total; hoje está quebrado.

**Independent Test**: informar o valor da peça de uma linha e conferir os dois indicadores.

**Acceptance Scenarios**:

1. **Given** uma linha, **When** a pessoa altera o Valor da peça e sai do campo (ou Enter), **Then**
   o valor é gravado, aparece "Valor da peça salvo." e Gasto com peças e Total líquido são
   atualizados sem recarregar a tela.
2. **Given** um valor negativo, **Then** o campo fica em vermelho e nada é gravado.
3. **Given** uma falha ao gravar, **Then** aparece a mensagem e o campo volta ao valor anterior.

---

### User Story 3 - Período sem dados e erros (Priority: P2)

**Acceptance Scenarios**:

1. **Given** um período sem OS, **Then** os indicadores mostram 0 / R$ 0,00 / 0%, os gráficos
   mostram "Sem dados no período" e a tabela "Nenhum produto entregue no período.".
2. **Given** De sem Até (ou o contrário) ou Até antes de De, **Then** o campo fica em vermelho e a
   consulta não roda.
3. **Given** uma falha ao carregar, **Then** aparece a mensagem de erro e os filtros continuam.

### Edge Cases

- **Tempo médio de ciclo negativo** (a imagem mostra "-0,6 dias"): acontece quando a data de
  entrada de uma OS foi alterada para depois da entrega. Essas OS ficam fora da média.
- **Taxa de aprovação** sem orçamento respondido no período: 0%.
- **OS de venda**: contam só nos indicadores de venda; nunca em reparos, ordens ativas ou gráficos
  de status/resultado.
- **Mês/Ano e intervalo**: o último usado vale; ao consultar por Mês/Ano, De/Até são limpos, e
  vice-versa.
- **Valor da peça de uma venda**: editável também (entra no Gasto com peças).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A tela **Métricas** MUST existir na rota `/metricas` (só para usuário logado), ligada
  ao item "Métricas" do menu, com o layout da imagem, no visual das telas atuais.
- **FR-002**: Filtros de período (Mês + Ano, ou De/Até) e botão Consultar, abrindo no mês atual.
- **FR-003**: Os 11 indicadores com as definições da US1-4, em cartões com título, valor e legenda
  curta (como na imagem).
- **FR-004**: Os 2 gráficos da US1-5 como **barras horizontais ordenadas** (maior primeiro, com
  quantidade e %), no lugar das roscas da imagem: comparam melhor valores próximos (guia de
  visualização de dados do projeto).
- **FR-005**: A tabela de produtos entregues no período (reparos e vendas), mais recentes primeiro,
  com as colunas da imagem e o total no título.
- **FR-006**: O Valor da peça MUST ser editável na tabela e gravado como no
  `old-handlers/UpdatePartPurchaseCostHandler.cs`, atualizando Gasto com peças e Total líquido.
- **FR-007**: As rotas de métricas e a de valor da peça MUST seguir o padrão REST e sem MediatR das
  features anteriores; as antigas MUST ser substituídas.
- **FR-008**: Os cálculos dos indicadores e a regra do valor da peça MUST ter testes automatizados.

### Key Entities

- **Ordem de serviço** (reparo ou venda): existente; o "valor da peça" é o custo de compra da peça
  já guardado na OS. Nada muda no banco.
- **Período**: mês/ano ou intervalo de datas.

## Success Criteria *(mandatory)*

- **SC-001**: A tela abre com os números do mês atual em menos de 3 segundos.
- **SC-002**: 100% dos indicadores batem com a conferência manual de um mês.
- **SC-003**: Informar o valor da peça de um produto leva menos de 5 segundos e atualiza o Total
  líquido na hora.

## Assumptions

- **Definições**: as do serviço de métricas atual (que reproduz o sistema antigo), com a correção do
  tempo de ciclo negativo.
- **Fora do escopo**: gráficos por tipo de produto e por empresa (o serviço calcula, mas não estão
  na imagem), lista de ordens abandonadas, exportação.
- **Padrão técnico**: igual às features anteriores (REST, handlers sem MediatR, textos em pt-BR,
  visual das telas atuais, tela carregada sob demanda).
