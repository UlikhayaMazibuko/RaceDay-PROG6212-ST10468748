using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services
{
    // This class does all the calls to the Part 2 API.
    // Every page uses this, so the MVC project has no database access and no business logic
    public class ApiClient
    {
        private const string ApiCookieName = ".RaceDay.Session";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _context;

        public ApiClient(HttpClient http, IHttpContextAccessor context)
        {
            _http = http;
            _context = context;
        }

        private ISession? Session => _context.HttpContext?.Session;

        // Auth
        public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request) =>
            SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", request);

        public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request) =>
            SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/register", request);

        public Task<ApiResult<NoData>> LogoutAsync() =>
            SendAsync<NoData>(HttpMethod.Post, "api/auth/logout");

        // Profile
        public Task<ApiResult<UserProfile>> GetProfileAsync() =>
            SendAsync<UserProfile>(HttpMethod.Get, "api/users/me");

        public Task<ApiResult<UserProfile>> UpdateProfileAsync(UpdateProfileRequest request) =>
            SendAsync<UserProfile>(HttpMethod.Put, "api/users/me", request);

        public Task<ApiResult<ImageUploadResponse>> UploadProfilePictureAsync(IFormFile file) =>
            UploadAsync("api/users/me/picture", file);

        // Disciplines and events
        public Task<ApiResult<List<Discipline>>> GetDisciplinesAsync() =>
            SendAsync<List<Discipline>>(HttpMethod.Get, "api/disciplines");

        public Task<ApiResult<List<EventSummary>>> GetEventsAsync(string? city, int? disciplineId, DateOnly? fromDate)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(city)) query.Add($"city={Uri.EscapeDataString(city.Trim())}");
            if (disciplineId.HasValue) query.Add($"disciplineId={disciplineId.Value}");
            if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");

            var path = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
            return SendAsync<List<EventSummary>>(HttpMethod.Get, path);
        }

        public Task<ApiResult<EventDetail>> GetEventAsync(int id) =>
            SendAsync<EventDetail>(HttpMethod.Get, $"api/events/{id}");

        public Task<ApiResult<List<EventSummary>>> GetMyEventsAsync() =>
            SendAsync<List<EventSummary>>(HttpMethod.Get, "api/events/mine");

        public Task<ApiResult<CreatedEvent>> CreateEventAsync(EventRequest request) =>
            SendAsync<CreatedEvent>(HttpMethod.Post, "api/events", request);

        public Task<ApiResult<CreatedEvent>> UpdateEventAsync(int id, EventRequest request) =>
            SendAsync<CreatedEvent>(HttpMethod.Put, $"api/events/{id}", request);

        public Task<ApiResult<NoData>> DeleteEventAsync(int id) =>
            SendAsync<NoData>(HttpMethod.Delete, $"api/events/{id}");

        public Task<ApiResult<ImageUploadResponse>> UploadEventBannerAsync(int eventId, IFormFile file) =>
            UploadAsync($"api/events/{eventId}/banner", file);

        // Categories
        public Task<ApiResult<List<Category>>> GetCategoriesAsync(int eventId) =>
            SendAsync<List<Category>>(HttpMethod.Get, $"api/events/{eventId}/categories");

        public Task<ApiResult<NoData>> CreateCategoryAsync(int eventId, CategoryRequest request) =>
            SendAsync<NoData>(HttpMethod.Post, $"api/events/{eventId}/categories", request);

        public Task<ApiResult<NoData>> DeleteCategoryAsync(int categoryId) =>
            SendAsync<NoData>(HttpMethod.Delete, $"api/categories/{categoryId}");

        // Enrolments
        public Task<ApiResult<NewEnrolment>> EnrolAsync(int categoryId) =>
            SendAsync<NewEnrolment>(HttpMethod.Post, $"api/categories/{categoryId}/enrolments");

        public Task<ApiResult<List<Enrolment>>> GetMyEnrolmentsAsync() =>
            SendAsync<List<Enrolment>>(HttpMethod.Get, "api/enrolments/mine");

        public Task<ApiResult<NoData>> WithdrawAsync(int enrolmentId) =>
            SendAsync<NoData>(HttpMethod.Delete, $"api/enrolments/{enrolmentId}");

        public Task<ApiResult<List<Enrolment>>> GetEventEnrolmentsAsync(int eventId) =>
            SendAsync<List<Enrolment>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

        public Task<ApiResult<NoData>> UpdateEnrolmentStatusAsync(int enrolmentId, string status) =>
            SendAsync<NoData>(HttpMethod.Put, $"api/enrolments/{enrolmentId}/status", new { enrolmentStatus = status });

        // Results
        public Task<ApiResult<NoData>> CaptureResultAsync(int enrolmentId, ResultRequest request) =>
            SendAsync<NoData>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/result", request);

        public Task<ApiResult<List<RaceResult>>> GetMyResultsAsync() =>
            SendAsync<List<RaceResult>>(HttpMethod.Get, "api/results/mine");

        public Task<ApiResult<List<RaceResult>>> GetEventResultsAsync(int eventId) =>
            SendAsync<List<RaceResult>>(HttpMethod.Get, $"api/events/{eventId}/results");

        // These methods do the actual sending and reading for all the calls above

        // Sends the image to the API as form data, the API is the one that uploads it to Azure
        private async Task<ApiResult<ImageUploadResponse>> UploadAsync(string path, IFormFile file)
        {
            var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            form.Add(fileContent, "File", file.FileName);

            return await SendRawAsync<ImageUploadResponse>(HttpMethod.Post, path, form);
        }

        private Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body = null)
        {
            HttpContent? content = body is null ? null : JsonContent.Create(body, body.GetType(), options: JsonOptions);
            return SendRawAsync<T>(method, path, content);
        }

        private async Task<ApiResult<T>> SendRawAsync<T>(HttpMethod method, string path, HttpContent? content)
        {
            using var request = new HttpRequestMessage(method, path) { Content = content };

            // Adds the user's API cookie so the API knows who is logged in
            var apiCookie = Session?.GetString(SessionKeys.ApiCookie);
            if (!string.IsNullOrEmpty(apiCookie))
            {
                request.Headers.Add("Cookie", $"{ApiCookieName}={apiCookie}");
            }

            try
            {
                using var response = await _http.SendAsync(request);
                RememberApiCookie(response);

                if (response.IsSuccessStatusCode)
                {
                    if (typeof(T) == typeof(NoData) || response.StatusCode == HttpStatusCode.NoContent)
                    {
                        return ApiResult<T>.Ok(default, (int)response.StatusCode);
                    }

                    var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                    return ApiResult<T>.Ok(data, (int)response.StatusCode);
                }

                return ApiResult<T>.Fail((int)response.StatusCode, await ReadErrorAsync(response));
            }
            catch (HttpRequestException)
            {
                return ApiResult<T>.Fail(503, "The RaceDay API could not be reached. Please make sure it is running and try again.");
            }
            catch (TaskCanceledException)
            {
                return ApiResult<T>.Fail(504, "The RaceDay API took too long to respond. Please try again.");
            }
            catch (JsonException)
            {
                return ApiResult<T>.Fail(502, "The RaceDay API sent back data the website could not read.");
            }
        }

        // When the API sends back its session cookie (on login and logout) we save it in our session
        // so the next calls are made as the same user
        private void RememberApiCookie(HttpResponseMessage response)
        {
            if (Session is null || !response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                return;
            }

            foreach (var cookie in cookies)
            {
                if (!cookie.StartsWith(ApiCookieName + "=", StringComparison.Ordinal))
                {
                    continue;
                }

                var value = cookie.Substring(ApiCookieName.Length + 1).Split(';')[0];
                if (string.IsNullOrEmpty(value))
                {
                    Session.Remove(SessionKeys.ApiCookie);
                }
                else
                {
                    Session.SetString(SessionKeys.ApiCookie, value);
                }
            }
        }

        // Gets the error message out of the API response so it can be shown to the user.
        // The API sends either { message } or a list of validation errors
        private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
        {
            try
            {
                var text = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    using var doc = JsonDocument.Parse(text);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    {
                        return message.GetString()!;
                    }

                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var messages = errors.EnumerateObject()
                            .SelectMany(field => field.Value.EnumerateArray())
                            .Select(item => item.GetString())
                            .Where(item => !string.IsNullOrWhiteSpace(item));
                        var joined = string.Join(" ", messages);
                        if (!string.IsNullOrWhiteSpace(joined))
                        {
                            return joined;
                        }
                    }

                    if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    {
                        return title.GetString()!;
                    }
                }
            }
            catch (JsonException)
            {
                // If it isn't JSON we just use the normal message below
            }

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Please log in to continue.",
                HttpStatusCode.Forbidden => "You do not have permission to do that.",
                HttpStatusCode.NotFound => "That item could not be found.",
                _ => "Something went wrong. Please try again."
            };
        }
    }
}
