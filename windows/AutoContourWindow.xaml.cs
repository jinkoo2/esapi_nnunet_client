using nnunet_client.models;
using nnunet_client.viewmodels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace nnunet_client
{
    /// <summary>
    /// Interaction logic for AutoContour.xaml
    /// </summary>
    public partial class AutoContourWindow : Window
    {


        public AutoContourWindow(VMS.TPS.Common.Model.API.Application esapiApp)
        {
            InitializeComponent();

            this.nav.DataContext = new viewmodels.StepNavigatorViewModel()
            {
                StepPages = new System.Collections.ObjectModel.ObservableCollection<StepPage>()
                {
                    new StepPage("Page1", new TextBlock(){Text="Welcome!"}),
                    new StepPage("Page2", new TextBlock(){Text="Welcome2"}),
                    new StepPage("Page3", new TextBlock(){Text="Welcome3"})
                },
                CurrentIndex = 0
            };
            
        }
    }
}

