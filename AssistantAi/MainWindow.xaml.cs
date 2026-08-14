using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AssistantAi.Classes;
using AssistantAi.Helpers;
using AssistantAi.Models;
using AssistantAi.Services;

namespace AssistantAi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    ///
    /// This class is the view layer only: it wires controls to the services in
    /// <c>Services/</c>, renders their results through <see cref="ResponseRenderer"/>,
    /// and manages which controls are enabled. Network calls, file encoding and
    /// model configuration live in Services/, Helpers/ and Models/ respectively.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AppPaths _paths;
        private readonly ErrorLog _log;
        private readonly OpenAiClient _openAi;
        private readonly OpenAiChatService _chat;
        private readonly OpenAiAudioService _audio;
        private readonly OpenAiImageService _images;
        private readonly ApiStatusService _apiStatus;
        private readonly AudioCaptureService _capture;
        private readonly ResponseRenderer _renderer;

        private ConversationDatabase? _conversationDb;
        private int _currentConversationId;
        private bool _isLoadingConversation;

        private readonly MediaPlayer _mediaPlayer;
        private DispatcherTimer _countdownTimer = null!;
        private System.Timers.Timer? _apiCheckTimer;
        private int _countdownValue = AppDefaults.StandardListeningSeconds;

        private int _tokenCount;
        private double _estimatedCost;
        private string _apiStatusText = "";
        private string _listeningMode = "Standard";

        private string? _currentPlayingFilePath;
        private string? _currentImageFilePath;
        private string? _currentImageCreationFilePath;

        private readonly SolidColorBrush _redOn = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        private readonly SolidColorBrush _redOff = new SolidColorBrush(Color.FromRgb(128, 0, 0));
        private readonly SolidColorBrush _yellowOn = new SolidColorBrush(Color.FromRgb(255, 255, 0));
        private readonly SolidColorBrush _yellowOff = new SolidColorBrush(Color.FromRgb(128, 128, 0));
        private readonly SolidColorBrush _greenOn = new SolidColorBrush(Color.FromRgb(0, 255, 0));
        private readonly SolidColorBrush _greenOff = new SolidColorBrush(Color.FromRgb(0, 128, 0));

        public MainWindow()
        {
            InitializeComponent();

            _paths = new AppPaths();
            _log = new ErrorLog(_paths.ErrorLogs);
            _openAi = new OpenAiClient();
            _chat = new OpenAiChatService(_openAi, _log);
            _audio = new OpenAiAudioService(_openAi, _log);
            _images = new OpenAiImageService(_openAi, _log);
            _apiStatus = new ApiStatusService(_log);
            _capture = new AudioCaptureService();
            _renderer = new ResponseRenderer(txtAssistantResponse, _log);

            UpdateTrafficLight(TrafficLight.Yellow);

            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            _mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;

            InitializeCountdownTimer();

            _ = SetDefaultsAsync();
            _ = RefreshApiStatusAsync();
            StartApiStatusTimer();
        }

        // ─── Startup ──────────────────────────────────────────────────────────────

        private async Task SetDefaultsAsync()
        {
            _renderer.Clear();

            // Not used by any request yet.
            txtUserId.Text = AppDefaults.UserId;
            txtTemperature.Text = AppDefaults.Temperature;
            txtMaxTokens.Text = AppDefaults.MaxTokens;
            txtMaxDollars.Text = AppDefaults.MaxDollars;

            foreach (var model in ModelCatalog.ChatModels)
                cmbModel.Items.Add(model);

            foreach (var endPoint in ModelCatalog.WhisperEndPoints)
                cmbVoice.Items.Add(endPoint);

            foreach (var voice in ModelCatalog.Voices)
                cmbAudioVoice.Items.Add(voice);

            foreach (var audioModel in ModelCatalog.TtsModels)
                cmbVoiceModel.Items.Add(audioModel);

            ckbxMute.IsChecked = true;
            ckbxImageReview.IsChecked = false;
            btnPickupFolder.IsEnabled = false;

            cmbModel.SelectedItem = AppDefaults.ChatModel;
            cmbVoice.SelectedItem = AppDefaults.WhisperEndPoint;
            cmbAudioVoice.SelectedItem = AppDefaults.AudioVoice;
            cmbVoiceModel.SelectedItem = AppDefaults.TtsModel;

            ApplyEditorStyle(txtQuestion);
            ApplyEditorStyle(txtAssistantResponse);

            await LoadApiKey();
            await CheckApiKey();
            await InitializeConversationsAsync();

            txtQuestion.Focus();
        }

        private static void ApplyEditorStyle(Control control)
        {
            control.Background = new SolidColorBrush(Colors.LightGray);
            control.Foreground = new SolidColorBrush(Colors.Black);
            control.FontFamily = new FontFamily("Courier New");
            control.FontSize = 15;
        }

        private async Task InitializeConversationsAsync()
        {
            _conversationDb = new ConversationDatabase(_paths.ConversationDatabase);

            var existing = await _conversationDb.GetConversationsAsync();

            if (existing.Count == 0)
            {
                int newId = await _conversationDb.CreateConversationAsync($"Chat {DateTime.Now:yyyy-MM-dd HH:mm}");
                await LoadConversationsAsync(newId);
            }

            else
            {
                await LoadConversationsAsync(existing[0].Id);
            }
        }

        // ─── API key ──────────────────────────────────────────────────────────────

        public async Task LoadApiKey()
        {
            var configuration = new OpenAiConfiguration();
            var (isLoaded, config) = await configuration.LoadFromFileAsync(_paths.ApiKeyFile);

            if (isLoaded && config != null)
            {
                _openAi.ApiKey = config.OpenAiKey;
                Console.WriteLine("OpenAI API Key loaded.");
            }

            else
            {
                Console.WriteLine("Failed to load the OpenAI API Key.");
            }
        }

        private async Task CheckApiKey()
        {
            if (!string.IsNullOrEmpty(_openAi.ApiKey))
                return;

            var request = new OpenAiKeyRequest(_paths.ApiKeyFile, _openAi.ApiKey);
            request.ShowDialog();

            await LoadApiKey();

            AssistantControls.IsEnabled = !string.IsNullOrEmpty(_openAi.ApiKey);
        }

        private async void btnUpdateApiKey_Click(object sender, RoutedEventArgs e)
        {
            var keyManager = new ApiKeyManager(_paths.ApiKeyFile, _openAi.ApiKey);
            keyManager.ShowDialog();

            if (keyManager.KeyWasUpdated)
            {
                await LoadApiKey();
                AssistantControls.IsEnabled = !string.IsNullOrEmpty(_openAi.ApiKey);
            }
        }

        // ─── Sending ──────────────────────────────────────────────────────────────

        private async void OnSendButtonClick(object sender, RoutedEventArgs e)
        {
            AssistantControls.IsEnabled = false;
            btnSend.IsEnabled = false;
            btnClear.IsEnabled = false;

            if (!CostCheck())
                MessageBox.Show("Either the token or the cost threshold is too high for your default settings.\r\n\r\nEither adjust your token/cost threshold, rephrase your question, or change your model.");

            else
            {
                SpinnerStatus.Visibility = Visibility.Visible;
                await SendMessage();
                SpinnerStatus.Visibility = Visibility.Collapsed;
            }

            AssistantControls.IsEnabled = true;
            btnSend.IsEnabled = true;
            btnClear.IsEnabled = true;
            txtQuestion.Focus();
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            _renderer.Clear();
        }

        /// <summary>
        /// Routes the typed question to whichever mode the checkboxes select.
        /// The order of these branches is the app's mode precedence: create-image
        /// wins over image-review, which wins over the muted (text) modes.
        /// </summary>
        private async Task SendMessage()
        {
            string sQuestion = txtQuestion.Text;

            if (string.IsNullOrEmpty(sQuestion))
            {
                MessageBox.Show("Type in your question!");
                txtQuestion.Focus();
                return;
            }

            if (!_renderer.IsEmpty)
                _renderer.Append("\r\n");

            _renderer.AppendResponse("\r\nMe: ", sQuestion);
            txtQuestion.Text = "";

            if (ckbxCreateImage.IsChecked == true)
            {
                await RunWithSpinner(() => GenerateImageAsync(sQuestion));
            }

            else if (ckbxImageReview.IsChecked == true)
            {
                // Content starts out null, so this must not dereference it blindly.
                if (string.IsNullOrEmpty(lblPickupFolder.Content?.ToString()))
                {
                    MessageBox.Show("Please select a folder to pick up images.");
                    return;
                }

                await RunWithSpinner(() => ReviewImageFolderAsync(sQuestion));
            }

            else if (ckbxMute.IsChecked == true)
            {
                if (File.Exists(_currentImageFilePath))
                {
                    try
                    {
                        SpinnerStatus.Visibility = Visibility.Visible;
                        string base64Image = ImageEncoding.ToBase64(_currentImageFilePath!);
                        string answer = await _chat.SendImageAsync(cmbModel.Text, sQuestion, "png", ReadMaxTokens(), base64Image);
                        _renderer.AppendResponse("\r\nChat GPT: ", answer);
                    }

                    catch (Exception ex)
                    {
                        ReportError(ex);
                    }

                    finally
                    {
                        await FileHelper.DeleteAsync(_currentImageFilePath, _log);
                        ImgPreviewImage.Source = null;
                        _currentImageFilePath = null;
                        btnGetImage.IsEnabled = true;
                        btnResetImage.IsEnabled = false;
                        SpinnerStatus.Visibility = Visibility.Collapsed;
                    }
                }

                else if (ckbxTts.IsChecked == true)
                {
                    await RunWithSpinner(() => SpeakAsync(sQuestion));
                }

                else
                {
                    await RunWithSpinner(async () =>
                    {
                        string answer = await SendChatMessageAsync(sQuestion);
                        _renderer.AppendResponse("\r\nChat GPT: ", answer);
                    });
                }
            }

            else if (ckbxTts.IsChecked == true)
            {
                await RunWithSpinner(async () =>
                {
                    _renderer.AppendResponse("\r\nChat GPT Should be repeating this phrase: ", sQuestion);
                    await SpeakAsync(sQuestion);
                });
            }

            // Reached only when nothing is muted and TTS is off: answer, then speak it.
            else
            {
                await RunWithSpinner(async () =>
                {
                    string answer = await SendChatMessageAsync(sQuestion);
                    _renderer.AppendResponse("\r\nChat GPT: ", answer);
                    await SpeakAsync(answer);
                });
            }
        }

        /// <summary>Shows the spinner for the duration of <paramref name="action"/>, reporting any failure.</summary>
        private async Task RunWithSpinner(Func<Task> action)
        {
            try
            {
                SpinnerStatus.Visibility = Visibility.Visible;
                await action();
            }

            catch (Exception ex)
            {
                ReportError(ex);
            }

            finally
            {
                SpinnerStatus.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>Logs an exception and shows it in the output window.</summary>
        private void ReportError(Exception ex)
        {
            _log.Write(ex);
            _renderer.Append("\r\nError: " + ex.Message);
            _renderer.ScrollToEnd();
        }

        /// <summary>
        /// Sends a chat message with the current conversation as history, then stores
        /// both halves of the exchange.
        /// </summary>
        private async Task<string> SendChatMessageAsync(string question)
        {
            string model = cmbModel.Text;
            var history = new List<ChatMessage>();
            bool storing = _conversationDb != null && _currentConversationId > 0;

            if (storing)
            {
                var stored = await _conversationDb!.GetMessagesAsync(_currentConversationId);

                // Name the conversation after the first thing asked in it.
                if (stored.Count == 0)
                {
                    string autoName = question.Length > 50 ? question.Substring(0, 50) + "..." : question;
                    await _conversationDb.RenameConversationAsync(_currentConversationId, autoName);
                }

                foreach (var message in stored)
                    history.Add(new ChatMessage(message.Role, message.Content));
            }

            string answer = await _chat.SendMessageAsync(model, history, question);

            if (storing)
            {
                await _conversationDb!.AddMessageAsync(_currentConversationId, "user", question, model);
                await _conversationDb.AddMessageAsync(_currentConversationId, "assistant", answer, model);
                await RefreshConversationListAsync();
            }

            return answer;
        }

        private async Task GenerateImageAsync(string prompt)
        {
            Directory.CreateDirectory(_paths.ImageCreations);
            _currentImageCreationFilePath = AppPaths.TimestampedFile(_paths.ImageCreations, "GPTIMAGE", "png");

            await _images.GenerateAsync(prompt, _currentImageCreationFilePath);

            _renderer.AppendResponse("\r\nGPT-IMAGE: ", "Below is an image located under: " + _currentImageCreationFilePath + "\r\n");
            _renderer.AppendImage(_currentImageCreationFilePath);
        }

        private async Task ReviewImageFolderAsync(string question)
        {
            string folder = lblPickupFolder.Content?.ToString() ?? "";

            if (!Directory.Exists(folder))
            {
                MessageBox.Show("The folder does not exist.");
                ckbxImageReview.IsChecked = false;
                lblPickupFolder.Content = "";
                return;
            }

            string answer = await _chat.SendImageFolderAsync(cmbModel.Text, question, folder, ReadMaxTokens());
            _renderer.AppendResponse("\r\nChat GPT: ", answer);
            await Task.Delay(5000);

            ckbxImageReview.IsChecked = false;
            lblPickupFolder.Content = "";
        }

        /// <summary>Renders <paramref name="text"/> to speech and plays it.</summary>
        private async Task SpeakAsync(string text)
        {
            Directory.CreateDirectory(_paths.Speech);
            string speechPath = AppPaths.TimestampedFile(_paths.Speech, "Speech", "mp3");

            await _audio.TextToSpeechAsync(speechPath, text, cmbVoiceModel.Text, cmbAudioVoice.Text);
            PlayMp3File(speechPath);
        }

        private int ReadMaxTokens()
        {
            return int.Parse(txtMaxTokens.Text);
        }

        // ─── Screenshot capture ───────────────────────────────────────────────────

        private async void btnGetImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Hide the app so the screenshot captures what's behind it.
                Visibility = Visibility.Hidden;
                await Task.Delay(500);

                _currentImageFilePath = ScreenCapture.CaptureFullScreen(
                    AppPaths.TimestampedFile(_paths.ImageCaptures, "Image", "png"));

                Console.WriteLine($"Image saved to {_currentImageFilePath}");

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = new Uri(_currentImageFilePath, UriKind.Absolute);
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                ImgPreviewImage.Source = bitmapImage;
            }

            catch (Exception ex)
            {
                _log.Write(ex);
                MessageBox.Show($"Error: {ex.Message}");
            }

            finally
            {
                Visibility = Visibility.Visible;
                btnGetImage.IsEnabled = false;
                btnResetImage.IsEnabled = true;
            }
        }

        private async void btnResetImage_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(_currentImageFilePath))
                return;

            try
            {
                ImgPreviewImage.Source = null;
                await FileHelper.DeleteAsync(_currentImageFilePath, _log);
            }

            catch (Exception ex)
            {
                ReportError(ex);
            }

            finally
            {
                _currentImageFilePath = null;
                btnGetImage.IsEnabled = true;
                btnResetImage.IsEnabled = false;
            }
        }

        private void btnPickupFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                lblPickupFolder.Content = folderBrowserDialog.SelectedPath;
        }

        // ─── Playback ─────────────────────────────────────────────────────────────

        private void PlayMp3File(string filePath)
        {
            try
            {
                _mediaPlayer.Open(new Uri(filePath));
                _mediaPlayer.Play();
                _currentPlayingFilePath = filePath;
            }

            catch (Exception ex)
            {
                _log.Write(ex);
                MessageBox.Show($"Playback Exception: {ex.Message}");
                _ = FileHelper.DeleteAsync(filePath, _log);
            }
        }

        private async void MediaPlayer_MediaEnded(object? sender, EventArgs e)
        {
            await Task.Delay(500); // let the player release the file before deleting it
            await FileHelper.DeleteAsync(_currentPlayingFilePath, _log);
        }

        private void MediaPlayer_MediaFailed(object? sender, EventArgs e)
        {
            MessageBox.Show("Error playback on file: " + _currentPlayingFilePath);
        }

        // ─── Listening modes ──────────────────────────────────────────────────────

        private void ckbxListeningMode_Checked(object sender, RoutedEventArgs e)
        {
            lblRecordLength.Content = "Record Max 30 Seconds";
            btnSend.IsEnabled = false;
            btnClear.IsEnabled = false;
            btnGetImage.IsEnabled = false;
            cmbVoice.IsEnabled = false;
            cmbAudioVoice.IsEnabled = false;
            cmbModel.IsEnabled = false;
            ckbxMute.IsChecked = true;
            ckbxMute.IsEnabled = false;
            ckbxTts.IsEnabled = false;
            ckbxCreateImage.IsEnabled = false;
            ckbxImageReview.IsEnabled = false;
            ckbxContinuousListeningMode.IsEnabled = false;

            ResetCountdown(AppDefaults.StandardListeningSeconds);
            _listeningMode = "Standard";

            StartAudioRecording();
            _countdownTimer.Start();
        }

        private async void ckbxListeningMode_Unchecked(object sender, RoutedEventArgs e)
        {
            SpinnerStatus.Visibility = Visibility.Visible;
            _countdownTimer.Stop();
            StopAudioRecording();

            string whisperType = cmbVoice.Text;
            var response = await _audio.TranscribeAsync(_capture.CurrentRecordingPath!, AppDefaults.WhisperModel, whisperType);

            if (response != null)
            {
                if (ckbxMute.IsChecked == true)
                {
                    try
                    {
                        _renderer.AppendResponse("\r\nWhisper " + TextFormatting.ToProperCase(whisperType) + ":\r\n ", response);
                    }

                    catch (Exception ex)
                    {
                        ReportError(ex);
                    }
                }

                else
                {
                    try
                    {
                        _renderer.AppendResponse("\r\nWhisper Translate: ", response);
                        await SpeakAsync(response);
                    }

                    catch (Exception ex)
                    {
                        ReportError(ex);
                    }
                }
            }

            cmbVoice.IsEnabled = true;
            cmbAudioVoice.IsEnabled = true;
            cmbModel.IsEnabled = true;
            ckbxMute.IsEnabled = true;
            btnSend.IsEnabled = true;
            btnClear.IsEnabled = true;
            ckbxImageReview.IsEnabled = true;
            ckbxCreateImage.IsEnabled = true;
            ckbxContinuousListeningMode.IsEnabled = true;
            btnGetImage.IsEnabled = true;
            ListeningModeProgressBar.Value = 0;
            SpinnerStatus.Visibility = Visibility.Collapsed;
            lblRecordLength.Content = "Record Timer";
        }

        private void ckbxSttMode_Checked(object sender, RoutedEventArgs e)
        {
            lblRecordLength.Content = "Loop Records Every 5 Seconds";
            DisableUI();

            ResetCountdown(AppDefaults.ContinuousListeningSeconds);
            _listeningMode = "Continuous";

            _capture.ClearQueue();
            StartAudioRecording();
            _countdownTimer.Start();
        }

        private async void ckbxSttModeMode_Unchecked(object sender, RoutedEventArgs e)
        {
            SpinnerStatus.Visibility = Visibility.Visible;
            _countdownTimer.Stop();
            StopAudioRecording();

            // Drains newest-first, which is also the order the transcripts get appended in.
            for (int i = _capture.Queue.Count - 1; i >= 0; i--)
            {
                await TranscribeQueuedFileAsync(_capture.Queue[i], useTranslatePrefix: true);
                _capture.RemoveFromQueue(i);
            }

            EnableUI();
            SpinnerStatus.Visibility = Visibility.Collapsed;
            ListeningModeProgressBar.Value = 0;
        }

        /// <summary>
        /// Transcribes one queued recording and renders it, speaking it back when
        /// the app isn't muted.
        /// </summary>
        private async Task TranscribeQueuedFileAsync(string audioFile, bool useTranslatePrefix)
        {
            string whisperType = cmbVoice.Text;
            var response = await _audio.TranscribeAsync(audioFile, AppDefaults.WhisperModel, whisperType);

            if (response == null)
                return;

            if (ckbxMute.IsChecked == true)
            {
                try
                {
                    _renderer.AppendResponse("", response);
                }

                catch (Exception ex)
                {
                    ReportError(ex);
                }
            }

            else
            {
                try
                {
                    _renderer.AppendResponse(useTranslatePrefix ? "Whisper Translate: " : "", response);
                    await SpeakAsync(response);
                }

                catch (Exception ex)
                {
                    ReportError(ex);
                }
            }
        }

        /// <summary>
        /// Drains the queue mid-session for continuous mode.
        ///
        /// NOTE: removing at <c>i</c> while walking forwards skips the next entry —
        /// preserved from the original so this refactor stays behaviour-for-behaviour.
        /// See the follow-up notes before changing it.
        /// </summary>
        private async Task ContinuousSstAsync()
        {
            for (int i = 0; i <= _capture.Queue.Count - 1; i++)
            {
                await TranscribeQueuedFileAsync(_capture.Queue[i], useTranslatePrefix: false);
                _capture.RemoveFromQueue(i);
            }
        }

        private void StartAudioRecording()
        {
            try
            {
                _capture.StartRecording(_paths.Recordings);
            }

            catch (Exception ex)
            {
                _log.Write(ex);
                MessageBox.Show($"An error occurred while starting recording: {ex.Message}");
            }
        }

        private void StopAudioRecording()
        {
            try
            {
                _capture.StopAll();
            }

            catch (Exception ex)
            {
                _log.Write(ex);
                MessageBox.Show($"An error occurred while stopping recording: {ex.Message}");
            }
        }

        // ─── Countdown timer ──────────────────────────────────────────────────────

        private void InitializeCountdownTimer()
        {
            _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _countdownTimer.Tick += CountdownTimer_Tick;
            ListeningModeProgressBar.Maximum = _countdownValue;
        }

        private void ResetCountdown(int seconds)
        {
            _countdownValue = seconds;
            ListeningModeProgressBar.Value = seconds;
            ListeningModeProgressBar.Maximum = seconds;
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            _countdownValue--;
            ListeningModeProgressBar.Value = _countdownValue;

            if (_countdownValue > 0)
                return;

            if (_listeningMode == "Standard")
            {
                _countdownTimer.Stop();
                ResetCountdown(AppDefaults.StandardListeningSeconds);
                ckbxListeningMode.IsChecked = false; // triggers Unchecked, which transcribes
            }

            else if (_listeningMode == "Continuous")
            {
                _countdownTimer.Stop();
                StopAudioRecording();

                // Deliberately not awaited: the next chunk starts recording immediately
                // while the previous one transcribes in the background.
                _ = ContinuousSstAsync();

                ResetCountdown(AppDefaults.ContinuousListeningSeconds);
                _countdownTimer.Start();
                StartAudioRecording();
            }
        }

        // ─── Checkbox interlocks ──────────────────────────────────────────────────

        private void DisableUI()
        {
            btnSend.IsEnabled = false;
            btnClear.IsEnabled = false;
            btnGetImage.IsEnabled = false;
            cmbVoice.IsEnabled = false;
            cmbAudioVoice.IsEnabled = false;
            cmbModel.IsEnabled = false;
            ckbxMute.IsChecked = true;
            ckbxMute.IsEnabled = false;
            ckbxTts.IsChecked = false;
            ckbxTts.IsEnabled = false;
            ckbxCreateImage.IsEnabled = false;
            ckbxImageReview.IsEnabled = false;
            ckbxListeningMode.IsEnabled = false;
        }

        private void EnableUI()
        {
            btnSend.IsEnabled = true;
            btnClear.IsEnabled = true;
            btnGetImage.IsEnabled = true;
            cmbVoice.IsEnabled = true;
            cmbAudioVoice.IsEnabled = true;
            cmbModel.IsEnabled = true;
            ckbxMute.IsEnabled = true;
            ckbxCreateImage.IsEnabled = true;
            ckbxImageReview.IsEnabled = true;
            ckbxListeningMode.IsEnabled = true;
            lblRecordLength.Content = "Record Timer";
        }

        private void ckbxCreateImage_Checked(object sender, RoutedEventArgs e)
        {
            ckbxImageReview.IsEnabled = false;
            ckbxMute.IsChecked = true;
            ckbxMute.IsEnabled = false;
            ckbxTts.IsChecked = false;
            ckbxTts.IsEnabled = false;
            ckbxListeningMode.IsChecked = false;
            ckbxListeningMode.IsEnabled = false;
            ckbxContinuousListeningMode.IsChecked = false;
            ckbxContinuousListeningMode.IsEnabled = false;
        }

        private void ckbxCreateImage_Unchecked(object sender, RoutedEventArgs e)
        {
            ckbxMute.IsEnabled = true;
            ckbxListeningMode.IsEnabled = true;
            ckbxContinuousListeningMode.IsEnabled = true;
            ckbxImageReview.IsEnabled = true;
        }

        private void ckbxImageReview_Checked(object sender, RoutedEventArgs e)
        {
            ckbxMute.IsChecked = true;
            ckbxMute.IsEnabled = false;
            ckbxListeningMode.IsEnabled = false;
            ckbxCreateImage.IsEnabled = false;
            ckbxContinuousListeningMode.IsEnabled = false;
            btnPickupFolder.IsEnabled = true;
            txtQuestion.AppendText(AppDefaults.ImageReviewInstructions);
        }

        private void ckbxImageReview_Unchecked(object sender, RoutedEventArgs e)
        {
            ckbxMute.IsEnabled = true;
            ckbxListeningMode.IsEnabled = true;
            ckbxCreateImage.IsEnabled = true;
            ckbxContinuousListeningMode.IsEnabled = true;
            btnPickupFolder.IsEnabled = false;
            txtQuestion.Text = txtQuestion.Text.Replace(AppDefaults.ImageReviewInstructions, "");
            lblPickupFolder.Content = "";
        }

        private void ckbxMute_Checked(object sender, RoutedEventArgs e)
        {
            ckbxTts.IsEnabled = false;
        }

        private void ckbxMute_Unchecked(object sender, RoutedEventArgs e)
        {
            ckbxTts.IsEnabled = true;
        }

        private void ckbxckbxTts_Checked(object sender, RoutedEventArgs e)
        {
            ckbxMute.IsEnabled = false;
            ckbxListeningMode.IsEnabled = false;
            ckbxContinuousListeningMode.IsEnabled = false;
            ckbxImageReview.IsEnabled = false;
            ckbxCreateImage.IsChecked = false;
            ckbxCreateImage.IsEnabled = false;
            btnGetImage.IsEnabled = false;
        }

        private void ckbxckbxTts_Unchecked(object sender, RoutedEventArgs e)
        {
            ckbxMute.IsEnabled = true;
            ckbxListeningMode.IsEnabled = true;
            ckbxContinuousListeningMode.IsEnabled = true;
            ckbxImageReview.IsEnabled = true;
            ckbxCreateImage.IsEnabled = true;
            btnGetImage.IsEnabled = true;
        }

        private void cmbVoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbVoice.SelectedItem?.ToString() == "transcriptions")
                ckbxMute.IsChecked = true;

            else
                ckbxMute.IsEnabled = true;
        }

        // ─── Cost estimation ──────────────────────────────────────────────────────

        private void txtQuestion_TextChanged(object sender, TextChangedEventArgs e)
        {
            TokenCheck();
        }

        private void cmbModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TokenCheck();
        }

        /// <summary>True when the pending question is within both the token and dollar caps.</summary>
        private bool CostCheck()
        {
            return _tokenCount < Convert.ToInt32(txtMaxTokens.Text)
                && _estimatedCost < Convert.ToDouble(txtMaxDollars.Text);
        }

        private void TokenCheck()
        {
            _tokenCount = TokenEstimator.CountTokens(txtQuestion.Text);
            _estimatedCost = PricingTable.EstimateCost(_tokenCount, cmbModel.SelectedItem?.ToString());

            lblEstimatedTokens.Content = "Estimated Tokens = " + _tokenCount;
            lblEstimatedCost.Content = $"Estimated Cost = ${_estimatedCost:F2}";
        }

        // ─── API status light ─────────────────────────────────────────────────────

        private void StartApiStatusTimer()
        {
            _apiCheckTimer = new System.Timers.Timer(AppDefaults.ApiStatusCheckIntervalMs) { Enabled = true };
            _apiCheckTimer.Elapsed += ApiStatusTimerElapsed;
            _apiCheckTimer.Start();
        }

        private async void ApiStatusTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _apiCheckTimer?.Stop();
            await RefreshApiStatusAsync();
            _apiCheckTimer?.Start();
        }

        private async Task RefreshApiStatusAsync()
        {
            var result = await _apiStatus.CheckAsync();

            _apiStatusText = result.StatusText;
            UpdateTrafficLight(result.Light);

            if (result.NetworkError != null)
                Dispatcher.Invoke(() => MessageBox.Show($"Error: {result.NetworkError.Message}"));
        }

        public void UpdateTrafficLight(TrafficLight light)
        {
            Dispatcher.Invoke(() =>
            {
                RedLight.Fill = _redOff;
                YellowLight.Fill = _yellowOff;
                GreenLight.Fill = _greenOff;
                ApiStatusTextBlock.Text = _apiStatusText;

                switch (light)
                {
                    case TrafficLight.Red:
                        RedLight.Fill = _redOn;
                        break;
                    case TrafficLight.Yellow:
                        YellowLight.Fill = _yellowOn;
                        break;
                    case TrafficLight.Green:
                        GreenLight.Fill = _greenOn;
                        break;
                }
            });
        }

        // ─── Conversation management ──────────────────────────────────────────────

        /// <summary>Loads all conversations into the ComboBox and optionally selects one by Id.</summary>
        private async Task LoadConversationsAsync(int? selectId = null)
        {
            if (_conversationDb == null)
                return;

            _isLoadingConversation = true;

            try
            {
                var conversations = await _conversationDb.GetConversationsAsync();
                cmbConversation.Items.Clear();

                foreach (var conversation in conversations)
                    cmbConversation.Items.Add(conversation);

                if (conversations.Count > 0)
                {
                    var toSelect = selectId.HasValue
                        ? conversations.Find(c => c.Id == selectId.Value) ?? conversations[0]
                        : conversations[0];

                    cmbConversation.SelectedItem = toSelect;
                    _currentConversationId = toSelect.Id;
                    await LoadConversationHistoryAsync(toSelect.Id);
                }
            }

            finally
            {
                _isLoadingConversation = false;
            }
        }

        /// <summary>Refreshes the conversation list without reloading the output window.</summary>
        private async Task RefreshConversationListAsync()
        {
            if (_conversationDb == null)
                return;

            _isLoadingConversation = true;

            try
            {
                int currentId = _currentConversationId;
                var conversations = await _conversationDb.GetConversationsAsync();
                cmbConversation.Items.Clear();

                foreach (var conversation in conversations)
                    cmbConversation.Items.Add(conversation);

                var toSelect = conversations.Find(c => c.Id == currentId);

                if (toSelect != null)
                    cmbConversation.SelectedItem = toSelect;
            }

            finally
            {
                _isLoadingConversation = false;
            }
        }

        /// <summary>Rebuilds the output window from stored messages for the given conversation.</summary>
        private async Task LoadConversationHistoryAsync(int conversationId)
        {
            if (_conversationDb == null)
                return;

            _renderer.Clear();

            foreach (var message in await _conversationDb.GetMessagesAsync(conversationId))
            {
                string prefix = message.Role == "user" ? "\r\nMe: " : "\r\nChat GPT: ";
                _renderer.Append(prefix + message.Content);
            }

            _renderer.ScrollToEnd();
        }

        private async void cmbConversation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingConversation)
                return;

            if (cmbConversation.SelectedItem is ConversationEntry entry)
            {
                _currentConversationId = entry.Id;
                await LoadConversationHistoryAsync(entry.Id);
            }
        }

        private async void btnNewConversation_Click(object sender, RoutedEventArgs e)
        {
            if (_conversationDb == null)
                return;

            int newId = await _conversationDb.CreateConversationAsync($"Chat {DateTime.Now:yyyy-MM-dd HH:mm}");
            await LoadConversationsAsync(newId);
            _renderer.Clear();
        }

        private async void btnDeleteConversation_Click(object sender, RoutedEventArgs e)
        {
            if (_conversationDb == null || _currentConversationId == 0)
                return;

            var result = MessageBox.Show(
                "Delete this conversation and all its messages?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            await _conversationDb.DeleteConversationAsync(_currentConversationId);
            _currentConversationId = 0;

            var remaining = await _conversationDb.GetConversationsAsync();

            if (remaining.Count == 0)
            {
                int newId = await _conversationDb.CreateConversationAsync($"Chat {DateTime.Now:yyyy-MM-dd HH:mm}");
                await LoadConversationsAsync(newId);
            }

            else
            {
                await LoadConversationsAsync(remaining[0].Id);
            }

            _renderer.Clear();
        }
    }
}
