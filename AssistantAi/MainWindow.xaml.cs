using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YourNamespace;
using System.Windows.Threading;
using System.Drawing.Imaging;
using AssistantAi.Class;
using System.Timers;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Diagnostics.Tracing;

namespace AssistantAi
{
    #region COSTS

    /* Notes on prices 02/10/2024
    https://openai.com/pricing#language-models
    "gpt-3.5-turbo", "gpt-3.5-turbo-16k", "gpt-4", "gpt-4-32k" 

    GPT-4 Turbo
    Model	Input	Output
    gpt-4-0125-preview	$0.01 / 1K tokens	$0.03 / 1K tokens
    gpt-4-1106-preview	$0.01 / 1K tokens	$0.03 / 1K tokens
    gpt-4-1106-vision-preview	$0.01 / 1K tokens	$0.03 / 1K tokens

    GPT-4
    Model Input   Output
    gpt-4	$0.03 / 1K tokens	$0.06 / 1K tokens
    gpt-4-32k	$0.06 / 1K tokens	$0.12 / 1K tokens

    GPT-3.5 Turbo
    Model	Input	Output
    gpt-3.5-turbo-0125	$0.0005 / 1K tokens	$0.0015 / 1K tokens
    gpt-3.5-turbo-instruct	$0.0015 / 1K tokens	$0.0020 / 1K tokens

    Assistants API => Not used yet
    Tool	Input
    Code interpreter	$0.03 / session
    Retrieval	$0.20 / GB / assistant / day (free until 03/01/2024)

    Fine-tuning models => Not used
    Model	Training	Input usage	Output usage
    gpt-3.5-turbo	$0.0080 / 1K tokens	$0.0030 / 1K tokens	$0.0060 / 1K tokens
    davinci-002	$0.0060 / 1K tokens	$0.0120 / 1K tokens	$0.0120 / 1K tokens
    babbage-002	$0.0004 / 1K tokens	$0.0016 / 1K tokens	$0.0016 / 1K tokens

    Embedding models => Not used
    Model	Usage
    text-embedding-3-small	$0.00002 / 1K tokens
    text-embedding-3-large	$0.00013 / 1K tokens
    ada v2	$0.00010 / 1K tokens

    Base models => Not used
    Model	Usage
    davinci-002	$0.0020 / 1K tokens
    babbage-002	$0.0004 / 1K tokens
    
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

    Older models
    Model	Input	Output
    gpt-3.5-turbo-1106	$0.0010 / 1K tokens	$0.0020 / 1K tokens
    gpt-3.5-turbo-0613	$0.0015 / 1K tokens	$0.0020 / 1K tokens
    gpt-3.5-turbo-16k-0613	$0.0030 / 1K tokens	$0.0040 / 1K tokens
    gpt-3.5-turbo-0301	$0.0015 / 1K tokens	$0.0020 / 1K tokens
    */

    #endregion COSTS

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> models = new List<string>() { "gpt-3.5-turbo", "gpt-3.5-turbo-16k", "gpt-3.5-turbo-1106", "gpt-4" }; //These seem broken in the program, "gpt-4-32k" };

        public AudioRecorder audioRecorder = new AudioRecorder();
        private MediaPlayer mediaPlayer;
        public DispatcherTimer countdownTimer;
        System.Timers.Timer apiCheckTimer;
        public int countdownValue = 30;
        int tokenCount = 0;
        double estimatedCost = 0;
        string instructionalText = @"You have selected to upload a list of images for an AI to review.  Please replace this text with your request.  This request will be the same for each image.  An example request is 'Attached is a review sheet that I completed.  Can you please review my answers for mistakes, and provide the correct answers if possible as well as a description for why that answer is correct";

        private readonly SolidColorBrush redOn = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
        private readonly SolidColorBrush redOff = new SolidColorBrush(System.Windows.Media.Color.FromRgb(128, 0, 0)); 
        private readonly SolidColorBrush yellowOn = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 0));
        private readonly SolidColorBrush yellowOff = new SolidColorBrush(System.Windows.Media.Color.FromRgb(128, 128, 0));
        private readonly SolidColorBrush greenOn = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0));         
        private readonly SolidColorBrush greenOff = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 128, 0));

        string programLocation = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            openAIApiKey = @"",
            defaultChatGptModel = @"gpt-3.5-turbo",
            defaultWhisperModel = @"transcriptions",
            defaultAudioVoice = @"onyx", 
            defaultImageModel = @"gpt-4-vision-preview",
            recordingsDirectory,
            currentRecordingPath, 
            speechDirectory,
            speechRecordingPath,
            currentPlayingFilePath,
            imageDirectory,            
            imageSavePath,            
            currentImageFilePath,
            imageCreationDirectory,
            currentImageCreationFilePath,
            errorLogDirectory,
            apiStatus;

        public MainWindow()
        {
            InitializeComponent();
            UpdateTrafficLight("yellow");
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            InitializeCountdownTimer();
            _ = SetDefaultsAsync();
            _ = DownDetectorAsync();
            TimerForApiCheck();
        }

        private async void OnSendButtonClick(object sender, RoutedEventArgs e)
        {
            AssistantControls.IsEnabled = false;
            btnSend.IsEnabled = false;
            btnClear.IsEnabled = false;
            if (!CostCheck())
                System.Windows.MessageBox.Show("Either the token or the cost threshold is too high for your default settings.\r\n\r\nEither adjust your token/cost threshold, rephrase your question, or change your model.");

            else
            {
                SpinnerStatus.Visibility = Visibility.Visible;
                await SendMessage();
                SpinnerStatus.Visibility = Visibility.Collapsed;

                #region TestCode

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

                //Image Creations
                //Directory.CreateDirectory(imageCreationDirectory);
                //string fileName = $"DALLE_{DateTime.Now:yyyyMMddHHmmss}.png";
                //currentImageCreationFilePath = System.IO.Path.Combine(imageCreationDirectory, fileName);
                //GenerateImageAsync(txtQuestion.Text, currentImageCreationFilePath);

                //var response = await WhisperMsgAsync(currentRecordingPath, @"whisper-1", @"transcriptions");
                //var response = await WhisperMsgAsync(currentRecordingPath, @"whisper-1", @"translations");
                //txtAssistantResponse.Text = response;  
                */

                #endregion TestCode
            }

            AssistantControls.IsEnabled = true;
            btnSend.IsEnabled = true;
            btnClear.IsEnabled = true;
            txtQuestion.Focus();
        }

        private async void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            txtAssistantResponse.Document.Blocks.Clear();
        }

        private async void btnGetImage_Click(object sender, RoutedEventArgs e)
        {            
            try
            {
                this.Visibility = Visibility.Hidden;
                await Task.Delay(500);

                string fileName = $"Image_{DateTime.Now:yyyyMMddHHmmss}.png";
                imageSavePath = System.IO.Path.Combine(imageDirectory, fileName);
                Directory.CreateDirectory(imageDirectory);

                // Capturing the entire screen
                System.Drawing.Rectangle bounds = System.Windows.Forms.Screen.GetBounds(System.Drawing.Point.Empty);
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(System.Drawing.Point.Empty, System.Drawing.Point.Empty, bounds.Size);
                    }

                    string imagePath = imageSavePath;
                    bitmap.Save(imagePath, ImageFormat.Png);
                    currentImageFilePath = imagePath;
                    //MessageBox.Show($"Image saved to {imagePath}");
                    Console.WriteLine($"Image saved to {imagePath}");
                }

                BitmapImage bitmapImage = new BitmapImage();

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = new Uri(currentImageFilePath, UriKind.Absolute);
                bitmapImage.EndInit();

                bitmapImage.Freeze();

                ImgPreviewImage.Source = bitmapImage;
            }

            catch (Exception ex)
            {
                LogWriter errorLog = new LogWriter();
                errorLog.WriteLog(errorLogDirectory, ex.ToString());
                System.Windows.MessageBox.Show($"Error: {ex.Message}");                
            }

            finally
            {
                this.Visibility = Visibility.Visible;                
                btnGetImage.IsEnabled = false;
                btnResetImage.IsEnabled = true;
            }
        }

        private async void btnResetImage_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(currentImageFilePath))
            {
                try
                {
                    ImgPreviewImage.Source = null;
                    await DeleteFileAsync(currentImageFilePath);
                }

                catch (Exception ex)
                {
                    LogWriter errorLog = new LogWriter();
                    errorLog.WriteLog(errorLogDirectory, ex.ToString());
                    txtAssistantResponse.AppendText("Error:\r\n" + ex.Message);
                }

                finally
                {
                    currentImageFilePath = null;
                    btnGetImage.IsEnabled = true;
                    btnResetImage.IsEnabled = false;
                }
            }
        }

        private async Task SendMessage()
        {
            string sQuestion = txtQuestion.Text;

            if (string.IsNullOrEmpty(sQuestion))
            {
                System.Windows.MessageBox.Show("Type in your question!");
                txtQuestion.Focus();
                return;
            }

            if (txtAssistantResponse.Document.Blocks.Count > 0)
            {
                AppendTextToRichTextBox("\r\n");
            }

            //txtAssistantResponse.AppendText("Me: " + sQuestion);
            await AssistantResponseWindow("Me: ", sQuestion);
            txtQuestion.Text = "";

            if (ckbxCreateImage.IsChecked == true)
            {
                try
                {
                    SpinnerStatus.Visibility = Visibility.Visible;
                    //Image Creations
                    Directory.CreateDirectory(imageCreationDirectory);
                    string fileName = $"DALLE_{DateTime.Now:yyyyMMddHHmmss}.png";
                    currentImageCreationFilePath = System.IO.Path.Combine(imageCreationDirectory, fileName);
                    await GenerateImageAsync(sQuestion, currentImageCreationFilePath);
                    await AssistantResponseWindow("DALL-e: ", @"Below is an image located under: " + currentImageCreationFilePath.ToString() + "\r\n");
                    await AssistantResponseWindowImageAdd(currentImageCreationFilePath);
                }

                catch (Exception ex)
                {
                    LogWriter errorLog = new LogWriter();
                    errorLog.WriteLog(errorLogDirectory, ex.ToString());
                    txtAssistantResponse.AppendText("Error: " + ex.Message);
                }

                finally
                {
                    SpinnerStatus.Visibility = Visibility.Collapsed;
                }
            }

            else if (ckbxImageReview.IsChecked == true)
            {
                if (lblPickupFolder.Content.ToString() == "")
                {
                    System.Windows.MessageBox.Show("Please select a folder to pick up images.");
                    return;
                }

                try
                {
                    SpinnerStatus.Visibility = Visibility.Visible;
                    await SentQuestionWithImagesAsync(sQuestion, lblPickupFolder.Content.ToString(), int.Parse(txtMaxTokens.Text));
                }

                catch (Exception ex)
                {
                    LogWriter errorLog = new LogWriter();
                    errorLog.WriteLog(errorLogDirectory, ex.ToString());
                    txtAssistantResponse.AppendText("Error: " + ex.Message);
                }

                finally
                {
                    SpinnerStatus.Visibility = Visibility.Collapsed;
                }
            }

            else if (ckbxMute.IsChecked == true)
            {
                if (File.Exists(currentImageFilePath))
                {
                    try
                    {
                        SpinnerStatus.Visibility = Visibility.Visible;
                        string base64Image = EncodeImageToBase64(currentImageFilePath); // Provide the correct path
                        string sAnswer = await SendImageMsgAsync(sQuestion, "jpeg", int.Parse(txtMaxTokens.Text), base64Image);
                        await AssistantResponseWindow("Chat GPT: ", sAnswer);
                    }

                    catch (Exception ex)
                    {
                        LogWriter errorLog = new LogWriter();
                        errorLog.WriteLog(errorLogDirectory, ex.ToString());
                        txtAssistantResponse.AppendText("Error: " + ex.Message);
                    }

                    finally
                    {
                        await DeleteFileAsync(currentImageFilePath);
                        ImgPreviewImage.Source = null;
                        currentImageFilePath = null;
                        btnGetImage.IsEnabled = true;
                        btnResetImage.IsEnabled = false;
                        SpinnerStatus.Visibility = Visibility.Collapsed;
                    }
                }

                else
                {
                    try
                    {
                        SpinnerStatus.Visibility = Visibility.Visible;
                        //string sAnswer = SendMsg(sQuestion) + "";
                        string sAnswer = await SendMsgAsync(sQuestion) + "";
                        await AssistantResponseWindow("Chat GPT: ", sAnswer);
                        //txtAssistantResponse.AppendText("\r\nChat GPT: " + sAnswer.Replace("\n", "\r\n").Trim() + "\r\n");                
                    }

                    catch (Exception ex)
                    {
                        LogWriter errorLog = new LogWriter();
                        errorLog.WriteLog(errorLogDirectory, ex.ToString());
                        txtAssistantResponse.AppendText("Error: " + ex.Message);
                    }

                    finally
                    {
                        SpinnerStatus.Visibility = Visibility.Collapsed;
                    }
                }
            }

            else
            {
                try
                {
                    SpinnerStatus.Visibility = Visibility.Visible;
                    string fileName = $"Speech_{DateTime.Now:yyyyMMddHHmmss}.mp3";
                    speechRecordingPath = System.IO.Path.Combine(speechDirectory, fileName);
                    Directory.CreateDirectory(speechDirectory);

                    //string sAnswer = SendMsg(sQuestion) + "";
                    string sAnswer = await SendMsgAsync(sQuestion) + "";
                    await AssistantResponseWindow("Chat GPT: ", sAnswer);
                    await WhisperTextToSpeechAsync(speechRecordingPath, sAnswer, cmbAudioVoice.SelectedItem.ToString());
                }

                catch (Exception ex)
                {
                    LogWriter errorLog = new LogWriter();
                    errorLog.WriteLog(errorLogDirectory, ex.ToString());
                    txtAssistantResponse.AppendText("Error: " + ex.Message);
                }

                finally
                {
                    SpinnerStatus.Visibility = Visibility.Collapsed;
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
                        messages = new[] { new { role = "user", content = PadInput(sQuestion) } }
                    };
                }
                else
                {
                    payload = new
                    {
                        model = sModel,
                        prompt = PadInput(sQuestion),
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

                    return sResponse.Trim();
                }

                catch (HttpRequestException ex)
                {
                    LogWriter errorLog = new LogWriter();
                    errorLog.WriteLog(errorLogDirectory, sQuestion + ":\r\n " + ex.ToString());
                    Console.WriteLine($"Request exception: {ex.Message}");
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
                    await PlayMp3File(outputFilePath);
                }

                catch (HttpRequestException ex)
                {
                    LogWriter errorLog = new LogWriter();
                    errorLog.WriteLog(errorLogDirectory, textToConvert + ":\r\n " + ex.ToString());
                    Console.WriteLine($"Request exception: {ex.Message}");
                }
            }
        }

        public async Task<string> SendImageMsgAsync(string sQuestion, string imageType, int maxTokens, string base64Image = null)
        {
            if (base64Image == null)
            {
                System.Windows.MessageBox.Show("No image provided.");
                return "";
            }

            // Set up HttpClient
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAIApiKey);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Payload
                var payload = new
                {
                    model = defaultImageModel, // Make sure this is set to your image model
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = sQuestion },
                                //new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                                new { type = "image_url", image_url = new { url = $"data:image/{imageType};base64,{base64Image}" } }
                            }
                        }
                    },
                    max_tokens = maxTokens
                };

                // Send Request
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                try
                {
                    var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                    response.EnsureSuccessStatusCode();
                    var responseContent = await response.Content.ReadAsStringAsync();

                    // Deserialize the JSON response
                    var oJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                    var oChoices = (JArray)oJson["choices"]; 
                    var oChoice = (JObject)oChoices[0]; 
                    string sResponse = "";

                    var oMessage = (JObject)oChoice["message"];
                    sResponse = (string)oMessage["content"];

                    return sResponse.Trim();
                }

                catch (Exception ex)
                {
                    LogWriter errorLog = new LogWriter();
                    errorLog.WriteLog(errorLogDirectory, sQuestion + ":\r\n " + ex.ToString());
                    System.Windows.MessageBox.Show($"Error sending image to OpenAI: {ex.Message}");
                    return "";
                }
            }
        }

        public async Task<string> SendMultipleImagesMsgAsync(string sQuestion, string imageType, string pickupFolder, int maxTokens)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAIApiKey);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var sortedFiles = Directory.EnumerateFiles(pickupFolder, "*.png")
                    .Select(f => new FileInfo(f))
                    .OrderBy(fi => Regex.Match(fi.Name, @"\d+").Value.PadLeft(10, '0'))
                    .ThenBy(fi => fi.Name)
                    .Select(fi => fi.FullName);

                var contentList = new List<object> { new { type = "text", text = sQuestion } };
                foreach (var file in sortedFiles)
                {
                    string base64Image = await EncodeImageToBase64Async(file);
                    var imageContent = new
                    {
                        type = "image_url",
                        image_url = new
                        {
                            url = $"data:image/{imageType};base64,{base64Image}"
                        }
                    };
                    contentList.Add(imageContent);
                }

                var payload = new
                {
                    model = defaultImageModel,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = contentList.ToArray()
                        }
                    },
                    max_tokens = maxTokens
                };

                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                try
                {
                    var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                    response.EnsureSuccessStatusCode();
                    var responseContent = await response.Content.ReadAsStringAsync();

                    var oJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                    var oChoices = (JArray)oJson["choices"];
                    var oChoice = (JObject)oChoices[0];
                    string sResponse = (string)oChoice["message"]["content"];

                    return sResponse.Trim();
                }
                catch (Exception ex)
                {
                    LogWriter errorLog = new LogWriter();
                    errorLog.WriteLog(errorLogDirectory, sQuestion + ":\r\n " + ex.ToString());
                    System.Windows.MessageBox.Show($"Error sending images to OpenAI: {ex.Message}");
                    return "";
                }
            }
        }

        public async Task SentQuestionWithImagesAsync(string sQuestion, string fileLocation, int maxTokens)
        {
            if (Directory.Exists(fileLocation))
            {
                string response = await SendMultipleImagesMsgAsync(sQuestion, "png", fileLocation, maxTokens);
                await AssistantResponseWindow("Chat GPT: ", response);
                await Task.Delay(5000);                            
                
                ckbxImageReview.IsChecked = false;                
                lblPickupFolder.Content = "";

            }

            else
            {
                System.Windows.MessageBox.Show("The folder does not exist.");
                ckbxImageReview.IsChecked = false;
                lblPickupFolder.Content = "";
            }
        }

        private async Task PlayMp3File(string filePath)
        {
            try
            {
                mediaPlayer.Open(new Uri(filePath));
                mediaPlayer.Play();
                currentPlayingFilePath = filePath; // Track the currently playing file
            }
            catch (Exception ex)
            {
                LogWriter errorLog = new LogWriter();
                errorLog.WriteLog(errorLogDirectory, ex.ToString());
                System.Windows.MessageBox.Show($"Playback Exception: {ex.Message}");
                await DeleteFileAsync(filePath);
            }
        }

        private async void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            // Handle media ended event
            await Task.Delay(500); // Short delay to ensure media is fully released
            await DeleteFileAsync(currentPlayingFilePath); // Assuming you have a way to track this
        }

        private void MediaPlayer_MediaFailed(object sender, EventArgs e)
        {
            System.Windows.MessageBox.Show(@"Error playback on file: " + currentPlayingFilePath.ToString());
        }

        public async Task<string> WhisperMsgAsync(string audioFilePath, string modelName, string modelType)
        {
            string sUrl = "https://api.openai.com/v1/audio/" + modelType;

            using (var httpClient = new HttpClient())
            using (var formData = new MultipartFormDataContent())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAIApiKey);

                byte[] fileBytes = File.ReadAllBytes(audioFilePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
                formData.Add(fileContent, "file", System.IO.Path.GetFileName(audioFilePath));

                var modelContent = new StringContent(modelName);
                formData.Add(modelContent, "model");

                try
                {
                    var response = await httpClient.PostAsync(sUrl, formData);
                    response.EnsureSuccessStatusCode();
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var parsedResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
                    return parsedResponse.ContainsKey("text") ? parsedResponse["text"] : string.Empty;
                }

                catch (HttpRequestException ex)
                {
                    LogWriter errorLog = new LogWriter();
                    errorLog.WriteLog(errorLogDirectory, ex.ToString());
                    Console.WriteLine("An error occurred while sending the request: " + ex.Message);
                    return null;
                }

                finally
                {
                    await DeleteFileAsync(audioFilePath);
                }
            }
        }

        private string EncodeImageToBase64(string imagePath)
        {
            byte[] imageArray = System.IO.File.ReadAllBytes(imagePath);
            return Convert.ToBase64String(imageArray);
        }

        private async Task<string> EncodeImageToBase64Async(string imagePath)
        {
            byte[] imageArray = System.IO.File.ReadAllBytes(imagePath);
            return Convert.ToBase64String(imageArray);
        }

        private async Task DeleteFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            catch (Exception ex)
            {
                LogWriter errorLog = new LogWriter();
                errorLog.WriteLog(errorLogDirectory, ex.ToString());
                System.Windows.MessageBox.Show($"Delete File Exception: {ex.Message}");
            }
        }

        public async Task GenerateImageAsync(string prompt, string outputFilePath)
        {
            string sUrl = "https://api.openai.com/v1/images/generations";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAIApiKey);

                var payload = new
                {
                    model = "dall-e-3",
                    prompt = prompt,
                    n = 1,
                    size = "1024x1024"
                };

                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                try
                {
                    var response = await httpClient.PostAsync(sUrl, content);
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                    var data = jsonResponse["data"] as JArray;
                    var firstImage = data[0] as JObject;
                    var imageUrl = firstImage["url"].ToString();

                    // Download the image from the URL
                    using (var imageClient = new HttpClient())
                    {
                        var imageResponse = await imageClient.GetAsync(imageUrl);
                        imageResponse.EnsureSuccessStatusCode();

                        using (var imageStream = await imageResponse.Content.ReadAsStreamAsync())
                        {
                            // Save the image to a file
                            using (var fileStream = new FileStream(outputFilePath, FileMode.Create))
                            {
                                await imageStream.CopyToAsync(fileStream);
                            }
                        }
                    }
                }

                catch (HttpRequestException ex)
                {
                    LogWriter errorLog = new LogWriter();
                    errorLog.WriteLog(errorLogDirectory, prompt + ":\r\n " + ex.ToString());
                    Console.WriteLine($"Request exception: {ex.Message}");
                }
            }
        }

        //Incomplete
        private static double CalculatePrice(int tokens, string modelName)
        {
            
            //https://platform.openai.com/docs/models
            // Per 1000 tokens for each model
            var pricing = new Dictionary<string, (double inputPrice, double outputPrice)>
            {
                { "gpt-3.5-turbo-1106", (0.0010, 0.0020) },
                { "gpt-3.5-turbo", (0.0010, 0.0020) },
                { "gpt-3.5-turbo-16k", (0.0010, 0.0020) },
                { "gpt-4", (0.03, 0.06) }
                //{ "gpt-4-32k", (0.06, 0.12) }, May not be released yet because it is broken               
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
                return 0.0000;
            }

        }

        private async Task SetDefaultsAsync()
        {
            recordingsDirectory = System.IO.Path.Combine(programLocation, @"Files\Sound recordings", "Recordings");
            speechDirectory = System.IO.Path.Combine(programLocation, @"Files\Sound recordings", "Speech");
            imageDirectory = System.IO.Path.Combine(programLocation, @"Files\Images", "Captures");
            imageCreationDirectory = System.IO.Path.Combine(programLocation, @"Files\Images", "Creations");
            errorLogDirectory = System.IO.Path.Combine(programLocation, @"Files\ErrorLogs");
            txtAssistantResponse.Document.Blocks.Clear();

            // Add default user role
            // Not used right now
            txtUserId.Text = @"1";

            // Add default temperature
            //https://platform.openai.com/docs/guides/text-generation/reproducible-outputs
            txtTemperature.Text = @"0.5";

            // Add items to cmbModel
            // https://platform.openai.com/docs/guides/text-generation
            cmbModel.Items.Add("gpt-3.5-turbo"); //4,097 tokens	Up to Sep 2021
            cmbModel.Items.Add("gpt-3.5-turbo-16k"); //16,385 tokens	Up to Sep 2021
            cmbModel.Items.Add("gpt-3.5-turbo-1106");
            cmbModel.Items.Add("gpt-4"); //8,192 tokens	Up to Sep 2021
            //cmbModel.Items.Add("gpt-4-32k"); //32,768 tokens	Up to Sep 2021        

            // Add items to cmbWhisperModel
            // https://platform.openai.com/docs/guides/speech-to-text
            cmbWhisperModel.Items.Add("transcriptions");
            cmbWhisperModel.Items.Add("translations");

            // Adds audio voices currently active
            // https://platform.openai.com/docs/guides/text-to-speech
            cmbAudioVoice.Items.Add("alloy");
            cmbAudioVoice.Items.Add("echo");
            cmbAudioVoice.Items.Add("fable");
            cmbAudioVoice.Items.Add("onyx");
            cmbAudioVoice.Items.Add("nova");
            cmbAudioVoice.Items.Add("shimmer");

            // Set text for txtMaxTokens
            txtMaxTokens.Text = "2048";
            txtMaxDollars.Text = "0.50";

            // Set mute, ImageReview checkbox, and pickup folder disabled
            ckbxMute.IsChecked = true;
            ckbxImageReview.IsChecked = false;
            btnPickupFolder.IsEnabled = false;

            // Select default items by value
            cmbModel.SelectedItem = defaultChatGptModel;
            cmbWhisperModel.SelectedItem = defaultWhisperModel;
            cmbAudioVoice.SelectedItem = defaultAudioVoice;            

            // Set colors and fonts for txtQuestion
            txtQuestion.Background = new SolidColorBrush(Colors.LightGray);
            txtQuestion.Foreground = new SolidColorBrush(Colors.Black);
            txtQuestion.FontFamily = new System.Windows.Media.FontFamily("Courier New");
            txtQuestion.FontSize = 15; 

            // Set colors and fonts for txtAssistantResponse
            txtAssistantResponse.Background = new SolidColorBrush(Colors.LightGray);
            txtAssistantResponse.Foreground = new SolidColorBrush(Colors.Black);
            txtAssistantResponse.FontFamily = new System.Windows.Media.FontFamily("Courier New");
            txtAssistantResponse.FontSize = 15; 

            await LoadApiKey();
            await CheckApiKey();
            txtQuestion.Focus();
        }

        public async Task LoadApiKey()
        {
            string apiKeyPathway = System.IO.Path.Combine(programLocation, @"Files\ApiKey.json");
            var workBench = new AssistantAi.Classes.OpenAiConfiguration();

            // Destructure the tuple into two variables: isLoaded and config
            var (isLoaded, config) = await workBench.LoadFromFileAsync(apiKeyPathway);

            if (isLoaded && config != null)
            {
                openAIApiKey = config.OpenAiKey;
                Console.WriteLine($"OpenAI API Key loaded: {config.OpenAiKey}");
            }

            else
            {
                Console.WriteLine("Failed to load the OpenAI API Key.");
            }
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
            response = response.Trim();

            try
            {
                //This is incomplete
                // Check if the response contains code marked by ```
                int firstIndex = response.IndexOf("```");
                int lastIndex = response.LastIndexOf("```");

                if (firstIndex != -1 && lastIndex != -1 && firstIndex != lastIndex)
                {
                    // Append text before the code block as plain text
                    string beforeCode = response.Substring(0, firstIndex);
                    AppendTextToRichTextBox(beforeCode);

                    // Extract the code block and apply syntax highlighting
                    string code = response.Substring(firstIndex + 3, lastIndex - firstIndex - 3);
                    AppendTextToRichTextBox(code, isCodeBlock: true);

                    // Append text after the code block as plain text
                    string afterCode = response.Substring(lastIndex + 3);
                    AppendTextToRichTextBox(afterCode);
                }

                else
                {
                    // It's not code, append it as plain text
                    AppendTextToRichTextBox(typeResponse + " " + response);
                }

                txtAssistantResponse.ScrollToEnd();
            }

            catch (Exception ex)
            {
                LogWriter errorLog = new LogWriter();
                errorLog.WriteLog(errorLogDirectory, ex.ToString());
                AppendTextToRichTextBox("Error: " + ex.Message);
                txtAssistantResponse.ScrollToEnd();
            }
        }

        //This is messy but it works, needs refined
        private async Task AssistantResponseWindowImageAdd(string fileLocation)
        {
            try
            {
                // Load the image from the file and create an Image control
                BitmapImage bitmap = new BitmapImage(new Uri(fileLocation, UriKind.Absolute));
                System.Windows.Controls.Image imageControl = new System.Windows.Controls.Image
                {
                    Source = bitmap,
                    Width = 400, // Set the width you want
                    Height = 400, // Set the height you want
                    Stretch = Stretch.Uniform
                };

                // Create a new InlineUIContainer to host the Image, and add it to the existing document
                InlineUIContainer container = new InlineUIContainer(imageControl);
                Paragraph paragraph = new Paragraph(container);

                // Append the new paragraph to the existing FlowDocument
                txtAssistantResponse.Document.Blocks.Add(paragraph);

                txtAssistantResponse.ScrollToEnd();
            }

            catch (Exception ex)
            {
                LogWriter errorLog = new LogWriter();
                errorLog.WriteLog(errorLogDirectory, ex.ToString());
                AppendTextToRichTextBox("Error: " + ex.Message); // Make sure this method correctly appends text
                txtAssistantResponse.ScrollToEnd();
            }
        }

        //This is incomplete
        private void AppendTextToRichTextBox(string text, bool isCodeBlock = false)
        {
            Paragraph paragraph = new Paragraph();

            if (isCodeBlock)
            {
                paragraph.FontFamily = new System.Windows.Media.FontFamily("Courier");
                paragraph.Background = System.Windows.Media.Brushes.LightGray;
                paragraph.Padding = new Thickness(5);
                HighlightCode(paragraph, text);
            }

            else
            {
                paragraph.Inlines.Add(new Run(text));
            }

            txtAssistantResponse.Document.Blocks.Add(paragraph);
        }

        //This is incomplete
        private void HighlightCode(Paragraph paragraph, string code)
        {
            // Define colors for syntax highlighting
            SolidColorBrush keywordColor = System.Windows.Media.Brushes.Blue;
            SolidColorBrush stringColor = System.Windows.Media.Brushes.Brown;
            SolidColorBrush commentColor = System.Windows.Media.Brushes.Green;
            SolidColorBrush normalTextColor = System.Windows.Media.Brushes.Black;

            // Define a list of C# keywords
            var keywords = new HashSet<string> {
                "abstract", "as", "base", "bool",
                "break", "byte", "case", "catch",
                "char", "checked", "class", "const",
                "continue", "decimal", "default", "delegate",
                "do", "double", "else", "enum",
                "event", "explicit", "extern", "false",
                "finally", "fixed", "float", "for",
                "foreach", "goto", "if", "implicit",
                "in", "int", "interface", "internal",
                "is", "lock", "long", "namespace",
                "new", "null", "object", "operator",
                "out", "override", "params", "private",
                "protected", "public", "readonly", "ref",
                "return", "sbyte", "sealed", "short",
                "sizeof", "stackalloc", "static", "string",
                "struct", "switch", "this", "throw",
                "true", "try", "typeof", "uint",
                "ulong", "unchecked", "unsafe", "ushort",
                "using", "virtual", "void", "volatile",
                "while"
            };

            string[] lines = code.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                Span span = new Span();

                string[] tokens = line.Split(' ');

                foreach (var token in tokens)
                {
                    Run run = new Run(token + " ") { Foreground = normalTextColor };

                    if (keywords.Contains(token))
                    {
                        run.Foreground = keywordColor;
                    }

                    else if (token.StartsWith("//"))
                    {
                        run.Foreground = commentColor;
                    }

                    else if (token.StartsWith("\"") && token.EndsWith("\""))
                    {
                        run.Foreground = stringColor;
                    }

                    span.Inlines.Add(run);
                }

                paragraph.Inlines.Add(span);
                paragraph.Inlines.Add(new LineBreak());
            }
        }

        private async void ckbxListeningMode_Checked(object sender, RoutedEventArgs e)
        {
            btnSend.IsEnabled = false;
            btnClear.IsEnabled = false;
            cmbWhisperModel.IsEnabled = false; 
            cmbAudioVoice.IsEnabled = false;
            cmbModel.IsEnabled = false;
            ckbxMute.IsEnabled = false;
            ckbxCreateImage.IsEnabled = false;
            ckbxImageReview.IsEnabled = false;
            ckbxContinuousListeningMode.IsEnabled = false;
            countdownValue = 30; // reset countdown
            ListeningModeProgressBar.Value = countdownValue; // reset progress bar
            StartAudioRecording();            
            countdownTimer.Start(); // start countdown            
        }

        private async void ckbxListeningMode_Unchecked(object sender, RoutedEventArgs e)
        {
            SpinnerStatus.Visibility = Visibility.Visible;
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
                        CultureInfo cultureInfo = CultureInfo.CurrentCulture;
                        TextInfo textInfo = cultureInfo.TextInfo;
                        string whisperTypeString = whisperType.ToString(); // Assume whisperType is an enum or similar
                        string properCase = textInfo.ToTitleCase(whisperTypeString.ToLower());

                        await AssistantResponseWindow("Whisper " + properCase + ":\r\n ", response);
                    }

                    catch (Exception ex)
                    {
                        LogWriter errorLog = new LogWriter();
                        errorLog.WriteLog(errorLogDirectory, ex.ToString());
                        txtAssistantResponse.AppendText("Error:\r\n" + ex.Message);
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
                        await WhisperTextToSpeechAsync(speechRecordingPath, response, cmbAudioVoice.SelectedItem.ToString());
                    }

                    catch (Exception ex)
                    {
                        LogWriter errorLog = new LogWriter();
                        errorLog.WriteLog(errorLogDirectory, ex.ToString());
                        txtAssistantResponse.AppendText("Error: " + ex.Message);
                    }
                }
            }

            cmbWhisperModel.IsEnabled = true;
            cmbAudioVoice.IsEnabled = true;
            cmbModel.IsEnabled = true;
            ckbxMute.IsEnabled = true;
            btnSend.IsEnabled = true;
            btnClear.IsEnabled = true;
            ckbxImageReview.IsEnabled = true;
            ckbxCreateImage.IsEnabled = true;
            ckbxContinuousListeningMode.IsEnabled = true;
            ListeningModeProgressBar.Value = 0;
            SpinnerStatus.Visibility = Visibility.Collapsed;            
        }

        //This is incomplete
        private async void ckbxTtsMode_Checked(object sender, RoutedEventArgs e)
        {
            btnSend.IsEnabled = false;
            btnClear.IsEnabled = false;
            cmbWhisperModel.IsEnabled = false;
            cmbAudioVoice.IsEnabled = false;
            cmbModel.IsEnabled = false;
            ckbxMute.IsEnabled = false;
            ckbxCreateImage.IsEnabled = false;
            ckbxImageReview.IsEnabled = false;
            ckbxListeningMode.IsEnabled = false;
            countdownValue = 30; // reset countdown
            ListeningModeProgressBar.Value = countdownValue; // reset progress bar
            StartListening();
            countdownTimer.Start(); // start countdown            
        }

        private void StartListening()
        {
            StartAudioRecording();

            // Assuming GetAudioLevel is a method that returns the current audio level (0-100)
            // Threshold level for detecting silence
            int silenceThreshold = 10;
            int silenceDuration = 3000; // Time in ms to consider stopped after silence
            int checkInterval = 100; // Check every 100 ms

            System.Timers.Timer silenceTimer = new System.Timers.Timer();
            silenceTimer.Interval = checkInterval;
            silenceTimer.Elapsed += (sender, e) =>
            {
                int currentAudioLevel = GetAudioLevel();
                if (currentAudioLevel < silenceThreshold)
                {
                    // Increment the count of intervals with silence
                    silenceCount += checkInterval;
                    if (silenceCount >= silenceDuration)
                    {
                        StopAudioRecording();
                        StartAudioRecording();
                        silenceCount = 0; // Reset the silence count
                    }
                }
                else
                {
                    silenceCount = 0; // Reset the silence count if audio level is above the threshold
                }
            };
            silenceTimer.AutoReset = true;
            silenceTimer.Start();

            // Create a timer to stop recording after 30 seconds regardless
            System.Timers.Timer durationTimer = new System.Timers.Timer();
            durationTimer.Interval = 30000; // 30 seconds
            durationTimer.Elapsed += (sender, e) =>
            {
                StopAudioRecording();
                StartAudioRecording();
            };
            durationTimer.AutoReset = false; // Only trigger once
            durationTimer.Start();
        }

        private int silenceCount = 0; // Field to keep track of silence duration

        // Dummy implementation for GetAudioLevel, replace with actual method to get the audio level
        private int GetAudioLevel()
        {
            // This should interface with your audio input to determine the current level
            return 50; // Dummy value
        }


        //This is incomplete
        private async void ckbxTtsModeMode_Unchecked(object sender, RoutedEventArgs e)
        {
            SpinnerStatus.Visibility = Visibility.Visible;
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
                        CultureInfo cultureInfo = CultureInfo.CurrentCulture;
                        TextInfo textInfo = cultureInfo.TextInfo;
                        string whisperTypeString = whisperType.ToString(); // Assume whisperType is an enum or similar
                        string properCase = textInfo.ToTitleCase(whisperTypeString.ToLower());

                        await AssistantResponseWindow("", response);
                    }

                    catch (Exception ex)
                    {
                        LogWriter errorLog = new LogWriter();
                        errorLog.WriteLog(errorLogDirectory, ex.ToString());
                        txtAssistantResponse.AppendText("Error:\r\n" + ex.Message);
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
                        await WhisperTextToSpeechAsync(speechRecordingPath, response, cmbAudioVoice.SelectedItem.ToString());
                    }

                    catch (Exception ex)
                    {
                        LogWriter errorLog = new LogWriter();
                        errorLog.WriteLog(errorLogDirectory, ex.ToString());
                        txtAssistantResponse.AppendText("Error: " + ex.Message);
                    }
                }
            }

            cmbWhisperModel.IsEnabled = true;
            cmbAudioVoice.IsEnabled = true;
            cmbModel.IsEnabled = true;
            ckbxMute.IsEnabled = true;
            btnSend.IsEnabled = true;
            btnClear.IsEnabled = true;
            ckbxImageReview.IsEnabled = true;
            ckbxCreateImage.IsEnabled = true;
            ckbxListeningMode.IsEnabled = true;
            SpinnerStatus.Visibility = Visibility.Collapsed;
            ListeningModeProgressBar.Value = 0;
        }

        private void StartAudioRecording()
        {
            try
            {
                // Generate a unique file name for each recording session
                string fileName = $"Recording_{DateTime.Now:yyyyMMddHHmmss}.mp3";
                currentRecordingPath = System.IO.Path.Combine(recordingsDirectory, fileName);

                // Ensure the directory exists
                Directory.CreateDirectory(recordingsDirectory);

                // Start recording to the specified file
                audioRecorder.StartRecording(currentRecordingPath);
            }

            catch (Exception ex)
            {
                LogWriter errorLog = new LogWriter();
                errorLog.WriteLog(errorLogDirectory, ex.ToString());
                System.Windows.MessageBox.Show($"An error occurred while starting recording: {ex.Message}");
            }
        }

        private void ckbxCreateImage_Checked(object sender, RoutedEventArgs e)
        {
            if (ckbxMute.IsChecked == false || ckbxListeningMode.IsChecked == true)
            {
                ckbxMute.IsChecked = true;
                ckbxListeningMode.IsChecked = false;
                ckbxImageReview.IsEnabled = false;
            }

            ckbxMute.IsEnabled = false;
            ckbxListeningMode.IsEnabled = false;
        }

        private void ckbxCreateImage_Unchecked(object sender, RoutedEventArgs e)
        {
            ckbxMute.IsEnabled = true;
            ckbxListeningMode.IsEnabled = true;
            ckbxImageReview.IsEnabled = true;
        }

        private void ckbxImageReview_Checked(object sender, RoutedEventArgs e)
        {
            ckbxMute.IsEnabled = false;
            ckbxListeningMode.IsEnabled = false;
            ckbxCreateImage.IsEnabled = false;
            btnPickupFolder.IsEnabled = true;
            AddInstructionalText();
        }

        private void ckbxImageReview_Unchecked(object sender, RoutedEventArgs e)
        {
            ckbxMute.IsEnabled = true;
            ckbxListeningMode.IsEnabled = true;
            ckbxCreateImage.IsEnabled = true;
            btnPickupFolder.IsEnabled = false;
            RemoveInstructionalText();
        }
  
        private void AddInstructionalText()
        {
            string sText = instructionalText;
            txtQuestion.AppendText(sText);
        }

        private void RemoveInstructionalText()
        {
            string sText = instructionalText;
            txtQuestion.Text = txtQuestion.Text.Replace(sText, "");
        }

        private void btnPickupFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string folderPath = folderBrowserDialog.SelectedPath;
                lblPickupFolder.Content = folderPath;
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

        private void cmbWhisperModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string whisperType = cmbWhisperModel.SelectedItem.ToString();

            if (whisperType == "transcriptions")
            {
                ckbxMute.IsChecked = true;
            }

            else
            {
                ckbxMute.IsEnabled = true;
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
            lblEstimatedCost.Content = $"Estimated Cost = ${estimatedCost:F2}";
        }

        private static int CountTokens(string input)
        {            
            const int avgCharsPerToken = 4;            
            int characterCountIncludingSpaces = input.Length;
            int tokenCount = (int)Math.Ceiling((double)characterCountIncludingSpaces / avgCharsPerToken);

            return tokenCount;
        }

        private string PadInput(string s)
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

        private void TimerForApiCheck()
        {
            apiCheckTimer = new System.Timers.Timer();
            apiCheckTimer.Elapsed += new ElapsedEventHandler(ApiStockTimerElapsedAsync);
            apiCheckTimer.Interval = 30000;
            apiCheckTimer.Enabled = true;
            apiCheckTimer.Start();
        }

        private async void ApiStockTimerElapsedAsync(object sender, ElapsedEventArgs e)
        {
            apiCheckTimer.Stop();
            await DownDetectorAsync();
            apiCheckTimer.Start();
        }

        private async Task DownDetectorAsync()
        {
            bool isInternetAvailable = await IsInternetAvailable();
            if (isInternetAvailable)
            {
                bool isApiActive = await CheckApiStatusAsync();
                if (!isApiActive)
                {
                    UpdateTrafficLight("red");
                }

                else
                    UpdateTrafficLight("green");
            }

            else if (!isInternetAvailable)
            {
                UpdateTrafficLight("yellow");
            }   
        }

        private static string ToProperCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(input.ToLower());
        }

        private async Task<bool> CheckApiStatusAsync()
        {
            bool returnStatus = false;
            string urlPath = @"https://status.openai.com/api/v2/status.json";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    string json = await httpClient.GetStringAsync(urlPath);
                    dynamic statusData = JsonConvert.DeserializeObject(json);
                    string indicator = statusData.status.indicator;                    
                    string description = statusData.status.description;

                    switch (indicator)
                    {                        
                        case "minor":
                        case "major":
                        case "critical":
                            returnStatus = false;
                            break;
                        case "none":                            
                            returnStatus = true;
                            break;
                    }

                    apiStatus = ToProperCase(indicator) + " - " + description;
                }

                catch (HttpRequestException ex)
                {
                    LogWriter errorLog = new LogWriter();
                    errorLog.WriteLog(errorLogDirectory, ex.ToString());
                    Console.WriteLine($"Error fetching JSON: {ex.Message}");
                    returnStatus = false;

                    apiStatus = "Unknown";
                }
            }

            return returnStatus;
        }

        private async Task<bool> IsInternetAvailable()
        {
            try
            {
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    var reply = ping.Send("8.8.8.8", 3000);
                    if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                    {                        
                        return true;
                    }

                    else
                    {
                        apiStatus = "Network Issues";
                    }
                }
            }

            catch (Exception ex)
            {
                apiStatus = "network issues";
                LogWriter errorLog = new LogWriter();
                errorLog.WriteLog(errorLogDirectory, ex.ToString());
                System.Windows.MessageBox.Show($"Error: {ex.Message}");
            }

            return false;
        }

        public void UpdateTrafficLight(string color)
        {            
            Dispatcher.Invoke(() =>
            {
                RedLight.Fill = redOff;
                YellowLight.Fill = yellowOff;
                GreenLight.Fill = greenOff;
                //ApiStatus.Text = apiStatus;
                ApiStatusTextBlock.Text = apiStatus;

                switch (color.ToLower())
                {
                    case "red":
                        RedLight.Fill = redOn;
                        break;
                    case "yellow":
                        YellowLight.Fill = yellowOn;
                        break;
                    case "green":
                        GreenLight.Fill = greenOn;
                        break;
                }
            });
        }
    }
}
