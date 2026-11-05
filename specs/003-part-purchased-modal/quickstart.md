# Quickstart: modal "Comprado"

## 1. Build e testes

```bash
dotnet build TVSApp.sln
dotnet test src/TVSApp.Tests
cd web && npx ng build
```

## 2. Rotas

```bash
grep -rn "add-service-order-purchased-part\|AddPurchasedPartCommand" src web/src --include=*.cs --include=*.ts
grep -n 'service-orders/{id:long}/part/purchase' src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs
```

Esperado: o primeiro não retorna nada; o segundo mostra 1 rota com `RequireAuthorization()`.

## 3. Fluxo na tela (API + Angular rodando, usuário logado)

1. Aba **Aprovados** → coluna **Ação** com o botão **COMPRADO**.
2. **COMPRADO** → modal "Compra da peça" com OS, cliente, aparelho e resumo (solução, valor,
   custo da peça, garantia, "Aprovado em").
3. **Cancelar / X / Esc** → nada muda.
4. **Comprado** → "Compra da peça registrada.", o modal fecha, a OS sai de "Aprovados" e aparece
   em **Aguardando peça** com a data de hoje.
5. Com a API derrubada, clicar em Comprado → aparece a mensagem de erro e o modal continua aberto.
