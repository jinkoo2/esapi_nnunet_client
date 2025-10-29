using Microsoft.Win32;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using VMS.TPS.Common.Model.Types;
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

namespace nnunet_client.viewmodels
{
    public class BladderAutoPlanViewModel : BaseViewModel
    {
        public ICommand MakePTVCommand { get; }
        public ICommand CreatePlanAndOptimizeCommand { get; }

        public ICommand AddNewPrimaryReferencePointCommand { get; }

        public ICommand AddNewCourseCommand { get; }


        // construction
        public BladderAutoPlanViewModel()
        {
            MakePTVCommand = new RelayCommand(CreatePTV);
            CreatePlanAndOptimizeCommand = new RelayCommand(CreatePlanAndIOptimize);
            AddNewPrimaryReferencePointCommand = new RelayCommand(AddNewPrimaryReferencePoint);
            AddNewCourseCommand = new RelayCommand(AddNewCourse);

            ExternalBeamMachineParametersList = new ObservableCollection<ExternalBeamMachineParameters>()
            {
                new ExternalBeamMachineParameters("Edge", "6X", 600, "STATIC", ""),
                new ExternalBeamMachineParameters("TrueBeam", "6X", 600, "STATIC", ""),
                new ExternalBeamMachineParameters("SIL 21IX", "6X", 600, "STATIC", "")
            };

            SelectedExternalBeamMachineParameters = ExternalBeamMachineParametersList[0];
        }


        private ObservableCollection<ExternalBeamMachineParameters> _externalBeamMachineParametersList;
        public ObservableCollection<ExternalBeamMachineParameters> ExternalBeamMachineParametersList
        {
            get => _externalBeamMachineParametersList;
            set => SetProperty(ref _externalBeamMachineParametersList, value, nameof(ExternalBeamMachineParametersList));
        }

        private ExternalBeamMachineParameters _selectedExternalBeamMachineParameters;
        public ExternalBeamMachineParameters SelectedExternalBeamMachineParameters
        {
            get => _selectedExternalBeamMachineParameters;
            set => SetProperty(ref _selectedExternalBeamMachineParameters, value, nameof(SelectedExternalBeamMachineParameters));
        }

        private void AddNewPrimaryReferencePoint()
        {
            if(_patient == null)
            {
                helper.show_error_msg_box("Please select a patient first");
                return;
            }

            helper.log("Adding a new primary reference point");

            // 1. Create the dialog, passing the required prompt text
            var inputDialog = new views.InputDialog("Enter Reference Point ID:");

            // 2. Show the dialog modally and check the result
            bool? result = inputDialog.ShowDialog();

            if (result == true)
            {
                // User clicked OK or pressed Enter
                string userInput = inputDialog.InputText?.Trim();
                System.Console.WriteLine($"User entered: {userInput}");

                if (userInput.Trim() == "")
                {
                    helper.log("User input is blank.");
                    return;
                }

                _patient.BeginModifications();
                {

                    // add to the ref list of the patient
                    helper.log($"Adding a reference point [{userInput}] to patient...");
                    _patient.AddReferencePoint(true, userInput);
                }

                
                // notify the list changed
                OnPropertyChanged(nameof(ReferencePoints));

                // set as the primary reference selection.
                PrimaryReferencePoint = _patient.ReferencePoints.FirstOrDefault(rp=>rp.Id == userInput);
            }
            else
            {
                // User clicked Cancel or closed the window
                System.Console.WriteLine("Input cancelled by user.");
            }


        }

        private void AddNewCourse()
        {
            if (_patient == null)
            {
                helper.show_error_msg_box("Please select a patient first");
                return;
            }

            helper.log("Adding a new course");

            // 1. Create the dialog, passing the required prompt text
            var inputDialog = new views.InputDialog("Enter Course ID:");

            // 2. Show the dialog modally and check the result
            bool? result = inputDialog.ShowDialog();

            if (result == true)
            {
                // User clicked OK or pressed Enter
                string userInput = inputDialog.InputText?.Trim();
                helper.log($"User entered: {userInput}");

                if (userInput.Trim() == "")
                {
                    helper.log("User input is blank.");
                    return;
                }

                _patient.BeginModifications();

                // add course
                helper.log($"Adding a reference point [{userInput}] to patient...");
                VMSCourse cs = _patient.AddCourse();
                cs.Id = userInput;

                // notify the list changed
                OnPropertyChanged(nameof(Courses));

                // set as the selected course
                Course = cs;
            }
            else
            {
                // User clicked Cancel or closed the window
                System.Console.WriteLine("Input cancelled by user.");
            }


        }

        private VMSPatient _patient = null;
        public VMSPatient Patient
        {
            get => _patient;
            set
            {
                if (_patient == value) 
                    return;

                SetProperty<VMSPatient>(ref _patient, value);

                // update reference point list
                OnPropertyChanged(nameof(ReferencePoints));
                
                // update course list
                OnPropertyChanged(nameof(Courses));

                
            }
        }

        public IEnumerable<VMSCourse> Courses
        {
            get => (_patient != null) ? _patient.Courses : null;
        }

        private VMSCourse _course;
        public VMSCourse Course
        {
            get => _course;
            set => SetProperty<VMSCourse>(ref _course, value);
        }

        public ObservableCollection<VMSReferencePoint> ReferencePoints
        {
            get
            {
                return (_patient != null) ? new ObservableCollection<VMSReferencePoint>(_patient.ReferencePoints) : new ObservableCollection<VMSReferencePoint>();
            }
            
        }


        private VMSReferencePoint _primaryReferencePoint;
        public VMSReferencePoint PrimaryReferencePoint
        {
            get => _primaryReferencePoint;
            set => SetProperty(ref _primaryReferencePoint, value, nameof(PrimaryReferencePoint));
        }

        string bladder_id = "BladderAC";
        string bowel_id = "BowelAC";
        string rectum_id = "RectumAC";

        VMSImage _image = null;
        public VMSImage Image
        {
            get =>_image;
            set
            {
                if (_image == value)
                    return;


                helper.log($"Setting image[Id={_image?.Id}]");


                SetProperty<VMSImage>(ref _image, value);

                // update bladder/rectum/
                if (_image != null)
                {
                    List<VMSStructureSet> list = esapi.esapi.sset_list_of_image_id_FOR(_image.Id, _image.FOR, _patient);
                    if (list.Count == 1)
                    {
                        StructureSet = list[0];
                        helper.log($"StructureSet found ({StructureSet.Id})");
                    }
                    else
                    {
                        StructureSet = null;
                        helper.log("Image has no structure set.");
                    }
                }
                else
                {
                    StructureSet=null;
                }
            }
        }

        VMSStructureSet _structureSet = null;
        public VMSStructureSet StructureSet
        {
            get => _structureSet;
            set
            {
                if(_structureSet == value) return;

                SetProperty(ref _structureSet, value, nameof(StructureSet));

                if (_structureSet != null)
                {
                    Bladder = esapi.esapi.s_of_id(bladder_id, _structureSet, false);
                    Rectum = esapi.esapi.s_of_id(rectum_id, _structureSet, false);
                    Bowel = esapi.esapi.s_of_id(bowel_id, _structureSet, false);
                }
                else
                {
                    Bladder = null;
                    Rectum = null;
                    Bowel = null;
                }
            }
        }

        private VMSStructure _bladder;
        public VMSStructure Bladder
        {
            get => _bladder;
            set => SetProperty<VMSStructure>(ref _bladder, value);
        }

        private VMSStructure _bowel;
        public VMSStructure Bowel
        {
            get => _bowel;
            set => SetProperty<VMSStructure>(ref _bowel, value);
        }

        private VMSStructure _rectum;
        public VMSStructure Rectum
        {
            get => _rectum;
            set => SetProperty<VMSStructure>(ref _rectum, value);
        }

        private VMSStructure _ptv;
        public VMSStructure PTV
        {
            get => _ptv;
            set => SetProperty<VMSStructure>(ref _ptv, value);
        }

        private VMSPlanSetup _plan;
        public VMSPlanSetup Plan
        {
            get => _plan;
            set {

                Console.WriteLine($"BladderAutoPlanViewModel.Plan.set(value={value?.Id})");

                if (_plan == value)
                {
                    Console.WriteLine("BladderAutoPlanViewModel.Plan.set() - given plan is the same as the current plan. so, returning...");
                    return;
                }

                Console.WriteLine($"BladderAutoPlanViewModel.Plan.set() - setting plan...({value?.Id})");
                SetProperty<VMSPlanSetup>(ref _plan, value, nameof(Plan));
            }
        }

        
        private double _cropByBodyInnerMargin = 3.0; // 3mm
        public double CropByBodyInnerMargin
        {
            get => _cropByBodyInnerMargin;
            set => SetProperty<double>(ref _cropByBodyInnerMargin, value);
        }



        private double _pTVMargin1All = 3.0; 
        public double PTVMargin1All
        {
            get => _pTVMargin1All;
            set => SetProperty<double>(ref _pTVMargin1All, value);
        }


        private double _pTVMargin2Inf = 3.0; // 5mm inf
        public double PTVMargin2Inf
        {
            get => _pTVMargin2Inf;
            set => SetProperty<double>(ref _pTVMargin2Inf, value);
        }

        private string _newPTVId = "PTV";
        public string NewPTVId
        {
            get => _newPTVId;
            set => SetProperty<string>(ref _newPTVId, value);
        }
       
        string _optiRectumId = "_opti_rectum";
        string _optiBowelId = "_opti_bowel";

        Color ptv_color = Color.FromRgb(255, 0, 0);


        private string _newPlanId = $"ART_{DateTime.Now.ToString("MMdd")}";
        public string NewPlanId
        {
            get => _newPlanId;
            set => SetProperty<string>(ref _newPlanId, value);
        }

        

        private void CreatePTV()
        {
            helper.log("MakePTV()");

            // course selected?

            VMSPatient pt = _patient;
            if (pt == null)
            {
                helper.show_error_msg_box("Please select a patient!");
                return;
            }

            VMSImage image = _image;
            if (image == null)
            {
                helper.show_error_msg_box("Please select an image!");
                return;
            }

            VMSStructureSet sset = StructureSet;
            if (sset == null)
            {
                helper.show_error_msg_box("Error - the selected image has no structureset!");
                return;
            }

            // check bowel exists
            if (_bowel == null)
            {
                helper.show_error_msg_box($"Bowel not set!");
                return;
            }

            // check rectum exists
            if (_rectum == null)
            {
                helper.show_error_msg_box($"Rectum not set!");
                return;
            }

            // check bladder exists
            if (_bladder == null)
            {
                helper.show_error_msg_box($"Bladder not set!");
                return;
            }

            // print
            helper.log($"Patient={pt.Id}");
            helper.log($"Image={image.Id}");
            helper.log($"StructureSet={sset.Id}");
            helper.log($"Bowel={_bowel.Id}");
            helper.log($"Bladder={_bladder.Id}");
            helper.log($"Rectum={_rectum.Id}");

            // ptv already exists?
            if (esapi.esapi.s_of_id(_newPTVId, sset) != null)
            {
                // override?
                if (!helper.show_yes_no_msg_box($"PTV already exitsts (Id={_newPTVId}). Do you want to override?"))
                    return;
            }

            _patient.BeginModifications();

            // create ptv
            this.PTV = bladder_art.make_ptv(
                pt,
                sset,
                image,
                _bladder,
                _rectum,
                _bowel,
                _cropByBodyInnerMargin,
                _pTVMargin1All,
                _pTVMargin2Inf,
                _newPTVId,
                ptv_color);

            helper.log("ptv created!");

            
            // notify the structure set change
            OnPropertyChanged(nameof(StructureSet));

            helper.log("Done.");

            return;
        }

        private bool CreateOptiContours()
        {
            helper.log("MakeOptiContours()");

            // course selected?

            VMSPatient pt = _patient;
            if (pt == null)
            {
                helper.show_error_msg_box("Please select a patient!");
                return false;
            }

            VMSImage image = _image;
            if (image == null)
            {
                helper.show_error_msg_box("Please select an image!");
                return false;
            }

            VMSStructureSet sset = StructureSet;
            if (sset == null)
            {
                helper.show_error_msg_box("Error - the selected image has no structureset!");
                return false;
            }

            // check bowel exists
            if (_bowel == null)
            {
                helper.show_error_msg_box($"Bowel not set!");
                return false;
            }

            // check rectum exists
            if (_rectum == null)
            {
                helper.show_error_msg_box($"Rectum not set!");
                return false;
            }

            // PTV
            if (_ptv == null)
            {
                helper.show_error_msg_box($"PTV not set!");
                return false;
            }

            // print
            helper.log($"Patient={pt.Id}");
            helper.log($"Image={image.Id}");
            helper.log($"StructureSet={sset.Id}");
            helper.log($"Bowel={_bowel.Id}");
            helper.log($"Rectum={_rectum.Id}");
            helper.log($"PTV={_ptv.Id}");

            _patient.BeginModifications();

            helper.log("creating rectum/bowel optimization contours...");
            var result = bladder_art.make_opti_contours(
                _structureSet,
                _ptv,
                _rectum,
                _bowel,
                _optiRectumId,
                _optiBowelId);
            helper.log($"opti contours created ({_optiRectumId},{_optiBowelId})");

           
            // notify the structure set change
            OnPropertyChanged(nameof(StructureSet));

            helper.log("Done.");

            return true;
        }

        private int _numberOfBeams = 7;
        public int NumberOfBeams
        {
            get => _numberOfBeams; 
            set {
                if (_numberOfBeams == value) return;
                SetProperty(ref _numberOfBeams, value, nameof(NumberOfBeams));
            }
        }

        private double _dosePerFraction = 275;
        public double DosePerFraction
        { 
            get => _dosePerFraction;
            set
            {
                if(_dosePerFraction == value) return;

                SetProperty(ref _dosePerFraction, value, nameof(DosePerFraction));

                OnPropertyChanged(nameof(TotalDose));
            }
        }

        private int _numberOfFractions = 20;
        public int NumberOfFractions
        {
            get => _numberOfFractions;
            set
            {
                if (_numberOfFractions == value) return;

                SetProperty(ref _numberOfFractions, value, nameof(NumberOfFractions));

                OnPropertyChanged(nameof(TotalDose));
            }
        }


        public double TotalDose
        {
            get
            {
                return _numberOfFractions * _dosePerFraction;
            }
        }

        private bool _useJawTracking = true;
        public bool UseJawTracking
        {
            get => _useJawTracking;
            set => SetProperty(ref _useJawTracking, value, nameof(UseJawTracking));
        }

        private bool _useIntermediateDoseCalculation = true;
        public bool UseIntermediateDoseCalculation
        {
            get =>_useIntermediateDoseCalculation;
            set => SetProperty(ref _useIntermediateDoseCalculation, value, nameof(UseIntermediateDoseCalculation));
        }

        private async void CreatePlanAndIOptimize()
        {

            helper.log("CreatePlanAndIOptimize()");

            if (_patient == null)
            {
                helper.show_error_msg_box("Please select a patient!");
                return;
            }

            if (_image == null)
            {
                helper.show_error_msg_box("Please select an image!");
                return;
            }

            if (_structureSet == null)
            {
                helper.show_error_msg_box("Error - the selected image has no structureset!");
                return;
            }

            // check bowel exists
            if (_bowel == null)
            {
                helper.show_error_msg_box($"Bowel not set!");
                return;
            }

            // check rectum exists
            if (_rectum == null)
            {
                helper.show_error_msg_box($"Rectum not set!");
                return;
            }

            // check bladder exists
            if (_bladder == null)
            {
                helper.show_error_msg_box($"Bladder not set!");
                return;
            }

            // ptv 
            if (_ptv == null)
            {
                helper.show_error_msg_box($"PTV not set!");
                return;
            }

            // primary reference point
            if (_primaryReferencePoint == null)
            {
                helper.show_error_msg_box($"Primary Reference Point is not set!");
                return;
            }

            // primary reference point
            if (_selectedExternalBeamMachineParameters == null)
            {
                helper.show_error_msg_box($"Machine not selected!");
                return;
            }

            // print
            helper.log($"Patient={_patient.Id}");
            helper.log($"Image={_image.Id}");
            helper.log($"StructureSet={_structureSet.Id}");
            helper.log($"Bowel={_bowel.Id}");
            helper.log($"Bladder={_bladder.Id}");
            helper.log($"Rectum={_rectum.Id}");
            helper.log($"PTV={_ptv.Id}");

            // plan already exists?
            if (esapi.esapi.ps_of_id(_newPlanId, _patient) != null)
            {
                // override?
                helper.show_error_msg_box($"Plan already exitsts (Id={_newPlanId})!");
                return;
            }


            helper.show_info_msg_box("It will take about a couple of minutes. Please be patient...");

            // create opti contours
            if (!CreateOptiContours())
                return;

            int task_delay_milliseconds = 1000;

            try
            {
                // 1. Set the cursor to Wait at the very beginning
                Mouse.OverrideCursor = Cursors.Wait;

                _patient.BeginModifications();

                // if plan exists, remove it first.
                if(esapi.esapi.ps_of_id(_newPlanId,_patient) != null)
                {
                    helper.log($"Removing existing plan of Id={_newPlanId}");
                    esapi.esapi.remove_ps(_newPlanId, _patient);
                }

                helper.log("Beginning create plan and optimization");

                helper.log($"plan id={_newPlanId}");
                helper.log($"number of fractions={_numberOfFractions}");
                helper.log($"dose per fraction={_dosePerFraction}");

                // update UI
                await Task.Delay(task_delay_milliseconds);

                string default_imaging_device_id = "CTAWP96967";
                string optimization_model = "PO_13623";
                string volume_dose_calculation_model = "AAA_13623";

                ExternalBeamMachineParameters machineParameters = SelectedExternalBeamMachineParameters;
                helper.log($"MachineId={machineParameters.MachineId}");
                helper.log($"EnergyModeId={machineParameters.EnergyModeId}");

                VMSPlanSetup ps = (VMSPlanSetup) await bladder_art.create_bladder_plan4(
                    _patient,
                    _structureSet,
                    _course,
                    _primaryReferencePoint,
                    machineParameters,
                    _newPlanId,
                    _ptv.Id,
                    _bladder.Id,
                    _rectum.Id,
                    _optiRectumId,
                    _bowel.Id,
                    _optiBowelId,
                    _numberOfBeams,
                    _numberOfFractions,
                    _dosePerFraction,
                    default_imaging_device_id,
                    optimization_model,
                    volume_dose_calculation_model,
                    _useIntermediateDoseCalculation,
                    _useJawTracking,
                    task_delay_milliseconds
                );

                //ps.PlanNormalizationValue = 50.0;

                Plan = ps;

                if (Plan != null)
                {
                    // set dose limits
                    Plan.PrimaryReferencePoint.DailyDoseLimit = new VMS.TPS.Common.Model.Types.DoseValue(DosePerFraction, "cGy");
                    Plan.PrimaryReferencePoint.SessionDoseLimit = new VMS.TPS.Common.Model.Types.DoseValue(DosePerFraction, "cGy");
                    Plan.PrimaryReferencePoint.TotalDoseLimit = new VMS.TPS.Common.Model.Types.DoseValue(TotalDose, "cGy");

                    
                }

                helper.log("Done.");
            }
            catch (Exception ex)
            {
                helper.error($"Error: {ex.Message}");
            }
            finally
            {
                // 3. ALWAYS reset the cursor back to default in the finally block
                Mouse.OverrideCursor = null;
            }



        }

    }
}
