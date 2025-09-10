using System.Windows.Controls;

namespace nnunet_client.views
{
    public partial class LogViewControl : UserControl
    {
        public LogViewControl()
        {
            InitializeComponent();
        }

        public void AppendLine(string line)
        {
            LogTextBox.AppendText(line + "\n");
            LogTextBox.ScrollToEnd();
        }

        public void Clear()
        {
            LogTextBox.Clear();
        }
    }
}
