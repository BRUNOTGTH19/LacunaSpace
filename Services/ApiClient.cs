using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LacunaSpace.Models;

namespace LacunaSpace.Services
{
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private const string BaseUrl = "https://luma.lacuna.cc/";
        private static readonly MediaTypeHeaderValue JsonMediaType = MediaTypeHeaderValue.Parse("application/json");

        public ApiClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public void SetAuthorization(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        public async Task<StartResponse> StartAsync(string username, string email, int level = 1)
        {
            var request = new StartRequest
            {
                Username = username,
                Email = email
            };
            
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, JsonMediaType);

            var endpoint = level == 2 ? "/api/start/2" : "/api/start";
            var response = await _httpClient.PostAsync(endpoint, content);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StartResponse>(responseContent, _jsonOptions);
            return result ?? throw new InvalidOperationException("Failed to deserialize start response");
        }

        public async Task<ProbesResponse> GetProbesAsync()
        {
            var response = await _httpClient.GetAsync("/api/probe");
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ProbesResponse>(responseContent, _jsonOptions);
            return result ?? throw new InvalidOperationException("Failed to deserialize probes response");
        }

        public async Task<SyncResponse> SyncProbeAsync(string probeId)
        {
            var response = await _httpClient.PostAsync($"/api/probe/{probeId}/sync", null);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<SyncResponse>(responseContent, _jsonOptions);
            return result ?? throw new InvalidOperationException("Failed to deserialize sync response");
        }

        public async Task<JobResponse> TakeJobAsync()
        {
            var response = await _httpClient.PostAsync("/api/job/take", null);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<JobResponse>(responseContent, _jsonOptions);
            return result ?? throw new InvalidOperationException("Failed to deserialize job response");
        }

        public async Task<BaseResponse> CheckJobAsync(string jobId, JobCheckRequest request)
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, JsonMediaType);
            
            var response = await _httpClient.PostAsync($"/api/job/{jobId}/check", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<BaseResponse>(responseContent, _jsonOptions);
            return result ?? throw new InvalidOperationException("Failed to deserialize check response");
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}