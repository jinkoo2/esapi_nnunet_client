using System.Windows;

using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using VMSApplication = VMS.TPS.Common.Model.API.Application;
using VMSCourse = VMS.TPS.Common.Model.API.Course;
using VMSHospital = VMS.TPS.Common.Model.API.Hospital;
using VMSImage = VMS.TPS.Common.Model.API.Image;
using VMSPatient = VMS.TPS.Common.Model.API.Patient;
using VMSReferencePoint = VMS.TPS.Common.Model.API.ReferencePoint;
using VMSRegistration = VMS.TPS.Common.Model.API.Registration;
using VMSSeries = VMS.TPS.Common.Model.API.Series;
using VMSStructure = VMS.TPS.Common.Model.API.Structure;
using VMSStructureSet = VMS.TPS.Common.Model.API.StructureSet;
using VMSStudy = VMS.TPS.Common.Model.API.Study;
using System.Windows.Documents.DocumentStructures;




namespace nnunet_client
{
    public partial class MainWindow : Window
    {
        private VMSApplication _esapiApp;

        public MainWindow(VMSApplication esapiApp)
        {
            InitializeComponent();
            _esapiApp = esapiApp;
        }

        private void AutoContourButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new AutoContourWindow(_esapiApp);
            window.Show();
        }

        private void DoseLimitCheckerButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new DoseLimitChecker(_esapiApp);
            window.Show();
        }

        private void DoseLimitEditorButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new DoseLimitEditorWindow();
            window.Show();
        }

        private void SubmitImageAndLabelsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new SubmitImageAndLabelsWindow(_esapiApp);
            window.Show();
        }

        private void BladderARTButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new BladderART(_esapiApp);
            window.Show();
        }
    }
}


