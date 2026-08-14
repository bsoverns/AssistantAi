using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using AssistantAi.Helpers;
using Newtonsoft.Json.Linq;

namespace AssistantAi.Services
{
    public enum TrafficLight
    {
        Red,
        Yellow,
        Green
    }

    /// <summary>Outcome of one connectivity + API health check.</summary>
    public class ApiStatusResult
    {
        public ApiStatusResult(TrafficLight light, string statusText, Exception? networkError = null)
        {
            Light = light;
            StatusText = statusText;
            NetworkError = networkError;
        }

        /// <summary>Which indicator to light: green healthy, red degraded, yellow no network.</summary>
        public TrafficLight Light { get; }

        /// <summary>Text for the status tooltip.</summary>
        public string StatusText { get; }

        /// <summary>Set when the connectivity probe itself threw, for the caller to surface.</summary>
        public Exception? NetworkError { get; }
    }

    /// <summary>
    /// Polls network reachability and the public OpenAI status page. This endpoint
    /// needs no credentials, so it uses its own unauthenticated client.
    /// </summary>
    public class ApiStatusService
    {
        private const string StatusUrl = "https://status.openai.com/api/v2/status.json";
        private const string PingTarget = "8.8.8.8";
        private const int PingTimeoutMs = 3000;

        private static readonly HttpClient Http = new HttpClient();
        private readonly ErrorLog _log;

        public ApiStatusService(ErrorLog log)
        {
            _log = log;
        }

        /// <summary>Runs the full check: no network yields yellow, degraded API red, healthy green.</summary>
        public async Task<ApiStatusResult> CheckAsync()
        {
            Exception? pingError = null;

            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(PingTarget, PingTimeoutMs);

                    if (reply.Status != IPStatus.Success)
                        return new ApiStatusResult(TrafficLight.Yellow, "Network Issues");
                }
            }

            catch (Exception ex)
            {
                _log.Write(ex);
                pingError = ex;
                return new ApiStatusResult(TrafficLight.Yellow, "network issues", ex);
            }

            return await CheckApiHealthAsync(pingError);
        }

        private async Task<ApiStatusResult> CheckApiHealthAsync(Exception? pingError)
        {
            try
            {
                string json = await Http.GetStringAsync(StatusUrl);
                JObject parsed = JObject.Parse(json);

                string indicator = parsed["status"]?["indicator"]?.ToString() ?? "";
                string description = parsed["status"]?["description"]?.ToString() ?? "";
                string statusText = TextFormatting.ToProperCase(indicator) + " - " + description;

                // "none" means no active incident; minor/major/critical all count as degraded.
                bool healthy = indicator == "none";

                return new ApiStatusResult(
                    healthy ? TrafficLight.Green : TrafficLight.Red,
                    statusText,
                    pingError);
            }

            catch (HttpRequestException ex)
            {
                _log.Write(ex);
                Console.WriteLine($"Error fetching JSON: {ex.Message}");
                return new ApiStatusResult(TrafficLight.Red, "Unknown", pingError);
            }
        }
    }
}
