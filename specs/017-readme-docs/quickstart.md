# Quickstart: validar o README

## 1. Pré-requisitos
Repositório atualizado; .NET 9 SDK; Node 22; Docker.

## 2. Rotas: código × README (SC-001)
```bash
grep -rhoE 'Map(Get|Post|Put|Delete)\("[^"]+"' src/TVSApp.Api/Endpoints \
  | sed -E 's/Map(Get|Post|Put|Delete)\("([^"]+)"/\U\1\E \2/; s/:long//g' | sort -u > /tmp/rotas-codigo.txt
grep -oE '^\| `?(GET|POST|PUT|DELETE)`? \| `[^`]+`' README.md \
  | sed -E 's/^\| `?([A-Z]+)`? \| `([^`]+)`/\1 \2/' | sort -u > /tmp/rotas-readme.txt
diff /tmp/rotas-codigo.txt /tmp/rotas-readme.txt && echo "rotas OK"
```
Esperado: `rotas OK` (41 linhas).

## 3. Objetos e enums (SC-002)
Para cada arquivo em `src/TVSApp.Application/Requests/**` (exceto WhatsApp) e `src/TVSApp.Domain/Enums`,
o nome aparece no README e todos os campos/valores estão na tabela correspondente.

## 4. Variáveis (SC-005)
```bash
for v in $(grep -oE '^[A-Z_]+=' .env.example | tr -d =); do grep -q "$v" README.md || echo "faltando: $v"; done
```
Esperado: nenhuma linha.

## 5. Comandos (SC-006)
- Execução local: subir SQL Server local, `dotnet run` da API, `npm start` do web → tela de login.
- `dotnet test src/TVSApp.Tests` verde.
- `docker compose config --quiet` com `.env` de exemplo preenchido.

## 6. Qualidade
- Nenhum segredo real (`grep -iE 'password=|apikey' README.md` só mostra placeholders).
- Todos os links internos (âncoras e arquivos) resolvem.
- Texto todo em pt-BR.
- Pessoa sem contexto encontra uma rota pelo índice em ≤ 1 min (SC-004).
