using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VPet.Plugin.SmartLolis
{
    public class SmartLolisService
    {
        private readonly SmartLolisSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly List<ConversationMessage> _storedHistory = new();
        private readonly object _historyLock = new();
        private readonly object _stateLock = new();
        private readonly OpenAiCompatibleProvider _llmProvider = new();
        private readonly GroqProvider _groqProvider = new();
        private string _currentActivitySummary = string.Empty;
        private string _recentActionSummary = string.Empty;
        private string _compressedMemory = string.Empty;
        private string _persistPath;

        public SmartLolisService(SmartLolisSettings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        }

        public void SetPersistPath(string path)
        {
            _persistPath = path;
            LoadPersistedData();
        }

        private string GetPersistFilePath()
        {
            return _persistPath ?? "";
        }

        private void LoadPersistedData()
        {
            try
            {
                string path = GetPersistFilePath();
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return;

                string json = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<PersistedChatData>(json) ?? new PersistedChatData();
                lock (_historyLock)
                {
                    _storedHistory.Clear();
                    if (data.Messages != null)
                        _storedHistory.AddRange(data.Messages);
                    _compressedMemory = data.CompressedMemory ?? string.Empty;
                }
                SmartLolisLog.Info($"Chat history loaded: {_storedHistory.Count} messages, compressed memory: {(_compressedMemory.Length > 0 ? "yes" : "no")}.");
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Failed to load persisted chat history.", ex);
            }
        }

        private void PersistToFile()
        {
            try
            {
                string path = GetPersistFilePath();
                if (string.IsNullOrEmpty(path))
                    return;

                PersistedChatData data;
                lock (_historyLock)
                {
                    data = new PersistedChatData
                    {
                        Messages = _storedHistory.ToList(),
                        CompressedMemory = _compressedMemory
                    };
                }

                string json = JsonConvert.SerializeObject(data, Formatting.None);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Failed to persist chat history.", ex);
            }
        }

        public async Task<string> SendMessageAsync(string userMessage, Action<string> onPartialResponse = null)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return string.Empty;

            string providerName = GetLlmProviderName();
            SmartLolisLog.Info($"{providerName} request started. Message length: {userMessage.Length}");

            lock (_historyLock)
            {
                _storedHistory.Add(new ConversationMessage { Role = "user", Content = userMessage });
            }

            IReadOnlyList<ConversationMessage> apiSnapshot;
            lock (_historyLock)
            {
                int maxApi = _settings.MaxHistoryMessages > 0 ? _settings.MaxHistoryMessages : 16;
                int skip = Math.Max(0, _storedHistory.Count - maxApi);
                apiSnapshot = _storedHistory.Skip(skip).ToList().AsReadOnly();
            }

            string fullResponse;
            using (var request = BuildLlmRequest(apiSnapshot))
            {
                fullResponse = _settings.EnableStreaming
                    ? await SendStreamingRequestAsync(request, onPartialResponse)
                    : await SendStandardRequestAsync(request);
            }

            if (!string.IsNullOrWhiteSpace(fullResponse))
            {
                SmartLolisLog.Info($"{providerName} response completed. Response length: {fullResponse.Length}");
                lock (_historyLock)
                {
                    _storedHistory.Add(new ConversationMessage { Role = "assistant", Content = fullResponse });
                }
                PersistToFile();

                if (_settings.EnableAutoMemoryCompression && _settings.MemoryCompressionThreshold > 0)
                {
                    int count;
                    lock (_historyLock) { count = _storedHistory.Count; }
                    if (count >= _settings.MemoryCompressionThreshold)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await CompressHistoryAsync(4, _settings.UiLanguage ?? "en");
                            }
                            catch (Exception ex)
                            {
                                SmartLolisLog.Error("Auto memory compression failed.", ex);
                            }
                        });
                    }
                }
            }
            else
            {
                PersistToFile();
            }

            return fullResponse;
        }

        public void ClearHistory()
        {
            lock (_historyLock)
            {
                _storedHistory.Clear();
                _compressedMemory = string.Empty;
            }
            PersistToFile();
        }

        public List<ConversationMessage> GetHistorySnapshot()
        {
            lock (_historyLock)
            {
                return _storedHistory.ToList();
            }
        }

        public string GetCompressedMemory()
        {
            lock (_historyLock)
            {
                return _compressedMemory;
            }
        }

        public void SetCompressedMemory(string compressed)
        {
            lock (_historyLock)
            {
                _compressedMemory = compressed ?? string.Empty;
            }
            PersistToFile();
        }

        public void ReplaceHistory(List<ConversationMessage> newHistory)
        {
            lock (_historyLock)
            {
                _storedHistory.Clear();
                if (newHistory != null)
                    _storedHistory.AddRange(newHistory);
            }
            PersistToFile();
        }

        public async Task<string> CompressHistoryAsync(int keepRecentCount, string uiLanguage = "en")
        {
            List<ConversationMessage> snapshot;
            string compressedSoFar;
            lock (_historyLock)
            {
                snapshot = _storedHistory.ToList();
                compressedSoFar = _compressedMemory;
            }

            if (snapshot.Count <= keepRecentCount)
                return "Not enough messages to compress.";

            int toCompress = snapshot.Count - keepRecentCount;
            var oldMessages = snapshot.Take(toCompress).ToList();
            var recentMessages = snapshot.Skip(toCompress).ToList();

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(compressedSoFar))
                sb.AppendLine($"Previous summary:\n{compressedSoFar}");

            sb.AppendLine("New conversations to summarize:");
            foreach (var msg in oldMessages)
                sb.AppendLine($"{msg.Role}: {msg.Content}");

            string systemPrompt = "You are a memory compressor for an AI companion's conversation history. Create a structured summary that preserves:\n1. User's name, preferences, likes, dislikes, and personal details\n2. Key events, decisions, and important facts discussed\n3. Topics the user cares about and their opinions on them\n4. Any promises, plans, or commitments made\n5. Emotional context and relationship dynamics\n\nDo NOT include trivial small talk or greetings. Focus on information that would be lost and cannot be inferred later. YOU MUST detect the language used in the conversations and WRITE THE SUMMARY IN THAT SAME LANGUAGE. Use bullet points for clarity.";

            var compressMessages = new List<ConversationMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = sb.ToString() }
            };

            var config = _settings.GetActiveLlmConfig();
            using var request = _llmProvider.BuildRequest(
                config.ApiUrl,
                compressMessages,
                "",
                config.Model,
                512,
                false,
                config.ApiKey);

            string summary;
            try
            {
                summary = await SendStandardRequestAsync(request);
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Memory compression LLM request failed.", ex);
                return $"Compression failed: {ex.Message}";
            }

            if (string.IsNullOrWhiteSpace(summary))
                return "Compression returned empty result.";

            lock (_historyLock)
            {
                _compressedMemory = summary.Trim();
                _storedHistory.Clear();
                _storedHistory.AddRange(recentMessages);
            }
            PersistToFile();

            SmartLolisLog.Info($"Memory compressed: {toCompress} old messages summarized, {recentMessages.Count} recent kept.");
            return summary.Trim();
        }

        public void SetCurrentActivity(string activitySummary)
        {
            lock (_stateLock)
            {
                _currentActivitySummary = activitySummary?.Trim() ?? string.Empty;
            }
        }

        public void ClearCurrentActivity()
        {
            lock (_stateLock)
            {
                _currentActivitySummary = string.Empty;
            }
        }

        public void SetRecentAction(string actionSummary)
        {
            lock (_stateLock)
            {
                _recentActionSummary = actionSummary?.Trim() ?? string.Empty;
            }
        }

        public async Task<string> TranscribeAudioAsync(byte[] audioData, string fileName = "smartlolis-mic.wav")
        {
            if (audioData == null || audioData.Length == 0)
                return string.Empty;

            _settings.EnsureDefaults();
            var groqConfig = _settings.LlmProviderConfigs.TryGetValue("Groq", out var cfg) ? cfg : new LlmProviderConfig();
            if (string.IsNullOrWhiteSpace(groqConfig.ApiKey))
                throw new Exception("Set your Groq API key in Smart Lolis Settings first. Voice transcription still uses Groq.");

            SmartLolisLog.Info($"Groq transcription started. Audio bytes: {audioData.Length}");

            using var request = _groqProvider.BuildTranscriptionRequest(audioData, fileName, groqConfig.ApiKey);
            using var response = await _httpClient.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                SmartLolisLog.Error($"Groq transcription returned {(int)response.StatusCode} {response.ReasonPhrase}.{Environment.NewLine}{body}");
                throw new Exception(_groqProvider.ParseError(body));
            }

            string text = _groqProvider.ParseTranscriptionResponse(body);
            SmartLolisLog.Info($"Groq transcription completed. Text length: {text.Length}");
            return text;
        }

        private async Task<string> SendStandardRequestAsync(HttpRequestMessage request)
        {
            using var response = await _httpClient.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                SmartLolisLog.Error($"{GetLlmProviderName()} returned {(int)response.StatusCode} {response.ReasonPhrase}.{Environment.NewLine}{body}");
                throw new Exception(ParseLlmError(body));
            }
            return ParseLlmResponse(body);
        }

        private async Task<string> SendStreamingRequestAsync(HttpRequestMessage request, Action<string> onPartialResponse)
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                SmartLolisLog.Error($"{GetLlmProviderName()} streaming returned {(int)response.StatusCode} {response.ReasonPhrase}.{Environment.NewLine}{errorBody}");
                throw new Exception(ParseLlmError(errorBody));
            }

            var sb = new StringBuilder();
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                    continue;

                string data = line.Substring(6);
                if (data == "[DONE]")
                    break;

                string delta = ParseLlmStreamEvent(data);
                if (!string.IsNullOrEmpty(delta))
                {
                    sb.Append(delta);
                    onPartialResponse?.Invoke(delta);
                }
            }

            return sb.ToString();
        }

        private HttpRequestMessage BuildLlmRequest(IReadOnlyList<ConversationMessage> snapshot)
        {
            var config = _settings.GetActiveLlmConfig();
            return _llmProvider.BuildRequest(
                config.ApiUrl,
                snapshot,
                BuildEffectiveSystemPrompt(),
                config.Model,
                _settings.MaxTokens,
                _settings.EnableStreaming,
                config.ApiKey);
        }

        private string BuildEffectiveSystemPrompt()
        {
            string basePrompt = string.IsNullOrWhiteSpace(_settings.SystemPrompt)
                ? "You are Smart Lolis, a cute and clever desktop companion. Reply briefly, warmly, and with personality."
                : _settings.SystemPrompt.Trim();

            string currentActivity;
            string recentAction;
            string compressedMemory;
            lock (_stateLock)
            {
                currentActivity = _currentActivitySummary;
                recentAction = _recentActionSummary;
            }
            lock (_historyLock)
            {
                compressedMemory = _compressedMemory;
            }

            var context = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(compressedMemory))
                context.AppendLine($"Compressed memory of past conversations:\n{compressedMemory}");

            if (!string.IsNullOrWhiteSpace(currentActivity))
                context.AppendLine($"Current activity state: {currentActivity}");

            if (!string.IsNullOrWhiteSpace(recentAction))
                context.AppendLine($"Latest executed action: {recentAction}");

            if (context.Length == 0)
                return basePrompt;

            context.AppendLine("Use this state naturally in your replies. Do not say you are reading hidden system context.");
            return $"{basePrompt}\n\n{context.ToString().TrimEnd()}";
        }

        private string ParseLlmResponse(string body)
        {
            return _llmProvider.ParseResponse(body);
        }

        private string ParseLlmStreamEvent(string eventData)
        {
            return _llmProvider.ParseStreamEvent(eventData);
        }

        private string ParseLlmError(string body)
        {
            return _llmProvider.ParseError(body);
        }

        private string GetLlmProviderName()
        {
            return _settings.LlmProvider ?? "Groq";
        }
    }

    public class ConversationMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class PersistedChatData
    {
        public List<ConversationMessage> Messages { get; set; } = new();
        public string CompressedMemory { get; set; } = "";
    }
}
