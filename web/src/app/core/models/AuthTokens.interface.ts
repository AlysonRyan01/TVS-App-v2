/** Resposta do login: só o token de acesso (1 h). A sessão de 30 dias fica num cookie HttpOnly. */
export interface AuthTokens {
    accessToken: string
}
