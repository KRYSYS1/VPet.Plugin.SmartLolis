using System;
using System.Threading.Tasks;
using System.Windows;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet.Plugin.SmartLolis
{
    public class SmartLolisPlugin : MainPlugin
    {
        public SmartLolisSettings PluginSettings { get; private set; }
        public SmartLolisService LlmService { get; private set; }
        public SmartLolisTtsService TtsService { get; private set; }
        public SmartLolisTalkBox TalkBoxInstance { get; private set; }
        internal SmartLolisVoiceOverlayManager VoiceOverlayManager { get; private set; }
        private Action<SayInfo> _globalSayHook;

        private SmartLolisSettingsWindow _settingsWindow;
        private SmartLolisChatHistoryWindow _chatHistoryWindow;
        private SmartLolisCommandInfoWindow _commandInfoWindow;
        private SmartLolisLogWindow _logWindow;

        public override string PluginName => "SmartLolis";

        public SmartLolisPlugin(IMainWindow mainwin) : base(mainwin)
        {
        }

        public override void LoadPlugin()
        {
            PluginSettings = SmartLolisSettings.Load(this);
            LlmService = new SmartLolisService(PluginSettings);
            LlmService.SetPersistPath(ExtensionValue.BaseDirectory + $"\\SmartLolisChatHistory{MW.PrefixSave}.json");
            TtsService = new SmartLolisTtsService(PluginSettings);
            SmartLolisLog.Info("SmartLolis plugin loaded.");

            TalkBoxInstance = new SmartLolisTalkBox(this);
            MW.TalkAPI.Add(TalkBoxInstance);
            VoiceOverlayManager = new SmartLolisVoiceOverlayManager(this);
            VoiceOverlayManager.Start();
            _globalSayHook = sayInfo => _ = HandleGlobalSayAsync(sayInfo);
            MW.Main.SayProcess.Add(_globalSayHook);

            var menuItem = new System.Windows.Controls.MenuItem()
            {
                Header = "Smart Lolis",
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            menuItem.Click += (_, _) => Setting();
            MW.Main.ToolBar.MenuMODConfig.Items.Add(menuItem);
        }

        public override void Setting()
        {
            if (_settingsWindow != null)
            {
                try
                {
                    if (_settingsWindow.IsVisible)
                    {
                        if (_settingsWindow.WindowState == WindowState.Minimized)
                            _settingsWindow.WindowState = WindowState.Normal;
                        _settingsWindow.Activate();
                        return;
                    }
                }
                catch { }
            }
            _settingsWindow = new SmartLolisSettingsWindow(this);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
        }

        public void ShowChatHistoryWindow(string uiLanguage)
        {
            if (_chatHistoryWindow != null)
            {
                try
                {
                    if (_chatHistoryWindow.IsVisible)
                    {
                        if (_chatHistoryWindow.WindowState == WindowState.Minimized)
                            _chatHistoryWindow.WindowState = WindowState.Normal;
                        _chatHistoryWindow.Activate();
                        return;
                    }
                }
                catch { }
            }
            _chatHistoryWindow = new SmartLolisChatHistoryWindow(this, uiLanguage);
            _chatHistoryWindow.Closed += (_, _) => _chatHistoryWindow = null;
            _chatHistoryWindow.Show();
        }

        public void ShowCommandInfoWindow(string uiLanguage)
        {
            if (_commandInfoWindow != null)
            {
                try
                {
                    if (_commandInfoWindow.IsVisible)
                    {
                        if (_commandInfoWindow.WindowState == WindowState.Minimized)
                            _commandInfoWindow.WindowState = WindowState.Normal;
                        _commandInfoWindow.Activate();
                        return;
                    }
                }
                catch { }
            }
            _commandInfoWindow = new SmartLolisCommandInfoWindow(uiLanguage);
            _commandInfoWindow.Closed += (_, _) => _commandInfoWindow = null;
            _commandInfoWindow.Show();
        }

        public void ShowLogWindow()
        {
            if (_logWindow != null)
            {
                try
                {
                    if (_logWindow.IsVisible)
                    {
                        if (_logWindow.WindowState == WindowState.Minimized)
                            _logWindow.WindowState = WindowState.Normal;
                        _logWindow.Activate();
                        return;
                    }
                }
                catch { }
            }
            _logWindow = new SmartLolisLogWindow();
            _logWindow.Closed += (_, _) => _logWindow = null;
            _logWindow.Show();
        }

        public override void Save()
        {
            PluginSettings ??= new SmartLolisSettings();
            PluginSettings.Save(this);
            TalkBoxInstance?.RefreshUiFromSettings();
            VoiceOverlayManager?.RefreshUiFromSettings();
            SmartLolisLog.Info("SmartLolis settings saved.");
        }

        private async Task HandleGlobalSayAsync(SayInfo sayInfo)
        {
            if (sayInfo == null || !PluginSettings.EnableTts || sayInfo.IsGenVoice)
                return;

            string text;
            try
            {
                text = await sayInfo.GetSayText();
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Failed to read SayInfo text for global TTS.", ex);
                return;
            }

            if (string.IsNullOrWhiteSpace(text))
                return;

            sayInfo.IsGenVoice = true;
            try
            {
                SmartLolisLog.Info($"Global SayRnd TTS started. Text length: {text.Length}");
                await TtsService.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Global SayRnd TTS failed.", ex);
            }
        }
    }
}
