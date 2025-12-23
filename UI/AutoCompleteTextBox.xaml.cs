using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace nnunet_client.UI
{
    public partial class AutoCompleteTextBox : UserControl
    {
        public event EventHandler<string> SelectedItemChanged;

        // ItemsSource (list of options)
        public ObservableCollection<string> ItemsSource
        {
            get => (ObservableCollection<string>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<string>), typeof(AutoCompleteTextBox),
                new PropertyMetadata(new ObservableCollection<string>()));

        // SelectedItem (two-way bindable)
        public string SelectedItem
        {
            get => (string)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(string), typeof(AutoCompleteTextBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        public AutoCompleteTextBox()
        {
            InitializeComponent();
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string input = InputTextBox.Text.ToLower();
            var matches = ItemsSource
                .Where(s => s.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(10)
                .ToList();

            SuggestionsListBox.ItemsSource = matches;


            // 2. Control the Popup's state
            if (SuggestionsPopup != null)
            {
                // Logic to determine if the popup should be open:
                // Check if the filtered list has items AND the text box is focused
                bool hasResults = SuggestionsListBox.Items.Count > 0;

                // Set IsOpen property
                SuggestionsPopup.IsOpen = hasResults;
            }
        }

        private void SuggestionsListBox_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SuggestionsListBox.SelectedItem is string selected)
            {
                InputTextBox.Text = selected;
                SelectedItem = selected;

                if (SuggestionsPopup != null && SuggestionsListBox.SelectedItem != null)
                {
                    // 1. Transfer the selected item text to the TextBox
                    // InputTextBox.Text = SuggestionsListBox.SelectedItem.ToString();

                    // 2. Close the popup
                    SuggestionsPopup.IsOpen = false;
                }

                // Raise the event
                SelectedItemChanged?.Invoke(this, selected);
            }
        }

        private void SuggestionsListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Check if the new focus target is inside the popup itself.
            // If not, close the popup.
            if (SuggestionsPopup != null && !SuggestionsPopup.IsKeyboardFocusWithin)
            {
                SuggestionsPopup.IsOpen = false;
            }
        }
    }
}

