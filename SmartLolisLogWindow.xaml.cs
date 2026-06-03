using System;
using System.Windows;

namespace VPet.Plugin.SmartLolis
{
    public partial class SmartLolisLogWindow : Window
    {
        public SmartLolisLogWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SmartLolisLog.LogUpdated += OnLogUpdated;
            txtLog.Text = SmartLolisLog.GetText();
            txtLog.ScrollToEnd();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            SmartLolisLog.LogUpdated -= OnLogUpdated;
        }

        private void OnLogUpdated(string text)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.Text = text;
                txtLog.ScrollToEnd();
            });
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Text = SmartLolisLog.GetText();
            txtLog.ScrollToEnd();
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(SmartLolisLog.GetText());
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            SmartLolisLog.Clear();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
