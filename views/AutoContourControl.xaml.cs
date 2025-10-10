using esapi;
using itk.simple;
using Newtonsoft.Json;
using nnunet;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VMS.TPS.Common.Model.API;

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

using nnunet_client.models;

namespace nnunet_client.views
{
    public partial class AutoContourControl : UserControl
    {
        private Dictionary<string, SegmentationTemplate> _templates;

                       
        public Dictionary<string, string> NewContourIdDictionary;

        private viewmodels.AutoContourViewModel _GetViewModel()
        {
            return (viewmodels.AutoContourViewModel)this.DataContext;
        }

        // for backword compatibility
        public void SetImage(VMSImage image)
        {
            _GetViewModel().Image = image;
        }

        public AutoContourControl()
        {
            InitializeComponent();
            
            this.DataContext = new viewmodels.AutoContourViewModel();

            // load templates
            string dataDir = System.Configuration.ConfigurationManager.AppSettings["data_dir"];
            string templateDir = Path.Combine(dataDir, "seg", "templates");
            if (!Directory.Exists(templateDir))
            {
                MessageBox.Show($"Template folder not found:\n{templateDir}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _GetViewModel().LoadTemplates(templateDir);
        }

        private async void CheckStatusButton_Click(object sender, RoutedEventArgs e)
        {

            if (_GetViewModel().Image == null || _GetViewModel().SelectedTemplate == null)
            {
                MessageBox.Show("No image or template selected.");
                return;
            }

            await _GetViewModel().SegmentationTemplateEditorViewModel.UpdateStatus();
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_GetViewModel().Image == null || _GetViewModel().SelectedTemplate == null)
            {
                MessageBox.Show("No image or template selected.");
                return;
            }

            MessageBox.Show("Exporting an image can take a few minutes if not exported before...so be patient.");
            
            await _GetViewModel().SubmitPredictionRequests();
            
            helper.log("OK. Done submitting the image for AutoContour!");
        }


        private async void ImportContoursButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Imporing contours can take a few minutes...so be patient.");

            this.NewContourIdDictionary = await _GetViewModel().ImportPredictedContours();

            MessageBox.Show("Done imporing contours[N=].");

        }
    }
}
