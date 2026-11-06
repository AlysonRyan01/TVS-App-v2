# Implementation Plan: Login lembrado por cookie seguro

**Branch**: `019-cookie-session` | **Date**: 2026-11-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/019-cookie-session/spec.md`

## Summary

O **refresh token** (30 dias) passa a viver só num cookie `HttpOnly; Secure; SameSite=Strict`
emitido pela API no login; deixa de ir no corpo das respostas e de ser guardado pelo Angular. O
**access token** (1 h) continua no corpo do login/refresh, mas fica **só em memória** no Angular.
Ao abrir o sistema (ou uma aba nova), o Angular chama `/auth/refresh` uma vez — o navegador manda o
cookie sozinho — e recebe um access token novo. `/auth/logout` passa a funcionar só com o cookie
(sem Bearer), revoga o token e apaga o cookie; um `BroadcastChannel` avisa as outras abas.
Decisões em [research.md](research.md); contrato em [contracts/auth-cookie.md](contracts/auth-cookie.md).

## Technical Context

**Language/Version**: C# / .NET 9 (API) · TypeScript / Angular 22 (web)

**Primary Dependencies**: nenhuma nova — cookies do ASP.NET Core (`HttpResponse.Cookies`), `HttpClient` do Angular com `withCredentials`, `BroadcastChannel` (API do navegador)

**Storage**: tabela `RefreshTokens` existente (sem mudança de schema); cookie no navegador

**Testing**: integração em `src/TVSApp.IntegrationTests` (cookie no login, refresh/logout só com cookie, flags do cookie, corpo sem refresh token); unitários existentes intactos; build do Angular

**Target Platform**: navegadores modernos; produção atrás de nginx (HTTPS) → Caddy → API, mesmo domínio

**Project Type**: web application (API + Angular)

**Performance Goals**: 1 chamada extra (`/auth/refresh`) ao abrir o sistema; imperceptível

**Constraints**: refresh token nunca legível por script nem enviado a outros sites (FR-003/004/008); sem HTTPS (acesso pelo IP) o login não é lembrado (FR-009); sistema antigo, no mesmo banco, intacto

**Scale/Scope**: 4 rotas de auth ajustadas; `AuthService`, interceptor, guards e inicialização do Angular; testes de integração; docs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio / Gate | Status | Como é atendido |
|------------------|--------|-----------------|
| I. Clean Architecture | ✅ | Cookie é detalhe de transporte → só na Api (endpoints de auth + helper de cookie). `JwtService` (Infrastructure) inalterado. |
| II. Domain isolado | ✅ | Domain não muda. |
| III. EF Core + migrations | ✅ | Sem mudança de schema (`RefreshTokens` reaproveitada). |
| IV. Regras testadas | ✅ | Comportamento de sessão coberto por cenários de integração (login, refresh, logout, revogação, flags). |
| V. Português | ✅ | Mensagens ("Sessão expirada. Entre novamente.") em pt-BR. |
| Gates de build | ✅ | `dotnet build`/`dotnet test` + `npm run build` do web. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/019-cookie-session/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/auth-cookie.md
├── checklists/requirements.md
└── tasks.md            # /speckit-tasks
```

### Source Code (repository root)

```text
src/TVSApp.Api/
├── Endpoints/Auth/AuthCommandEndpoints.cs   # login/refresh/logout/register com cookie
└── Common/RefreshTokenCookie.cs             # NOVO: nome, opções, Set/Clear/Read

src/TVSApp.Application/DTOs/Auth/            # + AccessTokenDto (login sem refresh no corpo)

src/TVSApp.IntegrationTests/
├── Infrastructure/ApiFactory.cs             # cliente com BaseAddress https://localhost (cookie Secure)
└── Fluxos/SessaoPorCookieTests.cs           # NOVO

web/src/app/
├── core/auth/auth.service.ts                # token só em memória; restore(); logout por cookie; BroadcastChannel
├── core/auth/auth-interceptor.ts            # withCredentials nas chamadas /auth
├── core/auth/{auth-guard,guest-guard}.ts    # aguardam a restauração inicial
├── core/models/AuthTokens.interface.ts      # só accessToken
└── app.config.ts                            # provideAppInitializer(() => auth.restore())

README.md (Autenticação), DEPLOY.md (HTTPS obrigatório para lembrar o login)
```

**Structure Decision**: mudança concentrada na Api (transporte) e no `core/auth` do Angular; nenhum
handler ou entidade muda.

### Ordem de implementação sugerida

1. `RefreshTokenCookie` + endpoints de auth; testes de integração da sessão.
2. Angular: `AuthService` (memória + restore + logout por cookie + BroadcastChannel), interceptor, guards, initializer.
3. Ajustar o `ApiClient` dos testes (HTTPS) e rodar a verificação completa.
4. Docs e validação manual (quickstart).

## Complexity Tracking

Nenhuma violação.
