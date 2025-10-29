using System.Windows.Controls;
using nnunet_client.viewmodels;
using nnunet_client.models;
using System;

namespace nnunet_client.views
{
    public partial class StepNavigator : UserControl
    {
        private StepNavigatorViewModel ViewModel => DataContext as StepNavigatorViewModel;

        public StepNavigator()
        {
            InitializeComponent();
        }
    }
}
