using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using RunWay.Web.Models.Api;

namespace RunWay.Web.Services;

public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }
    public bool IsNotFound => StatusCode == (int)HttpStatusCode.NotFound;
}

public class FileData
{
    public byte[] Bytes { get; init; } = Array.Empty<byte>();
    public string ContentType { get; init; } = "application/octet-stream";
}

/// <summary>Adds the signed-in user's JWT (kept in server-side session) to every API call.</summary>
public class BearerTokenHandler : DelegatingHandler
{
    public const string SessionKey = "jwt";
    private readonly IHttpContextAccessor _accessor;

    public BearerTokenHandler(IHttpContextAccessor accessor) => _accessor = accessor;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = _accessor.HttpContext?.Session.GetString(SessionKey);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return base.SendAsync(request, ct);
    }
}

/// <summary>The only way the MVC app reads or writes data: HTTP calls to the RaceDay REST API.</summary>
public class RaceDayApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<RaceDayApiClient> _logger;

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor, ILogger<RaceDayApiClient> logger)
    {
        _http = http;
        _accessor = accessor;
        _logger = logger;
    }

    // ---------------------------------------------------------------- core

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };
            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return await FailureAsync<T>(response);

            T? data = default;
            if (response.StatusCode != HttpStatusCode.NoContent && response.Content.Headers.ContentLength != 0
                && typeof(T) != typeof(object))
            {
                data = await response.Content.ReadFromJsonAsync<T>(Json);
            }

            return new ApiResult<T> { Success = true, Data = data, StatusCode = (int)response.StatusCode };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "API call {Method} {Url} failed", method, url);
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = 503,
                Error = "The RaceDay service is not responding right now. Please try again in a moment."
            };
        }
    }

    private async Task<ApiResult<T>> FailureAsync<T>(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            _accessor.HttpContext?.Session.Clear();
        var message = ExtractMessage(await response.Content.ReadAsStringAsync());

        if (message == null)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    message = "Your session has expired. Please log in again.";
                    _accessor.HttpContext?.Session.Clear(); // forces a fresh login on the next request
                    break;
                case HttpStatusCode.Forbidden:
                    message = "You do not have permission to do that.";
                    break;
                case HttpStatusCode.NotFound:
                    message = "We could not find what you were looking for.";
                    break;
                default:
                    message = "Something went wrong. Please try again.";
                    break;
            }
        }

        return new ApiResult<T> { Success = false, Error = message, StatusCode = (int)response.StatusCode };
    }

    /// <summary>Turns an API ProblemDetails / validation response into one friendly sentence.</summary>
    private static string? ExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array ? p.Value.EnumerateArray() : Enumerable.Empty<JsonElement>())
                    .Select(v => v.GetString())
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                var joined = string.Join(" ", messages);
                if (joined.Length > 0) return joined;
            }

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                return detail.GetString();
        }
        catch (JsonException)
        {
            // Not JSON - fall through to a generic message.
        }
        return null;
    }

    private static StringContent JsonBody(object body) =>
        new(JsonSerializer.Serialize(body, Json), System.Text.Encoding.UTF8, "application/json");

    private static MultipartFormDataContent FileBody(IFormFile file)
    {
        var stream = file.OpenReadStream();
        var part = new StreamContent(stream);
        part.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        var form = new MultipartFormDataContent();
        form.Add(part, "file", file.FileName);
        return form;
    }

    private Task<ApiResult<T>> Get<T>(string url) => SendAsync<T>(HttpMethod.Get, url);
    private Task<ApiResult<T>> Post<T>(string url, object body) => SendAsync<T>(HttpMethod.Post, url, JsonBody(body));
    private Task<ApiResult<T>> Put<T>(string url, object body) => SendAsync<T>(HttpMethod.Put, url, JsonBody(body));
    private Task<ApiResult<object>> Delete(string url) => SendAsync<object>(HttpMethod.Delete, url);

    // ---------------------------------------------------------------- auth & profile

    public Task<ApiResult<object>> LogoutAsync() => SendAsync<object>(HttpMethod.Post, "api/auth/logout");
    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest r) => Post<AuthResponse>("api/auth/login", r);
    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest r) => Post<AuthResponse>("api/auth/register", r);
    public Task<ApiResult<ProfileDto>> GetProfileAsync() => Get<ProfileDto>("api/profile");
    public Task<ApiResult<ProfileDto>> UpdateProfileAsync(UpdateProfileRequest r) => Put<ProfileDto>("api/profile", r);
    public Task<ApiResult<ProfileDto>> UploadProfilePictureAsync(IFormFile file) =>
        SendAsync<ProfileDto>(HttpMethod.Post, "api/profile/picture", FileBody(file));

    // ---------------------------------------------------------------- events & categories

    public Task<ApiResult<List<EventDto>>> GetEventsAsync(string? search = null, EventType? type = null,
        DateTime? from = null, DateTime? to = null, decimal? minDistance = null, decimal? maxDistance = null)
    {
        var query = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(search)) query["search"] = search;
        if (type.HasValue) query["eventType"] = type.Value.ToString();
        if (from.HasValue) query["from"] = from.Value.ToString("yyyy-MM-dd");
        if (to.HasValue) query["to"] = to.Value.ToString("yyyy-MM-dd");
        if (minDistance.HasValue) query["minDistance"] = minDistance.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (maxDistance.HasValue) query["maxDistance"] = maxDistance.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Get<List<EventDto>>(QueryHelpers.AddQueryString("api/events", query));
    }

    public Task<ApiResult<List<EventDto>>> GetMyEventsAsync() => Get<List<EventDto>>("api/events/mine");
    public Task<ApiResult<EventDto>> GetEventAsync(int id) => Get<EventDto>($"api/events/{id}");
    public Task<ApiResult<EventDto>> CreateEventAsync(EventRequest r) => Post<EventDto>("api/events", r);
    public Task<ApiResult<EventDto>> UpdateEventAsync(int id, EventRequest r) => Put<EventDto>($"api/events/{id}", r);
    public Task<ApiResult<object>> DeleteEventAsync(int id) => Delete($"api/events/{id}");
    public Task<ApiResult<EventDto>> UploadBannerAsync(int id, IFormFile file) =>
        SendAsync<EventDto>(HttpMethod.Post, $"api/events/{id}/banner", FileBody(file));

    public Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, CategoryRequest r) => Post<CategoryDto>($"api/events/{eventId}/categories", r);
    public Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int id, CategoryRequest r) => Put<CategoryDto>($"api/categories/{id}", r);
    public Task<ApiResult<object>> DeleteCategoryAsync(int id) => Delete($"api/categories/{id}");

    // ---------------------------------------------------------------- enrolments & results

    public Task<ApiResult<EnrolmentDto>> EnrolAsync(EnrolRequest r) => Post<EnrolmentDto>("api/enrolments", r);
    public Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync() => Get<List<EnrolmentDto>>("api/enrolments/mine");
    public Task<ApiResult<object>> CancelEnrolmentAsync(int id) => SendAsync<object>(HttpMethod.Put, $"api/enrolments/{id}/cancel");
    public Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) => Get<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
    public Task<ApiResult<object>> SetEnrolmentStatusAsync(int id, EnrolmentStatus status) =>
        Put<object>($"api/enrolments/{id}/status", new UpdateEnrolmentStatusRequest { Status = status });

    public Task<ApiResult<List<ResultDto>>> GetEventResultsAsync(int eventId) => Get<List<ResultDto>>($"api/events/{eventId}/results");
    public Task<ApiResult<List<ResultDto>>> GetMyResultsAsync() => Get<List<ResultDto>>("api/results/mine");
    public Task<ApiResult<object>> SaveResultAsync(ResultRequest r) => Put<object>("api/results", r);

    // ---------------------------------------------------------------- dashboards & admin

    public Task<ApiResult<OrganiserDashboardDto>> GetOrganiserDashboardAsync() => Get<OrganiserDashboardDto>("api/dashboard/organiser");
    public Task<ApiResult<AdminDashboardDto>> GetAdminDashboardAsync() => Get<AdminDashboardDto>("api/dashboard/admin");
    public Task<ApiResult<List<UserWithRolesDto>>> GetUsersAsync() => Get<List<UserWithRolesDto>>("api/admin/users");
    public Task<ApiResult<UserWithRolesDto>> GetUserAsync(string id) => Get<UserWithRolesDto>($"api/admin/users/{id}");
    public Task<ApiResult<object>> SetUserRolesAsync(string id, List<string> roles) =>
        Put<object>($"api/admin/users/{id}/roles", new UpdateRolesRequest { Roles = roles });

    // ---------------------------------------------------------------- files

    /// <summary>Downloads an image through the API (which reads it from Azure Blob Storage).</summary>
    public async Task<ApiResult<FileData>> GetFileAsync(string blobName)
    {
        try
        {
            using var response = await _http.GetAsync($"api/files/{blobName}");
            if (!response.IsSuccessStatusCode)
                return new ApiResult<FileData> { Success = false, StatusCode = (int)response.StatusCode };

            return new ApiResult<FileData>
            {
                Success = true,
                StatusCode = 200,
                Data = new FileData
                {
                    Bytes = await response.Content.ReadAsByteArrayAsync(),
                    ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream"
                }
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Could not load image {Blob}", blobName);
            return new ApiResult<FileData> { Success = false, StatusCode = 503 };
        }
    }
}
