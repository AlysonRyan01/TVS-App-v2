# Quickstart: validar Buscar OS

## 1. Testes e builds

```bash
dotnet test src/TVSApp.Tests          # inclui os novos testes de Search
cd web && npx ng build               # sem erro; pacote inicial < 1 MB; chunk da busca sob demanda
```

## 2. Roteiro manual

1. Menu **Buscar OS** → `/buscar`; botão Buscar bloqueado sem filtro.
2. Número de uma OS existente → só ela. Número inexistente → "Nenhuma ordem de serviço encontrada."
3. Cliente "mar" → OS de clientes com "mar" no nome; "ma" → campo vermelho, sem busca.
4. Digitar no Modelo com o Cliente preenchido → Cliente é limpo. Modelo " lg " acha "LG".
5. Série parcial → OS com a série contendo o trecho.
6. Período de um mês → OS com entrada no mês, inclusive o último dia; paginação de 20 e total.
   Data final antes da inicial → campo vermelho, sem busca.
7. Uma OS de venda aparece com a situação "Venda".
8. Clicar numa linha → painel só leitura com cliente, aparelho, orçamento, valores e datas; Esc
   fecha e a lista continua na mesma página.
9. Limpar → filtros e resultados apagados.
