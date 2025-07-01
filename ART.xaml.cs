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
using nnunet_client.UI;

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
                    global.vmsPatient = null;

                    Patient pt = esapiApp.OpenPatientById(id);
                    global.vmsPatient = pt;

                    // set to patient contorl
                    PatientControl.SetPatient(pt);

                    // set image list
                    var allImages3D = pt.Studies.SelectMany(study => study.Images3D).ToList();
                    ImageListControl.SetImages(allImages3D);


                    AutoPlanControl.SetPatient(pt);

                }
            };


            // patient selected
            ImageListControl.SelectedItemChanged += (object sender, VMSImage selectedImage) =>
            {
                MessageBox.Show("You selected: " + selectedImage.Id);

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
                }
            };
        }
    }
}
