# Implementation Plan: README completo do projeto (pt-BR)

**Branch**: `017-readme-docs` | **Date**: 2026-11-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/017-readme-docs/spec.md`

## Summary

Reescrever o `README.md` da raiz (hoje só o título) como a porta de entrada do projeto, em pt-BR:
visão do produto, arquitetura, execução local, testes, **referência completa da API** (41 rotas,
comandos de entrada, DTOs de saída, enums com valores numéricos, envelope de resposta, erros),
tempo real, WhatsApp, Docker/produção (resumo + link para `DEPLOY.md`), observabilidade e
contribuição. Tudo derivado do código atual e conferido contra ele (rotas e campos), sem mudar
o sistema. Decisões em [research.md](research.md); estrutura obrigatória em
[contracts/readme-structure.md](contracts/readme-structure.md).

## Technical Context

**Language/Version**: Markdown (GitHub Flavored Markdown) com diagramas em bloco de texto/Mermaid

**Primary Dependencies**: nenhuma; fonte da verdade = código em `src/` e `web/`, `.env.example`, `docker-compose*.yml`, `DEPLOY.md`, `specs/016-observability`

**Storage**: N/A

**Testing**: conferência automática rotas-código × README (grep, roteiro no quickstart); execução dos comandos documentados; `dotnet build`/`dotnet test` inalterados

**Target Platform**: renderização no GitHub e em editores (VS Code)

**Project Type**: documentação de web application (API .NET + Angular + infraestrutura Docker)

**Performance Goals**: achar qualquer rota pelo índice em ≤ 1 min (SC-004)

**Constraints**: pt-BR; sem segredos; sem duplicar o `DEPLOY.md` inteiro; JSON em camelCase e enums como **números** (sem `JsonStringEnumConverter`), como a API realmente serializa

**Scale/Scope**: 1 arquivo (`README.md`); 41 rotas HTTP + hub `/osHub` + `/internal/metrics`; 13 objetos de entrada; ~10 DTOs; 6 enums; 25 eventos de negócio; ~15 variáveis de ambiente

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio / Gate | Status | Como é atendido |
|------------------|--------|-----------------|
| I. Clean Architecture | ✅ | Sem mudança de código; o README documenta as camadas e a regra de dependência. |
| II. Domain isolado | ✅ | Sem mudança; documentado. |
| III. EF Core + migrations | ✅ | Sem mudança de schema; o README ensina a criar migration (`dotnet ef migrations add`). |
| IV. Regras de negócio testadas | ✅ | Nenhuma regra nova; `dotnet test` segue verde (gate do fluxo). |
| V. Interface em português | ✅ | Documento inteiro em pt-BR. |
| Gates de build | ✅ | `dotnet build`/`dotnet test` sem alteração; frontend não muda. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/017-readme-docs/
├── plan.md
├── research.md
├── data-model.md                 # modelo do conteúdo (entidades documentadas e templates)
├── quickstart.md                 # roteiro de validação do README
├── contracts/readme-structure.md # seções obrigatórias e formato de cada rota/DTO
├── checklists/requirements.md
└── tasks.md                      # /speckit-tasks
```

### Source Code (repository root)

```text
README.md        # REESCRITO (único arquivo alterado)
```

Fontes consultadas (somente leitura): `src/TVSApp.Api/Endpoints/**`, `src/TVSApp.Api/Common/*`,
`src/TVSApp.Application/{Requests,DTOs}/**`, `src/TVSApp.Domain/{Enums,Entities}/**`,
`src/TVSApp.Application/Observability/*`, `web/src/environments/*`, `web/package.json`,
`docker-compose.yml`, `docker-compose.dev.yml`, `.env.example`, `DEPLOY.md`,
`observability/**`, `.specify/memory/constitution.md`.

**Structure Decision**: um único `README.md` com índice (research R1). Nada de código muda.

### Ordem de implementação sugerida

1. Inventário: listar rotas (`Map*`), objetos de entrada, DTOs e enums direto do código.
2. Esqueleto com todas as seções do contrato e o índice.
3. Seções gerais (produto, arquitetura, pastas, execução local, testes).
4. Referência da API por grupo, com DTOs e enums.
5. Tempo real, WhatsApp, Docker, observabilidade, contribuição.
6. Validação (quickstart): rotas código × README, comandos, links, ausência de segredos.

## Complexity Tracking

Nenhuma violação.
