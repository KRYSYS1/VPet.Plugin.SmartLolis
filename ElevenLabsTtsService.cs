using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;

namespace VPet.Plugin.SmartLolis
{
    public class SmartLolisTtsService
    {
        private readonly SmartLolisSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly MediaPlayer _mediaPlayer = new();

        public SmartLolisTtsService(SmartLolisSettings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        }

        public async Task SpeakAsync(string text)
        {
            if (!_settings.EnableTts || string.IsNullOrWhiteSpace(text))
            {
                SmartLolisLog.Info("TTS skipped because speech is disabled or text is empty.");
                return;
            }

            string provider = (_settings.TtsProvider ?? "ElevenLabs").Trim();
            switch (provider.ToLowerInvariant())
            {
                case "local windows":
                    await SpeakWithLocalWindowsAsync(text);
                    break;
                case "google":
                    await SpeakWithGoogleAsync(text);
                    break;
                case "polly":
                    await SpeakWithPollyAsync(text);
                    break;
                default:
                    await SpeakWithElevenLabsAsync(text);
                    break;
            }
        }

        private async Task SpeakWithLocalWindowsAsync(string text)
        {
            try
            {
                SmartLolisLog.Info($"Sending Local Windows TTS request. Voice: {(_settings.LocalWindowsVoiceName?.Trim() ?? "(default)")} | Text length: {text.Length}");

                await RunLocalWindowsSpeechAsync(text);
                SmartLolisLog.Info("Local Windows TTS playback finished.");
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Local Windows TTS playback failed.", ex);
                throw;
            }
        }

        private Task RunLocalWindowsSpeechAsync(string text)
        {
            var tcs = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                try
                {
                    Type voiceType = Type.GetTypeFromProgID("SAPI.SpVoice");
                    if (voiceType == null)
                        throw new InvalidOperationException("Windows SAPI voice is not available on this system.");

                    dynamic voice = Activator.CreateInstance(voiceType);
                    string voiceName = _settings.LocalWindowsVoiceName?.Trim();
                    if (!string.IsNullOrWhiteSpace(voiceName))
                    {
                        bool matched = false;
                        dynamic voices = voice.GetVoices();
                        int count = voices.Count;
                        for (int i = 0; i < count; i++)
                        {
                            dynamic candidate = voices.Item(i);
                            string description = candidate.GetDescription() as string ?? string.Empty;
                            string id = candidate.Id as string ?? string.Empty;
                            if (string.Equals(description, voiceName, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, voiceName, StringComparison.OrdinalIgnoreCase))
                            {
                                voice.Voice = candidate;
                                matched = true;
                                break;
                            }
                        }

                        if (!matched)
                            SmartLolisLog.Error($"Windows local TTS voice not found: {voiceName}");
                    }

                    voice.Volume = 100;
                    voice.Rate = 0;
                    voice.Speak(text, 0);
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        private async Task SpeakWithElevenLabsAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(_settings.ElevenLabsApiKey) || string.IsNullOrWhiteSpace(_settings.ElevenLabsVoiceId))
            {
                SmartLolisLog.Error("TTS skipped because ElevenLabs API key or Voice ID is empty.");
                return;
            }

            try
            {
                string url = $"https://api.elevenlabs.io/v1/text-to-speech/{_settings.ElevenLabsVoiceId}";
                var body = new
                {
                    text,
                    model_id = "eleven_multilingual_v2",
                    voice_settings = new
                    {
                        stability = 0.45,
                        similarity_boost = 0.75
                    }
                };

                SmartLolisLog.Info($"Sending ElevenLabs TTS request. Voice ID: {_settings.ElevenLabsVoiceId}");

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("xi-api-key", _settings.ElevenLabsApiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));
                request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    SmartLolisLog.Error($"ElevenLabs returned {(int)response.StatusCode} {response.ReasonPhrase}.{Environment.NewLine}{errorBody}");
                    return;
                }

                byte[] audioBytes = await response.Content.ReadAsByteArrayAsync();
                await PlayAudioAsync(audioBytes, "mp3");
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("ElevenLabs playback failed.", ex);
                throw;
            }
        }

        private async Task SpeakWithGoogleAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(_settings.GoogleApiKey))
            {
                SmartLolisLog.Error("TTS skipped because Google API key is empty.");
                return;
            }

            string voiceName = string.IsNullOrWhiteSpace(_settings.GoogleVoiceName)
                ? "ru-RU-Standard-A"
                : _settings.GoogleVoiceName.Trim();

            string languageCode = GetGoogleLanguageCode(voiceName);

            try
            {
                string url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={Uri.EscapeDataString(_settings.GoogleApiKey)}";
                var body = new
                {
                    input = new { text },
                    voice = new
                    {
                        languageCode,
                        name = voiceName
                    },
                    audioConfig = new
                    {
                        audioEncoding = "MP3"
                    }
                };

                SmartLolisLog.Info($"Sending Google TTS request. Voice: {voiceName}");

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    SmartLolisLog.Error($"Google TTS returned {(int)response.StatusCode} {response.ReasonPhrase}.{Environment.NewLine}{responseBody}");
                    return;
                }

                var googleResponse = JsonConvert.DeserializeObject<GoogleTtsResponse>(responseBody);
                if (string.IsNullOrWhiteSpace(googleResponse?.AudioContent))
                {
                    SmartLolisLog.Error("Google TTS returned an empty audio payload.");
                    return;
                }

                byte[] audioBytes = Convert.FromBase64String(googleResponse.AudioContent);
                await PlayAudioAsync(audioBytes, "mp3");
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Google TTS playback failed.", ex);
                throw;
            }
        }

        private async Task SpeakWithPollyAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(_settings.PollyAccessKey) ||
                string.IsNullOrWhiteSpace(_settings.PollySecretKey) ||
                string.IsNullOrWhiteSpace(_settings.PollyRegion) ||
                string.IsNullOrWhiteSpace(_settings.PollyVoiceId))
            {
                SmartLolisLog.Error("TTS skipped because Amazon Polly credentials, region, or voice id is empty.");
                return;
            }

            try
            {
                string region = _settings.PollyRegion.Trim();
                string host = $"polly.{region}.amazonaws.com";
                string url = $"https://{host}/v1/speech";
                string amzDate = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
                string dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");
                string payload = JsonConvert.SerializeObject(new
                {
                    OutputFormat = "mp3",
                    Text = text,
                    VoiceId = _settings.PollyVoiceId.Trim(),
                    TextType = "text"
                });

                string payloadHash = ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
                string canonicalHeaders =
                    $"content-type:application/json\n" +
                    $"host:{host}\n" +
                    $"x-amz-content-sha256:{payloadHash}\n" +
                    $"x-amz-date:{amzDate}\n";
                const string signedHeaders = "content-type;host;x-amz-content-sha256;x-amz-date";
                string canonicalRequest = $"POST\n/v1/speech\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
                string credentialScope = $"{dateStamp}/{region}/polly/aws4_request";
                string stringToSign =
                    $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)))}";

                byte[] signingKey = GetSignatureKey(_settings.PollySecretKey.Trim(), dateStamp, region, "polly");
                string signature = ToHexString(HmacSha256(signingKey, stringToSign));
                string authorizationHeader =
                    $"AWS4-HMAC-SHA256 Credential={_settings.PollyAccessKey.Trim()}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

                SmartLolisLog.Info($"Sending Amazon Polly TTS request. Voice ID: {_settings.PollyVoiceId}");

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
                request.Headers.TryAddWithoutValidation("X-Amz-Date", amzDate);
                request.Headers.TryAddWithoutValidation("X-Amz-Content-Sha256", payloadHash);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    SmartLolisLog.Error($"Amazon Polly returned {(int)response.StatusCode} {response.ReasonPhrase}.{Environment.NewLine}{errorBody}");
                    return;
                }

                byte[] audioBytes = await response.Content.ReadAsByteArrayAsync();
                await PlayAudioAsync(audioBytes, "mp3");
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Amazon Polly playback failed.", ex);
                throw;
            }
        }

        private async Task PlayAudioAsync(byte[] audioBytes, string extension)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"smartlolis-{Guid.NewGuid():N}.{extension}");
            await File.WriteAllBytesAsync(tempFile, audioBytes);

            SmartLolisLog.Info($"TTS audio saved to temp file: {tempFile}");

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Open(new Uri(tempFile));
                _mediaPlayer.Play();
            });

            SmartLolisLog.Info("MediaPlayer started playback.");
        }

        private static string GetGoogleLanguageCode(string voiceName)
        {
            string[] parts = voiceName.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0]}-{parts[1]}";

            return "en-US";
        }

        private static byte[] HmacSha256(byte[] key, string data)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
        {
            byte[] kDate = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + key), dateStamp);
            byte[] kRegion = HmacSha256(kDate, regionName);
            byte[] kService = HmacSha256(kRegion, serviceName);
            return HmacSha256(kService, "aws4_request");
        }

        private static string ToHexString(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private sealed class GoogleTtsResponse
        {
            [JsonProperty("audioContent")]
            public string AudioContent { get; set; }
        }
    }
}
