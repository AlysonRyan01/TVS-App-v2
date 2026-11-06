# Tasks: Login lembrado por cookie seguro

**Input**: Design documents from `/specs/019-cookie-session/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/auth-cookie.md, quickstart.md

**Tests**: incluídos — cenários de integração da sessão (Constituição IV; contrato em contracts/auth-cookie.md).

**Organization**: US1 continuar logado (fechar aba / nova aba) · US2 sessão protegida (cookie HttpOnly/Secure/Strict, refresh fora do corpo) · US3 sair encerra a sessão em todas as abas.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)

---

## Phase 1: Setup

- [X] T001 Conferir a base: `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (218 verdes), `dotnet test src/TVSApp.IntegrationTests` (25 verdes, Docker rodando) e `cd web && npm run build`

---

## Phase 2: Foundational (transporte da sessão na API)

- [X] T002 Em `src/TVSApp.Infrastructure/Security/JwtService.cs` tornar o prazo do refresh acessível: `public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);` (hoje `private`), sem outra mudança de comportamento
- [X] T003 Criar `src/TVSApp.Api/Common/RefreshTokenCookie.cs` (classe estática): `const string Name = "tvs_refresh"`; `Set(HttpResponse, string token)` grava com `HttpOnly = true`, `Secure = true`, `SameSite = SameSiteMode.Strict`, `Path = "/"`, `Expires = DateTimeOffset.UtcNow.Add(JwtService.RefreshTokenLifetime)`, sem `Domain`; `Clear(HttpResponse)` apaga com o mesmo `Path`/`Secure`/`SameSite`; `Read(HttpRequest) → string?`
- [X] T004 [P] Criar `src/TVSApp.Application/DTOs/Auth/AccessTokenDto.cs` (`public record AccessTokenDto(string AccessToken);`) e remover o uso de `AuthTokensDto` das respostas (o arquivo `src/TVSApp.Application/DTOs/Auth/AuthTokensDto.cs` pode ser apagado se ficar sem uso)
- [X] T005 Reescrever as rotas em `src/TVSApp.Api/Endpoints/Auth/AuthCommandEndpoints.cs` conforme contracts/auth-cookie.md:
  - `POST /auth/login`: valida como hoje; gera access + refresh; `RefreshTokenCookie.Set`; responde `BaseResponse<AccessTokenDto>` "Login realizado com sucesso!" (**sem** `refreshToken` no corpo); falha mantém HTTP 400 / `code` 401 "Email ou senha inválidos" e o log de e-mail mascarado; mantém `.WithBusinessEvent(BusinessEvents.AuthLogin)`
  - `POST /auth/refresh`: **sem corpo**; lê `RefreshTokenCookie.Read`; ausente ou `RefreshAccessTokenAsync` nulo → `RefreshTokenCookie.Clear` + **HTTP 401** com `BaseResponse<string>(null, 401, "Sessão expirada. Entre novamente.")`; sucesso → 200 `BaseResponse<string>(accessToken, 200, "Token renovado com sucesso!")`
  - `POST /auth/logout`: **remover `RequireAuthorization`**; sem corpo; se houver cookie → `RevokeRefreshTokenAsync`; sempre `RefreshTokenCookie.Clear` e 200 "Logout realizado com sucesso!"
  - `POST /auth/register`: mantém `RequireAuthorization` e as validações; cria o usuário; **não** gera tokens nem mexe no cookie; 200 `BaseResponse<string>(null, 200, "Usuário criado com sucesso!")`
  - Remover o uso de `RefreshTokenRequest` (apagar `src/TVSApp.Application/Requests/AuthRequests/RefreshTokenRequest.cs` se ficar sem uso)

**Checkpoint**: `dotnet build TVSApp.sln` compila.

---

## Phase 3: User Story 1 — Continuar logado depois de fechar a aba (P1) 🎯 MVP

**Goal**: reabrir o sistema ou abrir nova aba já logado, por até 30 dias.

**Independent Test**: quickstart §2 itens 3 (fechar/reabrir aba, nova aba) e cenários de refresh por cookie.

- [X] T006 [US1] Ajustar a infraestrutura de testes: em `src/TVSApp.IntegrationTests/Infrastructure/ApiFactory.cs` definir `ClientOptions.BaseAddress = new Uri("https://localhost")` (o `CookieContainer` do cliente só envia cookies `Secure` em HTTPS) e garantir `HandleCookies = true`; em `src/TVSApp.IntegrationTests/Infrastructure/ApiClient.cs` o login continua lendo `data.accessToken` do corpo
- [X] T007 [US1] Criar `src/TVSApp.IntegrationTests/Fluxos/SessaoPorCookieTests.cs` com: "Login grava o cookie de sessão" (resposta tem `Set-Cookie` `tvs_refresh` e corpo com `accessToken`); "Refresh com o cookie devolve novo acesso, sem corpo" (`POST /auth/refresh` sem corpo → 200 e `data` JWT não vazio; o token novo autoriza `GET /service-orders/pending-estimate`); "Refresh sem cookie é recusado" (cliente novo sem login → 401, `code` 401, "Sessão expirada. Entre novamente.")
- [X] T008 [US1] Atualizar `src/TVSApp.IntegrationTests/Fluxos/AutenticacaoTests.cs`: "Login com usuário inicial devolve tokens" passa a exigir `accessToken` no corpo e **ausência** de `refreshToken`
- [X] T009 [US1] Reescrever `web/src/app/core/auth/auth.service.ts`: access token **só em memória** (`signal<string|null>(null)`); `login()` com `withCredentials: true`, guarda só `data.accessToken`; `refresh()` → `POST {apiUrl}/auth/refresh` com corpo `{}` e `withCredentials: true`, sucesso grava o token em memória, falha limpa a memória e propaga o erro; `restore(): Promise<void>` chama `refresh()` e **nunca** rejeita (401 → deslogado); no construtor remover `accessToken` e `refreshToken` antigos do `sessionStorage`; nada é escrito em `sessionStorage`/`localStorage`
- [X] T010 [US1] Em `web/src/app/core/models/AuthTokens.interface.ts` manter só `accessToken: string` (ajustar usos)
- [X] T011 [US1] Em `web/src/app/app.config.ts` adicionar `provideAppInitializer(() => inject(AuthService).restore())` para restaurar a sessão antes do primeiro roteamento (guards `auth-guard.ts`/`guest-guard.ts` continuam lendo `isLoggedIn()`, já restaurado)
- [X] T012 [US1] Em `web/src/app/core/auth/auth-interceptor.ts`: chamadas para `/auth/login`, `/auth/refresh` e `/auth/logout` saem com `withCredentials: true` e sem `Authorization`; demais chamadas da API com Bearer da memória; em 401 tenta `refresh()` uma vez (compartilhado) e repete; falhou → `router.navigate(['/login'])` (comportamento atual)

**Checkpoint**: entrar, fechar a aba, reabrir → logado; nova aba → logada.

---

## Phase 4: User Story 2 — Sessão protegida contra roubo (P1)

**Goal**: a sessão longa nunca fica legível pela página nem vai para outros sites.

**Independent Test**: quickstart §2 itens 1–2 (atributos do cookie; `document.cookie` não mostra `tvs_refresh`; storages vazios) e cenários de atributos.

- [X] T013 [US2] Em `src/TVSApp.IntegrationTests/Fluxos/SessaoPorCookieTests.cs` adicionar "Cookie de sessão é HttpOnly, Secure, SameSite=Strict, Path=/ e expira em ~30 dias" (analisar o cabeçalho `Set-Cookie` do login: contém `httponly`, `secure`, `samesite=strict`, `path=/`, `expires` entre 29 e 31 dias a partir de agora) e "Nenhuma resposta de autenticação expõe o refresh token" (corpos de login e refresh não contêm a propriedade `refreshToken` nem o valor do cookie)
- [X] T014 [US2] Em `SessaoPorCookieTests.cs` adicionar "Cadastro de usuário não devolve tokens nem troca a sessão de quem está logado" (`POST /auth/register` logado → 200, `data` nulo, sem `Set-Cookie`; refresh seguinte continua válido para o usuário original)
- [X] T015 [US2] Conferir em `web/src/app` (busca por `sessionStorage`, `localStorage`, `refreshToken`) que nenhum token é persistido pelo Angular além da remoção das chaves antigas (T009)

**Checkpoint**: cookie com os atributos certos; nenhum token legível pela página.

---

## Phase 5: User Story 3 — Sair encerra a sessão de verdade (P2)

**Goal**: sair invalida a sessão no servidor, apaga o cookie e desloga as outras abas.

**Independent Test**: quickstart §2 itens 4–5.

- [X] T016 [US3] Em `SessaoPorCookieTests.cs` adicionar: "Sair sem token de acesso revoga a sessão e apaga o cookie" (logout **sem** `Authorization` → 200 e `Set-Cookie` apagando `tvs_refresh`; refresh depois → 401); "Sessão revogada é recusada e o cookie é apagado" (revogar via logout em um cliente e reenviar o cookie antigo manualmente em outro → 401 + `Set-Cookie` de remoção); "Sair sem sessão também responde OK" (cliente sem cookie → 200)
- [X] T017 [US3] Em `web/src/app/core/auth/auth.service.ts`: `logout()` chama `POST {apiUrl}/auth/logout` com `withCredentials: true` (sem depender do token), limpa a memória e publica `{ type: 'logout' }` em `new BroadcastChannel('tvs-auth')`; ao receber essa mensagem de outra aba, limpar a memória e navegar para `/login` (usar `Router` via `inject`; proteger com `typeof BroadcastChannel !== 'undefined'`)
- [X] T018 [US3] Conferir `web/src/app/shared/layout/base-layout/base-layout.component.ts` (`logout()` → `_auth.logout()` e navegação para `/login`) com o novo `AuthService`

**Checkpoint**: sair em uma aba desloga as outras; reabrir pede login.

---

## Phase 6: Polish & validação

- [X] T019 [P] Atualizar `README.md` › API › Autenticação: fluxo com cookie `tvs_refresh` (HttpOnly, Secure, SameSite=Strict, 30 dias), `/auth/login` devolve só `accessToken`, `/auth/refresh` e `/auth/logout` sem corpo (logout sem login), `/auth/register` sem tokens; remover `RefreshTokenRequest` e `AuthTokensDto` de Modelos de dados e incluir `AccessTokenDto`; nota: pelo IP sem HTTPS o login não é lembrado
- [X] T020 [P] Atualizar `DEPLOY.md`: lembrar o login exige acesso pelo domínio com HTTPS; ao publicar, todos entram uma vez
- [X] T021 `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` e `dotnet test src/TVSApp.IntegrationTests` (todos verdes, incluindo os novos cenários), `cd web && npm run build`
- [X] T022 Validação manual local (quickstart §2): SQL do `docker-compose.dev.yml`, API (`Cors:Origins=http://localhost:4200`) e `npm start`; conferir no navegador (headless ou real) os itens 1–5: cookie com atributos, storages sem token, reabrir/nova aba logado, sair em uma aba desloga a outra

---

## Dependencies & Execution Order

- T001 → T002 → T003 ∥ T004 → T005 (Foundational) → histórias.
- **US1**: T006 → T007 → T008 (API) ∥ T009 → T010 → T011 → T012 (Angular).
- **US2**: depende de T005/T006; T013 → T014 (mesmo arquivo de testes do US1); T015 depois de T009.
- **US3**: T016 (testes) ∥ T017 → T018 (Angular, depende de T009).
- Testes de integração (T007, T013, T014, T016) ficam no mesmo arquivo → sequenciais.
- Polish por último.

### Parallel Opportunities

- T003 ∥ T004; trilha API (T007–T008, T013–T014, T016) ∥ trilha Angular (T009–T012, T015, T017–T018); T019 ∥ T020.

### Parallel Example

```text
API:      T007 SessaoPorCookieTests (US1)  →  T013/T014 (US2)  →  T016 (US3)
Angular:  T009 AuthService  →  T011 initializer  →  T012 interceptor  →  T017 logout + BroadcastChannel
```

## Implementation Strategy

1. **MVP**: Foundational + US1 → reabrir/nova aba logado com cookie.
2. **US2**: provas de proteção (atributos, nada no corpo, register sem tokens).
3. **US3**: sair em todas as abas.
4. Docs e validação final.
