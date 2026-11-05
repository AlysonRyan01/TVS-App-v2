# Quickstart: modal "Consertado"

## 1. Build e testes

```bash
dotnet build TVSApp.sln
dotnet test src/TVSApp.Tests
cd web && npx ng build
```

## 2. Rotas

```bash
grep -rn "add-service-order-repair\|AddServiceOrderRepairCommand" src web/src --include=*.cs --include=*.ts
grep -n 'service-orders/{id:long}/repair' src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs
```

Esperado: o primeiro não retorna nada; o segundo mostra 1 rota com `RequireAuthorization()`.

## 3. Fluxo na tela (API + Angular rodando, usuário logado)

1. Aba **Aguardando peça** → coluna **Ação** com o botão **CONSERTADO**.
2. **CONSERTADO** → modal "Conserto" com OS, cliente, aparelho e resumo (solução, valor, garantia,
   "Peça comprada em").
3. **Cancelar / X / Esc** → nada muda.
4. **Consertado** → "Conserto registrado.", o modal fecha, a OS sai de "Aguardando peça" e aparece
   em **Prontos** com a data de hoje. O WhatsApp "aparelho pronto" é enfileirado (conferir no log
   do worker).
5. Com a API derrubada, clicar em Consertado → aparece a mensagem de erro e o modal continua aberto.
