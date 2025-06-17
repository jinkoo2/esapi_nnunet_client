using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace nnunet_client.UI
{
    public partial class AutoCompleteTextBox : UserControl
    {
        public event EventHandler<string> SelectedItemChanged;

        public List<string> SourceList { get; set; } = new List<string>();
        public string SelectedItem { get; private set; }

        public AutoCompleteTextBox()
        {
            InitializeComponent();
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string input = InputTextBox.Text.ToLower();
            var matches = SourceList
                .Where(s => s.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(10)
                .ToList();

            SuggestionsListBox.ItemsSource = matches;
            SuggestionsListBox.Visibility = matches.Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SuggestionsListBox_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SuggestionsListBox.SelectedItem is string selected)
            {
                InputTextBox.Text = selected;
                SelectedItem = selected;
                SuggestionsListBox.Visibility = Visibility.Collapsed;

                // Raise the event
                SelectedItemChanged?.Invoke(this, selected);
            }
        }
    }
}
