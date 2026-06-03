using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VPet.Plugin.SmartLolis
{
    public class OpenAiCompatibleProvider
    {
        public HttpRequestMessage BuildRequest(
            string apiUrl,
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
                model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model,
                max_tokens = maxTokens > 0 ? maxTokens : 512,
                messages = apiMessages.ToArray(),
                stream = streaming
            };

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
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
                return error["error"]?.ToString() ?? body;
            }
            catch
            {
                return body;
            }
        }
    }
}
