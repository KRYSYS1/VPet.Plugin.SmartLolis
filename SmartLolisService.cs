using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VPet.Plugin.SmartLolis
{
    public class SmartLolisService
    {
        private readonly SmartLolisSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly List<ConversationMessage> _history = new();
        private readonly object _historyLock = new();
        private readonly object _stateLock = new();
        private readonly GroqProvider _groqProvider = new();
        private readonly OpenRouterProvider _openRouterProvider = new();
        private string _currentActivitySummary = string.Empty;
        private string _recentActionSummary = string.Empty;

        public SmartLolisService(SmartLolisSettings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        }

        public async Task<string> SendMessageAsync(string userMessage, Action<string> onPartialResponse = null)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return string.Empty;

            string providerName = GetLlmProviderName();
            SmartLolisLog.Info($"{providerName} request started. Message length: {userMessage.Length}");

            IReadOnlyList<ConversationMessage> snapshot;
            lock (_historyLock)
            {
                _history.Add(new ConversationMessage { Role = "user", Content = userMessage });
                TrimHistory();
                snapshot = _history.ToList().AsReadOnly();
            }

            string fullResponse;
            using (var request = BuildLlmRequest(snapshot))
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
                    _history.Add(new ConversationMessage { Role = "assistant", Content = fullResponse });
                    TrimHistory();
                }
            }

            return fullResponse;
        }

        public void ClearHistory()
        {
            lock (_historyLock)
            {
                _history.Clear();
            }
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

            if (string.IsNullOrWhiteSpace(_settings.GroqApiKey))
                throw new Exception("Set your Groq API key in Smart Lolis Settings first. Voice transcription still uses Groq.");

            SmartLolisLog.Info($"Groq transcription started. Audio bytes: {audioData.Length}");

            using var request = _groqProvider.BuildTranscriptionRequest(audioData, fileName, _settings.GroqApiKey);
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
            if (IsOpenRouterProvider())
            {
                return _openRouterProvider.BuildRequest(
                    snapshot,
                    BuildEffectiveSystemPrompt(),
                    _settings.OpenRouterModel,
                    _settings.MaxTokens,
                    _settings.EnableStreaming,
                    _settings.OpenRouterApiKey);
            }

            return _groqProvider.BuildRequest(
                snapshot,
                BuildEffectiveSystemPrompt(),
                _settings.GroqModel,
                _settings.MaxTokens,
                _settings.EnableStreaming,
                _settings.GroqApiKey);
        }

        private string BuildEffectiveSystemPrompt()
        {
            string basePrompt = string.IsNullOrWhiteSpace(_settings.SystemPrompt)
                ? "You are Smart Lolis, a cute and clever desktop companion. Reply briefly, warmly, and with personality."
                : _settings.SystemPrompt.Trim();

            string currentActivity;
            string recentAction;
            lock (_stateLock)
            {
                currentActivity = _currentActivitySummary;
                recentAction = _recentActionSummary;
            }

            var context = new StringBuilder();
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
            return IsOpenRouterProvider() ? _openRouterProvider.ParseResponse(body) : _groqProvider.ParseResponse(body);
        }

        private string ParseLlmStreamEvent(string eventData)
        {
            return IsOpenRouterProvider() ? _openRouterProvider.ParseStreamEvent(eventData) : _groqProvider.ParseStreamEvent(eventData);
        }

        private string ParseLlmError(string body)
        {
            return IsOpenRouterProvider() ? _openRouterProvider.ParseError(body) : _groqProvider.ParseError(body);
        }

        private bool IsOpenRouterProvider()
        {
            return string.Equals(_settings.LlmProvider, "OpenRouter", StringComparison.OrdinalIgnoreCase);
        }

        private string GetLlmProviderName()
        {
            return IsOpenRouterProvider() ? "OpenRouter" : "Groq";
        }

        private void TrimHistory()
        {
            int maxMessages = _settings.MaxHistoryMessages > 0 ? _settings.MaxHistoryMessages : 16;
            while (_history.Count > maxMessages)
                _history.RemoveAt(0);

            while (_history.Count > 0 && _history[0].Role != "user")
                _history.RemoveAt(0);
        }
    }

    public class ConversationMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}
