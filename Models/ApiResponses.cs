using System.Text.Json.Serialization;

namespace LacunaSpace.Models
{
    public class BaseResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public class StartRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }

    public class StartResponse : BaseResponse
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }
    }

    public class ProbesResponse : BaseResponse
    {
        [JsonPropertyName("probes")]
        public List<Probe>? Probes { get; set; }
    }

    public class SyncResponse : BaseResponse
    {
        [JsonPropertyName("t1")]
        public string T1 { get; set; } = string.Empty;

        [JsonPropertyName("t2")]
        public string T2 { get; set; } = string.Empty;
    }

    public class Job
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("probeName")]
        public string ProbeName { get; set; } = string.Empty;
    }

    public class JobResponse : BaseResponse
    {
        [JsonPropertyName("job")]
        public Job? Job { get; set; }
    }

    public class JobCheckRequest
    {
        [JsonPropertyName("probeNow")]
        public string ProbeNow { get; set; } = string.Empty;

        [JsonPropertyName("roundTrip")]
        public long RoundTrip { get; set; }
    }

    public class Probe
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("encoding")]
        public string Encoding { get; set; } = string.Empty;

        [JsonPropertyName("timeDilationFactor")]
        public double? TimeDilationFactor { get; set; }
    }
}