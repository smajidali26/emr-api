namespace EMR.Application.Common.DTOs;

/// <summary>
/// Generic result DTO for operation outcomes
/// </summary>
public class ResultDto<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }

    private ResultDto(bool isSuccess, T? data, string? errorMessage, IDictionary<string, string[]>? errors)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
        Errors = errors;
    }

    public static ResultDto<T> Success(T data) =>
        new(true, data, null, null);

    public static ResultDto<T> Failure(string errorMessage) =>
        new(false, default, errorMessage, null);

    public static ResultDto<T> Failure(IDictionary<string, string[]> errors) =>
        new(false, default, null, errors);

    public static ResultDto<T> Failure(string errorMessage, IDictionary<string, string[]> errors) =>
        new(false, default, errorMessage, errors);
}

/// <summary>
/// Non-generic result DTO for operations without return values
/// </summary>
public class ResultDto
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }

    private ResultDto(bool isSuccess, string? errorMessage, IDictionary<string, string[]>? errors)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Errors = errors;
    }

    public static ResultDto Success() =>
        new(true, null, null);

    public static ResultDto Failure(string errorMessage) =>
        new(false, errorMessage, null);

    public static ResultDto Failure(IDictionary<string, string[]> errors) =>
        new(false, null, errors);

    public static ResultDto Failure(string errorMessage, IDictionary<string, string[]> errors) =>
        new(false, errorMessage, errors);
}
