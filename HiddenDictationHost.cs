using System.Drawing;
using WF = System.Windows.Forms;

namespace VPet.Plugin.SmartLolis
{
    public sealed class HiddenDictationHost : System.IDisposable
    {
        private readonly WF.Form _form;
        private readonly WF.TextBox _textBox;

        public HiddenDictationHost()
        {
            _textBox = new WF.TextBox
            {
                BorderStyle = WF.BorderStyle.None,
                Multiline = false,
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                Dock = WF.DockStyle.Fill
            };

            _form = new WF.Form
            {
                FormBorderStyle = WF.FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = WF.FormStartPosition.Manual,
                TopMost = true,
                Opacity = 0.01,
                BackColor = Color.White,
                AutoScaleMode = WF.AutoScaleMode.None
            };

            _form.Controls.Add(_textBox);
        }

        public string Text
        {
            get => _textBox.Text;
            set => _textBox.Text = value ?? string.Empty;
        }

        public bool Visible => _form.Visible;

        public void ShowAt(int x, int y, int width, int height, string initialText)
        {
            _form.Bounds = new Rectangle(x, y, width, height);
            Text = initialText;

            if (!_form.Visible)
                _form.Show();

            _form.Activate();
            _textBox.Focus();
            _textBox.SelectionStart = _textBox.TextLength;
            _textBox.SelectionLength = 0;
        }

        public void Hide()
        {
            if (_form.Visible)
                _form.Hide();
        }

        public void Dispose()
        {
            _form.Dispose();
        }
    }
}
