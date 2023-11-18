using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AssistantAi.Classes
{
    //This is just the class to load, save, and hold the API key
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
                return (true, config); 
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while loading the file: " + ex.Message);
                return (false, null); 
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

                if (!File.Exists(filePath))
                {
                    using (var stream = File.Create(filePath)) { }
                }

                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);
                return true; 
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while saving the file: " + ex.Message);
                return false; 
            }
        }
    }
}
