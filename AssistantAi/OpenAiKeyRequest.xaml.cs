using System;
using System.Threading.Tasks;
using System.Windows;
using AssistantAi.Classes;

namespace AssistantAi
{
    /// <summary>
    /// Interaction logic for OpenAiKeyRequest.xaml
    /// </summary>
    public partial class OpenAiKeyRequest : Window
    {
        string programLocation = @"";
        public OpenAiKeyRequest(string programLocationInbound, string apiKey)
        {
            InitializeComponent();
            SetApiKey(apiKey);
            programLocation = programLocationInbound;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            await SaveApiKey();
            this.Close();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            txtOpenAiKey.Text = @"";
        }

        private void SetApiKey(string apiKey)
        {
            txtOpenAiKey.Text = apiKey;
        }

        public async Task SaveApiKey()
        {
            string yourOpenAiApiKey = txtOpenAiKey.Text;
            bool saveSuccessful = await SaveApiKey(yourOpenAiApiKey);

            if (saveSuccessful)
            {
                Console.WriteLine("API key was successfully saved.");
            }
            else
            {
                Console.WriteLine("Failed to save the API key.");
            }
        }

        public async Task<bool> SaveApiKey(string openAiKey)
        {
            string apiKeyPathway = programLocation; // Adjust the path as necessary
            var config = new OpenAiConfiguration.OpenAiData { OpenAiKey = openAiKey };
            var workBench = new OpenAiConfiguration();

            return await workBench.SaveToFileAsync(apiKeyPathway, config);
        }
    }
}
