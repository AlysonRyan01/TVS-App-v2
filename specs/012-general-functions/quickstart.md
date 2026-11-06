# Quickstart: validar Funções gerais

## 1. Testes e builds

```bash
dotnet test src/TVSApp.Tests     # inclui UpdateServiceOrder e RegeneratePdf
cd web && npx ng build          # sem erro; inicial < 1 MB
```

## 2. Roteiro manual

1. Menu: "Geral" sumiu; "Clientes" aparece depois de "Buscar OS".
2. Buscar OS → abrir uma OS em andamento → **Reimprimir** → termo de entrada abre com impressão.
   Repetir com uma entregue (comprovante de entrega) e uma venda (comprovante de venda).
3. No painel, mudar a **Localização** e salvar → aparece no painel e nas abas/modais da Home.
4. **Alterar** → corrigir modelo e valor de peça → **Salvar** → "OS {n} alterada."; painel volta a
   leitura com os dados novos; a linha da busca mostra o modelo novo; Home atualiza.
5. **Alterar** → mudar algo → **Cancelar** (ou Esc) → valores voltam; nada salvo.
6. Alterar com modelo vazio → campo vermelho, Salvar bloqueado.
7. Alterar uma OS de venda → defeito/acessórios não aparecem; continua contando como venda.
8. Clientes → buscar "mar" → escolher → "Editar cliente" → mudar telefone → Salvar →
   "Cliente atualizado.".
