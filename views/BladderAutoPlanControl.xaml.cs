using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.HtmlControls;
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
    /// Interaction logic for AutoPlanControl.xaml
    /// </summary>
    public partial class BladderAutoPlanControl : UserControl
    {
        private viewmodels.BladderAutoPlanViewModel _viewmodel;

        public BladderAutoPlanControl()
        {
            InitializeComponent();

            _viewmodel = new viewmodels.BladderAutoPlanViewModel();

            _viewmodel.DoseLimitListEditorViewModel = new viewmodels.DoseLimitListEditorViewModel() { Title = "Dose Constraints" };
           
            this.DataContext = _viewmodel;
        }

        public void SetPatient(VMS.TPS.Common.Model.API.Patient patient)
        {
            _viewmodel.Patient = patient;
        }


        public void SetImage(VMS.TPS.Common.Model.API.Image image)
        {
            _viewmodel.Image = image;
        }

    }
}
