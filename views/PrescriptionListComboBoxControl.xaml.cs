using nnunet_client.viewmodels;
using System.Windows.Controls;

namespace nnunet_client.views
{
    
    /// <summary>
    /// Interaction logic for PrescriptionListUserControl.xaml
    /// </summary>
    public partial class PrescriptionListComboBoxControl : UserControl
    {
        public PrescriptionListComboBoxControl()
        {
            InitializeComponent();

            // Create an instance of the view model and set it as the DataContext.
            // This is the key to binding the UI to your data.
            //this.DataContext = new PrescriptionListViewModel();
        }
    }
}
