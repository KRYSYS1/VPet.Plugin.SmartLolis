using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using VPet_Simulator.Windows.Interface;

namespace VPet.Plugin.SmartLolis
{
    public class LlmProviderConfig
    {
        public string ApiUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string Model { get; set; } = "";
    }

    public class SmartLolisSettings
    {
        // --- Legacy flat properties (kept for backward compat deserialization) ---
        public string GroqApiKey { get; set; } = "";
        public string GroqModel { get; set; } = "llama-3.3-70b-versatile";
        public string OpenRouterApiKey { get; set; } = "";
        public string OpenRouterModel { get; set; } = "openrouter/free";

        // --- New per-provider config ---
        public string LlmProvider { get; set; } = "Groq";
        public Dictionary<string, LlmProviderConfig> LlmProviderConfigs { get; set; }

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
        public int MaxHistoryMessages { get; set; } = 20;
        public bool EnableLlm { get; set; } = true;
        public bool EnableStreaming { get; set; } = true;
        public bool EnableTts { get; set; } = true;
        public bool EnableCommandMode { get; set; } = true;
        public bool EnableVoiceInputButton { get; set; } = true;
        public bool EnableAutoMemoryCompression { get; set; } = true;
        public int MemoryCompressionThreshold { get; set; } = 24;

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
            catch (Exception ex)
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
                    var settings = JsonConvert.DeserializeObject<SmartLolisSettings>(json) ?? new SmartLolisSettings();
                    settings.MigrateLegacyConfigs();
                    settings.EnsureDefaults();
                    return settings;
                }
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Failed to load SmartLolis settings.", ex);
            }

            var fresh = new SmartLolisSettings();
            fresh.EnsureDefaults();
            return fresh;
        }

        public void MigrateLegacyConfigs()
        {
            if (LlmProviderConfigs == null)
                LlmProviderConfigs = new Dictionary<string, LlmProviderConfig>(StringComparer.OrdinalIgnoreCase);

            if (!LlmProviderConfigs.ContainsKey("Groq"))
            {
                LlmProviderConfigs["Groq"] = new LlmProviderConfig
                {
                    ApiUrl = "https://api.groq.com/openai/v1/chat/completions",
                    ApiKey = GroqApiKey ?? "",
                    Model = GroqModel ?? "llama-3.3-70b-versatile"
                };
            }

            if (!LlmProviderConfigs.ContainsKey("OpenRouter"))
            {
                LlmProviderConfigs["OpenRouter"] = new LlmProviderConfig
                {
                    ApiUrl = "https://openrouter.ai/api/v1/chat/completions",
                    ApiKey = OpenRouterApiKey ?? "",
                    Model = OpenRouterModel ?? "openrouter/free"
                };
            }
        }

        public void EnsureDefaults()
        {
            if (LlmProviderConfigs == null)
                LlmProviderConfigs = new Dictionary<string, LlmProviderConfig>(StringComparer.OrdinalIgnoreCase);

            var defaults = new Dictionary<string, LlmProviderConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ollama"] = new() { ApiUrl = "http://localhost:11434/v1/chat/completions", Model = "llama3.1:8b" },
                ["OpenAI"] = new() { ApiUrl = "https://api.openai.com/v1/chat/completions", Model = "gpt-4o-mini" },
                ["Groq"] = new() { ApiUrl = "https://api.groq.com/openai/v1/chat/completions", Model = "llama-3.3-70b-versatile" },
                ["NVIDIA"] = new() { ApiUrl = "https://integrate.api.nvidia.com/v1/chat/completions", Model = "meta/llama3-70b-instruct" },
                ["Google"] = new() { ApiUrl = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", Model = "gemini-1.5-flash" },
                ["GitHub"] = new() { ApiUrl = "https://models.inference.ai.azure.com/chat/completions", Model = "gpt-4o-mini" },
                ["Cohere"] = new() { ApiUrl = "https://api.cohere.ai/v1/chat/completions", Model = "command-r" },
                ["Cerebras"] = new() { ApiUrl = "https://api.cerebras.ai/v1/chat/completions", Model = "llama-3.3-70b" },
                ["Mistral"] = new() { ApiUrl = "https://api.mistral.ai/v1/chat/completions", Model = "mistral-large-latest" },
                ["OpenRouter"] = new() { ApiUrl = "https://openrouter.ai/api/v1/chat/completions", Model = "meta-llama/llama-3.3-8b-instruct:free" },
                ["LM Studio"] = new() { ApiUrl = "http://localhost:1234/v1/chat/completions", Model = "" },
                ["Custom"] = new() { ApiUrl = "", Model = "" },
            };

            foreach (var kvp in defaults)
            {
                if (!LlmProviderConfigs.ContainsKey(kvp.Key))
                    LlmProviderConfigs[kvp.Key] = kvp.Value;
            }
        }

        public LlmProviderConfig GetActiveLlmConfig()
        {
            EnsureDefaults();
            string provider = LlmProvider ?? "Groq";
            if (LlmProviderConfigs.TryGetValue(provider, out var config))
                return config;
            return LlmProviderConfigs["Groq"];
        }
    }
}
