using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Web;
using AssistantAi.Classes;

namespace AssistantAi
{
    #region COSTS

    /* Notes on prices 11/09/2023
    "gpt-3.5-turbo", "gpt-3.5-turbo-16k", "gpt-4", "gpt-4-32k" 

    GPT-4 Turbo
    Model Input Output
    gpt-4-1106-preview	$0.01 / 1K tokens	$0.03 / 1K tokens
    gpt-4-1106-vision-preview	$0.01 / 1K tokens	$0.03 / 1K tokens

    GPT-4
    Model Input   Output
    gpt-4	$0.03 / 1K tokens	$0.06 / 1K tokens
    gpt-4-32k	$0.06 / 1K tokens	$0.12 / 1K tokens

    GPT-3.5 Turbo
    gpt-3.5-turbo-1106	$0.0010 / 1K tokens	$0.0020 / 1K tokens
    gpt-3.5-turbo-instruct	$0.0015 / 1K tokens	$0.0020 / 1K tokens

    Assistants API
    Tool	Input
    Code interpreter	$0.03 / session (free until 11/17/2023)
    Retrieval	$0.20 / GB / assistant / day (free until 11/17/2023)

    Image models
    Model	Quality	Resolution	Price
    DALL·E 3	Standard	1024×1024	$0.040 / image
    Standard	1024×1792, 1792×1024	$0.080 / image
    DALL·E 3	HD	1024×1024	$0.080 / image
    HD	1024×1792, 1792×1024	$0.120 / image
    DALL·E 2		1024×1024	$0.020 / image
    512×512	$0.018 / image
    256×256	$0.016 / image

    Audio models
    Model	Usage
    Whisper	$0.006 / minute (rounded to the nearest second)
    TTS	$0.015 / 1K characters
    TTS HD	$0.030 / 1K characters
    */

    #endregion COSTS

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string programLocation = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), openAIApiKey = @"", defaultChatGptModel = @"gpt-3.5-turbo", defaultWhisperModel = @"transcriptions", defaultAudioVoice = @"onyx";
        int tokenCount = 0;
        double estimatedCost = 0;        

        public MainWindow()
        {
            InitializeComponent();                                  
            SetDefaultsAsync();            
        }       

        private async void OnSendButtonClick(object sender, RoutedEventArgs e)
        {
            if (!CostCheck())
                MessageBox.Show("Either the token or the cost threshold is too high for your default settings.\r\n\r\nEither adjust your token/cost threshold or rephrase your question.");

            
        }

        private async void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            txtAssistantResponse.Text = string.Empty;
        }

        private async Task SetDefaultsAsync()
        {
            // Add default user
            txtUserId.Text = @"1";

            // Add default temperature
            txtTemperature.Text = @"0.5";

            // Add items to cmbModel
            cmbModel.Items.Add("gpt-3.5-turbo"); //4,097 tokens	Up to Sep 2021
            cmbModel.Items.Add("gpt-3.5-turbo-16k"); //16,385 tokens	Up to Sep 2021
            cmbModel.Items.Add("gpt-3.5-turbo-1106");
            cmbModel.Items.Add("gpt-4"); //8,192 tokens	Up to Sep 2021
            cmbModel.Items.Add("gpt-4-32k"); //32,768 tokens	Up to Sep 2021

            // Add items to cmbWhisperModel
            cmbWhisperModel.Items.Add("transcriptions");
            cmbWhisperModel.Items.Add("translations");

            cmbAudtioVoice.Items.Add("alloy");
            cmbAudtioVoice.Items.Add("echo");
            cmbAudtioVoice.Items.Add("fable");
            cmbAudtioVoice.Items.Add("onyx");
            cmbAudtioVoice.Items.Add("nova");
            cmbAudtioVoice.Items.Add("shimmer");

            // Set text for txtMaxTokens
            txtMaxTokens.Text = "2048";
            txtMaxDollars.Text = "0.50";

            // Set mute checkbox
            ckbxMute.IsChecked = true; 

            // Select default items by value
            cmbModel.SelectedItem = defaultChatGptModel;
            cmbWhisperModel.SelectedItem = defaultWhisperModel;
            cmbAudtioVoice.SelectedItem = defaultAudioVoice;

            // Set colors and fonts for txtAnswer

            txtAssistantResponse.Background = new SolidColorBrush(Colors.Black);
            txtAssistantResponse.Foreground = new SolidColorBrush(Colors.White);
            txtAssistantResponse.FontFamily = new FontFamily("Courier New");
            txtAssistantResponse.FontSize = 15; // This sets the font size to 15

            // Set colors and fonts for txtQuestion
            txtQuestion.Background = new SolidColorBrush(Colors.Black);
            txtQuestion.Foreground = new SolidColorBrush(Colors.White);
            txtQuestion.FontFamily = new FontFamily("Courier New");
            txtQuestion.FontSize = 15; // This sets the font size to 15

            // Set default text for txtQuestion
            txtQuestion.Text = "This is a test of an API key, are you receiving this?";

            await LoadApiKey();
            await CheckApiKey();
        }

        private async Task CheckApiKey()
        {
            string apiKeyPathway = System.IO.Path.Combine(programLocation, @"Files\ApiKey.json");

            if (openAIApiKey == "" || openAIApiKey == null)
            {
                OpenAiKeyRequest openAiKeyRequestPageOpen = new OpenAiKeyRequest(apiKeyPathway, openAIApiKey);
                openAiKeyRequestPageOpen.ShowDialog();

                await LoadApiKey();

                if (openAIApiKey == "" || openAIApiKey == null)
                {
                    AssistantControls.IsEnabled = false;
                }

                else
                {
                    AssistantControls.IsEnabled = true;
                }           
            }                
        }

        private void txtQuestion_TextChanged(object sender, TextChangedEventArgs e)
        {
            TokenCheck();
        }

        private void cmbModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TokenCheck();
        }

        private bool CostCheck()
        {
            bool pass = false;

            if (tokenCount < Convert.ToInt32(txtMaxTokens.Text) && estimatedCost < Convert.ToDouble(txtMaxDollars.Text))
                pass = true;

            return pass;
        }

        private void TokenCheck()
        {
            tokenCount = CountTokens(txtQuestion.Text);
            string modelName = cmbModel.SelectedItem.ToString();
            estimatedCost = CalculatePrice(tokenCount, modelName);

            lblEstimatedTokens.Content = @"Estimated Tokens = " + tokenCount.ToString();
            lblEstimatedCost.Content = $"Estimated Cost: ${estimatedCost:F2}";
        }

        private static int CountTokens(string input)
        {
            // Average characters per token for English text
            const int avgCharsPerToken = 4;

            // Calculate the number of characters including spaces
            int characterCountIncludingSpaces = input.Length;

            // Estimate the number of tokens based on the character count including spaces.
            int tokenCount = (int)Math.Ceiling((double)characterCountIncludingSpaces / avgCharsPerToken);

            return tokenCount;
        }

        private static double CalculatePrice(int tokens, string modelName)
        {
            // Per 1000 tokens for each model
            var pricing = new Dictionary<string, (double inputPrice, double outputPrice)>
            {
                { "gpt-3.5-turbo-1106", (0.0010, 0.0020) },
                { "gpt-4", (0.03, 0.06) },
                { "gpt-4-32k", (0.06, 0.12) },
                { "gpt-3.5-turbo", (0.0010, 0.0020) },
                { "gpt-3.5-turbo-16k", (0.0010, 0.0020) }
            };

            // Calculate the price based on the number of tokens and the specified model
            if (pricing.TryGetValue(modelName.ToLower(), out var prices))
            {
                // Assuming the cost is the same for input and output tokens
                // Calculate the total price for both input and output
                double totalPrice = (tokens / 1000.0) * (prices.inputPrice + prices.outputPrice);
                return totalPrice;
            }
            else
            {
                throw new ArgumentException($"Model name '{modelName}' is not recognized.");
            }
        }

        public async Task LoadApiKey()
        {
            string apiKeyPathway = System.IO.Path.Combine(programLocation, @"Files\ApiKey.json"); // Assuming the file is named ApiKey.json
            var workBench = new AssistantAi.Classes.OpenAiWorkBench();

            // Destructure the tuple into two variables: isLoaded and config
            var (isLoaded, config) = await workBench.LoadFromFileAsync(apiKeyPathway);

            if (isLoaded && config != null)
            {
                openAIApiKey = config.OpenAiKey;
                Console.WriteLine($"OpenAI API Key loaded: {config.OpenAiKey}");
                // You can now use config.OpenAiKey in your application.
            }
            else
            {
                Console.WriteLine("Failed to load the OpenAI API Key.");
                // Handle the failure case as needed.
            }
        }

    }
}
