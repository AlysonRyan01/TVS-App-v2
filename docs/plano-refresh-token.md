# Plano — Refresh Token

## Objetivo

Hoje o login gera só um access token (JWT) de vida longa. Quando ele expira, o usuário é
jogado de volta pro login sem aviso, no meio do uso. Implementar refresh token para permitir
renovar a sessão silenciosamente, com access tokens de vida curta (mais seguro) e uma forma
de revogar sessões (logout de verdade, hoje inexistente).

## Como a autenticação funciona hoje

### Backend

- **Identity**: `User : IdentityUser` (`Src/TVSApp.Infrastructure/Models/User.cs`),
  `ApplicationDataContext : IdentityDbContext<User>`. `AddIdentity()` em
  `BuilderExtensions.cs:94` registra `AddIdentity<User, IdentityRole>()` +
  `AddEntityFrameworkStores` + `AddDefaultTokenProviders` (os token providers padrão do
  Identity, usados hoje só para reset de senha etc. — não têm relação com JWT).
- **`JwtService`** (`Src/TVSApp.Infrastructure/Security/JwtService.cs`):
  - `Generate(User)` cria um JWT assinado com HMAC-SHA256 (`Jwt:SecretKey` do
    `appsettings.json`), claims de `NameIdentifier`, `Email` e `Role`, **expira em 14 horas**
    (`DateTime.UtcNow.AddHours(14)`, linha 42).
  - `SetTokensInsideCookie(...)` seta um cookie `accessToken` HttpOnly — **método morto**,
    não é chamado por nenhum endpoint hoje (login devolve o token só no corpo JSON).
  - Não existe geração, armazenamento ou validação de refresh token.
- **`AddAuthentication()`** (`BuilderExtensions.cs:44`): `AddJwtBearer` valida só a
  assinatura (`IssuerSigningKey`); `ValidateIssuer = false` e `ValidateAudience = false` —
  ou seja, as chaves `Jwt:Issuer`/`Jwt:Audience` do `appsettings.json` existem mas não são
  usadas em lugar nenhum.
- **`AuthEndpoints.cs`**: `POST /login` e `POST /register` verificam credenciais via
  `UserManager<User>` e devolvem `BaseResponse<string>` com o JWT no `Data`. Não existe
  `/refresh-token`, `/logout` nem qualquer endpoint de revogação.
- **CORS** (`BuilderExtensions.cs:161`): já configurado com `WithOrigins` (lista explícita,
  não wildcard) + `AllowCredentials()` — ponto relevante porque **já dá pra usar cookies
  cross-origin** se optarmos por esse caminho (ver decisão 2 abaixo).

### Frontend (Blazor WASM)

- **`CustomAuthStateProvider`** (`Src/TVSApp.Web/Services/CustomAuthStateProvider.cs`):
  guarda o JWT em `localStorage` (chave `jwtToken`, via Blazored.LocalStorage). Decodifica o
  token no client pra pegar claims e checa expiração comparando `jwtToken.ValidTo` com
  `DateTime.UtcNow` — validação client-side, sem round-trip ao servidor.
- **`AuthorizationMessageHandler`** (`Src/TVSApp.Web/Services/AuthorizationMessageHandler.cs`):
  `DelegatingHandler` que injeta `Authorization: Bearer <token>` em toda chamada à API
  (`HttpClient` "api", registrado em `Src/TVSApp.Web/Common/BuilderExtension.cs:55`, **sem**
  `UseCookies`/`BrowserRequestCredentials` configurado — hoje não manda cookies pra API). Se a
  resposta vier `401`, limpa o `localStorage` e redireciona pra `/entrar` — sem tentar
  renovar antes.
- **Login** (`LoginPage.razor` → `CustomAuthStateProvider.Login`): chama `POST /login`, salva
  o token em `localStorage`, seta o header default do `HttpClient`.
- **Logout**: não existe hoje nenhum botão/fluxo de logout na UI (`MainLayout.razor` não tem
  nada de sign-out) — o usuário só "sai" quando o token expira ou limpa o navegador.

### Resumo dos gaps

1. Sem refresh token: sessão morre de vez em 14h, sem renovação silenciosa.
2. Sem tabela/entidade pra rastrear tokens emitidos → sem como revogar uma sessão específica.
3. Sem `/logout` real (nem no back nem no front).
4. `AuthorizationMessageHandler` reage ao 401 indo direto pro login, nunca tenta renovar.
5. `SetTokensInsideCookie` é código morto e sinaliza uma abordagem (cookie) que não é a usada
   hoje (Bearer + localStorage) — vale decidir explicitamente qual caminho seguir daqui pra
   frente em vez de deixar os dois pela metade.

## Decisões de arquitetura (confirmadas com o usuário)

1. **Duração do access token**: 1–2h (definido: **1h**, ajustável).
2. **Onde guardar o refresh token**: **localStorage**, mesmo padrão já usado hoje pro access
   token — sem mexer em CORS/credentials do `HttpClient`.
3. **Rotação do refresh token**: **reutilizável até expirar** — sem rotação/encadeamento por
   uso. Simplifica a entidade (não precisa de `ReplacedByToken`) e o `RefreshAsync` (não
   revoga o token usado, só confere `ExpiresAt`/`RevokedAt`).

## Plano de implementação

Baseline: access token 1h · refresh token 30 dias · localStorage · sem rotação por uso
(revogação só acontece em `/logout` ou expiração natural).

### 1. Domain/Infra — persistência do refresh token

- Nova entidade `RefreshToken` em `Src/TVSApp.Infrastructure/Models/RefreshToken.cs` (mesmo
  local de `User`, já que é infraestrutura de Identity, não uma entidade de domínio de
  negócio): `Id`, `Token` (string opaca aleatória, não-JWT), `UserId`, `ExpiresAt`,
  `CreatedAt`, `RevokedAt` (nullable — setado só no `/logout`), `CreatedByIp` (opcional,
  auditoria). Sem `ReplacedByToken`/encadeamento, já que não há rotação por uso.
- `DbSet<RefreshToken> RefreshTokens` em `ApplicationDataContext`, mapping/configuration
  (índice único em `Token`, índice em `UserId`).
- Migration EF (`dotnet ef migrations add AddRefreshTokens ...`).

### 2. `JwtService` — separar geração de access token e refresh token

- Renomear conceitualmente `Generate` → `GenerateAccessToken(User)`, reduzindo `Expires` para
  o novo valor curto.
- Novo `GenerateRefreshToken(User, string? ip)`: gera string aleatória
  (`RandomNumberGenerator`, 256 bits, base64), persiste um `RefreshToken` novo no banco
  (texto puro — volume de usuários internos, sem risco relevante em armazenar assim),
  válido por 30 dias.
- Novo `RefreshAsync(string refreshToken)`: busca o token no banco, valida
  `ExpiresAt`/`RevokedAt`; se válido, gera e devolve **só um novo access token** — o refresh
  token continua o mesmo até expirar ou até `/logout` revogá-lo (sem rotação por uso).
- Remover `SetTokensInsideCookie` morto (ou reaproveitar sua lógica se a decisão 2 for por
  cookie).

### 3. `AuthEndpoints.cs`

- `POST /login` e `POST /register`: passam a devolver `{ AccessToken, RefreshToken }` (novo
  DTO `AuthTokensDto` em vez de `BaseResponse<string>` cru).
- Novo `POST /refresh-token`: recebe o refresh token no corpo, chama
  `JwtService.RefreshAsync`, devolve um novo access token (refresh token inalterado) ou 401
  se inválido/expirado/revogado.
- Novo `POST /logout`: recebe o refresh token do usuário autenticado, marca `RevokedAt`,
  invalidando-o. Requer autenticação (endpoint atrás do Bearer atual).

### 4. Frontend

- **DTOs**: novo `AuthTokensDto` (ou similar) espelhando o backend.
- **`CustomAuthStateProvider`**:
  - Login passa a salvar os dois tokens.
  - Novo método `RefreshAsync()` que chama `/refresh-token` e atualiza o `localStorage` +
    header default.
  - Novo método `Logout()` que chama `/logout` (revoga no backend) e limpa `localStorage`.
- **`AuthorizationMessageHandler`**: ao receber `401`, tenta `RefreshAsync()` uma vez e
  repete a requisição original com o novo access token; só se o refresh também falhar é que
  limpa tudo e redireciona pra `/entrar`. Precisa de um lock (`SemaphoreSlim`) pra evitar
  múltiplas chamadas de refresh em paralelo quando várias requisições tomam 401 ao mesmo
  tempo (todas esperam o mesmo refresh em andamento).
- **UI de logout**: adicionar um botão "Sair" em `MainLayout.razor` (hoje não existe nenhum),
  chamando `CustomAuthStateProvider.Logout()`.

## Ordem sugerida de implementação

1. Domain/Infra (`RefreshToken`, mapping, migration).
2. `JwtService` (split access/refresh token, sem rotação).
3. `AuthEndpoints` (`/login`, `/register`, `/refresh-token`, `/logout`).
4. Frontend (`CustomAuthStateProvider`, `AuthorizationMessageHandler`, botão de logout).
