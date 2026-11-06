# Contract: autenticação com cookie de sessão

Caminhos da API (em produção com prefixo `/api`). Cookie: `tvs_refresh` (ver data-model.md).

## POST /auth/login

Corpo: `{ "email": "...", "password": "..." }`

| Resultado | HTTP | Corpo | Cookie |
|-----------|------|-------|--------|
| OK | 200 | `{ "data": { "accessToken": "<jwt>" }, "message": "Login realizado com sucesso!", "code": 200 }` | `Set-Cookie: tvs_refresh=<token>; expires=<+30d>; path=/; secure; samesite=strict; httponly` |
| Credenciais inválidas | 400 | `{ "data": null, "message": "Email ou senha inválidos", "code": 401 }` | — |

O corpo **não** contém `refreshToken`.

## POST /auth/refresh

Sem corpo. Lê `tvs_refresh` do cookie.

| Resultado | HTTP | Corpo | Cookie |
|-----------|------|-------|--------|
| Cookie válido | 200 | `{ "data": "<jwt>", "message": "Token renovado com sucesso!", "code": 200 }` | — |
| Sem cookie, inválido, expirado ou revogado | 401 | `{ "data": null, "message": "Sessão expirada. Entre novamente.", "code": 401 }` | apaga `tvs_refresh` |

## POST /auth/logout

Sem corpo, **sem login exigido**. Revoga o token do cookie (se existir e estiver ativo) e apaga o
cookie. Sempre `200` `{ "message": "Logout realizado com sucesso!" }`.

## POST /auth/register (exige login)

Corpo: `{ "email": "...", "password": "..." }`. `200` `{ "data": null, "message": "Usuário criado com sucesso!" }`;
erros como hoje. Não devolve tokens e não altera o cookie de quem está logado.

## Angular

- `login`, `refresh`, `logout` com `withCredentials: true`.
- Inicialização: `refresh()` uma vez; sucesso → logado; 401 → deslogado.
- Access token só em memória; nada de token em `sessionStorage`/`localStorage` (chaves antigas removidas ao iniciar).
- Sair: chama `/auth/logout`, limpa memória, publica `{type:"logout"}` em `BroadcastChannel('tvs-auth')`.
