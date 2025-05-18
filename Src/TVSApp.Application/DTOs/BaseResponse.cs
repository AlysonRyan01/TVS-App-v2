namespace TVS_App.Application.DTOs;

public class BaseResponse <T>
{
    public BaseResponse(T? data, int code = 200, string? message = null)
    {
        Data = data;
        Message = message;
        Code = code;
    }
    
    public T? Data { get; set; }
    public string? Message { get; set; }
    
    public int Code { get; set; } = 200;
    public bool IsSuccess => Code >= 200 && Code <= 299;
}