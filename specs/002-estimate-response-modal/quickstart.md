# Quickstart: validação do modal de resposta

## Pré-requisitos

- .NET 9 SDK, Node + Angular CLI, SQL Server (`docker-compose.yml`), usuário para login.
- Pelo menos 3 OS em "Aguardando resposta" (orçamento enviado com "Tem conserto", empresa
  diferente de PhilcoBritania) e 1 da PhilcoBritania, se houver.

## 1. Build e testes

```bash
dotnet build TVSApp.sln
dotnet test src/TVSApp.Tests
cd web && npx ng build
```

Esperado: tudo verde, incluindo os testes novos de Domain e Handler ([research.md](research.md) R8).

## 2. Rotas

```bash
grep -n "add-service-order-approve-estimate\|add-service-order-reject-estimate" -r src web/src
grep -n "service-orders/{id:long}/estimate/" src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs
```

Esperado: o primeiro não retorna nada; o segundo mostra as 2 rotas, ambas com
`RequireAuthorization()`.

## 3. Fluxo na tela

1. Subir a API (`dotnet run --project src/TVSApp.Api`) e o Angular (`cd web && npx ng serve`) e
   fazer login.
2. Aba **Aguardando resposta** → a coluna **Ação** aparece com o botão **RESPOSTA** em cada linha.
3. **RESPOSTA** → o modal abre com OS, cliente, aparelho e o resumo do orçamento.
4. **Cancelar / X / Esc** → o modal fecha e a OS continua na lista.
5. **Aprovado** → "Orçamento aprovado.", o modal fecha, a lista recarrega sem a OS. A OS
   aparece em **Aprovados**. Uma notificação interna é criada. O WhatsApp de agradecimento é
   enfileirado (conferir no log do worker).
6. Repetir com outra OS clicando em **Reprovado** → aparece em **Reprovados**, com notificação e
   WhatsApp de confirmação.
7. Abrir o modal da mesma OS em duas abas do navegador, aprovar em uma e depois clicar em
   Aprovado na outra → aparece "Esse orçamento já foi respondido.", o modal continua aberto e os
   botões voltam a ficar habilitados.
8. Derrubar a API e clicar em Aprovado → aparece a mensagem de erro, e o modal continua aberto.

## 4. Contrato

No DevTools (aba Network): o PUT vai para `/service-orders/{id}/estimate/approve|reject`; um erro
de validação volta com HTTP 4xx (não 200) e `message` em português ([contracts](contracts/http-endpoints.md)).
