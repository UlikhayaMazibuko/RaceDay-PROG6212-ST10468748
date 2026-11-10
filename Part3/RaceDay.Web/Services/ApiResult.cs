namespace RaceDay.Web.Services
{
    // The outcome of one API call: either the data, or the status code and a readable message.
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

    // Used for calls where the API returns no body (204 No Content).
    public class NoData
    {
    }
}
