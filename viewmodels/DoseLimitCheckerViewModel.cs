using Microsoft.Win32;
using Newtonsoft.Json;
using nnunet_client.models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using Xceed.Wpf.Toolkit.Core.Converters;
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
    [JsonObject(MemberSerialization.OptIn)] // only include explicitly marked properties
    public class DoseLimitCheckerViewModel : BaseViewModel
    {
        public ICommand ImportTemplateCommand { get; }


        public ICommand AddDoseLimitSetCommand { get; }

        public ICommand RemoveDoseLimitSetCommand { get; }

        public ICommand DuplicateDoseLimitSetCommand { get; }
        public ICommand SaveDoseLimitListEditorViewModelListCommand { get; }


        private void AddDoseLimitSet()
        {
            if (!validate_selections())
                return;

            var data = new DoseLimitListEditorViewModel();
            data.LoadButtonVisibility = Visibility.Collapsed;
            this.DoseLimitListEditorViewModelList.Add(data);
            this.SelectedDoseLimitListEditorViewModel = data;
        }

        private void RemoveDoseLimitSet()
        {
            if (!validate_selections())
                return;

            if (this.SelectedDoseLimitListEditorViewModel == null)
            {
                MessageBox.Show("Select an item to remove");
                return;
            }

            this.DoseLimitListEditorViewModelList.Remove(this.SelectedDoseLimitListEditorViewModel);
        }
        private bool CanRemoveDoseLimitSet()
        {
            // The button is only enabled if an item is currently selected.
            return this.SelectedDoseLimitListEditorViewModel != null;
        }

        private void DuplicateDoseLimitSet()
        {
            if (!validate_selections())
                return;

            if (this.SelectedDoseLimitListEditorViewModel == null)
            {
                MessageBox.Show("Select an item to duplicate");
                return;
            }

            var copy = this.SelectedDoseLimitListEditorViewModel.Duplicate();

            copy.Title = copy.Title + " Copy";

            this.DoseLimitListEditorViewModelList.Add(copy);
        }
        private bool CanDuplicateDoseLimitSet()
        {
            // The button is only enabled if an item is currently selected.
            return this.SelectedDoseLimitListEditorViewModel != null;
        }


        private void ImportTemplate()
        {
            if (!validate_selections())
                return;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.Multiselect = false;
            openFileDialog.InitialDirectory = get_templates_dir();

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // copy the template file as a case file
                    string case_file_path = get_plan_dose_limit_list_json_file_path();
                    if (case_file_path == null)
                        return;


                    // copy tempalte file to a case file
                    Console.WriteLine($"Copying tempalte file ({openFileDialog.FileName}) as plan case file {case_file_path}");
                    filesystem.copy_file(openFileDialog.FileName, case_file_path);

                    // load case file and set to the controler
                    Console.WriteLine($"Loading case file... {case_file_path}");
                    string json = File.ReadAllText(case_file_path);
                    DoseLimitListEditorViewModel data = JsonConvert.DeserializeObject<DoseLimitListEditorViewModel>(json);
                    data.LoadButtonVisibility = Visibility.Collapsed;

                    // add to the list and select
                    this.DoseLimitListEditorViewModelList.Add(data);
                    this.SelectedDoseLimitListEditorViewModel = data;
                }
                catch (Exception ex)
                {
                    // Handle potential errors during file reading or deserialization.
                    Console.WriteLine($"Error loading file: {openFileDialog.FileName}");
                }
            }
        }

        private void _p(string msg)
        {
            Console.WriteLine(msg);
        }


        [JsonIgnore]  // not include in JSON
        public ObservableCollection<string> PatientNameIdList { get; }
        private string _selectedPatientNameId;
        public string SelectedPatientNameId {

            get {
                return this._selectedPatientNameId;
            }

            set {
                this._selectedPatientNameId = value;

                _p($"Selected: {this._selectedPatientNameId}");

                string[] elms = this._selectedPatientNameId.Split(',');
                string patientId = elms[elms.Length - 1].Trim();

                // populate the courses
                _p($"Closing existing patient...");
                _esapiApp.ClosePatient();

                _p($"Opening patient by Id ({patientId})...");
                VMSPatient pt = _esapiApp.OpenPatientById(patientId);

                if (pt != null)
                {
                    _p($"Setting to Selected Patient property...");
                    SelectedPatient = pt;
                }
                else
                {
                    _p($"Failed opening a patient by Id!");
                }
            }
        }

        VMSPatient _patient;
        public VMSPatient SelectedPatient
        {
            get
            {
                return _patient;
            }

            set
            {
                if (value == _patient)
                    return;

                _patient = value;
                OnPropertyChanged(nameof(SelectedPatient));

                if (_patient == null)
                {
                    CourseIdList = new ObservableCollection<string>();
                }
                else
                {
                    // initialize collection from ESAPI
                    CourseIdList = new ObservableCollection<string>(
                        _patient.Courses.Select(c => $"{c.Id}")
                    );
                }
                OnPropertyChanged(nameof(CourseIdList));

                _p($"New Course Id List");
                foreach (string cid in CourseIdList)
                {
                     _p(cid);
                }
            }
        }

        public string PatientSearchQuery {  get; set; }


        public ObservableCollection<string> CourseIdList { get; set; }
        private string _selectedCourseId;
        private VMSCourse _course;
        public string SelectedCourseId {
            get { return _selectedCourseId; }
            set {


                if (value == _selectedCourseId)
                    return;

                _p($"SelectedCourseId Changed...{value}");

                SetProperty<string>(ref _selectedCourseId, value);

                // Assuming you have the course ID you want to find as a string, e.g., "Course1"
                string courseIdToFind = _selectedCourseId;

                // Use the FirstOrDefault method to find the matching course object
                VMSCourse selectedCourse = _patient.Courses.FirstOrDefault(c => c.Id == courseIdToFind);

                // Now you can work with the selectedCourse object
                if (selectedCourse != null)
                {
                    Console.WriteLine($"Found the course: {selectedCourse.Id}");
                    // Do something with the course object, like getting its plans
                    PlanIdList = new ObservableCollection<string>(selectedCourse.PlanSetups.Select(ps => ps.Id).ToList());
                    OnPropertyChanged(nameof(PlanIdList));

                    _course = selectedCourse;
                }
                else
                {
                    Console.WriteLine($"Course with ID '{courseIdToFind}' not found.");
                }
            }
        }

        private bool validate_selections()
        {
            if (_patient == null)
            {
                MessageBox.Show("Please select a patient!");
                return false;
            }

            if (_course == null)
            {
                MessageBox.Show("Please select a course!");
                return false;
            }

            if (_plan == null)
            {
                MessageBox.Show("Please select a plan!");
                return false;
            }

            return true;
        }

        private string get_plan_dose_limit_list_json_file_path()
        {
            if (!validate_selections())
                return null;

            string cases_dir = get_cases_dir();
            string pt_dir = filesystem.join(cases_dir, _patient.Id, true);
            string cs_dir = filesystem.join(pt_dir, _course.Id, true);
            string ps_dir = filesystem.join(cs_dir, _plan.Id, true);
            string dose_limit_file_path = filesystem.join(ps_dir, "dose_limits.json");

            return dose_limit_file_path;
        }

        private void load_plan_dose_limits_list(string file_path)
        {
            try
            {
                string json = File.ReadAllText(file_path);
                ObservableCollection<DoseLimitListEditorViewModel> list = JsonConvert.DeserializeObject<ObservableCollection<DoseLimitListEditorViewModel>>(json);

                if (list == null)
                {
                    Console.WriteLine("Loading returned a null object...");
                    return;
                }

                if (list.Count == 0)
                {
                    Console.WriteLine("Loaded object has no model list item...");
                }

                // Clear the existing list and add the loaded items.
                this.DoseLimitListEditorViewModelList.Clear();
                foreach (var item in list)
                {
                    Console.WriteLine($"Adding a view model [{item.Title}]...");
                    DoseLimitListEditorViewModelList.Add(item);
                }
            }
            catch (Exception ex)
            {
                // Handle potential errors during file reading or deserialization.
                Console.WriteLine($"Error loading prescriptions: {ex.Message}");
                // clear the list
                DoseLimitListEditorViewModelList = new ObservableCollection<DoseLimitListEditorViewModel>();
            }
        }

        public ObservableCollection<string> PlanIdList { get; set; }
        private string _selectedPlanId;
        private VMSPlanSetup _plan;
        public string SelectedPlanId
        {
            get { return _selectedPlanId; }
            set
            {
                if (_selectedPlanId == value)
                    return;

                _p($"SelectedPlanId Changed...{value}");
                SetProperty<string>(ref _selectedPlanId, value);

                // plan id
                string idToFind = value;

                // plan
                VMSPlanSetup selectedItem = _course.PlanSetups.FirstOrDefault(ps => ps.Id == idToFind);

                // a plan found
                if (selectedItem != null)
                {
                    _p($"Found the plan: {selectedItem.Id}");

                    // set plan
                    _plan = selectedItem;

                    // if dose limits list file exists, load.
                    string file_path = get_plan_dose_limit_list_json_file_path();
                    if (filesystem.file_exists(file_path))
                        load_plan_dose_limits_list(file_path);
                    else
                    {
                        // clear the list
                        DoseLimitListEditorViewModelList = new ObservableCollection<DoseLimitListEditorViewModel>();
                    }

                    // update the plan of the selected dose limit editor
                    if (this.SelectedDoseLimitListEditorViewModel != null)
                    {
                        _p("Setting the plan to the selected dose limit list editor...");
                        this.SelectedDoseLimitListEditorViewModel.Plan = (VMS.TPS.Common.Model.API.PlanningItem)_plan;
                    }
                
                }
                else
                {
                    _p($"Plan with ID '{idToFind}' not found.");
                    // clear the list
                    DoseLimitListEditorViewModelList = new ObservableCollection<DoseLimitListEditorViewModel>();
                }
            }
        }


        ObservableCollection<models.Contour> ContourList { 
            get {
                if (_plan != null)
                    return new ObservableCollection<models.Contour>(_plan.StructureSet.Structures.Select(s => new models.Contour() { Id = s.Id }));
                else
                    return new ObservableCollection<models.Contour>();
            }
        }


        // dose limit directories
        string root_dir;
        string get_cases_dir()
        {
            return filesystem.join(root_dir, "cases", true);
        }
        string get_templates_dir()
        {
            return filesystem.join(root_dir, "templates", true);
        }

        private ObservableCollection<DoseLimitListEditorViewModel> _doseLimitListEditorViewModelList = new ObservableCollection<DoseLimitListEditorViewModel>();
        public ObservableCollection<DoseLimitListEditorViewModel> DoseLimitListEditorViewModelList
        {
            get =>_doseLimitListEditorViewModelList;
            set => SetProperty<ObservableCollection<DoseLimitListEditorViewModel>>(ref _doseLimitListEditorViewModelList, value);
        }

        private DoseLimitListEditorViewModel _selectedDoseLimitListEditorViewModel = null;
        public DoseLimitListEditorViewModel SelectedDoseLimitListEditorViewModel
        {
            get { return _selectedDoseLimitListEditorViewModel; }
            set
            {
                if (_selectedDoseLimitListEditorViewModel != value) 
                {
                    _p($"editor view model changed to {_selectedDoseLimitListEditorViewModel?.Title}");
                    SetProperty<DoseLimitListEditorViewModel>(ref _selectedDoseLimitListEditorViewModel, value);
                    _selectedDoseLimitListEditorViewModel.LoadButtonVisibility = Visibility.Collapsed;

                    if(_selectedDoseLimitListEditorViewModel !=null)
                    {
                        _p($"Setting the plan to the selected dose limit editor");
                        this._selectedDoseLimitListEditorViewModel.Plan = this._plan;
                    }
                }
            }
        }


        private VMS.TPS.Common.Model.API.Application _esapiApp;
        public DoseLimitCheckerViewModel(VMS.TPS.Common.Model.API.Application esapiApp) 
        {

            _esapiApp = esapiApp;
            
            // init folders
            root_dir = filesystem.join(global.data_root_secure, "_dose_limits", true);
            
            // initialize collection from ESAPI
            PatientNameIdList = new ObservableCollection<string>(
                _esapiApp.PatientSummaries.Select(p => $"{p.LastName}, {p.FirstName}, {p.Id}")
            );

            // empty editor
            this.SelectedDoseLimitListEditorViewModel = new DoseLimitListEditorViewModel();
            this.SelectedDoseLimitListEditorViewModel.LoadButtonVisibility= Visibility.Collapsed;
            
            // commands
            AddDoseLimitSetCommand = new RelayCommand(AddDoseLimitSet);
            RemoveDoseLimitSetCommand = new RelayCommand(RemoveDoseLimitSet, CanRemoveDoseLimitSet);
            DuplicateDoseLimitSetCommand = new RelayCommand(DuplicateDoseLimitSet, CanDuplicateDoseLimitSet);
            ImportTemplateCommand = new RelayCommand(ImportTemplate);
            SaveDoseLimitListEditorViewModelListCommand = new RelayCommand(SaveDoseLimitListEditorViewModelList);
        }

        private void EvaluateDoseLimites()
        {
            Console.WriteLine("Evalute Dose Limites...");
        }

        private void LoadDoseLimitListEditorViewModelList()
        {
            string path = get_plan_dose_limit_list_json_file_path();
            Console.WriteLine($"loading vm list from {path}");

            string json = File.ReadAllText(path);
            ObservableCollection<DoseLimitListEditorViewModel> data = JsonConvert.DeserializeObject< ObservableCollection<DoseLimitListEditorViewModel>>(json);

            if (data != null) { 
                this.DoseLimitListEditorViewModelList = data;
            }
        }

        private void SaveDoseLimitListEditorViewModelList()
        {
            string path = get_plan_dose_limit_list_json_file_path();
            Console.WriteLine($"Save vm list to {path}");

            var json = JsonConvert.SerializeObject(this.DoseLimitListEditorViewModelList, Formatting.Indented);
            File.WriteAllText(path, json);

            MessageBox.Show("Saved!");
        }

        

    }




}
