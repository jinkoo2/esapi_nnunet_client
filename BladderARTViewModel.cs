using nnunet_client.viewmodels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static esapi.esapi;
using VMSCourse = VMS.TPS.Common.Model.API.Course;
using VMSHospital = VMS.TPS.Common.Model.API.Hospital;
using VMSImage = VMS.TPS.Common.Model.API.Image;
using VMSPatient = VMS.TPS.Common.Model.API.Patient;
using VMSPlanSetup = VMS.TPS.Common.Model.API.PlanSetup;
using VMSReferencePoint = VMS.TPS.Common.Model.API.ReferencePoint;
using VMSRegistration = VMS.TPS.Common.Model.API.Registration;
using VMSSeries = VMS.TPS.Common.Model.API.Series;
using VMSStructure = VMS.TPS.Common.Model.API.Structure;
using VMSStructureSet = VMS.TPS.Common.Model.API.StructureSet;
using VMSStudy = VMS.TPS.Common.Model.API.Study;

using esapi.UI;
using esapi.ViewModel;
using nnunet_client.views;
using System.ComponentModel;
using System.Windows;
using VMS.TPS.Common.Model.API;


namespace nnunet_client
{
    public class BladderARTViewModel: nnunet_client.viewmodels.BaseViewModel
    {

        public BladderARTViewModel()
        {
            _imageListViewModel.PropertyChanged += ImageListViewModel_PropertyChanged;
            _autoContourViewModel.PropertyChanged += AutoContourViewModel_PropertyChanged;
            _bladderAutoPlanViewModel.PropertyChanged += BladderAutoPlanViewModel_PropertyChanged;
            _doseLimitListEditorViewModel.PropertyChanged += DoseLimitListEditorViewModel_PropertyChanged;

            // default dose limit template
            _doseLimitListEditorViewModel.TemplateFilePath = @"G:\data_secure\_dose_limits\templates\bladder_art.json";
        }

        private VMSPatient _patient;
        public VMSPatient Patient
        {
            get { return _patient; }
            set
            {
                if (_patient == value) return;

                SetProperty(ref _patient, value, nameof(Patient));

                global.vmsPatient = _patient;

                if (_patient != null)
                {
                    helper.log($"New patient selected (Id={_patient.Id}). Populating image list...");
                    
                    // set image list
                    var allImages3D = _patient.Studies.SelectMany(study => study.Images3D).ToList();
                    var sortedImageVMList = allImages3D
                        .Select(img => new esapi.ViewModel.ImageViewModel(img))
                        .Where(imgVM => imgVM.Series.Modality == "CT")
                        .OrderByDescending(imgVM => imgVM.CreationDateTime)
                        .ToList();

                    _imageListViewModel.ImageList = new ObservableCollection<esapi.ViewModel.ImageViewModel>(sortedImageVMList);
                }
                else
                {
                    helper.log($"Clearing ImageList...");
                    _imageListViewModel.ImageList = new ObservableCollection<esapi.ViewModel.ImageViewModel>();
                }

                helper.log($"Setting patient to AutoPlanViewModel...");
                _bladderAutoPlanViewModel.Patient = value;
            }
        }


        private esapi.ViewModel.ImageListViewModel _imageListViewModel = new esapi.ViewModel.ImageListViewModel();
        public esapi.ViewModel.ImageListViewModel ImageListViewModel
        {
            get { return _imageListViewModel; }
            set
            {
                if (_imageListViewModel == value) return;

                SetProperty(ref _imageListViewModel, value, nameof(esapi.ViewModel.ImageListViewModel));
            }
        }

        private void ImageListViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Check which specific property changed (e.g., if you have multiple)
            if (e.PropertyName == "SelectedImage")
            {
                // Logic to execute when the property changes

                esapi.ViewModel.ImageViewModel selectedImageVM = _imageListViewModel.SelectedImage;
                VMSImage selectedImage = (selectedImageVM != null) ? selectedImageVM.VMSObject : null;

                helper.log($"You selected: {selectedImage?.Id}");

                if (selectedImage != null)
                {
                    helper.log($"Checking image is tilted...");

                    // check image is tilted (acquired non-zero angle)
                    double direction00 = selectedImage.XDirection[0];
                    double direction11 = selectedImage.YDirection[1];
                    double direction22 = selectedImage.ZDirection[2];
                    double prod = direction00 * direction11 * direction22;
                    helper.log_for_debug($"prod[{prod}] = direction00[{direction00}] * direction11[{direction11}] * direction22[{direction22}]");
                    if (prod < 0.9999999)
                    {
                        helper.show_warning_msg_box("Image is tilted (acquired at non-zero couch angle(s)! A plan cannot be added on a tilted image!");
                    }
                }

                helper.log($"Setting selected image (Id={selectedImage?.Id}) to AutoContour and AutoPlan Controls...");
                _autoContourViewModel.Image = selectedImage;
                _bladderAutoPlanViewModel.Image = selectedImage;
            }
        }

        private viewmodels.AutoContourViewModel _autoContourViewModel = new AutoContourViewModel();
        public  viewmodels.AutoContourViewModel AutoContourViewModel
        {
            get { return _autoContourViewModel; }
            set {
                if (_autoContourViewModel == value) return;

                SetProperty(ref _autoContourViewModel, value, nameof(viewmodels.AutoContourViewModel));
            }
        }

        private void AutoContourViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ImportedContourIds")
            {
                Console.WriteLine("ImportedContourIds Changed");
                if (_autoContourViewModel.ImportedContourIds != null)
                {
                    foreach (string contourId in _autoContourViewModel.ImportedContourIds) { Console.WriteLine(contourId); }
                }

                // reset the image so that the controls bound to the structureest/contours to update
                Console.WriteLine("Resetting image to AuotPlanControlViewModel.");
                _bladderAutoPlanViewModel.Image = null;
                _bladderAutoPlanViewModel.Image = _autoContourViewModel.Image;
            }
        }


        private viewmodels.BladderAutoPlanViewModel _bladderAutoPlanViewModel = new BladderAutoPlanViewModel();
        public viewmodels.BladderAutoPlanViewModel BladderAutoPlanViewModel
        {
            get { return _bladderAutoPlanViewModel; }
            set
            {
                if (_bladderAutoPlanViewModel == value) return;

                SetProperty(ref _bladderAutoPlanViewModel, value, nameof(BladderAutoPlanViewModel));
            }
        }

        private void BladderAutoPlanViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Check which specific property changed (e.g., if you have multiple)
            if (e.PropertyName == "Plan")
            {
                // Logic to execute when the property changes
                Console.WriteLine($"-----Plan Changed to {_bladderAutoPlanViewModel.Plan?.Id}-------------");

                _doseLimitListEditorViewModel.Plan = _bladderAutoPlanViewModel.Plan;
            }
        }

        private viewmodels.DoseLimitListEditorViewModel _doseLimitListEditorViewModel = new  DoseLimitListEditorViewModel();
        public viewmodels.DoseLimitListEditorViewModel DoseLimitListEditorViewModel
        {
            get { return _doseLimitListEditorViewModel; }
            set
            {
                if (_doseLimitListEditorViewModel == value) return;

                SetProperty(ref _doseLimitListEditorViewModel, value, nameof(DoseLimitListEditorViewModel));
            }
        }

        private void DoseLimitListEditorViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }


    }
}
