# Research: Login lembrado por cookie seguro

Estado atual: login devolve `{ accessToken, refreshToken }` no corpo; Angular guarda os dois em
`sessionStorage` (some ao fechar a aba; legível por script). `/auth/refresh` recebe o refresh token
no corpo; `/auth/logout` exige Bearer **e** o refresh no corpo (falha se o access token já expirou).
`/auth/register` (exige login) devolve tokens do usuário novo — não é usado pelo Angular. Refresh
token: 64 bytes aleatórios, tabela `RefreshTokens`, 30 dias, revogável. Access token: JWT 1 h.
CORS já tem `AllowCredentials()`. Produção: navegador → nginx (HTTPS) → Caddy → API, mesmo domínio,
API sob `/api`. Desenvolvimento: Angular `localhost:4200` → API `localhost:5001`.

## R1. O que vai no cookie

- **Decision**: **só o refresh token** vai em cookie `HttpOnly`. O access token continua no corpo e
  fica **em memória** no Angular (some ao fechar a aba; recuperado via refresh ao abrir).
- **Rationale**: o refresh token é o segredo de longa duração (30 dias) — é ele que precisa ficar
  fora do alcance de scripts (FR-003/004). Manter o Bearer nas chamadas da API evita transformar
  todas as rotas em autenticação por cookie (o que exigiria proteção CSRF em cada rota de escrita) e
  não muda nada nos handlers nem no SignalR.
- **Alternatives**: access token também em cookie (exige CSRF token em todas as escritas, muda o
  esquema de autenticação); `localStorage` (descartado na spec: legível por script).

## R2. Atributos do cookie

- **Decision**: nome `tvs_refresh`; `HttpOnly`; `Secure` **sempre**; `SameSite=Strict`; `Path=/`;
  `Expires` = `ExpiresAt` do refresh token (30 dias); sem `Domain` (fica do host exato).
- **Rationale**:
  - `Secure` sempre: em produção é HTTPS; navegadores tratam `http://localhost` como seguro, então o
    desenvolvimento local funciona; pelo IP sem HTTPS o navegador descarta o cookie → login não é
    lembrado, mas o sistema funciona (FR-009, comportamento aceito na spec). Não dependemos de
    `X-Forwarded-Proto` (a cadeia nginx→Caddy→API não o repassa de forma confiável).
  - `SameSite=Strict`: o cookie só é enviado em requisições originadas do próprio site → outros sites
    não conseguem usá-lo (FR-008) e `/auth/refresh` não sofre CSRF útil. `localhost:4200` e
    `localhost:5001` são o *mesmo site* (porta não conta), então funciona no desenvolvimento.
  - `Path=/`: o caminho visto pelo navegador difere entre produção (`/api/auth/...`) e local
    (`/auth/...`); `/` evita configuração por ambiente. Como a API só lê o cookie nas rotas de auth, o
    envio nas demais requisições é inofensivo.
- **Alternatives**: `SameSite=Lax` (desnecessário: nenhum fluxo depende de navegação vinda de fora);
  `Path=/api/auth` (quebraria o desenvolvimento local).

## R3. Rotas de autenticação

- **Decision**:
  - `POST /auth/login` → grava o cookie; corpo `{ accessToken }` (sem `refreshToken`).
  - `POST /auth/refresh` → lê o refresh **do cookie** (sem corpo); devolve novo access token; se
    ausente/inválido/expirado/revogado → 401 e **apaga o cookie**.
  - `POST /auth/logout` → **sem `RequireAuthorization`**; revoga o refresh do cookie (se houver) e
    apaga o cookie; sempre 200 (idempotente).
  - `POST /auth/register` → continua exigindo login, cria o usuário e **não** devolve tokens nem
    mexe no cookie de quem está logado.
- **Rationale**: FR-004 (refresh nunca no corpo), FR-006 (sair funciona mesmo com access token
  vencido), FR-007 (sessão recusada limpa o cookie).
- **Compatibilidade**: o sistema antigo tem sua própria API e não é afetado. Tokens antigos guardados
  em `sessionStorage` não são migrados (spec, edge case) — o Angular os apaga ao iniciar.
- **Rotação do refresh token**: fora do escopo (não exigida pela spec); pode vir depois sem mudar o
  contrato.

## R4. Angular: abrir o sistema já logado

- **Decision**: `provideAppInitializer(() => auth.restore())` — no carregamento chama
  `/auth/refresh` com `withCredentials`; sucesso → access token em memória; 401 → estado deslogado.
  Os guards consultam o estado já restaurado (o roteamento inicial espera o initializer). Remove
  `accessToken`/`refreshToken` antigos do `sessionStorage` na inicialização.
- `withCredentials: true` só nas chamadas `/auth/*` (login, refresh, logout) — necessário no
  desenvolvimento (origens diferentes); inofensivo em produção (mesma origem).
- Interceptor: em 401 nas rotas da API, chama `refresh()` (cookie) uma vez e repete; falhou → limpa e
  vai para `/login` (comportamento atual mantido).
- **Rationale**: FR-001/002 sem guardar nada legível; uma chamada extra no carregamento.

## R5. Sair em todas as abas

- **Decision**: `BroadcastChannel('tvs-auth')`. Ao sair, a aba publica `logout`; as outras limpam o
  access token em memória e navegam para `/login`. Abas novas abertas depois disso não têm cookie →
  vão para login.
- **Rationale**: FR-006 ("as demais abas deixam de estar logadas"); sem o aviso, uma aba aberta
  continuaria usando o access token em memória por até 1 h.
- **Alternatives**: evento `storage` (exigiria escrever um marcador no `localStorage`); polling.

## R6. Testes

- **Decision**: cenários novos em `TVSApp.IntegrationTests` (`SessaoPorCookieTests`):
  login define `tvs_refresh` com `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/`, expiração ~30 dias,
  e o corpo **não** contém `refreshToken`; refresh só com cookie → 200; sem cookie → 401; logout sem
  Bearer → 200, cookie apagado e refresh posterior → 401; cookie de token revogado → 401 e cookie
  apagado; register não altera o cookie de quem está logado. O cliente de teste usa
  `BaseAddress = https://localhost` para o `CookieContainer` enviar cookies `Secure`.
