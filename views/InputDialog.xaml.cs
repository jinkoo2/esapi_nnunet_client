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
    public partial class InputDialog : Window
    {
        // Public property to retrieve the entered text after the dialog closes
        public string InputText { get; private set; }

        // Constructor to set the prompt message
        public InputDialog(string prompt)
        {
            InitializeComponent();
            PromptLabel.Text = prompt;
            InputTextBox.Focus();
        }

        // Handles the OK button click
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // Store the input text and set the DialogResult to true
            InputText = InputTextBox.Text;
            DialogResult = true;
        }

        // Handles the Cancel button click
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Set the DialogResult to false
            DialogResult = false;
        }

        // Allows pressing Enter key to act as OK
        private void InputTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Ok_Click(sender, e);
            }
        }
    }
}
