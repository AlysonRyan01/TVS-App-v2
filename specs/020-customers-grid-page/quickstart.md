# Quickstart: validar a tela Clientes e o "Ordens"

## 1. Automático
```bash
dotnet test src/TVSApp.Tests                      # validações do handler
dotnet test src/TVSApp.IntegrationTests           # busca real: nome sem acento, telefone formatado, documento, paginação, ordem
cd web && npm run build
```

## 2. Manual (sistema local ou Docker)
1. Menu mostra **Ordens** (não "Buscar OS"); `/buscar` abre `/ordens`; título da aba "Ordens | TVS Eletrônica".
2. **Clientes** abre com a grade dos clientes mais recentes e "Últimos clientes · N no total".
3. Nome "maria" (minúsculo) e "joao" encontram "MARIA…" e "JOÃO…". Nome "ma" → aviso de 3 letras.
4. Telefone "(41) 99999-0000" encontra clientes com o número gravado com ou sem pontuação (principal ou secundário).
5. CPF com pontuação encontra o cliente.
6. Mais de 20 clientes → paginação funciona; **Limpar** volta à lista inicial.
7. Clicar numa linha → edição; salvar → linha atualizada, mesma página e filtros.
8. Em outra aba, alterar um cliente → a grade atualiza sozinha.
9. Celular (390 px): grade vira cartões; sem rolagem lateral; a lista rola até o fim.
10. Telas lado a lado (Ordens × Clientes): mesmo arranjo (SC-004).
