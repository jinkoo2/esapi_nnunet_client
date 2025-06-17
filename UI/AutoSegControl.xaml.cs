using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using esapi;

using VMSPatient = VMS.TPS.Common.Model.API.Patient;
using VMSStructureSet = VMS.TPS.Common.Model.API.StructureSet;
using VMSStructure = VMS.TPS.Common.Model.API.Structure;
using VMSImage = VMS.TPS.Common.Model.API.Image;
using VMSCourse = VMS.TPS.Common.Model.API.Course;
using VMSStudy = VMS.TPS.Common.Model.API.Study;
using VMSSeries = VMS.TPS.Common.Model.API.Series;
using VMSRegistration = VMS.TPS.Common.Model.API.Registration;
using VMSReferencePoint = VMS.TPS.Common.Model.API.ReferencePoint;
using VMSHospital = VMS.TPS.Common.Model.API.Hospital;

namespace nnunet_client.UI
{
    public partial class AutoSegControl : UserControl
    {
        private Dictionary<string, esapi.SegmentationTemplate> _templates;

        private VMSImage _image=null;

        public AutoSegControl()
        {
            InitializeComponent();
            LoadTemplates();
        }

        public void SetImage(VMSImage image)
        {
            _image = image;
            ImageId.Content = "Image: " + _image.Id;
        }

        private void LoadTemplates()
        {
            string dataDir = System.Configuration.ConfigurationManager.AppSettings["data_dir"];
            string templateDir = Path.Combine(dataDir, "seg", "templates");

            if (!Directory.Exists(templateDir))
            {
                MessageBox.Show($"Template folder not found:\n{templateDir}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TemplateSelector.ItemsSource = null;
                _templates = new Dictionary<string, SegmentationTemplate>();
                return;
            }

            try
            {
                TemplateManager templateManager = new TemplateManager();
                templateManager.LoadTemplates(templateDir);
                _templates = templateManager.Templates;
                TemplateSelector.ItemsSource = _templates.Keys;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load templates:\n{ex.Message}",
                                "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _templates = new Dictionary<string, SegmentationTemplate>();
                TemplateSelector.ItemsSource = null;
            }

        }


        private void TemplateSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TemplateSelector.SelectedItem is string selectedName &&
                _templates.TryGetValue(selectedName, out var template))
            {
                SegTemplateEditor.SetTemplate(template);
            }
        }
    }
}
