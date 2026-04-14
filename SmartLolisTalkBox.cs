using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Panuon.WPF.UI;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet.Plugin.SmartLolis
{
    public class SmartLolisTalkBox : TalkBox
    {
        private static readonly TimeSpan VoiceAutoSendDelay = TimeSpan.FromMilliseconds(1400);
        private static readonly TimeSpan VoiceAutoSendTimeout = TimeSpan.FromSeconds(20);

        private readonly SmartLolisPlugin _plugin;
        private readonly Button _btnVoice;
        private HiddenDictationHost _dictationHost;
        private PendingActivitySelection _pendingSelection;
        private PendingItemSelection _pendingItemSelection;
        private bool _isStartingVoiceInput;
        private bool _voiceSessionActive;
        private bool _voiceMonitorRunning;
        private string _voiceSessionInitialText = string.Empty;
        private string _lastObservedVoiceText = string.Empty;
        private DateTime _lastVoiceTextChangeUtc = DateTime.MinValue;
        private DateTime _voiceSessionStartedUtc = DateTime.MinValue;
        private readonly string _defaultTalkWatermark;

        public SmartLolisTalkBox(SmartLolisPlugin plugin) : base(plugin)
        {
            _plugin = plugin;
            _btnVoice = InitializeVoiceInputButton();
            _dictationHost = new HiddenDictationHost();
            _defaultTalkWatermark = TextBoxHelper.GetWatermark(tbTalk)?.ToString() ?? string.Empty;
            tbTalk.GotKeyboardFocus += (_, _) => ApplyFocusedTextInputVisualState();
            tbTalk.LostKeyboardFocus += (_, _) => RestoreTextInputVisualState();
            RestoreTextInputVisualState();
            Loaded += (_, _) =>
            {
                _plugin.VoiceOverlayManager?.CleanupNow();
                AttachVoiceButtonOutsideInputFrame();
            };
        }

        public override string APIName => "Smart Lolis";

        public override void Responded(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            if (TryHandleCommandMode(content))
                return;

            DisplayThink();

            if (!_plugin.PluginSettings.EnableLlm)
            {
                DisplayThinkToSayRnd("LLM is disabled in Smart Lolis Settings.");
                return;
            }

            bool useOpenRouter = string.Equals(_plugin.PluginSettings.LlmProvider, "OpenRouter", StringComparison.OrdinalIgnoreCase);
            string llmApiKey = useOpenRouter ? _plugin.PluginSettings.OpenRouterApiKey : _plugin.PluginSettings.GroqApiKey;
            if (string.IsNullOrWhiteSpace(llmApiKey))
            {
                DisplayThinkToSayRnd(useOpenRouter
                    ? "Set your OpenRouter API key in Smart Lolis Settings first."
                    : "Set your Groq API key in Smart Lolis Settings first.");
                return;
            }

            Dispatcher.Invoke(() => IsEnabled = false);

            if (_plugin.PluginSettings.EnableStreaming)
            {
                var streamingSay = new SayInfoWithStream();
                DisplayThinkToSayRnd(streamingSay);

                Task.Run(async () =>
                {
                    try
                    {
                        string fullResponse = await _plugin.LlmService.SendMessageAsync(content, delta =>
                        {
                            streamingSay.UpdateText(delta);
                        });

                        streamingSay.FinishGenerate();

                    }
                    catch (Exception ex)
                    {
                        streamingSay.UpdateAllText("Smart Lolis request failed\n" + ex.Message);
                        streamingSay.FinishGenerate();
                    }
                    finally
                    {
                        Dispatcher.Invoke(() => IsEnabled = true);
                    }
                });
            }
            else
            {
                Task.Run(async () =>
                {
                    try
                    {
                        string response = await _plugin.LlmService.SendMessageAsync(content);
                        DisplayThinkToSayRnd(string.IsNullOrWhiteSpace(response) ? "(No response)" : response);

                    }
                    catch (Exception ex)
                    {
                        DisplayThinkToSayRnd("Smart Lolis request failed\n" + ex.Message);
                    }
                    finally
                    {
                        Dispatcher.Invoke(() => IsEnabled = true);
                    }
                });
            }
        }

        public override void Setting()
        {
            _plugin.Setting();
        }

        public void RefreshUiFromSettings()
        {
            if (_btnVoice == null || _plugin?.PluginSettings == null)
                return;

            void Apply()
            {
                bool enabled = _plugin.PluginSettings.EnableVoiceInputButton;
                _btnVoice.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
                _btnVoice.IsHitTestVisible = enabled;
                _btnVoice.IsEnabled = enabled;

                if (!enabled && _voiceSessionActive)
                    StopVoiceInputSession(true);
            }

            if (Dispatcher == null || Dispatcher.CheckAccess())
            {
                Apply();
                return;
            }

            Dispatcher.Invoke(Apply);
        }

        private Button InitializeVoiceInputButton()
        {
            var btnVoice = new Button
            {
                Width = 52,
                Height = 52,
                Margin = new Thickness(0),
                ToolTip = "Voice input",
                Focusable = false,
                IsTabStop = false,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(1.4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(106, 196, 245)),
                Background = Brushes.White,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = CreateMicIcon(),
                Template = CreateRoundButtonTemplate()
            };

            btnVoice.PreviewMouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;
                StartVoiceInput();
            };
            bool enabled = _plugin.PluginSettings.EnableVoiceInputButton;
            btnVoice.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            btnVoice.IsHitTestVisible = enabled;
            btnVoice.IsEnabled = enabled;
            return btnVoice;
        }

        private void AttachVoiceButtonOutsideInputFrame()
        {
            Border inputBorder;
            Thickness hostMargin;

            if (Content is Border directBorder)
            {
                inputBorder = directBorder;
                hostMargin = directBorder.Margin;
            }
            else if (Content is Grid existingWrapper)
            {
                inputBorder = existingWrapper.Children.OfType<Border>().FirstOrDefault();
                if (inputBorder == null)
                    return;

                hostMargin = existingWrapper.Margin;
                if (inputBorder.Parent is Panel oldWrapperPanel)
                    oldWrapperPanel.Children.Remove(inputBorder);

                existingWrapper.Children.Clear();
                Content = null;
            }
            else
                return;

            if (_btnVoice.Parent is Panel oldPanel)
                oldPanel.Children.Remove(_btnVoice);

            RefreshUiFromSettings();

            var wrapper = new Grid
            {
                VerticalAlignment = VerticalAlignment.Top,
                Margin = hostMargin
            };
            wrapper.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            wrapper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            wrapper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            inputBorder.Margin = new Thickness(0);
            _btnVoice.Margin = new Thickness(0, 0, 0, 0);
            _btnVoice.BorderBrush = inputBorder.BorderBrush;
            _btnVoice.BorderThickness = inputBorder.BorderThickness;

            void SyncVoiceButtonSize()
            {
                double targetSize = inputBorder.ActualHeight > 1 ? inputBorder.ActualHeight : 38;
                _btnVoice.Width = targetSize;
                _btnVoice.Height = targetSize;
            }

            SyncVoiceButtonSize();
            Grid.SetColumn(_btnVoice, 0);
            Grid.SetColumn(inputBorder, 2);

            Content = null;
            wrapper.Children.Add(_btnVoice);
            wrapper.Children.Add(inputBorder);
            Content = wrapper;
        }

        private async void StartVoiceInput()
        {
            if (_isStartingVoiceInput)
                return;

            if (_voiceSessionActive)
            {
                StopVoiceInputSession(true);
                return;
            }

            _isStartingVoiceInput = true;
            UpdateVoiceButtonState();

            try
            {
                PrepareHiddenTextFocus();
                BeginVoiceSessionMonitor();
                await Task.Delay(60);
                SendWinH();
                SmartLolisLog.Info("Windows voice input started from Mic button.");
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Failed to start Windows voice input.", ex);
                DisplayThinkToSayRnd("Could not start Windows voice input.");
            }
            finally
            {
                _isStartingVoiceInput = false;
                UpdateVoiceButtonState();
            }
        }

        private void StopVoiceInputSession(bool closeWindowsDictation)
        {
            _voiceSessionActive = false;
            Dispatcher.Invoke(() =>
            {
                CloseDictationHost();
                UpdateVoiceButtonState();
            });

            if (!closeWindowsDictation)
                return;

            try
            {
                SendWinH();
                SmartLolisLog.Info("Windows voice input stopped from Mic button.");
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Failed to stop Windows voice input.", ex);
            }
        }

        private void BeginVoiceSessionMonitor()
        {
            _voiceSessionActive = true;
            UpdateVoiceButtonState();
            _voiceSessionStartedUtc = DateTime.UtcNow;
            _voiceSessionInitialText = Dispatcher.Invoke(() => tbTalk.Text ?? string.Empty);
            _lastObservedVoiceText = _voiceSessionInitialText;
            _lastVoiceTextChangeUtc = DateTime.UtcNow;

            if (_voiceMonitorRunning)
                return;

            _voiceMonitorRunning = true;
            _ = MonitorVoiceInputAsync();
        }

        private async Task MonitorVoiceInputAsync()
        {
            try
            {
                while (_voiceSessionActive)
                {
                    await Task.Delay(250);

                    string currentText = Dispatcher.Invoke(GetCurrentVoiceText);
                    if (!string.Equals(currentText, _lastObservedVoiceText, StringComparison.Ordinal))
                    {
                        _lastObservedVoiceText = currentText;
                        _lastVoiceTextChangeUtc = DateTime.UtcNow;
                        Dispatcher.Invoke(() => tbTalk.Text = currentText);
                    }

                    if (DateTime.UtcNow - _voiceSessionStartedUtc > VoiceAutoSendTimeout)
                    {
                        SmartLolisLog.Info("Voice input auto-send monitor timed out.");
                        _voiceSessionActive = false;
                        Dispatcher.Invoke(() =>
                        {
                            CloseDictationHost();
                            UpdateVoiceButtonState();
                        });
                        break;
                    }

                    if (currentText.Length <= _voiceSessionInitialText.Length)
                        continue;

                    if (DateTime.UtcNow - _lastVoiceTextChangeUtc < VoiceAutoSendDelay)
                        continue;

                    _voiceSessionActive = false;
                    Dispatcher.Invoke(() =>
                    {
                        tbTalk.Text = currentText;
                        CloseDictationHost();
                        UpdateVoiceButtonState();
                        AutoSendVoiceText();
                    });
                    break;
                }
            }
            finally
            {
                _voiceMonitorRunning = false;
                Dispatcher.Invoke(UpdateVoiceButtonState);
            }
        }

        private void AutoSendVoiceText()
        {
            string currentText = tbTalk.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(currentText))
                return;

            if (currentText.Length <= _voiceSessionInitialText.Length)
                return;

            SmartLolisLog.Info("Auto-sending voice-typed message.");
            btnSend.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        private void PrepareHiddenTextFocus()
        {
            Dispatcher.Invoke(() =>
            {
                RestoreTextInputVisualState();
                ShowDictationHost();
            });
        }

        private void RestoreTextInputVisualState()
        {
            tbTalk.CaretBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            tbTalk.ClearValue(TextBox.SelectionBrushProperty);
            tbTalk.SelectionOpacity = 0.4;
            tbTalk.IsReadOnly = false;
            tbTalk.IsEnabled = true;

            if (!_voiceSessionActive && string.IsNullOrWhiteSpace(tbTalk.Text))
                TextBoxHelper.SetWatermark(tbTalk, _defaultTalkWatermark);
        }

        private void ApplyFocusedTextInputVisualState()
        {
            if (_voiceSessionActive || _isStartingVoiceInput)
                return;

            tbTalk.CaretBrush = new SolidColorBrush(Color.FromRgb(33, 76, 109));
            tbTalk.ClearValue(TextBox.SelectionBrushProperty);
            tbTalk.SelectionOpacity = 0.4;
            TextBoxHelper.SetWatermark(tbTalk, string.Empty);
        }

        private void ShowDictationHost()
        {
            Point screenPoint = tbTalk.PointToScreen(new Point(24, tbTalk.ActualHeight / 2));
            double width = tbTalk.ActualWidth <= 1 ? 220 : tbTalk.ActualWidth;
            double height = tbTalk.ActualHeight <= 1 ? 34 : tbTalk.ActualHeight;
            string initialText = tbTalk.Text ?? string.Empty;

            _dictationHost ??= new HiddenDictationHost();
            _dictationHost.ShowAt((int)screenPoint.X, (int)(screenPoint.Y - height / 2), (int)width, (int)height, initialText);
        }

        private string GetCurrentVoiceText()
        {
            if (_dictationHost != null && _dictationHost.Visible)
                return _dictationHost.Text ?? string.Empty;

            return tbTalk.Text ?? string.Empty;
        }

        private void CloseDictationHost()
        {
            _dictationHost?.Hide();
        }

        private void UpdateVoiceButtonState()
        {
            if (_isStartingVoiceInput || _voiceSessionActive)
            {
                _btnVoice.Content = CreateMicIcon();
                _btnVoice.ToolTip = _isStartingVoiceInput ? "Starting voice input" : "Voice input active";
                _btnVoice.Background = new SolidColorBrush(Color.FromRgb(201, 229, 250));
                _btnVoice.BorderBrush = new SolidColorBrush(Color.FromRgb(96, 184, 241));
                return;
            }

            _btnVoice.Content = CreateMicIcon();
            _btnVoice.ToolTip = "Voice input";
            _btnVoice.Background = Brushes.White;
            _btnVoice.BorderBrush = new SolidColorBrush(Color.FromRgb(106, 196, 245));
        }

        private bool TryHandleCommandMode(string content)
        {
            if (_pendingSelection != null)
            {
                string pendingNormalized = NormalizeCommandText(content);
                if (IsPendingSelectionCancelCommand(pendingNormalized))
                {
                    _pendingSelection = null;
                    SpeakLocalResponse("Okay, canceled the activity choice.");
                    return true;
                }

                if (TryResolvePendingSelection(content, pendingNormalized))
                    return true;
            }

            if (_pendingItemSelection != null)
            {
                string pendingNormalized = NormalizeCommandText(content);
                if (IsPendingSelectionCancelCommand(pendingNormalized))
                {
                    _pendingItemSelection = null;
                    SpeakLocalResponse("Okay, canceled the item choice.");
                    return true;
                }

                if (TryResolvePendingItemSelection(content, pendingNormalized))
                    return true;
            }

            if (!_plugin.PluginSettings.EnableCommandMode)
                return false;

            string commandText;
            if (!TryExtractCommandText(content, out commandText))
            {
                if (IsLikelyQuestion(content))
                    return false;

                string rawNormalized = NormalizeCommandText(content);
                if (!LooksLikeDirectCommand(rawNormalized))
                    return false;

                commandText = content;
            }

            string normalized = NormalizeCommandText(commandText);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            if (ContainsAny(normalized,
                "\u0441\u0442\u043e\u043f",
                "\u043e\u0441\u0442\u0430\u043d\u043e\u0432",
                "\u043f\u0440\u0435\u043a\u0440\u0430\u0442\u0438",
                "\u0445\u0432\u0430\u0442\u0438\u0442",
                "\u043e\u0442\u043c\u0435\u043d\u0438",
                "cancel", "stop"))
            {
                StopCurrentActivity();
                return true;
            }

            if (TryHandleActivityCommand(commandText, normalized, GraphHelper.Work.WorkType.Study))
                return true;

            if (TryHandleActivityCommand(commandText, normalized, GraphHelper.Work.WorkType.Work))
                return true;

            if (TryHandleActivityCommand(commandText, normalized, GraphHelper.Work.WorkType.Play))
                return true;

            if (TryHandleItemCommand(commandText, normalized))
                return true;

            return false;
        }

        private bool TryHandleItemCommand(string originalText, string normalizedText)
        {
            List<Food> allItems = GetAvailableFoods();
            if (allItems.Count == 0)
                return false;

            if (!ContainsItemIntent(normalizedText))
                return false;

            List<Food> categoryItems = GetItemsForCommand(normalizedText, allItems);
            Food exactMatch = FindFoodMatch(originalText, categoryItems);
            if (exactMatch != null)
            {
                TakeItem(exactMatch);
                return true;
            }

            if (categoryItems.Count == 1)
            {
                TakeItem(categoryItems[0]);
                return true;
            }

            if (categoryItems.Count == 0)
            {
                Food fallbackMatch = FindFoodMatch(originalText, allItems);
                if (fallbackMatch != null)
                {
                    TakeItem(fallbackMatch);
                    return true;
                }

                SpeakLocalResponse("I couldn't find a matching item to buy or use.");
                return true;
            }

            string categoryLabel = GetItemCategoryLabel(categoryItems);
            _pendingItemSelection = new PendingItemSelection(categoryLabel, categoryItems);
            SpeakLocalResponse($"Sure. Which {categoryLabel} do you want: {string.Join(", ", categoryItems.Select(i => i.TranslateName))}?");
            return true;
        }

        private bool TryHandleActivityCommand(string originalText, string normalizedText, GraphHelper.Work.WorkType type)
        {
            if (!ContainsActivityKeyword(normalizedText, type))
                return false;

            List<GraphHelper.Work> works = GetAvailableWorks(type);
            if (works.Count == 0)
            {
                SpeakLocalResponse($"I couldn't find any {GetTypeLabel(type)} activities in this build.");
                return true;
            }

            GraphHelper.Work exactMatch = FindWorkMatch(originalText, works);
            if (exactMatch != null)
            {
                StartActivity(exactMatch);
                return true;
            }

            if (works.Count == 1)
            {
                StartActivity(works[0]);
                return true;
            }

            _pendingSelection = new PendingActivitySelection(type, works);
            SpeakLocalResponse($"Sure. Which {GetTypeLabel(type)} do you want: {string.Join(", ", works.Select(w => w.NameTrans))}?");
            return true;
        }

        private bool TryResolvePendingSelection(string originalText, string normalizedText)
        {
            if (_pendingSelection == null)
                return false;

            if (ContainsAny(normalizedText,
                "\u043d\u0435 \u0432\u0430\u0436\u043d\u043e",
                "\u043b\u044e\u0431",
                "any",
                "\u043b\u044e\u0431\u0430\u044f",
                "\u043b\u044e\u0431\u043e\u0439",
                "\u0440\u0430\u043d\u0434\u043e\u043c",
                "random",
                "\u043f\u0435\u0440\u0432"))
            {
                StartActivity(_pendingSelection.Options[0]);
                _pendingSelection = null;
                return true;
            }

            if (ContainsAny(normalizedText,
                "\u043e\u0442\u043c\u0435\u043d\u0430",
                "\u043e\u0442\u043c\u0435\u043d\u0438",
                "cancel",
                "\u043d\u0435 \u043d\u0430\u0434\u043e",
                "\u043d\u0435 \u043d\u0443\u0436\u043d\u043e"))
            {
                _pendingSelection = null;
                SpeakLocalResponse("Okay, command canceled.");
                return true;
            }

            GraphHelper.Work match = FindWorkMatch(originalText, _pendingSelection.Options);
            if (match == null)
            {
                string typeLabel = GetTypeLabel(_pendingSelection.Type);
                SpeakLocalResponse($"I didn't catch the {typeLabel} name. Available: {string.Join(", ", _pendingSelection.Options.Select(w => w.NameTrans))}.");
                return true;
            }

            StartActivity(match);
            _pendingSelection = null;
            return true;
        }

        private bool TryResolvePendingItemSelection(string originalText, string normalizedText)
        {
            if (_pendingItemSelection == null)
                return false;

            if (ContainsAny(normalizedText,
                "\u043d\u0435 \u0432\u0430\u0436\u043d\u043e",
                "\u043b\u044e\u0431",
                "any",
                "\u043b\u044e\u0431\u0430\u044f",
                "\u043b\u044e\u0431\u043e\u0439",
                "\u0440\u0430\u043d\u0434\u043e\u043c",
                "random",
                "\u043f\u0435\u0440\u0432"))
            {
                TakeItem(_pendingItemSelection.Options[0]);
                _pendingItemSelection = null;
                return true;
            }

            if (ContainsAny(normalizedText,
                "\u043e\u0442\u043c\u0435\u043d\u0430",
                "\u043e\u0442\u043c\u0435\u043d\u0438",
                "cancel",
                "\u043d\u0435 \u043d\u0430\u0434\u043e",
                "\u043d\u0435 \u043d\u0443\u0436\u043d\u043e"))
            {
                _pendingItemSelection = null;
                SpeakLocalResponse("Okay, command canceled.");
                return true;
            }

            Food match = FindFoodMatch(originalText, _pendingItemSelection.Options);
            if (match == null)
            {
                SpeakLocalResponse($"I didn't catch the item name. Available: {string.Join(", ", _pendingItemSelection.Options.Select(i => i.TranslateName))}.");
                return true;
            }

            TakeItem(match);
            _pendingItemSelection = null;
            return true;
        }

        private void StartActivity(GraphHelper.Work work)
        {
            _pendingSelection = null;
            bool started = false;

            _plugin.MW.Dispatcher.Invoke(() =>
            {
                started = _plugin.MW.Main.StartWork(work.Double(_plugin.MW.Set["workmenu"].GetInt("double_" + work.Name, 1)));
            });

            if (started)
            {
                string activitySummary = $"{GetTypeLabel(work.Type)}: {work.NameTrans}";
                _plugin.LlmService?.SetCurrentActivity(activitySummary);
                _plugin.LlmService?.SetRecentAction($"Started {activitySummary}.");
                SpeakLocalResponse($"Okay, starting {work.NameTrans}.");
            }
            else
            {
                _plugin.LlmService?.SetRecentAction($"Tried to start {work.NameTrans}, but the game blocked it.");
                SpeakLocalResponse($"I couldn't start {work.NameTrans}. The game blocked it.");
            }
        }

        private void StopCurrentActivity()
        {
            _pendingSelection = null;
            _pendingItemSelection = null;
            bool hadActiveWork = false;

            _plugin.MW.Dispatcher.Invoke(() =>
            {
                hadActiveWork = _plugin.MW.Main.State == Main.WorkingState.Work;
                _plugin.MW.Main.WorkTimer.Stop(reason: WorkTimer.FinishWorkInfo.StopReason.MenualStop);
            });

            if (hadActiveWork)
            {
                _plugin.LlmService?.ClearCurrentActivity();
                _plugin.LlmService?.SetRecentAction("Stopped the current activity.");
            }
            else
            {
                _plugin.LlmService?.SetRecentAction("Tried to stop an activity, but nothing was running.");
            }

            SpeakLocalResponse(hadActiveWork
                ? "Okay, stopping the current activity."
                : "Nothing is running right now.");
        }

        private void SpeakLocalResponse(string text)
        {
            DisplayThinkToSayRnd(text);
        }

        private List<GraphHelper.Work> GetAvailableWorks(GraphHelper.Work.WorkType type)
        {
            return _plugin.MW.Core.Graph.GraphConfig.Works
                .Where(w => w.Type == type)
                .ToList();
        }

        private List<Food> GetAvailableFoods()
        {
            return _plugin.MW.Foods?
                .Where(item => item != null && item.CanUse)
                .ToList() ?? new List<Food>();
        }

        private List<Food> GetItemsForCommand(string normalizedText, List<Food> allItems)
        {
            bool wantsDrink = ContainsAny(normalizedText,
                "\u043d\u0430\u043f\u043e\u0438",
                "\u043f\u043e\u043f\u0438\u0442\u044c",
                "\u0434\u0430\u0439 \u0432\u043e\u0434\u0443",
                "drink", "water",
                "\u0432\u043e\u0434\u0430",
                "\u043d\u0430\u043f\u0438\u0442\u043e\u043a",
                "\u043f\u0438\u0442\u044c",
                "\u0432\u044b\u043f\u0438\u0442\u044c",
                "\u559D", "\u559D\u6C34", "\u6C34", "\u996E\u6599", "\u996E\u7528");
            bool wantsMedicine = ContainsAny(normalizedText,
                "\u0434\u0430\u0439 \u043b\u0435\u043a\u0430\u0440",
                "\u043b\u0435\u0447\u0438",
                "medicine", "med", "drug", "heal",
                "\u043b\u0435\u043a\u0430\u0440\u0441\u0442\u0432",
                "\u836F", "\u5403\u836F", "\u7528\u836F", "\u6CBB\u7597");
            bool wantsGift = ContainsAny(normalizedText,
                "\u0434\u0430\u0439 \u043f\u043e\u0434\u0430\u0440\u043e\u043a",
                "gift", "present",
                "\u043f\u043e\u0434\u0430\u0440\u043e\u043a",
                "\u793C\u7269", "\u9001\u793C");
            bool wantsFood = ContainsAny(normalizedText,
                "\u043f\u043e\u043a\u043e\u0440\u043c\u0438",
                "\u0434\u0430\u0439 \u0435\u0434\u0443",
                "\u043f\u043e\u0435\u0441\u0442\u044c",
                "food", "meal", "snack",
                "\u0435\u0434\u0430",
                "\u043a\u0443\u0448\u0430\u0442\u044c",
                "\u043f\u043e\u0435\u0441\u0442\u044c",
                "\u5582", "\u5582\u5979", "\u5403\u7684", "\u98DF\u7269", "\u96F6\u98DF");

            if (wantsDrink)
                return allItems.Where(i => i.Type == Food.FoodType.Drink).ToList();

            if (wantsMedicine)
                return allItems.Where(i => i.Type == Food.FoodType.Drug || i.Type == Food.FoodType.Functional).ToList();

            if (wantsGift)
                return allItems.Where(i => i.Type == Food.FoodType.Gift).ToList();

            if (wantsFood)
            {
                return allItems.Where(i =>
                    i.Type == Food.FoodType.Food ||
                    i.Type == Food.FoodType.Meal ||
                    i.Type == Food.FoodType.Snack).ToList();
            }

            return allItems;
        }

        private void TakeItem(Food item)
        {
            _pendingItemSelection = null;
            string error = null;
            bool success = false;

            _plugin.MW.Dispatcher.Invoke(() =>
            {
                if (_plugin.MW.Set.EnableFunction)
                {
                    if (item.Price >= 10 && item.Price >= _plugin.MW.Core.Save.Money)
                    {
                        error = $"You don't have enough money for {item.TranslateName}.";
                        return;
                    }

                    if (_plugin.MW.HashCheck && item.IsOverLoad())
                    {
                        error = $"The game refused {item.TranslateName} because the item looks unsafe.";
                        return;
                    }

                    _plugin.MW.TakeItem(item);
                }

                _plugin.MW.DisplayFoodAnimation(item.GetGraph(), item.ImageSource);
                success = true;
            });

            if (success)
            {
                _plugin.LlmService?.SetRecentAction($"Used item: {item.TranslateName}.");
                SpeakLocalResponse($"Okay, taking {item.TranslateName}.");
            }
            else
            {
                _plugin.LlmService?.SetRecentAction(error ?? $"Failed to use item: {item.TranslateName}.");
                SpeakLocalResponse(error ?? $"I couldn't use {item.TranslateName}.");
            }
        }

        private static GraphHelper.Work FindWorkMatch(string text, IEnumerable<GraphHelper.Work> works)
        {
            string normalizedInput = NormalizeCommandText(text);
            var ranked = works
                .Select(w => new
                {
                    Work = w,
                    Score = GetWorkMatchScore(normalizedInput, w)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ToList();

            return ranked.FirstOrDefault()?.Work;
        }

        private static int GetWorkMatchScore(string normalizedInput, GraphHelper.Work work)
        {
            int score = 0;
            foreach (string candidate in GetWorkMatchCandidates(work))
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                string normalizedCandidate = NormalizeCommandText(candidate);
                if (normalizedInput == normalizedCandidate)
                    score = Math.Max(score, 100);
                else if (normalizedInput.Contains(normalizedCandidate, StringComparison.Ordinal))
                    score = Math.Max(score, 80);
                else if (normalizedCandidate.Contains(normalizedInput, StringComparison.Ordinal))
                    score = Math.Max(score, 60);
                else
                {
                    foreach (string token in normalizedCandidate.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (token.Length >= 3 && normalizedInput.Contains(token, StringComparison.Ordinal))
                            score = Math.Max(score, 40);
                    }
                }
            }

            return score;
        }

        private static IEnumerable<string> GetWorkMatchCandidates(GraphHelper.Work work)
        {
            yield return work.Name;
            yield return work.NameTrans;
            yield return work.Graph;
        }

        private static Food FindFoodMatch(string text, IEnumerable<Food> foods)
        {
            string normalizedInput = NormalizeCommandText(text);
            var ranked = foods
                .Select(item => new
                {
                    Item = item,
                    Score = GetFoodMatchScore(normalizedInput, item)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ToList();

            return ranked.FirstOrDefault()?.Item;
        }

        private static int GetFoodMatchScore(string normalizedInput, Food item)
        {
            int score = 0;
            foreach (string candidate in GetFoodMatchCandidates(item))
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                string normalizedCandidate = NormalizeCommandText(candidate);
                if (normalizedInput == normalizedCandidate)
                    score = Math.Max(score, 100);
                else if (normalizedInput.Contains(normalizedCandidate, StringComparison.Ordinal))
                    score = Math.Max(score, 80);
                else if (normalizedCandidate.Contains(normalizedInput, StringComparison.Ordinal))
                    score = Math.Max(score, 60);
                else
                {
                    foreach (string token in normalizedCandidate.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (token.Length >= 3 && normalizedInput.Contains(token, StringComparison.Ordinal))
                            score = Math.Max(score, 40);
                    }
                }
            }

            return score;
        }

        private static IEnumerable<string> GetFoodMatchCandidates(Food item)
        {
            yield return item.Name;
            yield return item.TranslateName;
            yield return item.Type.ToString();
        }

        private static bool ContainsActivityKeyword(string normalizedText, GraphHelper.Work.WorkType type)
        {
            return type switch
            {
                GraphHelper.Work.WorkType.Study => ContainsAny(normalizedText,
                    "\u0443\u0447\u0435\u0431",
                    "\u0443\u0447\u0438\u0441\u044c",
                    "\u0437\u0430\u0439\u043c\u0438\u0441\u044c \u0443\u0447\u0435\u0431",
                    "\u0437\u0430\u0439\u043c\u0438\u0441\u044c \u0443\u0447\u0451\u0431",
                    "learn", "study", "studying",
                    "\u5B66\u4E60", "\u53BB\u5B66\u4E60", "\u8BFB\u4E66", "\u4E0A\u8BFE"),
                GraphHelper.Work.WorkType.Work => ContainsAny(normalizedText,
                    "\u0440\u0430\u0431\u043e\u0442",
                    "\u0437\u0430\u0439\u043c\u0438\u0441\u044c \u0440\u0430\u0431\u043e\u0442",
                    "\u0438\u0434\u0438 \u0440\u0430\u0431\u043e\u0442",
                    "job", "work", "working",
                    "\u5DE5\u4F5C", "\u53BB\u5DE5\u4F5C", "\u4E0A\u73ED", "\u6253\u5DE5"),
                GraphHelper.Work.WorkType.Play => ContainsAny(normalizedText,
                    "\u0438\u0433\u0440",
                    "\u043f\u043e\u0438\u0433\u0440\u0430\u0439",
                    "\u043e\u0442\u0434\u043e\u0445\u043d\u0438",
                    "\u043f\u043e\u0433\u0443\u043b\u044f\u0439",
                    "play", "fun", "relax",
                    "\u73A9", "\u53BB\u73A9", "\u73A9\u800D", "\u4F11\u606F"),
                _ => false
            };
        }

        private static bool ContainsItemIntent(string normalizedText)
        {
            return ContainsAny(normalizedText,
                "\u043a\u0443\u043f\u0438",
                "\u0432\u0437\u044f\u0442\u044c",
                "\u0432\u043e\u0437\u044c\u043c\u0438",
                "\u0438\u0441\u043f\u043e\u043b\u044c\u0437",
                "\u0434\u0430\u0439",
                "\u043f\u043e\u0434\u0430\u0439",
                "\u043f\u043e\u043a\u043e\u0440\u043c\u0438",
                "\u043f\u043e\u0435\u0441\u0442\u044c",
                "\u043d\u0430\u043a\u043e\u0440\u043c\u0438",
                "\u043d\u0430\u043f\u043e\u0438",
                "\u043f\u0438\u0442\u044c",
                "\u0432\u044b\u043f\u0438\u0442\u044c",
                "\u0435\u0434\u0430", "\u0432\u043e\u0434\u0430",
                "food", "drink", "gift", "buy", "use", "feed",
                "\u4E70", "\u8D2D\u4E70", "\u4F7F\u7528", "\u7ED9\u5979", "\u5582", "\u5582\u5979", "\u559D", "\u559D\u6C34", "\u836F", "\u793C\u7269");
        }

        private static bool LooksLikeDirectCommand(string normalizedText)
        {
            if (string.IsNullOrWhiteSpace(normalizedText))
                return false;

            return ContainsAny(normalizedText,
                "\u0437\u0430\u0439\u043c\u0438\u0441\u044c",
                "\u0438\u0434\u0438",
                "\u043d\u0430\u0447\u043d\u0438",
                "\u0434\u0430\u0432\u0430\u0439",
                "\u043f\u043e\u0440\u0430\u0431\u043e\u0442\u0430\u0439",
                "\u043f\u043e\u0443\u0447\u0438\u0441\u044c",
                "\u0443\u0447\u0438\u0441\u044c",
                "\u0440\u0430\u0431\u043e\u0442\u0430\u0439",
                "\u043f\u043e\u0438\u0433\u0440\u0430\u0439",
                "\u0441\u0442\u043e\u043f",
                "\u043e\u0441\u0442\u0430\u043d\u043e\u0432",
                "\u043f\u0440\u0435\u043a\u0440\u0430\u0442\u0438",
                "\u043e\u0442\u043c\u0435\u043d\u0438",
                "\u043f\u043e\u043a\u043e\u0440\u043c\u0438",
                "\u043d\u0430\u043a\u043e\u0440\u043c\u0438",
                "\u043d\u0430\u043f\u043e\u0438",
                "\u0434\u0430\u0439 \u043b\u0435\u043a\u0430\u0440",
                "\u0434\u0430\u0439 \u043f\u043e\u0434\u0430\u0440\u043e\u043a",
                "\u043a\u0443\u043f\u0438",
                "\u043a\u0443\u0448\u0430\u0439",
                "\u043f\u043e\u0435\u0448\u044c",
                "start work", "start study", "start play", "buy", "feed", "drink", "use medicine", "stop",
                "\u53BB\u5B66\u4E60", "\u5B66\u4E60", "\u53BB\u5DE5\u4F5C", "\u5DE5\u4F5C", "\u53BB\u73A9", "\u73A9",
                "\u505C\u6B62", "\u53D6\u6D88", "\u4E70", "\u4E70\u6C34", "\u5582\u5979", "\u8BA9\u5979\u559D\u6C34", "\u7ED9\u5979\u5403\u836F", "\u4E70\u793C\u7269");
        }

        private static bool IsPendingSelectionCancelCommand(string normalizedText)
        {
            return ContainsAny(normalizedText,
                "\u0441\u0442\u043e\u043f",
                "\u043e\u0441\u0442\u0430\u043d\u043e\u0432\u0438\u0441\u044c",
                "\u043e\u0441\u0442\u0430\u043d\u043e\u0432\u0438",
                "\u043e\u0442\u043c\u0435\u043d\u0430",
                "\u043e\u0442\u043c\u0435\u043d\u0438",
                "\u043d\u0435 \u043d\u0430\u0434\u043e",
                "\u043d\u0435 \u043d\u0443\u0436\u043d\u043e",
                "\u0445\u0432\u0430\u0442\u0438\u0442",
                "cancel", "stop", "never mind", "\u505C\u6B62", "\u53D6\u6D88", "\u4E0D\u7528\u4E86", "\u7B97\u4E86");
        }

        private static string GetItemCategoryLabel(List<Food> items)
        {
            if (items.All(i => i.Type == Food.FoodType.Drink))
                return "drink";

            if (items.All(i => i.Type == Food.FoodType.Drug || i.Type == Food.FoodType.Functional))
                return "medicine";

            if (items.All(i => i.Type == Food.FoodType.Gift))
                return "gift";

            if (items.All(i => i.Type == Food.FoodType.Food || i.Type == Food.FoodType.Meal || i.Type == Food.FoodType.Snack))
                return "food";

            return "item";
        }

        private static string GetTypeLabel(GraphHelper.Work.WorkType type)
        {
            return type switch
            {
                GraphHelper.Work.WorkType.Study => "study",
                GraphHelper.Work.WorkType.Work => "work",
                GraphHelper.Work.WorkType.Play => "play",
                _ => "activity"
            };
        }

        private static bool ContainsAny(string text, params string[] needles)
        {
            return needles.Any(n => text.Contains(n, StringComparison.Ordinal));
        }

        private static bool IsLikelyQuestion(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string trimmed = text.Trim();
            if (trimmed.Contains('?'))
                return true;

            string normalized = NormalizeCommandText(trimmed);
            return normalized.StartsWith("\u0447\u0442\u043e ", StringComparison.Ordinal) ||
                   normalized.StartsWith("\u0447\u0435\u043c ", StringComparison.Ordinal) ||
                   normalized.StartsWith("\u043a\u0430\u043a\u043e\u0439 ", StringComparison.Ordinal) ||
                   normalized.StartsWith("\u043a\u0430\u043a\u0430\u044f ", StringComparison.Ordinal) ||
                   normalized.StartsWith("\u043a\u0430\u043a\u043e\u0435 ", StringComparison.Ordinal) ||
                   normalized.StartsWith("\u043a\u0430\u043a\u0438\u0435 ", StringComparison.Ordinal) ||
                   normalized.StartsWith("\u043f\u043e\u0447\u0435\u043c\u0443 ", StringComparison.Ordinal) ||
                   normalized.StartsWith("\u0437\u0430\u0447\u0435\u043c ", StringComparison.Ordinal) ||
                   normalized.StartsWith("how ", StringComparison.Ordinal) ||
                   normalized.StartsWith("what ", StringComparison.Ordinal) ||
                   normalized.StartsWith("why ", StringComparison.Ordinal);
        }

        private static string NormalizeCommandText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var chars = text
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) ? ch : ' ')
                .ToArray();
            return string.Join(" ", new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static bool TryExtractCommandText(string content, out string commandText)
        {
            commandText = string.Empty;
            if (string.IsNullOrWhiteSpace(content))
                return false;

            string trimmed = content.Trim();
            string[] prefixes = ["/", "!", "cmd ", "command ", "\u043a\u043e\u043c\u0430\u043d\u0434\u0430 ", "\u043f\u0440\u0438\u043a\u0430\u0437 "];
            foreach (string prefix in prefixes)
            {
                if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                commandText = trimmed[prefix.Length..].Trim();
                return !string.IsNullOrWhiteSpace(commandText);
            }

            return false;
        }

        private static object CreateMicIcon()
        {
            var accentBrush = new SolidColorBrush(Color.FromRgb(35, 96, 198));
            var outerArc = new Path
            {
                Stroke = accentBrush,
                StrokeThickness = 2.4,
                Data = Geometry.Parse("M 12.0 1.8 C 19.2 6.5 19.2 15.5 12.0 20.2"),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            var middleArc = new Path
            {
                Stroke = accentBrush,
                StrokeThickness = 2.4,
                Data = Geometry.Parse("M 7.8 4.5 C 13.4 8.0 13.4 14.0 7.8 17.5"),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            var innerArc = new Path
            {
                Stroke = accentBrush,
                StrokeThickness = 2.4,
                Data = Geometry.Parse("M 3.4 7.3 C 6.6 9.4 6.6 12.6 3.4 14.7"),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            var canvas = new Canvas
            {
                Width = 22,
                Height = 22
            };

            canvas.Children.Add(outerArc);
            canvas.Children.Add(middleArc);
            canvas.Children.Add(innerArc);
            return canvas;
        }

        private static object CreateBusyIcon()
        {
            return new TextBlock
            {
                Text = "...",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 111, 211)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static ControlTemplate CreateRoundButtonTemplate()
        {
            return (ControlTemplate)XamlReader.Parse(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'>" +
                "<Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='26' SnapsToDevicePixels='True'>" +
                "<ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>" +
                "</Border>" +
                "</ControlTemplate>");
        }

        private static void SendWinH()
        {
            INPUT[] inputs =
            {
                CreateKeyboardInput(VK_LWIN, false),
                CreateKeyboardInput((ushort)'H', false),
                CreateKeyboardInput((ushort)'H', true),
                CreateKeyboardInput(VK_LWIN, true),
            };

            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            if (sent != inputs.Length)
            {
                int error = Marshal.GetLastWin32Error();
                SendWinHWithKeybdEvent();
                if (error != 0)
                    SmartLolisLog.Info($"SendInput fallback used. Win32 error: {error}");
            }
        }

        private static void SendWinHWithKeybdEvent()
        {
            keybd_event(VK_LWIN, 0, 0, 0);
            keybd_event((byte)'H', 0, 0, 0);
            keybd_event((byte)'H', 0, KEYEVENTF_KEYUP_BYTE, 0);
            keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP_BYTE, 0);
        }

        private static INPUT CreateKeyboardInput(ushort key, bool keyUp)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = key,
                        wScan = 0,
                        dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte KEYEVENTF_KEYUP_BYTE = 0x02;
        private const byte VK_LWIN = 0x5B;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public INPUTUNION U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private sealed class PendingActivitySelection
        {
            public PendingActivitySelection(GraphHelper.Work.WorkType type, List<GraphHelper.Work> options)
            {
                Type = type;
                Options = options;
            }

            public GraphHelper.Work.WorkType Type { get; }
            public List<GraphHelper.Work> Options { get; }
        }

        private sealed class PendingItemSelection
        {
            public PendingItemSelection(string categoryLabel, List<Food> options)
            {
                CategoryLabel = categoryLabel;
                Options = options;
            }

            public string CategoryLabel { get; }
            public List<Food> Options { get; }
        }

    }
}
