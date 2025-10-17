using esapi.UI;
using esapi.ViewModel;
using nnunet_client.views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

                    // set image list
                    var allImages3D = pt.Studies.SelectMany(study => study.Images3D).ToList();
                    var sortedImageVMList = allImages3D
                        .Select(img => new esapi.ViewModel.ImageViewModel(img))
                        .Where(imgVM=> imgVM.Series.Modality == "CT")
                        .OrderByDescending(imgVM => imgVM.CreationDateTime)
                        .ToList();
                    
                    _GetImageListContourViewModel().ImageList = new ObservableCollection<esapi.ViewModel.ImageViewModel>(sortedImageVMList);

                    _GetAutoPlanlViewModel().Patient  = pt;
                    helper.log($"Now... select an image...");
                }
            };

            //
            DoseLimitEditorControl.DataContext = new viewmodels.DoseLimitListEditorViewModel();
            _GetDoseLimitEditorViewModel().TemplateFilePath = @"G:\data_secure\_dose_limits\templates\bladder_art.json";
            
            _GetAutoPlanlViewModel().PropertyChanged += HandleAutoPlanPropertyChanged;
            _GetAutoContourViewModel().PropertyChanged += HandleAutoContourPropertyChanged;
            _GetImageListContourViewModel().PropertyChanged += HandleImageListPropertyChanged;

            // log view controller
            //Logger.StartMonitoring(null);
            Logger.StartLogPolling(null);
            helper.Logger = Logger;

            helper.log("Now...select a patient...");
        }


        private void HandleImageListPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Check which specific property changed (e.g., if you have multiple)
            if (e.PropertyName == "SelectedImage")
            {
                // Logic to execute when the property changes

                esapi.ViewModel.ImageViewModel selectedImageVM = _GetImageListContourViewModel().SelectedImage;
                VMSImage selectedImage = (selectedImageVM != null)? selectedImageVM.VMSObject : null;

                helper.log($"You selected: {selectedImage?.Id}");

                _GetAutoContourViewModel().Image = selectedImage;
                _GetAutoPlanlViewModel().Image = selectedImage;

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
            }
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

        private void HandleAutoContourPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Check which specific property changed (e.g., if you have multiple)
            if (e.PropertyName == "Image")
            {
                // Logic to execute when the property changes
                Console.WriteLine($"-----Image Changed to {_GetAutoContourViewModel().Image?.Id}-------------");

                _GetAutoPlanlViewModel().Image = _GetAutoContourViewModel().Image;  
            }
            else if(e.PropertyName == "ImportedContourIds")
            {
                Console.WriteLine("ImportedContourIds Changed");
                if(_GetAutoContourViewModel().ImportedContourIds != null)
                {
                    foreach(string contourId in  _GetAutoContourViewModel().ImportedContourIds) { Console.WriteLine(contourId); }
                }

                // reset the image so that the controls bound to the structureest/contours to update
                Console.WriteLine("Resetting image to AuotPlanControlViewModel.");
                _GetAutoPlanlViewModel().Image = null;
                _GetAutoPlanlViewModel().Image = _GetAutoContourViewModel().Image;
            }

        }

        private esapi.ViewModel.ImageListViewModel _GetImageListContourViewModel()
        {
            return (esapi.ViewModel.ImageListViewModel) ImageListControl.DataContext;
        }


        private void BladderART_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private viewmodels.AutoContourViewModel _GetAutoContourViewModel()
        {
            return (viewmodels.AutoContourViewModel)AutoContourControl.DataContext;
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
