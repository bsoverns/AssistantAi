using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AssistantAi.Classes;

namespace AssistantAi
{
    /// <summary>
    /// Allows updating the OpenAI API key with validation testing before saving.
    /// </summary>
    public partial class ApiKeyManager : Window
    {
        private readonly string _apiKeyFilePath;
        private readonly string _originalApiKey;
        private bool _testPassed = false;

        public bool KeyWasUpdated { get; private set; } = false;

        public ApiKeyManager(string apiKeyFilePath, string currentApiKey)
        {
            InitializeComponent();
            _apiKeyFilePath = apiKeyFilePath;
            _originalApiKey = currentApiKey ?? string.Empty;
            pwdCurrentKey.Password = _originalApiKey;
        }

        // --- Toggle current key visibility ---

        private void btnToggleCurrent_Click(object sender, RoutedEventArgs e)
        {
            if (txtCurrentKeyVisible.Visibility == Visibility.Collapsed)
            {
                txtCurrentKeyVisible.Text = pwdCurrentKey.Password;
                txtCurrentKeyVisible.Visibility = Visibility.Visible;
                pwdCurrentKey.Visibility = Visibility.Collapsed;
                btnToggleCurrent.Content = "Hide";
            }
            else
            {
                txtCurrentKeyVisible.Visibility = Visibility.Collapsed;
                pwdCurrentKey.Visibility = Visibility.Visible;
                btnToggleCurrent.Content = "Show";
            }
        }

        // --- Toggle new key visibility ---

        private void btnToggleNew_Click(object sender, RoutedEventArgs e)
        {
            if (txtNewKeyVisible.Visibility == Visibility.Collapsed)
            {
                txtNewKeyVisible.Text = pwdNewKey.Password;
                txtNewKeyVisible.Visibility = Visibility.Visible;
                pwdNewKey.Visibility = Visibility.Collapsed;
                btnToggleNew.Content = "Hide";
            }
            else
            {
                pwdNewKey.Password = txtNewKeyVisible.Text;
                pwdNewKey.Visibility = Visibility.Visible;
                txtNewKeyVisible.Visibility = Visibility.Collapsed;
                btnToggleNew.Content = "Show";
            }
        }

        // --- Reset test state when user edits the new key ---

        private void pwdNewKey_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ResetTestState();
        }

        private void txtNewKeyVisible_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ResetTestState();
        }

        private void ResetTestState()
        {
            _testPassed = false;
            btnSaveKey.IsEnabled = false;
            lblStatus.Content = string.Empty;
        }

        // --- Retrieve the new key from whichever control is active ---

        private string GetNewKey()
        {
            return pwdNewKey.Visibility == Visibility.Visible
                ? pwdNewKey.Password
                : txtNewKeyVisible.Text;
        }

        // --- Test the new key ---

        private async void btnTestKey_Click(object sender, RoutedEventArgs e)
        {
            string newKey = GetNewKey();

            if (string.IsNullOrWhiteSpace(newKey))
            {
                SetStatus("Please enter a new API key before testing.", false);
                return;
            }

            if (newKey == _originalApiKey)
            {
                SetStatus("The new key is identical to the current key. Enter a different key.", false);
                return;
            }

            btnTestKey.IsEnabled = false;
            SetStatus("Testing API key — please wait...", null);

            bool valid = await TestApiKeyAsync(newKey);

            btnTestKey.IsEnabled = true;
            _testPassed = valid;

            if (valid)
            {
                SetStatus("API key is valid and accepted by OpenAI.", true);
                btnSaveKey.IsEnabled = true;
            }
            else
            {
                SetStatus("API key test failed. The key may be invalid, expired, or there is no network connection.", false);
                btnSaveKey.IsEnabled = false;
            }
        }

        private async Task<bool> TestApiKeyAsync(string apiKey)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                var response = await httpClient.GetAsync("https://api.openai.com/v1/models");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // --- Save the new key ---

        private async void btnSaveKey_Click(object sender, RoutedEventArgs e)
        {
            string newKey = GetNewKey();

            if (string.IsNullOrWhiteSpace(newKey))
            {
                SetStatus("Cannot save an empty key.", false);
                return;
            }

            if (newKey == _originalApiKey)
            {
                SetStatus("The new key matches the current key. No changes saved.", false);
                return;
            }

            if (!_testPassed)
            {
                SetStatus("Please test the key successfully before saving.", false);
                return;
            }

            var config = new OpenAiConfiguration.OpenAiData { OpenAiKey = newKey };
            var workBench = new OpenAiConfiguration();
            bool saved = await workBench.SaveToFileAsync(_apiKeyFilePath, config);

            if (saved)
            {
                KeyWasUpdated = true;
                SetStatus("API key saved successfully.", true);
                this.Close();
            }
            else
            {
                SetStatus("Failed to save the API key. Check file permissions.", false);
            }
        }

        // --- Cancel ---

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // --- Status helper ---

        private void SetStatus(string message, bool? success)
        {
            lblStatus.Content = message;
            lblStatus.Foreground = success switch
            {
                true => new SolidColorBrush(Colors.Green),
                false => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Black)
            };
        }
    }
}
