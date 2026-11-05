# Data Model: Estrutura compartilhada dos modais

Não há dados nem mudança de API. Este documento descreve a estrutura de componentes.

```text
<app-service-order-modal>                     ← casca (ServiceOrderModalComponent)
├── header.bar        [title] [closeLabel] [closeResult] [sending]
├── @if loading       → spinner
├── @else if order
│   └── .body
│       ├── .columns
│       │   ├── <app-service-order-ticket [order] [id]>   ← painel lateral
│       │   └── <ng-content select="[modalBody]">          ← conteúdo do modal
│       └── footer.summary
│           ├── <ng-content select="[modalFooterInfo]">    ← pergunta / dica / total
│           └── .actions <ng-content select="[modalActions]"> ← botões
└── @else             → erro + Fechar + Tentar de novo (retry interno)
```

## Responsabilidades

| Parte | Responsável |
|-------|-------------|
| Carregar a OS, estados de carregamento e erro, retry | casca |
| Painel lateral (número, cliente, telefones, aparelho, modelo, série, acessórios, defeito) | `ServiceOrderTicketComponent` |
| Título, texto do X, valor ao fechar, bloqueio do X durante o envio | casca (via inputs) |
| Resumo/formulário, botões, ação, mensagens, `isSending` | cada modal |

## Modais (6)

| Modal | Título | Conteúdo próprio | Estilos próprios |
|-------|--------|------------------|------------------|
| `add-service-order-estimate` | Orçamento | formulário (resultado, solução, mensagem, valores, garantia) + total/margem no rodapé | campos e totais |
| `estimate-response` | Resposta | resumo do orçamento + Reprovado/Aprovado | `.reject` |
| `part-purchase` | Compra da peça | resumo aprovado + Comprado | — |
| `rejected-release` | Liberação | resumo reprovado + Liberado | — |
| `repair` | Conserto | resumo do conserto + Consertado | — |
| `delivery` | Entrega | resumo da entrega + dica do PDF + Entregue | `.hint` |
