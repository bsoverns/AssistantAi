using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AssistantAi.Services
{
    /// <summary>
    /// Shared transport for every OpenAI call.
    ///
    /// One static <see cref="HttpClient"/> is reused for the life of the process —
    /// creating one per request (as the code used to) leaks sockets in TIME_WAIT.
    /// The key is attached per request rather than on DefaultRequestHeaders so that
    /// updating it through the API Key dialog takes effect immediately.
    /// </summary>
    public class OpenAiClient
    {
        private static readonly HttpClient Http = new HttpClient();

        public OpenAiClient(string apiKey = "")
        {
            ApiKey = apiKey;
        }

        /// <summary>Current OpenAI key. Assign to swap keys at runtime.</summary>
        public string ApiKey { get; set; }

        public const string ChatCompletionsUrl = "https://api.openai.com/v1/chat/completions";
        public const string ResponsesUrl = "https://api.openai.com/v1/responses";
        public const string ImageGenerationUrl = "https://api.openai.com/v1/images/generations";
        public const string SpeechUrl = "https://api.openai.com/v1/audio/speech";
        public const string AudioUrl = "https://api.openai.com/v1/audio/";
        public const string ModelsUrl = "https://api.openai.com/v1/models";

        /// <summary>Serializes <paramref name="payload"/> as JSON and POSTs it.</summary>
        public Task<HttpResponseMessage> PostJsonAsync(string url, object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            return SendAsync(HttpMethod.Post, url, new StringContent(json, Encoding.UTF8, "application/json"));
        }

        /// <summary>POSTs pre-built content, used for multipart audio uploads.</summary>
        public Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
        {
            return SendAsync(HttpMethod.Post, url, content);
        }

        public Task<HttpResponseMessage> GetAsync(string url)
        {
            return SendAsync(HttpMethod.Get, url, null);
        }

        /// <summary>Serializes a payload to JSON without sending it, for error logs.</summary>
        public static string Describe(object payload)
        {
            return JsonConvert.SerializeObject(payload);
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content)
        {
            using (var request = new HttpRequestMessage(method, url))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (content != null)
                    request.Content = content;

                return await Http.SendAsync(request);
            }
        }
    }
}
