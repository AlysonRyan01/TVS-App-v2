# Data Model: Login lembrado por cookie seguro

Sem mudança de schema.

## Refresh token (tabela `RefreshTokens`, existente)

| Campo | Uso |
|-------|-----|
| `Token` | valor aleatório (64 bytes, base64) — agora **só** no cookie `tvs_refresh` |
| `UserId` | dono |
| `CreatedAt`, `ExpiresAt` | `ExpiresAt` = login + 30 dias = expiração do cookie |
| `RevokedAt` | preenchido no logout; token revogado → refresh 401 e cookie apagado |
| `CreatedByIp` | IP do login |

Estados: **ativo** (não revogado e não expirado) → refresh OK · **revogado** ou **expirado** → 401 + apagar cookie.

## Cookie `tvs_refresh`

| Atributo | Valor |
|----------|-------|
| Valor | refresh token |
| `HttpOnly` | sim (scripts não leem) |
| `Secure` | sim (só HTTPS; `localhost` aceito pelos navegadores) |
| `SameSite` | `Strict` |
| `Path` | `/` |
| `Expires` | `ExpiresAt` do refresh token |
| Apagar | mesmo nome/`Path`, `Expires` no passado |

## Access token (Angular, em memória)

JWT de 1 h; não é persistido. Obtido no login e em cada `/auth/refresh`; descartado ao sair, ao fechar
a aba ou quando o refresh falha.

## Mensagem entre abas (`BroadcastChannel('tvs-auth')`)

`{ "type": "logout" }` — recebida → limpar access token e ir para `/login`.
