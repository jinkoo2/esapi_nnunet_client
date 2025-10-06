using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace nnunet_client.views
{
    /// <summary>
    /// Interaction logic for DoseLimitListDataGridControl.xaml
    /// </summary>
    public partial class DoseLimitListDataGridControl : UserControl
    {
        public DoseLimitListDataGridControl()
        {
            InitializeComponent();
        }

        private void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            // The sender is the TextBox instance that just loaded inside the DataGridCell
            if (sender is TextBox textBox)
            {
                // 1. Give the TextBox focus (often needed to ensure keyboard input is ready)
                textBox.Focus();

                // 2. Select all existing text
                textBox.SelectAll();
            }

        }
    }
}
