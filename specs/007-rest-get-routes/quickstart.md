# Quickstart: rotas GET no padrão REST

## 1. Build e testes

```bash
dotnet build TVSApp.sln
dotnet test src/TVSApp.Tests
cd web && npx ng build
```

Esperado: tudo verde; `ServiceOrderQueryHandlerTests` (mapeamento fila → consulta) continua passando.

## 2. Rotas antigas eliminadas

```bash
grep -rnE "get-(pending-estimate|awaiting-response|approved|rejected|awaiting-part|ready-for-pickup)-service-orders|get-service-order-by-id" src web/src --include=*.cs --include=*.ts
```

Esperado: nenhuma saída.

## 3. Chamadas diretas (API rodando, token de um usuário logado em `$TOKEN`)

```bash
API=http://localhost:5000   # ajustar para a porta da API
curl -s -H "Authorization: Bearer $TOKEN" "$API/service-orders/approved" | head -c 300           # padrão 1/25
curl -s -H "Authorization: Bearer $TOKEN" "$API/service-orders/approved?pageNumber=2&pageSize=5" | head -c 300
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" "$API/service-orders/approved?pageNumber=0"     # 400
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" "$API/service-orders/approved?pageNumber=abc"   # 400
curl -s -o /dev/null -w "%{http_code}\n" "$API/service-orders/approved"                                                     # 401 (sem login)
curl -s -H "Authorization: Bearer $TOKEN" "$API/service-orders/1" | head -c 300                 # OS 1
```

Esperado: na primeira chamada, `pageNumber: 1` e `pageSize: 25` na resposta; na segunda, `2` e `5`.

## 4. Tela

Abrir a Home, passar pelas 6 abas, trocar de página e de tamanho e abrir cada um dos 6 modais. No
DevTools (aba Network), as chamadas devem ir para `/service-orders/...`, e o conteúdo deve ser
igual ao de antes.
