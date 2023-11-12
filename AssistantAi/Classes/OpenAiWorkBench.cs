using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AssistantAi.Classes
{
    internal class OpenAiWorkBench
    {
        public class OpenAiConfig
        {
            [JsonProperty("OpenAiKey")]
            public string OpenAiKey { get; set; }
        }

        public async Task<(bool, OpenAiConfig)> LoadFromFileAsync(string filePath)
        {
            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                var config = JsonConvert.DeserializeObject<OpenAiConfig>(json);
                return (true, config); // Return true and the config
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while loading the file: " + ex.Message);
                return (false, null); // Return false and null
            }
        }

        public async Task<bool> SaveToFileAsync(string filePath, OpenAiConfig config)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Ensure the file exists, create it if it does not
                if (!File.Exists(filePath))
                {
                    using (var stream = File.Create(filePath)) { }
                }

                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);
                return true; // Return true on success
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while saving the file: " + ex.Message);
                return false; // Return false on failure
            }
        }
    }
}
