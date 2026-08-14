using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AssistantAi.Helpers;
using Newtonsoft.Json.Linq;

namespace AssistantAi.Services
{
    /// <summary>Speech-to-text and text-to-speech calls.</summary>
    public class OpenAiAudioService
    {
        private readonly OpenAiClient _client;
        private readonly ErrorLog _log;

        public OpenAiAudioService(OpenAiClient client, ErrorLog log)
        {
            _client = client;
            _log = log;
        }

        /// <summary>
        /// Transcribes or translates a recording, then deletes it either way.
        /// Returns null when the clip was silent or the request failed.
        /// </summary>
        /// <param name="endpoint">"transcriptions" or "translations".</param>
        public async Task<string?> TranscribeAsync(string audioFilePath, string model, string endpoint)
        {
            // Skip silent chunks so continuous mode doesn't pay to transcribe room noise.
            if (!AudioAnalysis.HasSpeech(audioFilePath))
            {
                await FileHelper.DeleteAsync(audioFilePath, _log);
                return null;
            }

            try
            {
                using (var formData = new MultipartFormDataContent())
                {
                    var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(audioFilePath));
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
                    formData.Add(fileContent, "file", Path.GetFileName(audioFilePath));
                    formData.Add(new StringContent(model), "model");

                    var response = await _client.PostAsync(OpenAiClient.AudioUrl + endpoint, formData);
                    response.EnsureSuccessStatusCode();

                    var body = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(body)["text"]?.ToString() ?? string.Empty;
                }
            }

            catch (HttpRequestException ex)
            {
                _log.Write(ex);
                Console.WriteLine("An error occurred while sending the request: " + ex.Message);
                return null;
            }

            finally
            {
                await FileHelper.DeleteAsync(audioFilePath, _log);
            }
        }

        /// <summary>
        /// Renders <paramref name="textToConvert"/> to speech and writes the MP3 to
        /// <paramref name="outputFilePath"/>. Playback is the caller's responsibility.
        /// </summary>
        public async Task TextToSpeechAsync(string outputFilePath, string textToConvert, string ttsModel, string voice)
        {
            var payload = new
            {
                model = ttsModel,
                input = textToConvert,
                instructions = "Speak in a tone that aligns with the tone of sentence.  If it sounds happy, make it happy.  If it sounds sad, make it sad.  Angry...ect.ect...",
                voice
            };

            try
            {
                var response = await _client.PostJsonAsync(OpenAiClient.SpeechUrl, payload);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Speech request failed ({(int)response.StatusCode} {response.ReasonPhrase}): {body}");
                }

                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(outputFilePath))
                {
                    await responseStream.CopyToAsync(fileStream);
                }
            }

            catch (Exception ex)
            {
                _log.Write(textToConvert, ex);
                throw;
            }
        }
    }
}
