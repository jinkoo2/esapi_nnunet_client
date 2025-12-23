using esapi.UI;
using esapi.ViewModel;
using nnunet_client.views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
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

namespace nnunet_client
{
    public partial class SubmitImageAndLabelsWindow : Window
    {
        // helper viewModel property
        private SubmitImageAndLabelsWindowViewModel _viewModel
        {
            get => (SubmitImageAndLabelsWindowViewModel)this.DataContext;
        }

        public SubmitImageAndLabelsWindow(VMS.TPS.Common.Model.API.Application esapiApp)
        {
            InitializeComponent();

            this.DataContext = new SubmitImageAndLabelsWindowViewModel();

            // log view controller
            //Logger.StartMonitoring(null);
            //Logger.StartLogPolling(null);
            //helper.Logger = Logger;

            // select a patient control
            PatientSearchBox.ItemsSource = new ObservableCollection<string>(global.vmsApplication.PatientSummaries
                .Select(p => $"{p.LastName}, {p.FirstName}, {p.Id}"));
            PatientSearchBox.SelectedItemChanged += PatientSearchBox_SelectedItemChanged;

            helper.log("Now...select a patient...");
        }

        private void PatientSearchBox_SelectedItemChanged(object s, string selectedString)
        {
            // Parse the selected string
            var parts = selectedString.Split(',');
            if (parts.Length == 3)
            {
                string lastName = parts[0].Trim();
                string firstName = parts[1].Trim();
                string id = parts[2].Trim();

                // Close previous patient
                global.vmsApplication.ClosePatient();
                global.vmsPatient = null;

                // Open new patient
                _viewModel.Patient = global.vmsApplication.OpenPatientById(id);
            }
            else
            {
                global.vmsApplication.ClosePatient();
                _viewModel.Patient = null;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (global.vmsPatient != null)
            {
                if(helper.show_yes_no_msg_box("Do you want to save any changes you made?"))
                {
                    helper.log("Saving...");
                    global.vmsApplication.SaveModifications();
                    helper.log("Saving done.");
                }
                global.vmsApplication.ClosePatient();
            }
        }
    }
}

