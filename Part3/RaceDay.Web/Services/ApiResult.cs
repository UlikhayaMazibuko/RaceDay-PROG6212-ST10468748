namespace RaceDay.Web.Services
{
    // This holds the result of an API call, either the data or the error message
    public class ApiResult<T>
    {
        public bool Success { get; init; }
        public int StatusCode { get; init; }
        public T? Data { get; init; }
        public string Error { get; init; } = string.Empty;

        public static ApiResult<T> Ok(T? data, int statusCode) =>
            new() { Success = true, StatusCode = statusCode, Data = data };

        public static ApiResult<T> Fail(int statusCode, string error) =>
            new() { Success = false, StatusCode = statusCode, Error = error };
    }

    // Used for API calls that don't return anything (204 No Content)
    public class NoData
    {
    }
}
