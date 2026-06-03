using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using VPet_Simulator.Windows.Interface;

namespace VPet.Plugin.SmartLolis
{
    internal sealed class SmartLolisVoiceOverlayManager
    {
        private static readonly TimeSpan VoiceAutoSendDelay = TimeSpan.FromMilliseconds(1400);
        private static readonly TimeSpan VoiceAutoSendTimeout = TimeSpan.FromSeconds(20);

        private readonly SmartLolisPlugin _plugin;
        private readonly DispatcherTimer _syncTimer;

        private TalkBox _hostTalkBox;
        private Button _btnVoice;
        private HiddenDictationHost _dictationHost;
        private bool _isStartingVoiceInput;
        private bool _voiceSessionActive;
        private bool _voiceMonitorRunning;
        private string _voiceSessionInitialText = string.Empty;
        private string _lastObservedVoiceText = string.Empty;
        private DateTime _lastVoiceTextChangeUtc = DateTime.MinValue;
        private DateTime _voiceSessionStartedUtc = DateTime.MinValue;

        public SmartLolisVoiceOverlayManager(SmartLolisPlugin plugin)
        {
            _plugin = plugin;
            _dictationHost = new HiddenDictationHost();
            _syncTimer = new DispatcherTimer(DispatcherPriority.Background, _plugin.MW.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(450)
            };
            _syncTimer.Tick += (_, _) => SyncCurrentTalkBox();
        }

        public void Start()
        {
            _syncTimer.Start();
            SyncCurrentTalkBox();
        }

        public void RefreshUiFromSettings()
        {
            _plugin.MW.Dispatcher.Invoke(() =>
            {
                ApplyButtonVisibility();
                if ((_plugin.PluginSettings?.EnableVoiceInputButton ?? true) == false && _voiceSessionActive)
                    StopVoiceInputSession(true);
            });
        }

        internal void CleanupNow()
        {
            _plugin.MW.Dispatcher.Invoke(() =>
            {
                if (_hostTalkBox != null && !ReferenceEquals(_hostTalkBox, _plugin.TalkBoxInstance))
                    Detach();
                CleanupStrayOverlays();
            });
        }

        private void SyncCurrentTalkBox()
        {
            var currentTalkBox = _plugin.MW.TalkBoxCurr?.This as TalkBox;
            if (ReferenceEquals(currentTalkBox, _plugin.TalkBoxInstance))
            {
                Detach();
                CleanupStrayOverlays();
                return;
            }

            if (currentTalkBox == null)
            {
                Detach();
                CleanupStrayOverlays();
                return;
            }

            if (ReferenceEquals(currentTalkBox, _hostTalkBox))
            {
                ApplyButtonVisibility();
                return;
            }

            Detach();
            Attach(currentTalkBox);
        }

        private void Attach(TalkBox hostTalkBox)
        {
            if (hostTalkBox?.Content is not Border inputBorder)
                return;

            _hostTalkBox = hostTalkBox;
            _btnVoice = CreateVoiceButton();

            var wrapper = new Grid
            {
                VerticalAlignment = VerticalAlignment.Top,
                Margin = inputBorder.Margin,
                Tag = "SmartLolisVoiceOverlay"
            };
            wrapper.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            wrapper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            wrapper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            inputBorder.Margin = new Thickness(0);
            _btnVoice.BorderBrush = inputBorder.BorderBrush;
            _btnVoice.BorderThickness = inputBorder.BorderThickness;

            void SyncVoiceButtonSize()
            {
                if (_btnVoice == null || !ReferenceEquals(_hostTalkBox, hostTalkBox))
                    return;

                double targetSize = inputBorder.ActualHeight > 1 ? inputBorder.ActualHeight : 38;
                _btnVoice.Width = targetSize;
                _btnVoice.Height = targetSize;
            }

            SyncVoiceButtonSize();
            inputBorder.SizeChanged += InputBorder_SizeChanged;

            Grid.SetColumn(_btnVoice, 0);
            Grid.SetColumn(inputBorder, 2);

            hostTalkBox.Content = null;
            wrapper.Children.Add(_btnVoice);
            wrapper.Children.Add(inputBorder);
            hostTalkBox.Content = wrapper;

            ApplyButtonVisibility();
            return;

            void InputBorder_SizeChanged(object sender, SizeChangedEventArgs e)
            {
                SyncVoiceButtonSize();
            }
        }

        private void Detach()
        {
            if (_voiceSessionActive)
                StopVoiceInputSession(true);

            RestoreTalkBoxContent(_hostTalkBox);

            _btnVoice = null;
            _hostTalkBox = null;
        }

        private void CleanupStrayOverlays()
        {
            if (_plugin?.MW?.Main?.ToolBar?.MainGrid == null)
                return;

            foreach (UIElement child in _plugin.MW.Main.ToolBar.MainGrid.Children)
            {
                if (child is TalkBox talkBox)
                    RestoreTalkBoxContent(talkBox);
            }
        }

        private static void RestoreTalkBoxContent(TalkBox talkBox)
        {
            if (talkBox?.Content is not Grid wrapper || !Equals(wrapper.Tag, "SmartLolisVoiceOverlay"))
                return;

            Border inputBorder = null;
            foreach (UIElement child in wrapper.Children)
            {
                if (child is Border border)
                {
                    inputBorder = border;
                    break;
                }
            }

            if (inputBorder == null)
                return;

            wrapper.Children.Remove(inputBorder);
            wrapper.Children.Clear();
            inputBorder.Margin = wrapper.Margin;
            talkBox.Content = null;
            talkBox.Content = inputBorder;
        }

        private Button CreateVoiceButton()
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

            return btnVoice;
        }

        private void ApplyButtonVisibility()
        {
            if (_btnVoice == null)
                return;

            bool enabled = _plugin.PluginSettings?.EnableVoiceInputButton ?? true;
            _btnVoice.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            _btnVoice.IsEnabled = enabled;
            _btnVoice.IsHitTestVisible = enabled;
            UpdateVoiceButtonState();
        }

        private async void StartVoiceInput()
        {
            if (_hostTalkBox == null || _btnVoice == null || _isStartingVoiceInput)
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
                SmartLolisLog.Info("Windows voice input started from SmartLolis overlay.");
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Failed to start Windows voice input from overlay.", ex);
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
            CloseDictationHost();
            UpdateVoiceButtonState();

            if (!closeWindowsDictation)
                return;

            try
            {
                SendWinH();
                SmartLolisLog.Info("Windows voice input stopped from SmartLolis overlay.");
            }
            catch (Exception ex)
            {
                SmartLolisLog.Error("Failed to stop Windows voice input from overlay.", ex);
            }
        }

        private void BeginVoiceSessionMonitor()
        {
            _voiceSessionActive = true;
            _voiceSessionStartedUtc = DateTime.UtcNow;
            _voiceSessionInitialText = _hostTalkBox?.tbTalk?.Text ?? string.Empty;
            _lastObservedVoiceText = _voiceSessionInitialText;
            _lastVoiceTextChangeUtc = DateTime.UtcNow;
            UpdateVoiceButtonState();

            if (_voiceMonitorRunning)
                return;

            _voiceMonitorRunning = true;
            _ = MonitorVoiceInputAsync();
        }

        private async Task MonitorVoiceInputAsync()
        {
            try
            {
                while (_voiceSessionActive && _hostTalkBox != null)
                {
                    await Task.Delay(250);

                    string currentText = GetCurrentVoiceText();
                    if (!string.Equals(currentText, _lastObservedVoiceText, StringComparison.Ordinal))
                    {
                        _lastObservedVoiceText = currentText;
                        _lastVoiceTextChangeUtc = DateTime.UtcNow;
                        if (_hostTalkBox?.tbTalk != null)
                            _hostTalkBox.tbTalk.Text = currentText;
                    }

                    if (DateTime.UtcNow - _voiceSessionStartedUtc > VoiceAutoSendTimeout)
                    {
                        SmartLolisLog.Info("Voice input overlay auto-send monitor timed out.");
                        _voiceSessionActive = false;
                        CloseDictationHost();
                        UpdateVoiceButtonState();
                        break;
                    }

                    if (currentText.Length <= _voiceSessionInitialText.Length)
                        continue;

                    if (DateTime.UtcNow - _lastVoiceTextChangeUtc < VoiceAutoSendDelay)
                        continue;

                    _voiceSessionActive = false;
                    if (_hostTalkBox?.tbTalk != null)
                        _hostTalkBox.tbTalk.Text = currentText;
                    CloseDictationHost();
                    UpdateVoiceButtonState();
                    AutoSendVoiceText();
                    break;
                }
            }
            finally
            {
                _voiceMonitorRunning = false;
                UpdateVoiceButtonState();
            }
        }

        private void AutoSendVoiceText()
        {
            if (_hostTalkBox?.tbTalk == null || _hostTalkBox?.btnSend == null)
                return;

            string currentText = _hostTalkBox.tbTalk.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(currentText) || currentText.Length <= _voiceSessionInitialText.Length)
                return;

            SmartLolisLog.Info("Auto-sending voice-typed message from SmartLolis overlay.");
            _hostTalkBox.btnSend.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        private void PrepareHiddenTextFocus()
        {
            ShowDictationHost();
        }

        private void ShowDictationHost()
        {
            if (_hostTalkBox?.tbTalk == null)
                return;

            Point screenPoint = _hostTalkBox.tbTalk.PointToScreen(new Point(24, _hostTalkBox.tbTalk.ActualHeight / 2));
            double width = _hostTalkBox.tbTalk.ActualWidth <= 1 ? 220 : _hostTalkBox.tbTalk.ActualWidth;
            double height = _hostTalkBox.tbTalk.ActualHeight <= 1 ? 34 : _hostTalkBox.tbTalk.ActualHeight;
            string initialText = _hostTalkBox.tbTalk.Text ?? string.Empty;

            _dictationHost ??= new HiddenDictationHost();
            _dictationHost.ShowAt((int)screenPoint.X, (int)(screenPoint.Y - height / 2), (int)width, (int)height, initialText);
        }

        private string GetCurrentVoiceText()
        {
            if (_dictationHost != null && _dictationHost.Visible)
                return _dictationHost.Text ?? string.Empty;

            return _hostTalkBox?.tbTalk?.Text ?? string.Empty;
        }

        private void CloseDictationHost()
        {
            _dictationHost?.Hide();
        }

        private void UpdateVoiceButtonState()
        {
            if (_btnVoice == null)
                return;

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
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = key,
                        dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                    }
                }
            };
        }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_KEYUP_BYTE = 0x0002;
        private const byte VK_LWIN = 0x5B;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
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
            public nint dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
    }
}
