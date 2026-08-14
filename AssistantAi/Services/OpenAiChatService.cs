using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AssistantAi.Helpers;
using AssistantAi.Models;
using Newtonsoft.Json.Linq;

namespace AssistantAi.Services
{
    /// <summary>
    /// Text and vision calls against the chat/completions and responses endpoints.
    /// Errors are logged and rethrown; presenting them is the caller's job.
    /// </summary>
    public class OpenAiChatService
    {
        private readonly OpenAiClient _client;
        private readonly ErrorLog _log;

        public OpenAiChatService(OpenAiClient client, ErrorLog log)
        {
            _client = client;
            _log = log;
        }

        /// <summary>
        /// Sends <paramref name="question"/> with <paramref name="history"/> prepended
        /// and returns the assistant's reply.
        /// </summary>
        public async Task<string> SendMessageAsync(string model, IEnumerable<ChatMessage> history, string question)
        {
            var messages = new List<object>();

            foreach (var message in history)
                messages.Add(new { role = message.Role, content = message.Content });

            // Newtonsoft handles JSON escaping, so the raw question goes in as-is.
            messages.Add(new { role = "user", content = question });

            var payload = new
            {
                model,
                messages = messages.ToArray()
            };

            try
            {
                var response = await _client.PostJsonAsync(OpenAiClient.ChatCompletionsUrl, payload);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw BuildError("Chat request", response, body);

                return ReadMessageContent(body);
            }

            catch (Exception ex)
            {
                _log.Write(question, ex);
                throw;
            }
        }

        /// <summary>Sends a single base64 image plus a question to a vision-capable model.</summary>
        public async Task<string> SendImageAsync(string model, string question, string imageType, int maxTokens, string base64Image)
        {
            var payload = new
            {
                model = ResolveVisionModel(model),
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = question },
                            new { type = "image_url", image_url = new { url = $"data:image/{imageType};base64,{base64Image}" } }
                        }
                    }
                },
                max_tokens = maxTokens
            };

            try
            {
                var response = await _client.PostJsonAsync(OpenAiClient.ChatCompletionsUrl, payload);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw BuildError("Image request", response, body);

                return ReadMessageContent(body);
            }

            catch (Exception ex)
            {
                _log.Write(question, ex);
                throw;
            }
        }

        /// <summary>
        /// Sends every PNG/JPG/JPEG in <paramref name="pickupFolder"/> in one request,
        /// ordered by the first number in each filename so page 2 precedes page 10.
        /// </summary>
        public async Task<string> SendImageFolderAsync(string model, string question, string pickupFolder, int maxTokens)
        {
            var sortedFiles = SortImagesNumerically(pickupFolder);

            if (!sortedFiles.Any())
                throw new InvalidOperationException("No PNG, JPG, or JPEG files were found in the selected folder.");

            var inputContent = new List<object>
            {
                new { type = "input_text", text = question }
            };

            foreach (var file in sortedFiles)
            {
                inputContent.Add(new
                {
                    type = "input_image",
                    image_url = $"data:{ImageEncoding.GetMimeType(file)};base64,{await ImageEncoding.ToBase64Async(file)}",
                    detail = "auto"
                });
            }

            var payload = new
            {
                model = ResolveVisionModel(model),
                input = new object[]
                {
                    new { role = "user", content = inputContent }
                },
                max_output_tokens = maxTokens
            };

            try
            {
                var response = await _client.PostJsonAsync(OpenAiClient.ResponsesUrl, payload);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _log.Write($"OpenAI API error.\r\nQuestion: {question}\r\nPayload: {OpenAiClient.Describe(payload)}\r\nResponse: {body}");
                    throw new HttpRequestException($"Error sending images to OpenAI: {response.StatusCode}\r\n{body}");
                }

                return ReadResponsesOutput(body);
            }

            catch (HttpRequestException)
            {
                throw; // already logged above with the full payload
            }

            catch (Exception ex)
            {
                _log.Write(question, ex);
                throw;
            }
        }

        /// <summary>
        /// Raw JSON from /v1/models. Unused by the UI — the API doesn't report
        /// modality, so the drop-down lists can't be built from it directly.
        /// </summary>
        public async Task<string> GetModelListAsync()
        {
            try
            {
                var response = await _client.GetAsync(OpenAiClient.ModelsUrl);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }

            catch (Exception ex)
            {
                _log.Write("Error fetching model list", ex);
                throw;
            }
        }

        /// <summary>Falls back to the default vision model when the selection can't take images.</summary>
        private static string ResolveVisionModel(string model)
        {
            return ModelCatalog.IsChatModel(model) ? model : AppDefaults.VisionModel;
        }

        internal static List<string> SortImagesNumerically(string folder)
        {
            return Directory.EnumerateFiles(folder, "*.png")
                .Concat(Directory.EnumerateFiles(folder, "*.jpg"))
                .Concat(Directory.EnumerateFiles(folder, "*.jpeg"))
                .Select(f => new FileInfo(f))
                .OrderBy(fi =>
                {
                    var match = Regex.Match(fi.Name, @"\d+");
                    return match.Success ? match.Value.PadLeft(10, '0') : "0000000000";
                })
                .ThenBy(fi => fi.Name)
                .Select(fi => fi.FullName)
                .ToList();
        }

        /// <summary>Pulls choices[0].message.content out of a chat/completions response.</summary>
        private static string ReadMessageContent(string json)
        {
            JObject parsed = JObject.Parse(json);
            string? content = parsed["choices"]?[0]?["message"]?["content"]?.ToString();
            return content?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Pulls the text out of a /v1/responses reply, preferring the flat
        /// output_text and falling back to walking the nested output array.
        /// </summary>
        private static string ReadResponsesOutput(string json)
        {
            JObject parsed = JObject.Parse(json);
            string? text = parsed["output_text"]?.ToString();

            if (!string.IsNullOrWhiteSpace(text))
                return text.Trim();

            if (parsed["output"] is JArray outputs)
            {
                foreach (var outputItem in outputs)
                {
                    if (outputItem["content"] is not JArray contentArray)
                        continue;

                    foreach (var contentItem in contentArray)
                    {
                        var value = contentItem["text"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(value))
                            return value.Trim();
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Builds an exception carrying the response body — EnsureSuccessStatusCode
        /// throws away the part of the reply that says what actually went wrong.
        /// </summary>
        private static HttpRequestException BuildError(string what, HttpResponseMessage response, string body)
        {
            return new HttpRequestException($"{what} failed ({(int)response.StatusCode} {response.ReasonPhrase}): {body}");
        }
    }
}
