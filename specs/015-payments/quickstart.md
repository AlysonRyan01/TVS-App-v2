# Quickstart: validar Pagamentos

## 1. Testes e builds

```bash
dotnet test src/TVSApp.Tests     # validação, resumo do comprovante, valor por extenso, rota do comprovante
cd web && npx ng build          # sem erro
```

## 2. Roteiro manual

1. Abrir uma OS em cada modal da Home (Orçamento … Entrega) e no painel da Buscar OS: a caixa
   **Pagamentos** aparece igual nos 7 lugares.
2. Adicionar R$ 100 hoje → "Pagamento adicionado."; totais: Total pago 100, Falta = valor − 100.
3. Editar para R$ 120 → atualiza; remover (confirmar) → some.
4. Pagar o valor total → "Quitado"; pagar a mais → "Crédito de R$ X".
5. Valor 0 ou data vazia → campo vermelho, Salvar bloqueado.
6. **Imprimir comprovante** de um pagamento → PDF em nova aba com impressão: Nº, OS, cliente,
   aparelho, valor por extenso, data, resumo (valor da OS, pago até este, saldo).
7. Abrir o mesmo pagamento em outro modal → mesmos dados.
