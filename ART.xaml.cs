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

namespace nnunet_client
{
    public partial class ART : Window
    {
        public ART(VMS.TPS.Common.Model.API.Application esapiApp)
        {
            InitializeComponent();

            // select a patient
            PatientSearchBox.SourceList = esapiApp.PatientSummaries
                .Select(p=> $"{p.LastName}, {p.FirstName}, {p.Id}")
                .ToList();

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
                    Patient pt = esapiApp.OpenPatientById(id);

                    // set to patient contorl
                    PatientControl.SetPatient(pt);

                    // set image list
                    var allImages3D = pt.Studies.SelectMany(study => study.Images3D).ToList();
                    ImageListControl.SetImages(allImages3D);
                }
            };


            // patient selected
            ImageListControl.SelectedItemChanged += (object sender, VMSImage selectedImage) =>
            {
                MessageBox.Show("You selected: " + selectedImage.Id);
                
            };
        }
    }
}
