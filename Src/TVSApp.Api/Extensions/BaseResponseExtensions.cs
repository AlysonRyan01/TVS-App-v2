using TVS_App.Domain.Shared;

namespace TVS_App.Api.Extensions;

public static class BaseResponseExtensions
{
    public static IResult ToResult<T>(this BaseResponse<T> response)
        => response.Code switch
        {
            200 => Results.Ok(response),
            201 => Results.Created(string.Empty, response),
            202 => Results.Accepted(string.Empty, response),
            204 => Results.NoContent(),
            304 => Results.StatusCode(StatusCodes.Status304NotModified),
            400 => Results.BadRequest(response),
            401 => Results.Json(response, statusCode: StatusCodes.Status401Unauthorized),
            403 => Results.Json(response, statusCode: StatusCodes.Status403Forbidden),
            404 => Results.NotFound(response),
            405 => Results.Json(response, statusCode: StatusCodes.Status405MethodNotAllowed),
            408 => Results.Json(response, statusCode: StatusCodes.Status408RequestTimeout),
            409 => Results.Conflict(response),
            410 => Results.Json(response, statusCode: StatusCodes.Status410Gone),
            413 => Results.Json(response, statusCode: StatusCodes.Status413PayloadTooLarge),
            415 => Results.Json(response, statusCode: StatusCodes.Status415UnsupportedMediaType),
            422 => Results.UnprocessableEntity(response),
            429 => Results.Json(response, statusCode: StatusCodes.Status429TooManyRequests),
            500 => Results.Json(response, statusCode: StatusCodes.Status500InternalServerError),
            501 => Results.Json(response, statusCode: StatusCodes.Status501NotImplemented),
            502 => Results.Json(response, statusCode: StatusCodes.Status502BadGateway),
            503 => Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable),
            504 => Results.Json(response, statusCode: StatusCodes.Status504GatewayTimeout),
            _ => Results.Json(response, statusCode: response.Code)
        };
}