using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VPet.Plugin.SmartLolis
{
    public class GroqProvider
    {
        public string DefaultApiUrl => "https://api.groq.com/openai/v1/chat/completions";
        public string DefaultTranscriptionUrl => "https://api.groq.com/openai/v1/audio/transcriptions";
        public string DefaultModel => "llama-3.3-70b-versatile";
        public string DefaultTranscriptionModel => "whisper-large-v3-turbo";

        public HttpRequestMessage BuildRequest(
            IReadOnlyList<ConversationMessage> messages,
            string systemPrompt,
            string model,
            int maxTokens,
            bool streaming,
            string apiKey)
        {
            var apiMessages = new List<object>();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                apiMessages.Add(new { role = "system", content = systemPrompt });

            apiMessages.AddRange(messages.Select(m => (object)new { role = m.Role, content = m.Content }));

            var body = new
            {
                model = string.IsNullOrWhiteSpace(model) ? DefaultModel : model,
                max_tokens = maxTokens > 0 ? maxTokens : 512,
                messages = apiMessages.ToArray(),
                stream = streaming
            };

            var request = new HttpRequestMessage(HttpMethod.Post, DefaultApiUrl);
            request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return request;
        }

        public string ParseResponse(string body)
        {
            var result = JObject.Parse(body);
            return result["choices"]?[0]?["message"]?["content"]?.ToString() ?? string.Empty;
        }

        public string ParseStreamEvent(string eventData)
        {
            var obj = JObject.Parse(eventData);
            return obj["choices"]?[0]?["delta"]?["content"]?.ToString();
        }

        public string ParseError(string body)
        {
            try
            {
                var error = JObject.Parse(body);
                return error["error"]?["message"]?.ToString() ?? body;
            }
            catch
            {
                return body;
            }
        }

        public HttpRequestMessage BuildTranscriptionRequest(byte[] audioData, string fileName, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, DefaultTranscriptionUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(audioData);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent(DefaultTranscriptionModel), "model");

            request.Content = form;
            return request;
        }

        public string ParseTranscriptionResponse(string body)
        {
            var result = JObject.Parse(body);
            return result["text"]?.ToString() ?? string.Empty;
        }
    }
}
