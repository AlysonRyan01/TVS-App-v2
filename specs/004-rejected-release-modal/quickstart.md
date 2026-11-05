# Quickstart: modal "Liberado"

## 1. Build e testes

```bash
dotnet build TVSApp.sln
dotnet test src/TVSApp.Tests
cd web && npx ng build
```

## 2. Rotas

```bash
grep -rn "mark-estimate-rejected-as-ready-to-pick-up\|MarkEstimateRejectedAsReadyToPickUpCommand" src web/src --include=*.cs --include=*.ts
grep -n 'service-orders/{id:long}/ready-for-pickup' src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs
```

Esperado: o primeiro não retorna nada; o segundo mostra 1 rota com `RequireAuthorization()`.

## 3. Fluxo na tela (API + Angular rodando, usuário logado)

1. Aba **Reprovados** → coluna **Ação** com o botão **LIBERADO**.
2. **LIBERADO** → modal "Liberação" com OS, cliente, aparelho e resumo (solução, valor,
   garantia, "Reprovado em").
3. **Cancelar / X / Esc** → nada muda.
4. **Liberado** → "Ordem de serviço liberada para retirada.", o modal fecha, a OS sai de
   "Reprovados" e aparece em **Prontos**. O WhatsApp "reprovado pronto para retirada" é
   enfileirado (conferir no log do worker).
5. Abrir o modal de outra OS reprovada em duas abas do navegador e liberar nas duas → a segunda
   também é aceita e reenvia o WhatsApp (comportamento do handler antigo, documentado na spec).
6. Com a API derrubada, clicar em Liberado → aparece a mensagem de erro e o modal continua aberto.
