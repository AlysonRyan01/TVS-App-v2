# Quickstart: validar a busca de clientes em memória

**Feature**: [spec.md](./spec.md) | **Contracts**: [contracts/customers-api.md](./contracts/customers-api.md)

## Pré-requisitos

- Ambiente local com `docker compose up -d` (API + SQL Server) ou a API rodando com `dotnet run`
  em `src/TVSApp.Api`.
- `API` apontando para a API: `export API=http://localhost:5001` (`dotnet run`) ou
  `export API=http://localhost:8080/api` (compose, via Caddy).
- Um token de acesso: `export TOKEN=$(curl -s -X POST $API/auth/login -H "Content-Type: application/json" -d '{"email":"...","password":"..."}' | jq -r .data.accessToken)`.
- Base com volume real de clientes, idealmente uma cópia da produção com cerca de 30 mil clientes,
  para medir os tempos.

## 1. Testes automatizados

```bash
dotnet build
dotnet test src/TVSApp.Tests
```

Devem cobrir, no mínimo:
- `CustomerNameNormalizer`: acento, `Ç`, maiúsculas e minúsculas, espaços extras, `%`/`_` literais.
- `CustomerQueryHandler.SearchByName`: termo curto → 400, ordem `Id` desc, limite 21, sem acento.
- `CustomerCommandHandler.Create`/`Update`: chamam `Invalidate()` quando dão certo e **não**
  chamam quando o repositório lança exceção.
- `CustomerSearchCache`: montagem única com buscas simultâneas, nada gravado após invalidação
  no meio da montagem (geração), nada gravado após falha.

```bash
dotnet test src/TVSApp.IntegrationTests --filter "FullyQualifiedName~Clientes"
```

Deve cobrir o fluxo cadastrar → buscar (aparece) → renomear → buscar (nome novo aparece, nome
antigo some) e `GET /customers/{id}` (200 e 404).

## 2. Busca e tempos (US1, SC-001, SC-006)

```bash
# reinicia a API para começar sem cache
docker compose restart api

# primeira busca: monta a lista (meta: < 3 s)
time curl -s -H "Authorization: Bearer $TOKEN" "$API/customers?name=silva" | head -c 300

# buscas seguintes: vêm da memória
time curl -s -H "Authorization: Bearer $TOKEN" "$API/customers?name=silva" > /dev/null
time curl -s -H "Authorization: Bearer $TOKEN" "$API/customers?name=jose%20conceicao" | head -c 300
```

Esperado: a primeira chamada é a mais lenta. Nas seguintes, o tempo do servidor é de poucos ms.
A busca "jose conceicao" encontra clientes gravados como "JOSÉ CONCEIÇÃO".

No log (Loki ou `docker logs`), deve aparecer **uma** linha de montagem do snapshot, com
quantidade e duração. Use essa duração para confirmar a estimativa do research (R6).

## 3. Invalidação ao criar e editar (US2, SC-003)

```bash
# cadastra (resposta traz o id)
curl -s -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  $API/customers \
  -d '{"name":"MARIA TESTE ÚNICA","phone":"41999990000","phone2":"","email":"","document":null,
       "street":"RUA","number":"1","neighborhood":"CENTRO","city":"CURITIBA","state":"PR","zipCode":"80000000"}'

# deve aparecer no topo
curl -s -H "Authorization: Bearer $TOKEN" "$API/customers?name=teste%20unica"

# renomeia (troque {id})
curl -s -X PUT -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  $API/customers/{id} \
  -d '{"name":"MARIA RENOMEADA XYZ","phone":"41999990000","phone2":"","email":"","document":null,
       "street":"RUA","number":"1","neighborhood":"CENTRO","city":"CURITIBA","state":"PR","zipCode":"80000000"}'

curl -s -H "Authorization: Bearer $TOKEN" "$API/customers?name=renomeada%20xyz"   # aparece
curl -s -H "Authorization: Bearer $TOKEN" "$API/customers?name=teste%20unica"     # não aparece mais
```

## 4. Ficha completa (FR-004)

```bash
curl -s -H "Authorization: Bearer $TOKEN" $API/customers/{id}    # 200, CustomerDto completo
curl -s -H "Authorization: Bearer $TOKEN" $API/customers/999999999 # 404
```

## 5. Tela (Criar ordem e Criar venda)

1. Digite "sil" na barra "Buscar cliente". A lista mostra nome + telefone ("Sem telefone"
   quando vazio) e, com muitos resultados, "Refine a busca para ver mais".
2. Digite "jose conceicao" sem acento. Os clientes com acento aparecem.
3. Clique em um resultado. A ficha completa abre com endereço e documento.
4. Na tela **Clientes** (grade), os filtros por nome, telefone e CPF/CNPJ funcionam como antes (FR-010).

## 6. Memória (SC-005)

```bash
docker stats --no-stream tvs-app-v2-api-1   # antes da primeira busca e depois
```

Esperado: aumento de no máximo cerca de 20 MB depois da montagem. No Grafana, painel de memória
por container, imagem `tvs-app-v2-api`.
