using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace VPet.Plugin.SmartLolis
{
    public partial class SmartLolisChatHistoryWindow : Window
    {
        private readonly SmartLolisPlugin _plugin;
        private string _currentUiLanguage = "ru";
        private string _originalHistoryText;
        private string _originalCompressedText;

        public SmartLolisChatHistoryWindow(SmartLolisPlugin plugin, string uiLanguage = "ru")
        {
            InitializeComponent();
            _plugin = plugin;
            _currentUiLanguage = string.IsNullOrWhiteSpace(uiLanguage) ? "ru" : uiLanguage;
            ApplyLocalization();
            RefreshHistory();
        }

        private void RefreshHistory()
        {
            var history = _plugin.LlmService?.GetHistorySnapshot() ?? new List<ConversationMessage>();
            var sb = new StringBuilder();
            foreach (var msg in history)
            {
                string label = msg.Role switch
                {
                    "user" => "You",
                    "assistant" => "Lolis",
                    "system" => "System",
                    _ => msg.Role
                };
                sb.AppendLine($"[{label}]");
                sb.AppendLine(msg.Content);
                sb.AppendLine();
            }
            txtHistory.Text = sb.ToString();
            txtCount.Text = $"{history.Count} messages";

            string compressed = _plugin.LlmService?.GetCompressedMemory() ?? string.Empty;
            txtCompressed.Text = compressed;

            TakeSnapshot();
            CheckDirty();

            txtHistory.TextChanged += AnyChange;
            txtCompressed.TextChanged += AnyChange;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtHistory.ScrollToHome();
            }));
        }

        private void TakeSnapshot()
        {
            _originalHistoryText = txtHistory.Text ?? string.Empty;
            _originalCompressedText = txtCompressed.Text ?? string.Empty;
        }

        private void AnyChange(object sender, EventArgs e)
        {
            CheckDirty();
        }

        private void CheckDirty()
        {
            bool dirty = false;
            dirty |= !string.Equals(txtHistory.Text ?? "", _originalHistoryText ?? "", StringComparison.Ordinal);
            dirty |= !string.Equals(txtCompressed.Text ?? "", _originalCompressedText ?? "", StringComparison.Ordinal);

            btnSave.Style = dirty
                ? (Style)FindResource("PrimaryButtonStyle")
                : (Style)FindResource("SecondaryButtonStyle");
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshHistory();
        }

        private async void BtnCompress_Click(object sender, RoutedEventArgs e)
        {
            btnCompress.IsEnabled = false;
            txtCompressed.Text = _currentUiLanguage switch
            {
                "zh" => "正在压缩记忆...",
                "ru" => "Сжатие памяти...",
                _ => "Compressing memory..."
            };

            try
            {
                string result = await _plugin.LlmService.CompressHistoryAsync(4, _currentUiLanguage);
                txtCompressed.Text = result;
                RefreshHistory();
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Memory compression failed from chat history window.", ex);
                txtCompressed.Text = $"Error: {ex.Message}";
            }
            finally
            {
                btnCompress.IsEnabled = true;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var history = ParseHistoryFromText(txtHistory.Text);
            _plugin.LlmService?.ReplaceHistory(history);
            _plugin.LlmService?.SetCompressedMemory(txtCompressed.Text);
            SmartLolisLog.Info("Chat history and compressed memory saved from history window.");
            TakeSnapshot();
            CheckDirty();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = "SmartLolis_ChatHistory",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var data = new PersistedChatData
                {
                    Messages = ParseHistoryFromText(txtHistory.Text),
                    CompressedMemory = txtCompressed.Text ?? string.Empty
                };
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
                SmartLolisLog.Info($"Chat history exported to {dialog.FileName}");
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Failed to export chat history.", ex);
                MessageBox.Show(ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = "SmartLolis_ChatHistory",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string content = File.ReadAllText(dialog.FileName, Encoding.UTF8);

                if (dialog.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<PersistedChatData>(content);
                    if (data != null)
                    {
                        var sb = new StringBuilder();
                        foreach (var msg in data.Messages ?? new List<ConversationMessage>())
                        {
                            string label = msg.Role switch
                            {
                                "user" => "You",
                                "assistant" => "Lolis",
                                "system" => "System",
                                _ => msg.Role
                            };
                            sb.AppendLine($"[{label}]");
                            sb.AppendLine(msg.Content);
                            sb.AppendLine();
                        }
                        txtHistory.Text = sb.ToString();
                        txtCount.Text = $"{(data.Messages?.Count ?? 0)} messages";

                        if (!string.IsNullOrWhiteSpace(data.CompressedMemory))
                            txtCompressed.Text = data.CompressedMemory;
                    }
                }
                else
                {
                    txtHistory.Text = content;
                    txtCount.Text = "? messages";
                }

                SmartLolisLog.Info($"Chat history imported from {dialog.FileName}");
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Failed to import chat history.", ex);
                MessageBox.Show(ex.Message, "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static List<ConversationMessage> ParseHistoryFromText(string text)
        {
            var messages = new List<ConversationMessage>();
            if (string.IsNullOrWhiteSpace(text))
                return messages;

            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            string currentRole = null;
            var contentLines = new List<string>();

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    if (currentRole != null && contentLines.Count > 0)
                    {
                        string content = string.Join("\n", contentLines).Trim();
                        if (!string.IsNullOrWhiteSpace(content))
                            messages.Add(new ConversationMessage { Role = currentRole, Content = content });
                    }

                    string label = trimmed[1..^1].Trim();
                    currentRole = label switch
                    {
                        "You" => "user",
                        "Lolis" => "assistant",
                        "System" => "system",
                        _ => label.ToLowerInvariant()
                    };
                    contentLines.Clear();
                }
                else
                {
                    contentLines.Add(line);
                }
            }

            if (currentRole != null && contentLines.Count > 0)
            {
                string content = string.Join("\n", contentLines).Trim();
                if (!string.IsNullOrWhiteSpace(content))
                    messages.Add(new ConversationMessage { Role = currentRole, Content = content });
            }

            return messages;
        }

        private void ApplyLocalization()
        {
            Dictionary<string, string> text = _currentUiLanguage switch
            {
                "en" => BuildEnglishText(),
                "zh" => BuildChineseText(),
                _ => BuildRussianText()
            };

            Title = text["window_title"];
            txtTitle.Text = text["title"];
            txtSubtitle.Text = text["subtitle"];
            btnRefresh.Content = text["refresh"];
            txtCompressedLabel.Text = text["compressed_label"];
            txtCompressedHint.Text = text["compressed_hint"];
            btnCompress.Content = text["compress"];
            btnExport.Content = text["export"];
            btnImport.Content = text["import"];
            btnSave.Content = text["save"];
            btnCancel.Content = text["close"];
        }

        private static Dictionary<string, string> BuildRussianText() => new()
        {
            ["window_title"] = "История чата — Smart Lolis",
            ["title"] = "ИСТОРИЯ ЧАТА",
            ["subtitle"] = "Просмотр и редактирование сообщений разговора.",
            ["refresh"] = "Обновить",
            ["compressed_label"] = "СЖАТАЯ ПАМЯТЬ",
            ["compressed_hint"] = "Сводка старых разговоров. Можно редактировать вручную или вставить сводку от внешнего AI.",
            ["compress"] = "Сжать память",
            ["export"] = "Экспорт",
            ["import"] = "Импорт",
            ["save"] = "Сохранить",
            ["close"] = "Закрыть"
        };

        private static Dictionary<string, string> BuildEnglishText() => new()
        {
            ["window_title"] = "Chat History — Smart Lolis",
            ["title"] = "CHAT HISTORY",
            ["subtitle"] = "View and edit conversation messages.",
            ["refresh"] = "Refresh",
            ["compressed_label"] = "COMPRESSED MEMORY",
            ["compressed_hint"] = "Summary of older conversations. You can edit this manually or paste an AI-processed summary.",
            ["compress"] = "Compress Memory",
            ["export"] = "Export",
            ["import"] = "Import",
            ["save"] = "Save",
            ["close"] = "Close"
        };

        private static Dictionary<string, string> BuildChineseText() => new()
        {
            ["window_title"] = "聊天记录 — Smart Lolis",
            ["title"] = "聊天记录",
            ["subtitle"] = "查看和编辑对话消息。",
            ["refresh"] = "刷新",
            ["compressed_label"] = "压缩记忆",
            ["compressed_hint"] = "旧对话的摘要。可以手动编辑，也可以粘贴外部AI处理后的摘要。",
            ["compress"] = "压缩记忆",
            ["export"] = "导出",
            ["import"] = "导入",
            ["save"] = "保存",
            ["close"] = "关闭"
        };
    }
}
