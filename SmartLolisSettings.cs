using System.IO;
using Newtonsoft.Json;
using VPet_Simulator.Windows.Interface;

namespace VPet.Plugin.SmartLolis
{
    public class SmartLolisSettings
    {
        public string GroqApiKey { get; set; } = "";
        public string GroqModel { get; set; } = "llama-3.3-70b-versatile";
        public string LlmProvider { get; set; } = "Groq";
        public string OpenRouterApiKey { get; set; } = "";
        public string OpenRouterModel { get; set; } = "openrouter/free";
        public string TtsProvider { get; set; } = "ElevenLabs";
        public string ElevenLabsApiKey { get; set; } = "";
        public string ElevenLabsVoiceId { get; set; } = "";
        public string GoogleApiKey { get; set; } = "";
        public string GoogleVoiceName { get; set; } = "ru-RU-Standard-A";
        public string LocalWindowsVoiceName { get; set; } = "";
        public string PollyAccessKey { get; set; } = "";
        public string PollySecretKey { get; set; } = "";
        public string PollyRegion { get; set; } = "us-east-1";
        public string PollyVoiceId { get; set; } = "Tatyana";
        public string UiLanguage { get; set; } = "en";
        public string SystemPrompt { get; set; } = "You are Smart Lolis, a cute and clever desktop companion. Reply briefly, warmly, and with personality.";
        public int MaxTokens { get; set; } = 512;
        public int MaxHistoryMessages { get; set; } = 16;
        public bool EnableLlm { get; set; } = true;
        public bool EnableStreaming { get; set; } = true;
        public bool EnableTts { get; set; } = true;
        public bool EnableCommandMode { get; set; } = true;
        public bool EnableVoiceInputButton { get; set; } = true;

        private static string GetSettingsPath(MainPlugin plugin)
        {
            return ExtensionValue.BaseDirectory + $"\\SmartLolisSettings{plugin.MW.PrefixSave}.json";
        }

        public void Save(MainPlugin plugin)
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(GetSettingsPath(plugin), json);
            }
            catch (System.Exception ex)
            {
                SmartLolisLog.Error("Failed to save SmartLolis settings.", ex);
            }
        }

        public static SmartLolisSettings Load(MainPlugin plugin)
        {
            try
            {
                string path = GetSettingsPath(plugin);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<SmartLolisSettings>(json) ?? new SmartLolisSettings();
                }
            }
            catch (System.Exception ex)
            {
                SmartLolisLog.Error("Failed to load SmartLolis settings.", ex);
            }

            return new SmartLolisSettings();
        }
    }
}
