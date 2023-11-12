﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Authentication;
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
using System.Windows.Media.Media3D;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YourNamespace;
using System.Drawing;
using System.Configuration;
using System.Windows.Threading;

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
        public AudioRecorder audioRecorder = new AudioRecorder();
        public DispatcherTimer countdownTimer;
        public int countdownValue = 10; 

        string programLocation = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            openAIApiKey = @"",
            defaultChatGptModel = @"gpt-3.5-turbo",
            defaultWhisperModel = @"transcriptions",
            defaultAudioVoice = @"onyx",
            recordingsDirectory = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Files\Sound recordings", "Recordings"),
            currentRecordingPath, 
            speechDirectory = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Files\Sound recordings", "Speech"),
            speechRecordingPath;

        List<string> models = new List<string>() { "gpt-3.5-turbo", "gpt-3.5-turbo-16k", "gpt-4", "gpt-4-32k" };
        int tokenCount = 0;
        double estimatedCost = 0;        

        public MainWindow()
        {
            InitializeComponent();
            InitializeCountdownTimer();
            SetDefaultsAsync();            
        }       

        private async void OnSendButtonClick(object sender, RoutedEventArgs e)
        {
            btnSend.IsEnabled = false;
            if (!CostCheck())
                MessageBox.Show("Either the token or the cost threshold is too high for your default settings.\r\n\r\nEither adjust your token/cost threshold or rephrase your question.");

            else
            {
                //Text question
                await SendMessage();

                /* All test code below; do not remove
                //Whisper Speech return test
                //string fileName = $"Speech_{DateTime.Now:yyyyMMddHHmmss}.wav";
                //speechRecordingPath = System.IO.Path.Combine(speechDirectory, fileName);
                //Directory.CreateDirectory(speechDirectory);
                //txtWhisperSpeechResponse.Text = txtQuestion.Text;
                //await WhisperTextToSpeechAsync(speechRecordingPath, txtQuestion.Text, @"onyx");                

                //Whisper Transcriptions
                //string fileName = $"Record_{DateTime.Now:yyyyMMddHHmmss}.wav";
                //Directory.CreateDirectory(recordingsDirectory);
                //currentRecordingPath = System.IO.Path.Combine(recordingsDirectory, fileName);               
                //currentRecordingPath = System.IO.Path.Combine(recordingsDirectory, "RecordingTests", "Recording.m4a");

                //Whisper Translations
                //string fileName = $"Record_{DateTime.Now:yyyyMMddHHmmss}.wav";
                //Directory.CreateDirectory(recordingsDirectory);
                //currentRecordingPath = System.IO.Path.Combine(recordingsDirectory, "RecordingTests", "German.m4a");
                //currentRecordingPath = System.IO.Path.Combine(recordingsDirectory, "RecordingTests", "Telugu.m4a");
                //currentRecordingPath = System.IO.Path.Combine(recordingsDirectory, "RecordingTests", "Polish.m4a");

                //var response = await WhisperMsgAsync(currentRecordingPath, @"whisper-1", @"transcriptions");
                //var response = await WhisperMsgAsync(currentRecordingPath, @"whisper-1", @"translations");
                //txtAssistantResponse.Text = response;  
                */
            }

            btnSend.IsEnabled = true;
        }

        private async void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            txtAssistantResponse.Text = string.Empty;
        }

        private async Task SendMessage()
        {
            string sQuestion = txtQuestion.Text;
            if (string.IsNullOrEmpty(sQuestion))
            {
                MessageBox.Show("Type in your question!");
                txtQuestion.Focus();
                return;
            }

            if (txtAssistantResponse.Text != "")
            {
                txtAssistantResponse.AppendText("\r\n");
            }

            txtAssistantResponse.AppendText("Me: " + sQuestion);
            txtQuestion.Text = "";

            if (ckbxMute.IsChecked == true)
            {
                try
                {
                    //string sAnswer = SendMsg(sQuestion) + "";
                    string sAnswer = await SendMsgAsync(sQuestion) + "";
                    await AssistantResponseWindow("Chat GPT: ", sAnswer);
                    //txtAssistantResponse.AppendText("\r\nChat GPT: " + sAnswer.Replace("\n", "\r\n").Trim() + "\r\n");                
                }

                catch (Exception ex)
                {
                    txtAssistantResponse.AppendText("Error: " + ex.Message);
                }
            }

            else
            {
                try
                {
                    string fileName = $"Speech_{DateTime.Now:yyyyMMddHHmmss}.wav";
                    speechRecordingPath = System.IO.Path.Combine(speechDirectory, fileName);
                    Directory.CreateDirectory(speechDirectory);

                    //string sAnswer = SendMsg(sQuestion) + "";
                    string sAnswer = await SendMsgAsync(sQuestion) + "";
                    await AssistantResponseWindow("Chat GPT: ", sAnswer);
                    await WhisperTextToSpeechAsync(speechRecordingPath, sAnswer, cmbAudioVoice.Text);               
                }

                catch (Exception ex)
                {
                    txtAssistantResponse.AppendText("Error: " + ex.Message);
                }
            }
        }

        public async Task<string> SendMsgAsync(string sQuestion)
        {
            string sModel = cmbModel.Text;
            string sUrl = "https://api.openai.com/v1/completions";

            if (models.Any(sub => sModel.Contains(sub)))
            {
                sUrl = "https://api.openai.com/v1/chat/completions";
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAIApiKey);

                object payload;
                if (models.Any(sub => sModel.Contains(sub)))
                {
                    payload = new
                    {
                        model = sModel,
                        messages = new[] { new { role = "user", content = PadQuotes(sQuestion) } }
                    };
                }
                else
                {
                    payload = new
                    {
                        model = sModel,
                        prompt = PadQuotes(sQuestion),
                        max_tokens = int.Parse(txtMaxTokens.Text),
                        temperature = double.Parse(txtTemperature.Text),
                        // Other parameters if needed
                    };
                }

                var data = JsonConvert.SerializeObject(payload);
                var content = new StringContent(data, Encoding.UTF8, "application/json");

                try
                {
                    var response = await httpClient.PostAsync(sUrl, content);
                    response.EnsureSuccessStatusCode();

                    var sJson = await response.Content.ReadAsStringAsync();

                    // Deserialize the JSON response
                    var oJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(sJson);
                    var oChoices = (JArray)oJson["choices"]; // Changed to JArray
                    var oChoice = (JObject)oChoices[0]; // Changed to JObject
                    string sResponse = "";

                    if (models.Any(sub => sModel.Contains(sub)))
                    {
                        var oMessage = (JObject)oChoice["message"];
                        sResponse = (string)oMessage["content"];
                    }
                    else
                    {
                        sResponse = (string)oChoice["text"];
                    }

                    return sResponse;
                }

                catch (HttpRequestException e)
                {
                    // Handle exception.
                    Console.WriteLine($"Request exception: {e.Message}");
                    return "";
                }
            }
        }

        public async Task WhisperTextToSpeechAsync(string outputFilePath, string textToConvert, string voiceModel)
        {
            string sUrl = "https://api.openai.com/v1/audio/speech";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAIApiKey);

                var payload = new
                {
                    model = "tts-1",
                    input = textToConvert,
                    voice = voiceModel
                };

                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                try
                {
                    var response = await httpClient.PostAsync(sUrl, content);
                    response.EnsureSuccessStatusCode();

                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        // Save the response stream (MP3 file) to a file
                        using (var fileStream = File.OpenWrite(outputFilePath))
                        {
                            await responseStream.CopyToAsync(fileStream);
                        }
                    }

                    // Optionally play the MP3 file after saving
                    PlayMp3File(outputFilePath);
                }

                catch (HttpRequestException e)
                {
                    // Handle exception.
                    Console.WriteLine($"Request exception: {e.Message}");
                }


            }
        }

        private void PlayMp3File(string filePath)
        {
            var mediaPlayer = new MediaPlayer();

            mediaPlayer.Open(new Uri(filePath));

            mediaPlayer.Play();

            // Event handler for the MediaEnded event to dispose of the MediaPlayer once playback is finished
            mediaPlayer.MediaEnded += (sender, e) =>
            {
                mediaPlayer.Close();
                DeleteFile(filePath); // Ensure DeleteFile is thread-safe or dispatch it to the UI thread if necessary
            };
        }

        public async Task<string> WhisperMsgAsync(string audioFilePath, string modelName, string modelType)
        {
            string sUrl = "https://api.openai.com/v1/audio/" + modelType;

            using (var httpClient = new HttpClient())
            using (var formData = new MultipartFormDataContent())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAIApiKey);

                // Load the file into a StreamContent
                byte[] fileBytes = File.ReadAllBytes(audioFilePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
                formData.Add(fileContent, "file", System.IO.Path.GetFileName(audioFilePath));

                // Add model name part
                var modelContent = new StringContent(modelName);
                formData.Add(modelContent, "model");

                try
                {
                    var response = await httpClient.PostAsync(sUrl, formData);
                    response.EnsureSuccessStatusCode();
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var parsedResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
                    return parsedResponse.ContainsKey("text") ? parsedResponse["text"] : string.Empty;
                    //return responseContent;
                }
                catch (HttpRequestException ex)
                {
                    // Handle exception
                    Console.WriteLine("An error occurred while sending the request: " + ex.Message);
                    return null;
                }

                finally
                {
                    DeleteFile(audioFilePath);
                }
            }
        }

        private void DeleteFile(string filePath)
        {
            File.Delete(filePath);
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

            cmbAudioVoice.Items.Add("alloy");
            cmbAudioVoice.Items.Add("echo");
            cmbAudioVoice.Items.Add("fable");
            cmbAudioVoice.Items.Add("onyx");
            cmbAudioVoice.Items.Add("nova");
            cmbAudioVoice.Items.Add("shimmer");

            // Set text for txtMaxTokens
            txtMaxTokens.Text = "2048";
            txtMaxDollars.Text = "0.50";

            // Set mute checkbox
            ckbxMute.IsChecked = true; 

            // Select default items by value
            cmbModel.SelectedItem = defaultChatGptModel;
            cmbWhisperModel.SelectedItem = defaultWhisperModel;
            cmbAudioVoice.SelectedItem = defaultAudioVoice;            

            // Set colors and fonts for txtQuestion
            txtQuestion.Background = new SolidColorBrush(Colors.Black);
            txtQuestion.Foreground = new SolidColorBrush(Colors.White);
            txtQuestion.FontFamily = new System.Windows.Media.FontFamily("Courier New");
            txtQuestion.FontSize = 15; // This sets the font size to 15

            // Set colors and fonts for txtAssistantResponse
            txtAssistantResponse.Background = new SolidColorBrush(Colors.Black);
            txtAssistantResponse.Foreground = new SolidColorBrush(Colors.White);
            txtAssistantResponse.FontFamily = new System.Windows.Media.FontFamily("Courier New");
            txtAssistantResponse.FontSize = 15; // This sets the font size to 15

            // Set default text for testing
            //txtQuestion.Text = "This is a test of an API key, are you receiving this?";
            //txtAssistantResponse.Text = "Response";
            //txtWhisperSpeechResponse.Text = "Response";

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

        private async Task AssistantResponseWindow(string typeResponse, string response)
        {
            try
            {
                txtAssistantResponse.AppendText("\r\n" + typeResponse + response.Replace("\n", "\r\n").Trim() + "\r\n");
                txtAssistantResponse.ScrollToEnd();
            }

            catch (Exception ex)
            {
                txtAssistantResponse.AppendText("Error: " + ex.Message);
                txtAssistantResponse.ScrollToEnd();
            }
        }

        private async void ckbxListeningMode_Checked(object sender, RoutedEventArgs e)
        {
            btnSend.IsEnabled = false;
            btnClear.IsEnabled = false;
            countdownValue = 10; // reset countdown
            ListeningModeProgressBar.Value = countdownValue; // reset progress bar
            StartAudioRecording();            
            countdownTimer.Start(); // start countdown
        }

        private async void ckbxListeningMode_Unchecked(object sender, RoutedEventArgs e)
        {
            countdownTimer.Stop();
            StopAudioRecording();
            string whisperType = cmbWhisperModel.Text;
            var response = await WhisperMsgAsync(currentRecordingPath, @"whisper-1", whisperType);
            if (response != null)
            {               
                if (ckbxMute.IsChecked == true)
                {
                    try
                    {
                        await AssistantResponseWindow("Whisper Translate: ", response);
                    }

                    catch (Exception ex)
                    {
                        txtAssistantResponse.AppendText("Error: " + ex.Message);
                    }
                }

                else
                {
                    try
                    {
                        string fileName = $"Speech_{DateTime.Now:yyyyMMddHHmmss}.wav";
                        speechRecordingPath = System.IO.Path.Combine(speechDirectory, fileName);
                        Directory.CreateDirectory(speechDirectory);

                        //string sAnswer = SendMsg(sQuestion) + "";
                        await AssistantResponseWindow("Whisper Translate: ", response);
                        await WhisperTextToSpeechAsync(speechRecordingPath, response, cmbAudioVoice.Text);
                    }

                    catch (Exception ex)
                    {
                        txtAssistantResponse.AppendText("Error: " + ex.Message);
                    }
                }
            }

            btnSend.IsEnabled = true;
            btnClear.IsEnabled = true;
        }

        private void StartAudioRecording()
        {
            try
            {
                // Generate a unique file name for each recording session
                string fileName = $"Recording_{DateTime.Now:yyyyMMddHHmmss}.wav";
                currentRecordingPath = System.IO.Path.Combine(recordingsDirectory, fileName);

                // Ensure the directory exists
                Directory.CreateDirectory(recordingsDirectory);

                // Start recording to the specified file
                audioRecorder.StartRecording(currentRecordingPath);
            }

            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while starting recording: {ex.Message}");
            }
        }

        private void StopAudioRecording()
        {
            audioRecorder.StopRecording();
        }

        private void InitializeCountdownTimer()
        {
            countdownTimer = new DispatcherTimer();
            countdownTimer.Interval = TimeSpan.FromSeconds(1);
            countdownTimer.Tick += CountdownTimer_Tick;
            ListeningModeProgressBar.Maximum = countdownValue;
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdownValue--;
            ListeningModeProgressBar.Value = countdownValue;

            if (countdownValue <= 0)
            {
                countdownTimer.Stop();
                ckbxListeningMode.IsChecked = false; 
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

        private string PadQuotes(string s)
        {
            if (s.IndexOf("\\") != -1)
                s = s.Replace("\\", "\\\\");

            if (s.IndexOf("\r\n") != -1)
                s = s.Replace("\r\n", "\\n");

            if (s.IndexOf("\r") != -1)
                s = s.Replace("\r", "\\r");

            if (s.IndexOf("\n") != -1)
                s = s.Replace("\n", "\\n");

            if (s.IndexOf("\t") != -1)
                s = s.Replace("\t", "\\t");

            if (s.IndexOf("\"") != -1)
                return s.Replace("\"", "\\\"");
            else
                return s;
        }
    }
}