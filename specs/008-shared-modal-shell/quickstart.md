# Quickstart: estrutura compartilhada dos modais

## 1. Build e limites de estilo

```bash
cd web && npx ng build
```

Esperado: build sem erros e **sem** aviso "exceeded maximum budget" para os estilos de
`estimate-response`, `part-purchase`, `rejected-release`, `repair`, `delivery` e da casca
(`shared/service-order-modal`). O aviso do orçamento pode continuar.

## 2. Redução de código (SC-002)

```bash
cd web/src/app/features/home/modals
find . -type f \( -name "*.component.*" -o -name "*.service.ts" -o -name "_*.scss" \) | xargs wc -l | tail -1
```

Esperado: ≤ ~1.950 linhas (antes: 3.901). **Medido na implementação: 1.811 linhas (−54%).**

## 3. Sem duplicação

```bash
grep -rln "ticket-device\|getServiceOrderById" web/src/app/features/home/modals
```

Esperado: `ticket-device` só nos arquivos de `shared/`; `getServiceOrderById` em nenhum arquivo.

## 4. Fluxo na tela (API + Angular rodando, usuário logado)

Para cada aba, abrir o modal e conferir que é visualmente igual ao de antes (tela larga e
estreita), e repetir o fluxo da feature correspondente:

| Aba | Modal | Roteiro |
|-----|-------|---------|
| Orçamentos pendentes | Orçamento | specs/001 (enviar orçamento, validação, Enter envia) |
| Aguardando resposta | Resposta | specs/002 §3 |
| Aprovados | Compra da peça | specs/003 §3 |
| Reprovados | Liberação | specs/004 §3 |
| Aguardando peça | Conserto | specs/005 §3 |
| Prontos | Entrega | specs/006 §3 (PDF + impressão automática) |

Também conferir, em qualquer modal: derrubar a API ao abrir → estado de erro → subir a API →
"Tentar de novo" carrega a OS.
