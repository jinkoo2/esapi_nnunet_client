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
using MahApps.Metro.Controls;

namespace nnunet_client
{


    /// <summary>
    /// Interaction logic for DoseLimitCheckerWindow.xaml
    /// </summary>
    public partial class DoseLimitChecker : Window
    {
        public DoseLimitChecker(VMS.TPS.Common.Model.API.Application esapiApp)
        {

            InitializeComponent();

            this.DataContext = new nnunet_client.viewmodels.DoseLimitCheckerViewModel(esapiApp);
        }


    }
}
