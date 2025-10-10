using esapi.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using VMS.TPS.Common.Model.API;

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
using nnunet_client.views;
using System.Collections.ObjectModel;

using System.ComponentModel;

namespace nnunet_client
{
    public partial class BladderART : Window
    {
        public BladderART(VMS.TPS.Common.Model.API.Application esapiApp)
        {
            InitializeComponent();

            // select a patient
            PatientSearchBox.ItemsSource = new ObservableCollection<string>(esapiApp.PatientSummaries
                .Select(p => $"{p.LastName}, {p.FirstName}, {p.Id}"));


            // patient selected
            PatientSearchBox.SelectedItemChanged += (s, selectedString) =>
            {
                //MessageBox.Show("You selected: " + selectedString);

                // Or split the string
                var parts = selectedString.Split(',');
                if (parts.Length == 3)
                {
                    string lastName = parts[0].Trim();
                    string firstName = parts[1].Trim();
                    string id = parts[2].Trim();

                    // Do something with lastName, firstName, and id
                    esapiApp.ClosePatient();
                    global.vmsPatient = null;

                    Patient pt = esapiApp.OpenPatientById(id);
                    helper.log($"Opened patient: {pt.LastName},{pt.FirstName},{pt.Id} ");
                    global.vmsPatient = pt;

                    // set to patient contorl
                    PatientControl.SetPatient(pt);

                    // set image list
                    var allImages3D = pt.Studies.SelectMany(study => study.Images3D).ToList();
                    ImageListControl.SetImages(allImages3D);

                    AutoPlanControl.SetPatient(pt);

                    
                    

                    helper.log($"Now... select an image...");
                }
            };

            // patient selected
            ImageListControl.SelectedItemChanged += (object sender, VMSImage selectedImage) =>
            {
                helper.log($"You selected: {selectedImage.Id}");

                AutoSegControl.SetImage(selectedImage);
                AutoPlanControl.SetImage(selectedImage);

                // check image is tilted (acquired non-zero angle)
                double direction00 = selectedImage.XDirection[0];
                double direction11 = selectedImage.YDirection[1];
                double direction22 = selectedImage.ZDirection[2];
                double prod = direction00 * direction11 * direction22;
                if (prod < 0.9999)
                {
                    MessageBox.Show("Image is tilted (acquired at non-zero couch angle(s)! A plan cannot be added on a tilted image!");
                    return;
                }
            };

            //
            DoseLimitEditorControl.DataContext = new viewmodels.DoseLimitListEditorViewModel();
            _GetDoseLimitEditorViewModel().TemplateFilePath = @"G:\data_secure\_dose_limits\templates\bladder_art.json";


            _GetAutoPlanlViewModel().PropertyChanged += HandleAutoPlanPropertyChanged;

            // log view controller
            //Logger.StartMonitoring(null);
            Logger.StartLogPolling(null);
            helper.Logger = Logger;

            helper.log("Now...select a patient...");
        }

        private void HandleAutoPlanPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Check which specific property changed (e.g., if you have multiple)
            if (e.PropertyName == "Plan")
            {
                // Logic to execute when the property changes
                Console.WriteLine($"-----Plan Changed to {_GetAutoPlanlViewModel().Plan?.Id}-------------");

                _GetDoseLimitEditorViewModel().Plan = _GetAutoPlanlViewModel().Plan;
            }
        }

        private void BladderART_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private viewmodels.BladderAutoPlanViewModel _GetAutoPlanlViewModel()
        {
            return (viewmodels.BladderAutoPlanViewModel)AutoPlanControl.DataContext;
        }

        private viewmodels.DoseLimitListEditorViewModel _GetDoseLimitEditorViewModel()
        {
            return (viewmodels.DoseLimitListEditorViewModel) DoseLimitEditorControl.DataContext;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (global.vmsPatient != null)
            {
                global.vmsApplication.ClosePatient();
            }

            Logger.StopLogPolling();
        }
    }
}
