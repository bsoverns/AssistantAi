using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AssistantAi.Helpers;
using AssistantAi.Models;
using Newtonsoft.Json.Linq;

namespace AssistantAi.Services
{
    /// <summary>Image generation against /v1/images/generations.</summary>
    public class OpenAiImageService
    {
        private readonly OpenAiClient _client;
        private readonly ErrorLog _log;

        public OpenAiImageService(OpenAiClient client, ErrorLog log)
        {
            _client = client;
            _log = log;
        }

        /// <summary>
        /// Generates an image for <paramref name="prompt"/> and writes it to
        /// <paramref name="outputFilePath"/>.
        /// </summary>
        public async Task GenerateAsync(
            string prompt,
            string outputFilePath,
            string? model = null,
            string? size = null,
            string? quality = null)
        {
            // gpt-image-* models reject response_format and always return b64_json.
            var payload = new
            {
                model = model ?? AppDefaults.ImageGenerationModel,
                prompt,
                n = 1,
                size = size ?? AppDefaults.ImageSize,
                quality = quality ?? AppDefaults.ImageQuality,
                output_format = "png"
            };

            try
            {
                var response = await _client.PostJsonAsync(OpenAiClient.ImageGenerationUrl, payload);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // The API puts the real reason in the body, which EnsureSuccessStatusCode throws away.
                    throw new HttpRequestException(
                        $"Image generation failed ({(int)response.StatusCode} {response.ReasonPhrase}): {body}");
                }

                var firstImage = JObject.Parse(body)["data"]?.FirstOrDefault() as JObject;

                if (firstImage == null)
                    throw new HttpRequestException($"Image generation returned no image data: {body}");

                var base64Image = firstImage["b64_json"]?.ToString();

                if (!string.IsNullOrEmpty(base64Image))
                {
                    await File.WriteAllBytesAsync(outputFilePath, Convert.FromBase64String(base64Image));
                    return;
                }

                // Older models returned a URL instead; keep the path working if one ever comes back.
                var imageUrl = firstImage["url"]?.ToString();

                if (string.IsNullOrEmpty(imageUrl))
                    throw new HttpRequestException($"Image generation response contained neither b64_json nor url: {body}");

                await DownloadAsync(imageUrl, outputFilePath);
            }

            catch (Exception ex)
            {
                _log.Write(prompt, ex);
                Console.WriteLine($"Request exception: {ex.Message}");
                throw; // let the caller report it instead of claiming an image was saved
            }
        }

        private static async Task DownloadAsync(string imageUrl, string outputFilePath)
        {
            using (var imageClient = new HttpClient())
            {
                var imageResponse = await imageClient.GetAsync(imageUrl);
                imageResponse.EnsureSuccessStatusCode();

                using (var imageStream = await imageResponse.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(outputFilePath, FileMode.Create))
                {
                    await imageStream.CopyToAsync(fileStream);
                }
            }
        }
    }
}
