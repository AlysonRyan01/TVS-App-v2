# Quickstart: validar o login por cookie

## 1. Automático
```bash
dotnet test src/TVSApp.IntegrationTests --logger "console;verbosity=normal"
```
Cenários de `SessaoPorCookieTests` (contrato em [contracts/auth-cookie.md](contracts/auth-cookie.md))
e todos os anteriores aprovados.

## 2. Manual (pelo domínio com HTTPS ou localmente)
Local: SQL do `docker-compose.dev.yml`, `dotnet run --project src/TVSApp.Api`, `cd web && npm start`
(com `Cors:Origins=http://localhost:4200`).

1. Entrar → DevTools › Application › Cookies: `tvs_refresh` com HttpOnly ✓, Secure ✓, SameSite Strict,
   expira em ~30 dias. Session/Local Storage **sem** tokens.
2. `document.cookie` no console → **não** mostra `tvs_refresh`.
3. Fechar a aba, reabrir o sistema → tela inicial sem login. Abrir nova aba → logada.
4. Duas abas abertas; sair em uma → a outra vai para `/login` (na hora ou na próxima ação).
   Reabrir → pede login.
5. Com o sistema aberto, apagar o cookie no DevTools e esperar o access token vencer (ou recarregar)
   → tela de login, sem erro técnico.
6. Pelo IP sem HTTPS (`http://IP:8080`): login funciona; ao reabrir, pede login (esperado).

## 3. Gates
`dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests`, `cd web && npm run build`.
