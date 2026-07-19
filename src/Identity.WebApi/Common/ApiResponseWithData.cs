namespace Identity.WebApi.Common;

/// <summary>
/// Standard API response envelope carrying a typed payload.
/// </summary>
public class ApiResponseWithData<T> : ApiResponse
{
    public T? Data { get; set; }
}
