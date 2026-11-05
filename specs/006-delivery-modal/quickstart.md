# Quickstart: modal "Entregue"

## 1. Build e testes

```bash
dotnet build TVSApp.sln
dotnet test src/TVSApp.Tests
cd web && npx ng build
```

## 2. Rotas

```bash
grep -rn "add-service-order-delivery\|AddServiceOrderDeliveryAndReturnPdfCommand" src web/src --include=*.cs --include=*.ts
grep -n 'service-orders/{id:long}/delivery' src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs
```

Esperado: o primeiro não retorna nada; o segundo mostra 1 rota com `RequireAuthorization()`.

## 3. Fluxo na tela (API + Angular rodando, usuário logado)

1. Aba **Prontos** → coluna **Ação** com o botão **ENTREGUE**.
2. **ENTREGUE** numa OS consertada → modal "Entrega" com o resumo e a dica "O termo de entrega
   será aberto para impressão.".
3. **Cancelar / X / Esc** → nada muda.
4. **Entregue** (OS consertada) → abre uma nova aba com o termo em PDF e, logo em seguida, a
   janela de impressão do navegador; "Ordem de serviço
   entregue."; o modal fecha; a OS sai de "Prontos". O WhatsApp de garantia é enfileirado
   (conferir no log do worker).
5. Bloquear pop-ups do site e repetir com outra OS consertada → o PDF é baixado como
   `ordem_servico_{número}.pdf`.
6. **Entregue** numa OS reprovada liberada → sem PDF e sem nova aba; "Ordem de serviço entregue."
   e a OS sai de "Prontos".
7. Com a API derrubada, clicar em Entregue → aparece a mensagem de erro, o modal continua aberto e a
   aba vazia (se tiver sido aberta) é fechada.
