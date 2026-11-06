# Quickstart: validar a limpeza das rotas

## 1. Testes e builds

```bash
dotnet test src/TVSApp.Tests       # inclui GetForCustomer, Approve/RejectByCustomer e pagamentos
cd web && npx ng build            # sem erro
```

## 2. Inventário

- Swagger (ou `grep -rn "app.Map" src/TVSApp.Api`): 37 rotas, iguais ao mapa do data-model.
- `grep -rn "MediatR\|ISender" src` vazio.

## 3. Sistema interno (US1)

Login → Home (todas as abas e ações, incluindo **Orçamento** que usa a rota nova) → Adicionar OS
→ Vender → Buscar OS (painel, alterar, reimprimir, prateleira) → Clientes → Métricas → deixar o
token expirar e conferir a renovação → Sair.

## 4. Site público (US2)

Com a API nova e o site **sem alterações**:
1. Consultar uma OS pelo link `consultar-os` com número e código → aparece. Código errado → erro.
2. Aprovar (e, em outra OS, reprovar) o orçamento → OS vai para Aprovados/Reprovados na Home,
   WhatsApp enfileirado.
3. Formulário de contato → mensagem chega.
