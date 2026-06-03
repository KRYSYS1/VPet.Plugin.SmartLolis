using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace VPet.Plugin.SmartLolis
{
    public partial class SmartLolisSettingsWindow : Window
    {
        private readonly SmartLolisPlugin _plugin;
        private string _currentUiLanguage = "ru";
        private string _currentLlmProvider = "Groq";
        private string _currentTtsProvider = "ElevenLabs";

        private readonly List<Button> _llmProviderButtons = new();
        private readonly List<Button> _ttsProviderButtons = new();

        private static readonly Dictionary<string, string> TtsKeyUrls = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ElevenLabs"] = "https://elevenlabs.io/app/settings/api-keys",
            ["Google"] = "https://console.cloud.google.com/apis/credentials",
            ["Polly"] = "https://aws.amazon.com/polly/",
        };

        private static readonly Dictionary<string, string> TtsVoiceUrls = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ElevenLabs"] = "https://elevenlabs.io/app/voice-lab",
        };

        private static readonly Dictionary<string, string> ProviderKeyUrls = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Ollama"] = "",
            ["OpenAI"] = "https://platform.openai.com/api-keys",
            ["Groq"] = "https://console.groq.com/keys",
            ["NVIDIA"] = "https://build.nvidia.com/",
            ["Google"] = "https://aistudio.google.com/app/apikey",
            ["GitHub"] = "https://github.com/settings/tokens",
            ["Cohere"] = "https://dashboard.cohere.com/api-keys",
            ["Cerebras"] = "https://cloud.cerebras.ai/",
            ["Mistral"] = "https://console.mistral.ai/api-keys/",
            ["OpenRouter"] = "https://openrouter.ai/keys",
            ["LM Studio"] = "",
            ["Custom"] = "",
        };

        private static readonly Dictionary<string, string[]> ProviderModelPresets = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Ollama"] = new[] { "llama3.1:8b", "llama3.1:70b", "gemma2:9b", "mistral:7b" },
            ["OpenAI"] = new[] { "gpt-4o-mini", "gpt-4o", "gpt-3.5-turbo" },
            ["Groq"] = new[] { "llama-3.3-70b-versatile", "llama-3.1-8b-instant", "mixtral-8x7b-32768", "gemma2-9b-it" },
            ["NVIDIA"] = new[] { "meta/llama3-70b-instruct", "meta/llama3-8b-instruct" },
            ["Google"] = new[] { "gemini-1.5-flash", "gemini-1.5-pro" },
            ["GitHub"] = new[] { "gpt-4o-mini", "Phi-3-medium-128k-instruct" },
            ["Cohere"] = new[] { "command-r", "command-r-plus" },
            ["Cerebras"] = new[] { "llama-3.3-70b", "llama-3.1-8b" },
            ["Mistral"] = new[] { "mistral-large-latest", "mistral-small-latest", "codestral-latest" },
            ["OpenRouter"] = new[] { "openrouter/free", "meta-llama/llama-3.3-8b-instruct:free", "microsoft/mai-ds-r1:free" },
            ["LM Studio"] = Array.Empty<string>(),
            ["Custom"] = Array.Empty<string>(),
        };

        public SmartLolisSettingsWindow(SmartLolisPlugin plugin)
        {
            InitializeComponent();
            _plugin = plugin;
            InitProviderButtons();
            LoadLocalWindowsVoices();
            LoadSettings();
        }

        private void InitProviderButtons()
        {
            _llmProviderButtons.Add(btnProvOllama);
            _llmProviderButtons.Add(btnProvOpenAI);
            _llmProviderButtons.Add(btnProvGroq);
            _llmProviderButtons.Add(btnProvNVIDIA);
            _llmProviderButtons.Add(btnProvGoogle);
            _llmProviderButtons.Add(btnProvGitHub);
            _llmProviderButtons.Add(btnProvCohere);
            _llmProviderButtons.Add(btnProvCerebras);
            _llmProviderButtons.Add(btnProvMistral);
            _llmProviderButtons.Add(btnProvOpenRouter);
            _llmProviderButtons.Add(btnProvLMStudio);
            _llmProviderButtons.Add(btnProvCustom);

            _ttsProviderButtons.Add(btnTtsElevenLabs);
            _ttsProviderButtons.Add(btnTtsGoogle);
            _ttsProviderButtons.Add(btnTtsLocalWindows);
            _ttsProviderButtons.Add(btnTtsPolly);
        }

        private void LoadSettings()
        {
            var s = _plugin.PluginSettings;
            s.EnsureDefaults();
            SelectLlmProvider(string.IsNullOrWhiteSpace(s.LlmProvider) ? "Groq" : s.LlmProvider);
            SelectTtsProvider(string.IsNullOrWhiteSpace(s.TtsProvider) ? "ElevenLabs" : s.TtsProvider);
            txtElevenLabsApiKey.Password = s.ElevenLabsApiKey ?? string.Empty;
            cmbElevenLabsVoiceId.Text = s.ElevenLabsVoiceId ?? string.Empty;
            txtGoogleApiKey.Password = s.GoogleApiKey ?? string.Empty;
            cmbGoogleVoiceName.Text = string.IsNullOrWhiteSpace(s.GoogleVoiceName) ? "ru-RU-Standard-A" : s.GoogleVoiceName;
            cmbLocalWindowsVoiceName.Text = s.LocalWindowsVoiceName ?? string.Empty;
            txtPollyAccessKey.Text = s.PollyAccessKey ?? string.Empty;
            txtPollySecretKey.Password = s.PollySecretKey ?? string.Empty;
            cmbPollyRegion.Text = string.IsNullOrWhiteSpace(s.PollyRegion) ? "us-east-1" : s.PollyRegion;
            cmbPollyVoiceId.Text = string.IsNullOrWhiteSpace(s.PollyVoiceId) ? "Tatyana" : s.PollyVoiceId;
            SelectUiLanguage(string.IsNullOrWhiteSpace(s.UiLanguage) ? "ru" : s.UiLanguage);
            txtMaxTokens.Text = (s.MaxTokens > 0 ? s.MaxTokens : 512).ToString();
            txtMaxHistory.Text = (s.MaxHistoryMessages > 0 ? s.MaxHistoryMessages : 16).ToString();
            chkLlm.IsChecked = s.EnableLlm;
            chkStreaming.IsChecked = s.EnableStreaming;
            chkTts.IsChecked = s.EnableTts;
            chkCommandMode.IsChecked = s.EnableCommandMode;
            chkVoiceInputButton.IsChecked = s.EnableVoiceInputButton;
            txtSystemPrompt.Text = s.SystemPrompt ?? string.Empty;
        }

        private void SnapshotCurrentLlmConfig()
        {
            var s = _plugin.PluginSettings;
            s.EnsureDefaults();
            if (s.LlmProviderConfigs.TryGetValue(_currentLlmProvider, out var config))
            {
                config.ApiUrl = txtLlmApiUrl.Text ?? "";
                config.ApiKey = txtLlmApiKey.Password ?? "";
                config.Model = cmbLlmModel.Text ?? "";
            }
        }

        private void LoadLlmProviderConfig(string provider)
        {
            var s = _plugin.PluginSettings;
            s.EnsureDefaults();
            if (!s.LlmProviderConfigs.TryGetValue(provider, out var config))
                config = s.LlmProviderConfigs["Groq"];

            txtLlmApiUrl.Text = config.ApiUrl ?? "";
            txtLlmApiKey.Password = config.ApiKey ?? "";
            cmbLlmModel.Text = config.Model ?? "";

            bool isCustom = string.Equals(provider, "Custom", StringComparison.OrdinalIgnoreCase);
            bool isLocal = string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(provider, "LM Studio", StringComparison.OrdinalIgnoreCase);
            txtLlmApiUrl.IsReadOnly = !(isCustom || isLocal);
            txtLlmApiUrl.Opacity = txtLlmApiUrl.IsReadOnly ? 0.6 : 1.0;

            if (ProviderKeyUrls.TryGetValue(provider, out var keyUrl) && !string.IsNullOrEmpty(keyUrl))
            {
                btnGetApiKey.Visibility = Visibility.Visible;
                btnGetApiKey.Tag = keyUrl;
            }
            else
            {
                btnGetApiKey.Visibility = Visibility.Collapsed;
            }

            cmbLlmModel.Items.Clear();
            if (ProviderModelPresets.TryGetValue(provider, out var presets))
            {
                foreach (var p in presets)
                    cmbLlmModel.Items.Add(new ComboBoxItem { Content = p });
            }
            cmbLlmModel.IsEditable = true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var s = _plugin.PluginSettings;
            SnapshotCurrentLlmConfig();
            s.LlmProvider = GetSelectedLlmProvider();
            s.TtsProvider = GetSelectedTtsProvider();
            s.ElevenLabsApiKey = txtElevenLabsApiKey.Password;
            s.ElevenLabsVoiceId = cmbElevenLabsVoiceId.Text;
            s.GoogleApiKey = txtGoogleApiKey.Password;
            s.GoogleVoiceName = cmbGoogleVoiceName.Text;
            s.LocalWindowsVoiceName = cmbLocalWindowsVoiceName.Text;
            s.PollyAccessKey = txtPollyAccessKey.Text;
            s.PollySecretKey = txtPollySecretKey.Password;
            s.PollyRegion = cmbPollyRegion.Text;
            s.PollyVoiceId = cmbPollyVoiceId.Text;
            s.UiLanguage = GetSelectedUiLanguage();
            s.EnableLlm = chkLlm.IsChecked ?? true;
            s.EnableStreaming = chkStreaming.IsChecked ?? true;
            s.EnableTts = chkTts.IsChecked ?? true;
            s.EnableCommandMode = chkCommandMode.IsChecked ?? true;
            s.EnableVoiceInputButton = chkVoiceInputButton.IsChecked ?? true;
            s.SystemPrompt = txtSystemPrompt.Text;

            if (int.TryParse(txtMaxTokens.Text, out int maxTokens) && maxTokens > 0)
                s.MaxTokens = maxTokens;

            if (int.TryParse(txtMaxHistory.Text, out int maxHistory) && maxHistory > 0)
                s.MaxHistoryMessages = maxHistory;

            _plugin.Save();
            _plugin.TalkBoxInstance?.RefreshUiFromSettings();
            ShowStatus("Settings saved.", StatusKind.Info);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _plugin.LlmService?.ClearHistory();
            ShowStatus("Chat history cleared.", StatusKind.Error);
        }

        private void BtnOpenConsole_Click(object sender, RoutedEventArgs e)
        {
            new SmartLolisLogWindow().Show();
        }

        private void BtnCommandInfo_Click(object sender, RoutedEventArgs e)
        {
            new SmartLolisCommandInfoWindow(GetSelectedUiLanguage()).Show();
        }

        private void ApplyFormToSettings()
        {
            var s = _plugin.PluginSettings;
            SnapshotCurrentLlmConfig();
            s.LlmProvider = GetSelectedLlmProvider();
            s.TtsProvider = GetSelectedTtsProvider();
            s.ElevenLabsApiKey = txtElevenLabsApiKey.Password;
            s.ElevenLabsVoiceId = cmbElevenLabsVoiceId.Text;
            s.GoogleApiKey = txtGoogleApiKey.Password;
            s.GoogleVoiceName = cmbGoogleVoiceName.Text;
            s.LocalWindowsVoiceName = cmbLocalWindowsVoiceName.Text;
            s.PollyAccessKey = txtPollyAccessKey.Text;
            s.PollySecretKey = txtPollySecretKey.Password;
            s.PollyRegion = cmbPollyRegion.Text;
            s.PollyVoiceId = cmbPollyVoiceId.Text;
            s.UiLanguage = GetSelectedUiLanguage();
            s.EnableLlm = chkLlm.IsChecked ?? true;
            s.EnableStreaming = chkStreaming.IsChecked ?? true;
            s.EnableTts = chkTts.IsChecked ?? true;
            s.EnableCommandMode = chkCommandMode.IsChecked ?? true;
            s.EnableVoiceInputButton = chkVoiceInputButton.IsChecked ?? true;
            s.SystemPrompt = txtSystemPrompt.Text;

            if (int.TryParse(txtMaxTokens.Text, out int maxTokens) && maxTokens > 0)
                s.MaxTokens = maxTokens;

            if (int.TryParse(txtMaxHistory.Text, out int maxHistory) && maxHistory > 0)
                s.MaxHistoryMessages = maxHistory;
        }

        private async void BtnTestLlm_Click(object sender, RoutedEventArgs e)
        {
            ApplyFormToSettings();
            _plugin.Save();

            try
            {
                if (!(_plugin.PluginSettings.EnableLlm))
                {
                    ShowStatus("LLM is disabled. Turn it on first.", StatusKind.Warning);
                    return;
                }

                SmartLolisLog.Info("Manual LLM test started from settings window.");
                string response = await _plugin.LlmService.SendMessageAsync("Say: Smart Lolis LLM test OK.");
                ShowStatus(
                    string.IsNullOrWhiteSpace(response) ? "LLM test returned an empty response." : response,
                    StatusKind.Success);
            }
            catch (System.Exception ex)
            {
                SmartLolisLog.Error("Manual LLM test failed.", ex);
                ShowStatus(ex.Message, StatusKind.Error);
            }
        }

        private async void BtnTestTts_Click(object sender, RoutedEventArgs e)
        {
            ApplyFormToSettings();
            _plugin.Save();

            try
            {
                SmartLolisLog.Info("Manual TTS test started from settings window.");
                await _plugin.TtsService.SpeakAsync("Smart Lolis voice test.");
                ShowStatus("TTS test request sent. Check the console log if you hear nothing.", StatusKind.Success);
            }
            catch (System.Exception ex)
            {
                SmartLolisLog.Error("Manual TTS test failed.", ex);
                ShowStatus(ex.Message, StatusKind.Error);
            }
        }

        private void CmbTtsProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTtsProviderUi();
        }

        private void BtnProv_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string provider)
            {
                SnapshotCurrentLlmConfig();
                _currentLlmProvider = provider;
                _plugin.PluginSettings.LlmProvider = provider;
                SelectLlmProvider(provider);
            }
        }

        private void BtnGetApiKey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string url && !string.IsNullOrEmpty(url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    SmartLolisLog.Error("Failed to open API key URL.", ex);
                }
            }
        }

        private void CmbUiLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyLocalization(GetSelectedUiLanguage());
        }

        private void UpdateLlmProviderUi()
        {
            string provider = _currentLlmProvider;
            foreach (var btn in _llmProviderButtons)
            {
                bool isActive = string.Equals(btn.Tag?.ToString(), provider, StringComparison.OrdinalIgnoreCase);
                btn.Style = isActive ? (Style)FindResource("ProviderButtonActiveStyle") : (Style)FindResource("ProviderButtonStyle");
            }
        }

        private void UpdateTtsProviderUi()
        {
            string provider = _currentTtsProvider.Trim().ToLowerInvariant();

            panelElevenLabs.Visibility = provider == "elevenlabs" ? Visibility.Visible : Visibility.Collapsed;
            panelLocalWindows.Visibility = provider == "local windows" ? Visibility.Visible : Visibility.Collapsed;
            panelGoogle.Visibility = provider == "google" ? Visibility.Visible : Visibility.Collapsed;
            panelPolly.Visibility = provider == "polly" ? Visibility.Visible : Visibility.Collapsed;

            foreach (var btn in _ttsProviderButtons)
            {
                bool isActive = string.Equals(btn.Tag?.ToString(), _currentTtsProvider, StringComparison.OrdinalIgnoreCase);
                btn.Style = isActive ? (Style)FindResource("ProviderButtonActiveStyle") : (Style)FindResource("ProviderButtonStyle");
            }

            switch (provider)
            {
                case "local windows":
                    txtTtsTestHint.Text = GetLocalizedTtsTestHint("local_windows");
                    break;
                case "google":
                    txtTtsTestHint.Text = GetLocalizedTtsTestHint("google");
                    break;
                case "polly":
                    txtTtsTestHint.Text = GetLocalizedTtsTestHint("polly");
                    break;
                default:
                    txtTtsTestHint.Text = GetLocalizedTtsTestHint("elevenlabs");
                    break;
            }
        }

        private string GetLocalizedTtsTestHint(string provider)
        {
            bool isRu = string.Equals(_currentUiLanguage, "ru", StringComparison.OrdinalIgnoreCase);
            bool isZh = string.Equals(_currentUiLanguage, "zh", StringComparison.OrdinalIgnoreCase);

            return provider switch
            {
                "local_windows" => isRu
                    ? "Запускает локальный тест Windows TTS с выбранным системным голосом."
                    : isZh
                        ? "使用当前选中的系统语音执行本地 Windows TTS 测试。"
                        : "Runs a direct Windows local TTS test with the selected system voice.",
                "google" => isRu
                    ? "Запускает тест Google Cloud TTS с текущим API key и именем голоса."
                    : isZh
                        ? "使用当前 API key 和语音名称执行 Google Cloud TTS 测试。"
                        : "Runs a direct Google Cloud TTS test with the current API key and voice name.",
                "polly" => isRu
                    ? "Запускает тест Amazon Polly с текущим access key, регионом и voice id."
                    : isZh
                        ? "使用当前 access key、区域和 voice id 执行 Amazon Polly 测试。"
                        : "Runs a direct Amazon Polly test with the current access key, region, and voice id.",
                _ => isRu
                    ? "Запускает тест ElevenLabs с текущим API key и voice id."
                    : isZh
                        ? "使用当前 API key 和 voice id 执行 ElevenLabs 测试。"
                        : "Runs a direct ElevenLabs voice test with the current API key and voice id."
            };
        }

        private string GetSelectedUiLanguage()
        {
            if (cmbUiLanguage.SelectedItem is ComboBoxItem item && item.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
                return tag;

            return _currentUiLanguage;
        }

        private string GetSelectedTtsProvider()
        {
            return _currentTtsProvider;
        }

        private string GetSelectedLlmProvider()
        {
            return _currentLlmProvider;
        }

        private void SelectLlmProvider(string provider)
        {
            _currentLlmProvider = provider ?? "Groq";
            UpdateLlmProviderUi();
            LoadLlmProviderConfig(_currentLlmProvider);
        }

        private void SelectTtsProvider(string provider)
        {
            _currentTtsProvider = provider ?? "ElevenLabs";
            UpdateTtsProviderUi();
        }

        private void BtnTtsProv_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string provider)
            {
                _currentTtsProvider = provider;
                _plugin.PluginSettings.TtsProvider = provider;
                SelectTtsProvider(provider);
            }
        }

        private void BtnGetTtsApiKey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string provider)
            {
                if (TtsKeyUrls.TryGetValue(provider, out var url) && !string.IsNullOrEmpty(url))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        SmartLolisLog.Error("Failed to open TTS API key URL.", ex);
                    }
                }
            }
        }

        private void BtnGetVoiceId_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string provider)
            {
                if (TtsVoiceUrls.TryGetValue(provider, out var url) && !string.IsNullOrEmpty(url))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        SmartLolisLog.Error("Failed to open voice ID URL.", ex);
                    }
                }
            }
        }

        private void SelectUiLanguage(string language)
        {
            foreach (var item in cmbUiLanguage.Items)
            {
                if (item is ComboBoxItem comboItem &&
                    string.Equals(comboItem.Tag?.ToString(), language, StringComparison.OrdinalIgnoreCase))
                {
                    cmbUiLanguage.SelectedItem = comboItem;
                    ApplyLocalization(language);
                    return;
                }
            }

            ApplyLocalization("ru");
        }

        private void ApplyLocalization(string language)
        {
            _currentUiLanguage = string.IsNullOrWhiteSpace(language) ? "ru" : language;
            Dictionary<string, string> text = _currentUiLanguage switch
            {
                "en" => BuildEnglishText(),
                "zh" => BuildChineseText(),
                _ => BuildRussianText()
            };

            Title = text["window_title"];
            txtHeaderTitle.Text = text["header_title"];
            txtHeaderSubtitle1.Text = text["header_subtitle_1"];
            txtHeaderSubtitle2.Text = text["header_subtitle_2"];
            txtUiLanguageLabel.Text = text["language_label"];
            txtTextSectionTitle.Text = text["text_section_title"];
            txtTextToggleLabel.Text = text["text_toggle_label"];
            txtLlmProviderLabel.Text = text["llm_provider_label"];
            txtLlmProviderHint.Text = text["llm_provider_hint"];
            txtLlmApiUrlLabel.Text = text["llm_api_url_label"];
            txtLlmApiKeyLabel.Text = text["llm_api_key_label"];
            txtLlmApiKeyHint.Text = text["llm_api_key_hint"];
            txtLlmModelLabel.Text = text["llm_model_label"];
            txtLlmModelHint.Text = text["llm_model_hint"];
            btnGetApiKey.Content = text["get_key_button"];
            btnGetElevenLabsKey.Content = text["get_key_button"];
            btnGetGoogleKey.Content = text["get_key_button"];
            btnGetPollyKey.Content = text["get_key_button"];
            btnGetElevenLabsVoice.Content = text["get_voice_button"];
            btnTestLlm.Content = text["test_llm"];
            txtVoiceSectionTitle.Text = text["voice_section_title"];
            txtTtsToggleLabel.Text = text["tts_toggle_label"];
            txtTtsProviderLabel.Text = text["tts_provider_label"];
            txtTtsProviderHint.Text = text["tts_provider_hint"];
            btnTestTts.Content = text["test_tts"];
            btnTtsElevenLabs.Content = "ElevenLabs";
            btnTtsLocalWindows.Content = _currentUiLanguage switch
            {
                "ru" => "Локальный Windows",
                "zh" => "本地 Windows",
                _ => "Local Windows"
            };
            btnTtsGoogle.Content = "Google";
            btnTtsPolly.Content = "Polly";
            btnProvCustom.Content = _currentUiLanguage switch
            {
                "ru" => "Свой",
                "zh" => "自定义",
                _ => "Custom"
            };
            txtElevenLabsApiKeyLabel.Text = text["elevenlabs_api_key_label"];
            txtElevenLabsApiKeyHint.Text = text["elevenlabs_api_key_hint"];
            txtElevenLabsVoiceIdLabel.Text = text["elevenlabs_voice_id_label"];
            txtElevenLabsVoiceIdHint.Text = text["elevenlabs_voice_id_hint"];
            txtGoogleApiKeyLabel.Text = text["google_api_key_label"];
            txtGoogleApiKeyHint.Text = text["google_api_key_hint"];
            txtGoogleVoiceNameLabel.Text = text["google_voice_name_label"];
            txtGoogleVoiceNameHint.Text = text["google_voice_name_hint"];
            txtLocalWindowsVoiceLabel.Text = text["local_windows_voice_label"];
            txtLocalWindowsVoiceHint.Text = text["local_windows_voice_hint"];
            txtPollyAccessKeyLabel.Text = text["polly_access_key_label"];
            txtPollyAccessKeyHint.Text = text["polly_access_key_hint"];
            txtPollySecretKeyLabel.Text = text["polly_secret_key_label"];
            txtPollyRegionLabel.Text = text["polly_region_label"];
            txtPollyRegionHint.Text = text["polly_region_hint"];
            txtPollyVoiceIdLabel.Text = text["polly_voice_id_label"];
            txtPollyVoiceIdHint.Text = text["polly_voice_id_hint"];
            txtBehaviorSectionTitle.Text = text["behavior_title"];
            chkStreaming.Content = text["streaming_toggle"];
            chkCommandMode.Content = text["command_mode_toggle"];
            chkVoiceInputButton.Content = text["voice_input_toggle"];
            txtCommandModeTitle.Text = text["command_mode_title"];
            btnCommandInfo.Content = text["info_button"];
            txtCommandModeHint.Text = text["command_mode_hint"];
            txtCommandExample1.Text = text["command_example_1"];
            txtCommandExample2.Text = text["command_example_2"];
            txtCommandExample3.Text = text["command_example_3"];
            txtCommandExample4.Text = text["command_example_4"];
            txtMaxTokensLabel.Text = text["max_tokens_label"];
            txtMaxHistoryLabel.Text = text["max_history_label"];
            txtSystemPromptLabel.Text = text["system_prompt_label"];
            txtSystemPromptHint.Text = text["system_prompt_hint"];
            btnOpenConsole.Content = text["open_console"];
            btnClearHistory.Content = text["clear_history"];
            btnSave.Content = text["save"];
            btnCancel.Content = text["cancel"];
            UpdateLlmProviderUi();
            UpdateTtsProviderUi();
        }

        private static Dictionary<string, string> BuildRussianText() => new()
        {
            ["window_title"] = "Smart Lolis Settings",
            ["header_title"] = "SMART LOLIS",
            ["header_subtitle_1"] = "Умный чат для VPet: диалог, голосовой ввод, команды и озвучка.",
            ["header_subtitle_2"] = "Настрой текст, голос и поведение AI-компаньона.",
                ["language_label"] = "ЯЗЫК",
            ["text_section_title"] = "TEXT",
            ["text_toggle_label"] = "LLM",
            ["llm_provider_label"] = "Провайдер текста",
            ["llm_provider_hint"] = "Выбери, какой сервис будет генерировать ответы.",
            ["llm_api_url_label"] = "API URL",
            ["llm_api_key_label"] = "API Key",
            ["llm_api_key_hint"] = "Ключ для выбранного провайдера.",
            ["llm_model_label"] = "Модель",
            ["llm_model_hint"] = "ID модели для выбранного провайдера.",
            ["get_key_button"] = "Получить ключ",
            ["get_voice_button"] = "Получить голос",
            ["test_llm"] = "TEST LLM",
            ["voice_section_title"] = "VOICE",
            ["tts_toggle_label"] = "TTS",
            ["tts_provider_label"] = "Провайдер голоса",
            ["tts_provider_hint"] = "Выбери, какой сервис будет озвучивать ответы.",
            ["test_tts"] = "TEST TTS",
            ["elevenlabs_api_key_label"] = "ElevenLabs API Key",
            ["elevenlabs_api_key_hint"] = "Нужен только для озвучки через ElevenLabs.",
            ["elevenlabs_voice_id_label"] = "ElevenLabs Voice ID",
            ["elevenlabs_voice_id_hint"] = "Вставь точный идентификатор голоса ElevenLabs, а не отображаемое имя.",
            ["google_api_key_label"] = "Google API Key",
            ["google_api_key_hint"] = "Включи Google Cloud Text-to-Speech и вставь свой API key.",
            ["google_voice_name_label"] = "Google Voice Name",
            ["google_voice_name_hint"] = "Пример: ru-RU-Standard-A или en-US-Standard-C.",
            ["local_windows_voice_label"] = "Голос Windows",
            ["local_windows_voice_hint"] = "Выбери один из установленных голосов Windows или впиши имя вручную. Оставь пустым для голоса по умолчанию.",
            ["polly_access_key_label"] = "Polly Access Key",
            ["polly_access_key_hint"] = "AWS access key для Amazon Polly.",
            ["polly_secret_key_label"] = "Polly Secret Key",
            ["polly_region_label"] = "Polly Region",
            ["polly_region_hint"] = "Пример: us-east-1",
            ["polly_voice_id_label"] = "Polly Voice ID",
            ["polly_voice_id_hint"] = "Пример: Tatyana, Maxim, Joanna.",
            ["behavior_title"] = "ПОВЕДЕНИЕ",
            ["streaming_toggle"] = "Потоковый ответ",
            ["command_mode_toggle"] = "Командный режим",
            ["voice_input_toggle"] = "Кнопка голосового ввода",
            ["command_mode_title"] = "КОМАНДНЫЙ РЕЖИМ",
            ["info_button"] = "Инфо",
            ["command_mode_hint"] = "Можно писать и свободнее: `займись учебой`, `может, поработаешь?`, `купи воды`, `отмена`. Префиксы тоже работают: /, !, cmd, command, команда, приказ.",
            ["command_example_1"] = "займись учебой / может, поучишься?",
            ["command_example_2"] = "купи воды / купи еду",
            ["command_example_3"] = "стоп / отмена / не надо",
            ["command_example_4"] = "займись учебой  ->  рисование",
            ["max_tokens_label"] = "Макс. токены",
            ["max_history_label"] = "Макс. история сообщений",
            ["system_prompt_label"] = "Системный промпт",
            ["system_prompt_hint"] = "Этот промпт задаёт тон, характер, ограничения и стиль роли Smart Lolis.",
            ["open_console"] = "Открыть консоль",
            ["clear_history"] = "Очистить историю",
            ["save"] = "Сохранить",
            ["cancel"] = "Отмена"
        };

        private static Dictionary<string, string> BuildEnglishText() => new()
        {
            ["window_title"] = "Smart Lolis Settings",
            ["header_title"] = "SMART LOLIS",
            ["header_subtitle_1"] = "Smart chat for VPet: dialogue, voice input, commands, and voice playback.",
            ["header_subtitle_2"] = "Configure text, voice, and AI companion behavior.",
                ["language_label"] = "LANGUAGE",
            ["text_section_title"] = "TEXT",
            ["text_toggle_label"] = "LLM",
            ["llm_provider_label"] = "Text Provider",
            ["llm_provider_hint"] = "Choose which service will generate chat replies.",
            ["llm_api_url_label"] = "API URL",
            ["llm_api_key_label"] = "API Key",
            ["llm_api_key_hint"] = "API key for the selected provider.",
            ["llm_model_label"] = "Model",
            ["llm_model_hint"] = "Model ID for the selected provider.",
            ["get_key_button"] = "Get Key",
            ["get_voice_button"] = "Get Voice",
            ["test_llm"] = "TEST LLM",
            ["voice_section_title"] = "VOICE",
            ["tts_toggle_label"] = "TTS",
            ["tts_provider_label"] = "Voice Provider",
            ["tts_provider_hint"] = "Choose which service will generate voice playback.",
            ["test_tts"] = "TEST TTS",
            ["elevenlabs_api_key_label"] = "ElevenLabs API Key",
            ["elevenlabs_api_key_hint"] = "Needed only for ElevenLabs playback.",
            ["elevenlabs_voice_id_label"] = "ElevenLabs Voice ID",
            ["elevenlabs_voice_id_hint"] = "Paste the exact voice identifier from ElevenLabs. Not the visible voice name.",
            ["google_api_key_label"] = "Google API Key",
            ["google_api_key_hint"] = "Enable Google Cloud Text-to-Speech and paste your API key.",
            ["google_voice_name_label"] = "Google Voice Name",
            ["google_voice_name_hint"] = "Example: ru-RU-Standard-A or en-US-Standard-C.",
            ["local_windows_voice_label"] = "Windows Voice",
            ["local_windows_voice_hint"] = "Choose one of the installed Windows voices, or type a voice name manually. Leave empty to use the default voice.",
            ["polly_access_key_label"] = "Polly Access Key",
            ["polly_access_key_hint"] = "AWS access key for Amazon Polly.",
            ["polly_secret_key_label"] = "Polly Secret Key",
            ["polly_region_label"] = "Polly Region",
            ["polly_region_hint"] = "Example: us-east-1",
            ["polly_voice_id_label"] = "Polly Voice ID",
            ["polly_voice_id_hint"] = "Example: Tatyana, Maxim, Joanna.",
            ["behavior_title"] = "BEHAVIOR",
            ["streaming_toggle"] = "Enable streaming reply",
            ["command_mode_toggle"] = "Enable command mode",
            ["voice_input_toggle"] = "Enable voice input button",
            ["command_mode_title"] = "COMMAND MODE",
            ["info_button"] = "Info",
            ["command_mode_hint"] = "You can phrase commands naturally: `start studying`, `maybe go work`, `buy some water`, `cancel`. Prefixes still work: /, !, cmd, command.",
            ["command_example_1"] = "start studying / maybe go study",
            ["command_example_2"] = "buy some water / buy some food",
            ["command_example_3"] = "stop / cancel / never mind",
            ["command_example_4"] = "start studying  ->  drawing",
            ["max_tokens_label"] = "Max Tokens",
            ["max_history_label"] = "Max History Messages",
            ["system_prompt_label"] = "System Prompt",
            ["system_prompt_hint"] = "This prompt defines tone, personality, limits, and roleplay style for Smart Lolis.",
            ["open_console"] = "Open Console",
            ["clear_history"] = "Clear History",
            ["save"] = "Save",
            ["cancel"] = "Cancel"
        };

        private static Dictionary<string, string> BuildChineseText() => new()
        {
            ["window_title"] = "Smart Lolis 设置",
            ["header_title"] = "SMART LOLIS",
            ["header_subtitle_1"] = "适用于 VPet 的智能聊天：对话、语音输入、指令与语音播放。",
            ["header_subtitle_2"] = "配置文本、语音和 AI 伙伴的行为。",
                ["language_label"] = "语言",
            ["text_section_title"] = "TEXT",
            ["text_toggle_label"] = "LLM",
            ["llm_provider_label"] = "文本提供商",
            ["llm_provider_hint"] = "选择用于生成聊天回复的服务。",
            ["llm_api_url_label"] = "API URL",
            ["llm_api_key_label"] = "API Key",
            ["llm_api_key_hint"] = "所选提供商的 API 密钥。",
            ["llm_model_label"] = "模型",
            ["llm_model_hint"] = "所选提供商的模型 ID。",
            ["get_key_button"] = "获取密钥",
            ["get_voice_button"] = "获取声音",
            ["test_llm"] = "测试 LLM",
            ["voice_section_title"] = "VOICE",
            ["tts_toggle_label"] = "TTS",
            ["tts_provider_label"] = "语音提供商",
            ["tts_provider_hint"] = "选择用于语音播放的服务。",
            ["test_tts"] = "测试 TTS",
            ["elevenlabs_api_key_label"] = "ElevenLabs API Key",
            ["elevenlabs_api_key_hint"] = "仅在使用 ElevenLabs 语音播放时需要。",
            ["elevenlabs_voice_id_label"] = "ElevenLabs Voice ID",
            ["elevenlabs_voice_id_hint"] = "填写 ElevenLabs 的精确语音标识，不是显示名称。",
            ["google_api_key_label"] = "Google API Key",
            ["google_api_key_hint"] = "启用 Google Cloud Text-to-Speech 并填写你的 API key。",
            ["google_voice_name_label"] = "Google Voice Name",
            ["google_voice_name_hint"] = "例如：ru-RU-Standard-A 或 en-US-Standard-C。",
            ["local_windows_voice_label"] = "Windows 语音",
            ["local_windows_voice_hint"] = "可从已安装的 Windows 语音中选择，或手动输入名称。留空则使用默认语音。",
            ["polly_access_key_label"] = "Polly Access Key",
            ["polly_access_key_hint"] = "Amazon Polly 的 AWS access key。",
            ["polly_secret_key_label"] = "Polly Secret Key",
            ["polly_region_label"] = "Polly Region",
            ["polly_region_hint"] = "例如：us-east-1",
            ["polly_voice_id_label"] = "Polly Voice ID",
            ["polly_voice_id_hint"] = "例如：Tatyana、Maxim、Joanna。",
            ["behavior_title"] = "行为",
            ["streaming_toggle"] = "启用流式回复",
            ["command_mode_toggle"] = "启用命令模式",
            ["voice_input_toggle"] = "启用语音输入按钮",
            ["command_mode_title"] = "命令模式",
            ["info_button"] = "说明",
            ["command_mode_hint"] = "也可以用更自然的说法：`去学习`、`去工作一下吧`、`买点水`、`取消`。前缀也支持：/, !, cmd, command。",
            ["command_example_1"] = "去学习 / 要不要去学习一下",
            ["command_example_2"] = "买点水 / 买点吃的",
            ["command_example_3"] = "停止 / 取消 / 不用了",
            ["command_example_4"] = "去学习  ->  画画",
            ["max_tokens_label"] = "最大 Tokens",
            ["max_history_label"] = "最大历史消息数",
            ["system_prompt_label"] = "系统提示词",
            ["system_prompt_hint"] = "这个提示词定义 Smart Lolis 的语气、个性、限制与角色风格。",
            ["open_console"] = "打开控制台",
            ["clear_history"] = "清空历史",
            ["save"] = "保存",
            ["cancel"] = "取消"
        };

        private void LoadLocalWindowsVoices()
        {
            cmbLocalWindowsVoiceName.Items.Clear();

            try
            {
                Type voiceType = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (voiceType == null)
                    return;

                dynamic voice = Activator.CreateInstance(voiceType);
                dynamic voices = voice.GetVoices();
                int count = voices.Count;
                for (int i = 0; i < count; i++)
                {
                    dynamic candidate = voices.Item(i);
                    string description = candidate.GetDescription() as string ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        cmbLocalWindowsVoiceName.Items.Add(description);
                    }
                }
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Failed to enumerate installed Windows voices.", ex);
            }
        }

        private void ShowStatus(string message, StatusKind kind)
        {
            statusPanel.Visibility = Visibility.Visible;
            txtStatusMessage.Text = message;

            switch (kind)
            {
                case StatusKind.Success:
                    statusPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EAF8EE"));
                    statusPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9FD0AA"));
                    txtStatusIcon.Text = "!";
                    statusIconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64B878"));
                    break;
                case StatusKind.Warning:
                    statusPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7E8"));
                    statusPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3C37D"));
                    txtStatusIcon.Text = "!";
                    statusIconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D5A845"));
                    break;
                case StatusKind.Error:
                    statusPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF0F2"));
                    statusPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E19AA8"));
                    txtStatusIcon.Text = "!";
                    statusIconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D96B80"));
                    break;
                default:
                    statusPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F4FAFE"));
                    statusPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C9D7E2"));
                    txtStatusIcon.Text = "!";
                    statusIconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#59B7EA"));
                    break;
            }
        }

        private enum StatusKind
        {
            Info,
            Success,
            Warning,
            Error
        }
    }
}
