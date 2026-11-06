namespace TVS_App.Application.DTOs.Auth;

/// <summary>Resposta do login: só o token de acesso (1 h). A sessão de 30 dias vai no cookie.</summary>
public record AccessTokenDto(string AccessToken);
