using System.Windows;
using System.Windows.Controls;

namespace nnunet_client.views
{
    /// <summary>
    /// Interaction logic for SubmitImageAndLabels.xaml
    /// </summary>
    public partial class SubmitImageAndLabels : UserControl
    {
        public SubmitImageAndLabels()
        {
            InitializeComponent();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Get the LabelStructureMapping from the DataContext (which is set in the DataTemplate)
                var mapping = button.DataContext as viewmodels.LabelStructureMapping;
                if (mapping != null)
                {
                    mapping.SelectedStructure = null;
                }
            }
        }
    }
}

