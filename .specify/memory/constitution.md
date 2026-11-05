# TVS-App Constitution

## Core Principles

### I. Clean Architecture com Dependências Unidirecionais

O backend é organizado nas camadas `TVSApp.Domain`, `TVSApp.Application`,
`TVSApp.Infrastructure` e `TVSApp.Api`, e as dependências MUST apontar sempre para dentro:

- `Application` MAY depender apenas de `Domain`.
- `Infrastructure` MAY depender de `Application` e `Domain`; implementa as abstrações
  (repositórios, serviços externos, persistência) definidas nas camadas internas.
- `Api` é a camada de composição (DI, configuração, endpoints) e MUST NOT conter regra de negócio.
- Camadas internas MUST NOT referenciar camadas externas, nem diretamente nem por pacote.
- Novos projetos de backend MUST se encaixar em uma dessas camadas ou ter justificativa
  registrada no plano da feature.

**Rationale**: dependências unidirecionais mantêm as regras de negócio testáveis e
independentes de framework, banco e transporte.

### II. Domain Isolado (NÃO NEGOCIÁVEL)

`TVSApp.Domain` MUST NOT depender de nenhum outro projeto da solução nem de pacotes de
infraestrutura (EF Core, ASP.NET Core, clientes HTTP, SDKs externos etc.). Entidades,
value objects, invariantes e regras de negócio centrais MUST residir no Domain. Atributos ou
configurações de persistência MUST ficar na Infrastructure (ex.: `IEntityTypeConfiguration`),
nunca nas entidades.

**Rationale**: o Domain é o núcleo estável do sistema; qualquer dependência externa o acopla a
detalhes que mudam com mais frequência que as regras de negócio.

### III. Persistência via EF Core e Migrations

A persistência MUST usar EF Core com SQL Server, implementada em `TVSApp.Infrastructure`.
Toda mudança de schema MUST ser feita por uma migration do EF Core versionada no repositório:

- Alterações manuais no banco (DDL fora de migration) são proibidas em qualquer ambiente.
- Migrations já aplicadas/commitadas em `main` MUST NOT ser editadas; correções exigem uma nova
  migration.
- Cada migration MUST ter nome descritivo (ex.: `AddRefreshTokensAndPartPurchaseCost`) e ser
  revisada junto com a mudança de modelo que a originou.

**Rationale**: migrations garantem schema reproduzível, auditável e consistente entre
desenvolvimento, Docker e produção.

### IV. Regras de Negócio Testadas (NÃO NEGOCIÁVEL)

Toda regra de negócio nova ou alterada MUST vir acompanhada de testes automatizados em
`TVSApp.Tests` no mesmo PR. Isso inclui invariantes de entidades, validações, cálculos e
casos de uso da Application. Correções de bug em regra de negócio MUST incluir um teste que
reproduza o defeito. Uma tarefa que introduz regra de negócio não é considerada concluída
enquanto `dotnet test` não passar.

**Rationale**: regras de negócio são o ativo mais crítico do sistema; testes são a única
garantia objetiva de que continuam corretas após mudanças.

### V. Interface em Português

Todo texto exibido ao usuário final MUST estar em português (pt-BR): telas, rótulos, botões,
mensagens de validação, notificações, e-mails/WhatsApp e mensagens de erro retornadas pela API
para exibição na UI. Identificadores de código (classes, métodos, variáveis) MAY permanecer em
inglês, seguindo o padrão já existente no código.

**Rationale**: o produto atende usuários brasileiros; consistência de idioma é requisito de
usabilidade.

## Stack e Restrições Técnicas

- **Backend**: .NET 9 (`net9.0`), ASP.NET Core em `src/TVSApp.Api`, solução `TVSApp.sln`.
- **Persistência**: EF Core + SQL Server (ver Princípio III).
- **Frontend**: Angular em `web/`. O frontend MUST consumir o backend exclusivamente via API
  HTTP e MUST NOT conter regra de negócio que não esteja também validada no backend.
- **Testes**: projeto `src/TVSApp.Tests` (ver Princípio IV).
- **Ambiente local**: `docker-compose.yml` na raiz.
- Introdução de nova tecnologia, banco ou framework MUST ser justificada no `plan.md` da
  feature e, se alterar esta seção, exige emenda desta constituição.

## Fluxo de Desenvolvimento e Quality Gates

Antes de integrar uma mudança em `main`, ela MUST satisfazer:

1. `dotnet build` e `dotnet test` sem falhas.
2. Build do frontend (`web/`) sem erros quando houver alterações nele.
3. Nenhuma referência de projeto violando os Princípios I e II.
4. Mudanças de modelo persistido acompanhadas da respectiva migration (Princípio III).
5. Regras de negócio novas/alteradas cobertas por testes (Princípio IV).
6. Textos de UI novos em português (Princípio V).

O `Constitution Check` dos planos do Spec Kit (`/speckit-plan`) MUST verificar estes itens;
qualquer violação MUST ser registrada na tabela de Complexity Tracking com justificativa.

## Governance

Esta constituição prevalece sobre quaisquer outras práticas ou convenções do projeto.

- **Emendas**: feitas via `/speckit-constitution`, em PR próprio, descrevendo a mudança e o
  impacto em código e artefatos existentes (plano de migração quando aplicável).
- **Versionamento** (SemVer):
  - MAJOR: remoção ou redefinição incompatível de princípio ou regra de governança.
  - MINOR: novo princípio/seção ou expansão material de orientação.
  - PATCH: esclarecimentos, redação e correções sem mudança semântica.
- **Conformidade**: todo PR e revisão de código MUST verificar aderência aos princípios.
  Complexidade adicional MUST ser justificada. Revisão desta constituição recomendada a cada
  grande marco do produto.

**Version**: 1.0.0 | **Ratified**: 2026-11-05 | **Last Amended**: 2026-11-05
