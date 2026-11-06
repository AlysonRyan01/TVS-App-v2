# Research: README completo (pt-BR)

Estado atual: `README.md` só com `# TVS-App`. Documentação existente: `DEPLOY.md` (produção e
observabilidade), specs por feature em `specs/`, constituição em `.specify/memory/constitution.md`,
Swagger só em `Development`.

## R1. Um arquivo ou vários

- **Decision**: um único `README.md` na raiz, com índice no topo e seções âncora; a referência
  da API fica no próprio README (pedido explícito do usuário: "rotas, dtos ... completo").
- **Rationale**: um lugar só para procurar; o GitHub gera âncoras e o índice cobre o SC-004.
- **Alternatives**: `docs/api.md` separado (melhor para tamanho, mas espalha a informação; fica
  como saída se o arquivo passar de ~2.000 linhas — spec, Assumptions).

## R2. Fonte da verdade e conferência

- **Decision**: tudo é extraído do código atual: rotas dos `Map*` em `src/TVSApp.Api/Endpoints`
  (41) + `MapHub("/osHub")` + `/internal/metrics` + `GET /`; campos dos records de `Requests/` e
  `DTOs/`; valores dos enums de `Domain/Enums`. A validação compara a lista de rotas do código com
  a do README por script de linha de comando (quickstart §2).
- **Rationale**: atende SC-001/SC-002 de forma objetiva.

## R3. Formato de serialização a documentar

- **Decision**: documentar JSON em **camelCase** (`PropertyNamingPolicy = CamelCase`) e enums como
  **números** (não há `JsonStringEnumConverter`), com tabela "valor → significado". Respostas no
  envelope `{ "data": ..., "message": "...", "code": 200, "isSuccess": true }` — conferir na
  implementação se `isSuccess` é serializado.
- Erros: `ExceptionMiddleware` (400 validação de comando, 404/500) devolvendo o mesmo envelope
  com `data: null`. Rotas de PDF devolvem `application/pdf` (e `X-Service-Order-Id` ao criar).

## R4. Formato de cada rota

- **Decision**: por grupo, uma tabela-resumo (método, caminho, login, descrição) seguida de um
  bloco por rota com parâmetros, corpo (tabela de campos + exemplo JSON), resposta e evento de
  negócio registrado. DTOs compartilhados ficam numa seção "Modelos de dados" e as rotas apontam
  para eles por link, evitando repetição.
- **Rationale**: tabela para achar rápido; detalhe para integrar.

## R5. Execução local

- **Decision**: documentar dois caminhos: (a) `dotnet run` da API (porta 5001, `Development`,
  Swagger em `/swagger`) + `npm start` do `web/` (usa `http://localhost:5001`), com SQL Server
  local via `docker-compose.dev.yml` ou `docker run`; (b) pilha completa com `docker compose up`.
  Configuração por `dotnet user-secrets`/variáveis de ambiente (chaves do `appsettings.json`).
  Conferir na implementação a porta do `docker-compose.dev.yml` (mapeia `5001:80`).
- **Rationale**: cobre desenvolvimento diário e reprodução do ambiente de produção.

## R6. Docker e observabilidade

- **Decision**: README traz os comandos essenciais, a lista de serviços e a tabela de variáveis
  (todas do `.env.example`); detalhes de domínio/HTTPS/Azure ficam no `DEPLOY.md` (link).
  Observabilidade: componentes, acesso, painéis, alertas, retenção, correlação, achar OS,
  catálogo de eventos (25), métricas próprias, privacidade — a partir de `specs/016-observability`
  e do estado implementado (`/internal/metrics`, meter `TVS`).

## R7. Segredos

- **Decision**: somente nomes de variáveis e valores fictícios (`SUA_SENHA`, `admin@exemplo.com`).
  Senha do SQL de desenvolvimento (`docker-compose.dev.yml`) pode ser citada por já estar no
  repositório e ser só local, com aviso.
