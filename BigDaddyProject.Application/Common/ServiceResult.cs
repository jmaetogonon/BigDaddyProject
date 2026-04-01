namespace BigDaddyProject.Application.Common;

public class ServiceResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> Errors { get; init; } = new();

    public static ServiceResult Ok() => new() { Success = true };
    public static ServiceResult Fail(string message) => new() { Success = false, ErrorMessage = message };
    public static ServiceResult Fail(List<string> errors) => new() { Success = false, Errors = errors };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
    public new static ServiceResult<T> Fail(string message) => new() { Success = false, ErrorMessage = message };
    public new static ServiceResult<T> Fail(List<string> errors) => new() { Success = false, Errors = errors };
}
