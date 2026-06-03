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

        public override string PluginName => "SmartLolis";

        public SmartLolisPlugin(IMainWindow mainwin) : base(mainwin)
        {
        }

        public override void LoadPlugin()
        {
            PluginSettings = SmartLolisSettings.Load(this);
            LlmService = new SmartLolisService(PluginSettings);
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
            new SmartLolisSettingsWindow(this).ShowDialog();
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
