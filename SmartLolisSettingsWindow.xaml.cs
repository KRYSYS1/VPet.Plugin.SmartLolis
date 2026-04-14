using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Media;
using System;
using System.Collections.Generic;

namespace VPet.Plugin.SmartLolis
{
    public partial class SmartLolisSettingsWindow : Window
    {
        private readonly SmartLolisPlugin _plugin;
        private string _currentUiLanguage = "ru";

        public SmartLolisSettingsWindow(SmartLolisPlugin plugin)
        {
            InitializeComponent();
            _plugin = plugin;
            LoadLocalWindowsVoices();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var s = _plugin.PluginSettings;
            SelectLlmProvider(string.IsNullOrWhiteSpace(s.LlmProvider) ? "Groq" : s.LlmProvider);
            txtGroqApiKey.Password = s.GroqApiKey ?? string.Empty;
            txtOpenRouterApiKey.Password = s.OpenRouterApiKey ?? string.Empty;
            cmbOpenRouterModel.Text = string.IsNullOrWhiteSpace(s.OpenRouterModel) ? "openrouter/free" : s.OpenRouterModel;
            SelectTtsProvider(string.IsNullOrWhiteSpace(s.TtsProvider) ? "ElevenLabs" : s.TtsProvider);
            txtElevenLabsApiKey.Password = s.ElevenLabsApiKey ?? string.Empty;
            cmbElevenLabsVoiceId.Text = s.ElevenLabsVoiceId ?? string.Empty;
            txtGoogleApiKey.Password = s.GoogleApiKey ?? string.Empty;
            txtGoogleVoiceName.Text = string.IsNullOrWhiteSpace(s.GoogleVoiceName) ? "ru-RU-Standard-A" : s.GoogleVoiceName;
            cmbLocalWindowsVoiceName.Text = s.LocalWindowsVoiceName ?? string.Empty;
            txtPollyAccessKey.Text = s.PollyAccessKey ?? string.Empty;
            txtPollySecretKey.Password = s.PollySecretKey ?? string.Empty;
            txtPollyRegion.Text = string.IsNullOrWhiteSpace(s.PollyRegion) ? "us-east-1" : s.PollyRegion;
            txtPollyVoiceId.Text = string.IsNullOrWhiteSpace(s.PollyVoiceId) ? "Tatyana" : s.PollyVoiceId;
            SelectUiLanguage(string.IsNullOrWhiteSpace(s.UiLanguage) ? "ru" : s.UiLanguage);
            cmbGroqModel.Text = string.IsNullOrWhiteSpace(s.GroqModel) ? "llama-3.3-70b-versatile" : s.GroqModel;
            txtMaxTokens.Text = (s.MaxTokens > 0 ? s.MaxTokens : 512).ToString();
            txtMaxHistory.Text = (s.MaxHistoryMessages > 0 ? s.MaxHistoryMessages : 16).ToString();
            chkLlm.IsChecked = s.EnableLlm;
            chkStreaming.IsChecked = s.EnableStreaming;
            chkTts.IsChecked = s.EnableTts;
            chkCommandMode.IsChecked = s.EnableCommandMode;
            chkVoiceInputButton.IsChecked = s.EnableVoiceInputButton;
            txtSystemPrompt.Text = s.SystemPrompt ?? string.Empty;
            UpdateLlmProviderUi();
            UpdateTtsProviderUi();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var s = _plugin.PluginSettings;
            s.LlmProvider = GetSelectedLlmProvider();
            s.GroqApiKey = txtGroqApiKey.Password;
            s.OpenRouterApiKey = txtOpenRouterApiKey.Password;
            s.OpenRouterModel = cmbOpenRouterModel.Text;
            s.TtsProvider = GetSelectedTtsProvider();
            s.ElevenLabsApiKey = txtElevenLabsApiKey.Password;
            s.ElevenLabsVoiceId = cmbElevenLabsVoiceId.Text;
            s.GoogleApiKey = txtGoogleApiKey.Password;
            s.GoogleVoiceName = txtGoogleVoiceName.Text;
            s.LocalWindowsVoiceName = cmbLocalWindowsVoiceName.Text;
            s.PollyAccessKey = txtPollyAccessKey.Text;
            s.PollySecretKey = txtPollySecretKey.Password;
            s.PollyRegion = txtPollyRegion.Text;
            s.PollyVoiceId = txtPollyVoiceId.Text;
            s.UiLanguage = GetSelectedUiLanguage();
            s.GroqModel = cmbGroqModel.Text;
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
            s.LlmProvider = GetSelectedLlmProvider();
            s.GroqApiKey = txtGroqApiKey.Password;
            s.OpenRouterApiKey = txtOpenRouterApiKey.Password;
            s.OpenRouterModel = cmbOpenRouterModel.Text;
            s.TtsProvider = GetSelectedTtsProvider();
            s.ElevenLabsApiKey = txtElevenLabsApiKey.Password;
            s.ElevenLabsVoiceId = cmbElevenLabsVoiceId.Text;
            s.GoogleApiKey = txtGoogleApiKey.Password;
            s.GoogleVoiceName = txtGoogleVoiceName.Text;
            s.LocalWindowsVoiceName = cmbLocalWindowsVoiceName.Text;
            s.PollyAccessKey = txtPollyAccessKey.Text;
            s.PollySecretKey = txtPollySecretKey.Password;
            s.PollyRegion = txtPollyRegion.Text;
            s.PollyVoiceId = txtPollyVoiceId.Text;
            s.UiLanguage = GetSelectedUiLanguage();
            s.GroqModel = cmbGroqModel.Text;
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

        private void CmbLlmProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateLlmProviderUi();
        }

        private void CmbUiLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyLocalization(GetSelectedUiLanguage());
        }

        private void UpdateLlmProviderUi()
        {
            string provider = GetSelectedLlmProvider();
            bool isOpenRouter = string.Equals(provider, "OpenRouter", System.StringComparison.OrdinalIgnoreCase);

            panelGroq.Visibility = isOpenRouter ? Visibility.Collapsed : Visibility.Visible;
            panelOpenRouter.Visibility = isOpenRouter ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateTtsProviderUi()
        {
            string provider = GetSelectedTtsProvider().Trim().ToLowerInvariant();

            panelElevenLabs.Visibility = provider == "elevenlabs" ? Visibility.Visible : Visibility.Collapsed;
            panelLocalWindows.Visibility = provider == "local windows" ? Visibility.Visible : Visibility.Collapsed;
            panelGoogle.Visibility = provider == "google" ? Visibility.Visible : Visibility.Collapsed;
            panelPolly.Visibility = provider == "polly" ? Visibility.Visible : Visibility.Collapsed;

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
            if (cmbTtsProvider.SelectedItem is ComboBoxItem item)
            {
                if (item.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
                    return tag;
                if (item.Content is string content && !string.IsNullOrWhiteSpace(content))
                    return content;
            }

            if (!string.IsNullOrWhiteSpace(cmbTtsProvider.Text))
                return cmbTtsProvider.Text;

            return "ElevenLabs";
        }

        private string GetSelectedLlmProvider()
        {
            if (cmbLlmProvider.SelectedItem is ComboBoxItem item)
            {
                if (item.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
                    return tag;
                if (item.Content is string content && !string.IsNullOrWhiteSpace(content))
                    return content;
            }

            if (!string.IsNullOrWhiteSpace(cmbLlmProvider.Text))
                return cmbLlmProvider.Text;

            return "Groq";
        }

        private void SelectLlmProvider(string provider)
        {
            foreach (var item in cmbLlmProvider.Items)
            {
                if (item is ComboBoxItem comboItem &&
                    (string.Equals(comboItem.Tag?.ToString(), provider, System.StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(comboItem.Content?.ToString(), provider, System.StringComparison.OrdinalIgnoreCase)))
                {
                    cmbLlmProvider.SelectedItem = comboItem;
                    UpdateLlmProviderUi();
                    return;
                }
            }

            cmbLlmProvider.Text = provider;
            UpdateLlmProviderUi();
        }

        private void SelectTtsProvider(string provider)
        {
            foreach (var item in cmbTtsProvider.Items)
            {
                if (item is ComboBoxItem comboItem &&
                    (string.Equals(comboItem.Tag?.ToString(), provider, System.StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(comboItem.Content?.ToString(), provider, System.StringComparison.OrdinalIgnoreCase)))
                {
                    cmbTtsProvider.SelectedItem = comboItem;
                    UpdateTtsProviderUi();
                    return;
                }
            }

            cmbTtsProvider.Text = provider;
            UpdateTtsProviderUi();
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
            txtGroqApiKeyLabel.Text = text["groq_api_key_label"];
            txtGroqApiKeyHint.Text = text["groq_api_key_hint"];
            txtGroqModelLabel.Text = text["groq_model_label"];
            txtGroqModelHint.Text = text["groq_model_hint"];
            txtOpenRouterApiKeyLabel.Text = _currentUiLanguage switch
            {
                "ru" => "OpenRouter API Key",
                "zh" => "OpenRouter API Key",
                _ => "OpenRouter API Key"
            };
            txtOpenRouterApiKeyHint.Text = _currentUiLanguage switch
            {
                "ru" => "OpenRouter использует Bearer API key и OpenAI-совместимый chat completions endpoint.",
                "zh" => "OpenRouter 使用 Bearer API key 和 OpenAI 兼容的 chat completions endpoint。",
                _ => "OpenRouter uses a Bearer API key with an OpenAI-compatible chat completions endpoint."
            };
            txtOpenRouterModelLabel.Text = _currentUiLanguage switch
            {
                "ru" => "OpenRouter Model",
                "zh" => "OpenRouter 模型",
                _ => "OpenRouter Model"
            };
            txtOpenRouterModelHint.Text = _currentUiLanguage switch
            {
                "ru" => "Основано на OpenRouter OpenAI-совместимом chat-completions API. Список free-моделей может меняться.",
                "zh" => "基于 OpenRouter 官方 OpenAI 兼容 chat-completions 流程。免费模型列表可能会变化。",
                _ => "Based on the official OpenRouter OpenAI-compatible chat-completions flow. Free models may change over time."
            };
            itemLlmProviderGroq.Content = "Groq";
            itemLlmProviderOpenRouter.Content = "OpenRouter";
            btnTestLlm.Content = text["test_llm"];
            txtVoiceSectionTitle.Text = text["voice_section_title"];
            txtTtsToggleLabel.Text = text["tts_toggle_label"];
            txtTtsProviderLabel.Text = text["tts_provider_label"];
            txtTtsProviderHint.Text = text["tts_provider_hint"];
            btnTestTts.Content = text["test_tts"];
            itemTtsProviderElevenLabs.Content = "ElevenLabs";
            itemTtsProviderLocalWindows.Content = _currentUiLanguage switch
            {
                "ru" => "Локальный Windows",
                "zh" => "本地 Windows",
                _ => "Local Windows"
            };
            itemTtsProviderGoogle.Content = "Google";
            itemTtsProviderPolly.Content = "Polly";
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
            ["groq_api_key_label"] = "Groq API Key",
            ["groq_api_key_hint"] = "Основной ключ для текстовых запросов Groq.",
            ["groq_model_label"] = "Groq Model",
            ["groq_model_hint"] = "Список готовых моделей с возможностью вписать свою.",
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
            ["groq_api_key_label"] = "Groq API Key",
            ["groq_api_key_hint"] = "Primary key for Groq text requests.",
            ["groq_model_label"] = "Groq Model",
            ["groq_model_hint"] = "Preset list with manual override support.",
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
            ["groq_api_key_label"] = "Groq API Key",
            ["groq_api_key_hint"] = "用于 Groq 文本请求的主密钥。",
            ["groq_model_label"] = "Groq 模型",
            ["groq_model_hint"] = "可从预设列表选择，也可手动输入。",
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
