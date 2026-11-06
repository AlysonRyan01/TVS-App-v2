# Quickstart: validar a tela Vender

## 1. Testes

```bash
dotnet test src/TVSApp.Tests
```

Esperado: verdes, incluindo os novos de `CreateSalesServiceOrder` em
`ServiceOrderCommandHandlerTests` (validação, normalização, valores da OS de venda, falha ao criar,
falha no PDF, WhatsApp).

## 2. Build do frontend

```bash
cd web && npx ng build
```

Esperado: sem erro; pacote inicial < 1 MB; `add-service-order` e `sell` como chunks sob demanda.

## 3. Roteiro manual (API + web rodando)

1. Menu **Vender** → `/vender`; coluna ② desfocada.
2. Buscar e confirmar um cliente; preencher modelo, série, garantia "90 dias" e valor; **Criar
   venda**. Esperado: comprovante abre com impressão; "Venda registrada (OS {n})."; tela limpa.
3. No banco: a OS {n} tem defeito `VENDA`, empresa Particular, situação Entregue, `PartCost` = valor,
   garantia em maiúsculas. Não aparece nas abas da Home; aparece nas métricas como venda.
4. Valor 0 ou garantia vazia → campo vermelho, botão bloqueado. "Trocar cliente" mantém os dados.
5. Derrubar a API e tentar → mensagem de erro, campos preservados.
6. `/adicionar` continua igual (coluna de cliente agora no picker).
