using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TVS_App.Application.Commands;
using TVS_App.Application.DTOs;
using TVS_App.Application.Exceptions;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Exceptions;
using TVS_App.Domain.ValueObjects;

namespace TVS_App.Api.Exceptions;

public static class EndpointExceptions
{
    public static BaseResponse<T> Handle<T>(Exception ex)
    {
        return ex switch
        {
            DbUpdateException dbEx => new BaseResponse<T>(default, 400, $"Erro ao salvar no banco: {dbEx.InnerException?.Message ?? dbEx.Message}"),
            InvalidOperationException invEx => new BaseResponse<T>(default, 400, $"Operação inválida: {invEx.Message}"),
            ArgumentNullException argEx => new BaseResponse<T>(default, 400, $"Argumento inválido: {argEx.Message}"),
            SqlException sqlEx => new BaseResponse<T>(default, 500, $"Erro no SQL: {sqlEx.Message}"),
            TimeoutException timeoutEx => new BaseResponse<T>(default, 408, $"Tempo de resposta do banco expirou: {timeoutEx.Message}"),
            DBConcurrencyException dbConcurrencyEx => new BaseResponse<T>(default, 409, $"Erro de concorrência: {dbConcurrencyEx.Message}"),
            DbException dbEx => new BaseResponse<T>(default, 500, $"Erro de banco de dados: {dbEx.Message}"),
            CommandException<ICommand> cmdEx => new BaseResponse<T>(default, 400, $"Erro de comando: {cmdEx.Message}"),
            EntityException<Entity> entityEx => new BaseResponse<T>(default, 422, $"Erro na entidade: {entityEx.Message}"),
            ValueObjectException<ValueObject> voEx => new BaseResponse<T>(default, 422, $"Erro no Value Object: {voEx.Message}"),
            _ => new BaseResponse<T>(default, 500, $"Erro inesperado: {ex.Message}")
        };
    }
}