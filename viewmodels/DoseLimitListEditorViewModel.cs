using itk.simple;
using Microsoft.Win32;
using Newtonsoft.Json;
using nnunet_client.models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace nnunet_client.viewmodels
{
    [JsonObject(MemberSerialization.OptIn)] // only include explicitly marked properties
    public class DoseLimitListEditorViewModel : BaseViewModel
    {
        [JsonIgnore]  // not include in JSON
        public ICommand LoadCommand { get; }
        [JsonIgnore]  // not include in JSON
        public ICommand SaveCommand { get; }

        [JsonIgnore]  // not include in JSON
        public ICommand EvaluateCommand { get; }

        private Visibility _saveButtonVisibility = Visibility.Visible;
        [JsonIgnore]
        public Visibility SaveButtonVisibility
        {
            get => _saveButtonVisibility;
            set => SetProperty<Visibility>(ref _saveButtonVisibility, value);
        }

        private Visibility _loadButtonVisibility = Visibility.Visible;
        [JsonIgnore]
        public Visibility LoadButtonVisibility
        {
            get => _loadButtonVisibility;
            set => SetProperty<Visibility>(ref _loadButtonVisibility, value);
        }


        private string _title = "Dose Limits";
        [JsonProperty]  // include in JSON
        public string Title { 
            get=>_title;
            set
            {
                if (_title == value) return;

                SetProperty<string>(ref _title, value);
            }
        }

    
        private VMS.TPS.Common.Model.API.PlanningItem _plan;
        [JsonIgnore]  // not include in JSON
        public VMS.TPS.Common.Model.API.PlanningItem Plan
        {
            get
            {
                return _plan;
            }
            set
            {
                if (value == _plan)
                {
                    Console.WriteLine($"DoseLimitListEditorViewModel - given plan is the same as the current plan. so, returning...");
                    return;
                }

                Console.WriteLine($"DoseLimitListEditorViewModel - Setting a new plan...{value?.Id}");
                SetProperty(ref _plan, value, nameof(Plan));

                OnPropertyChanged(nameof(PlanNormalizationValue));

                DoseLimitListViewModel.Plan = _plan;
            }
        }

        public double PlanNormalizationValue
        {
            get
            {
                if (_plan != null)
                {
                    double value = ((VMS.TPS.Common.Model.API.PlanSetup)_plan).PlanNormalizationValue;
                    
                    Console.WriteLine($"PlanNormalizationValue={value}");

                    return value;
                }
                else 
                {
                    Console.WriteLine("_plan is null. returning Nan.");
                    return double.NaN;
                }
            }

            set
            {
                Console.WriteLine($"PlanNormalizationValue.set({value})");

                if (PlanNormalizationValue == value) return;

                if (_plan != null && value != double.NaN)
                {
                    global.vmsPatient.BeginModifications();

                    Console.WriteLine($"Plan is not null, value is not NaN. Setting PlanNormalizationValue to {value}");
                    ((VMS.TPS.Common.Model.API.PlanSetup)_plan).PlanNormalizationValue = value;
                }
                else if(_plan == null) 
                {
                    Console.WriteLine("Plan is null.");
                    DoseLimitListViewModel.Plan = null;
                }
                else
                {
                    Console.WriteLine("value is NaN.");
                    DoseLimitListViewModel.Plan = null;
                }

                DoseLimitListViewModel.Evaluate();

                OnPropertyChanged(nameof(Plan));
            }
        }


        private PrescriptionListViewModel _prescriptionListViewModel;
        [JsonIgnore]
        public PrescriptionListViewModel PrescriptionListViewModel
        {
            get=>_prescriptionListViewModel;
            set=> SetProperty<PrescriptionListViewModel>(ref _prescriptionListViewModel, value);
        }
        
        private DoseLimitListViewModel _doseLimitListViewModel;
        [JsonProperty]  // include in JSON
        public DoseLimitListViewModel DoseLimitListViewModel
        {
            get=>_doseLimitListViewModel;
            set=>SetProperty<DoseLimitListViewModel>(ref _doseLimitListViewModel, value);
        }


        // constructor
        public DoseLimitListEditorViewModel() { 
            
            // prescription and contours
            ObservableCollection<Prescription> prescriptions = new ObservableCollection<Prescription>();
            
            // prescription view model
            this.PrescriptionListViewModel = new PrescriptionListViewModel();
            this.PrescriptionListViewModel.SaveLoadButtonsVisibility = Visibility.Collapsed;
            this.PrescriptionListViewModel.Prescriptions = prescriptions;
            
            // doselimit list view model
            this.DoseLimitListViewModel = new DoseLimitListViewModel();
            this.DoseLimitListViewModel.SaveLoadButtonsVisibility = Visibility.Collapsed;
            this.Title = "Dose Limits";
            this.DoseLimitListViewModel.Prescriptions = prescriptions;
            
            // commands
            LoadCommand = new RelayCommand(Load);
            SaveCommand = new RelayCommand(Save);

            EvaluateCommand = new RelayCommand(Evaluate);

        }

        public DoseLimitListEditorViewModel Duplicate()
        {
            DoseLimitListEditorViewModel copy = new DoseLimitListEditorViewModel();
            copy.Title = this.Title;
            copy.DoseLimitListViewModel = this.DoseLimitListViewModel.Duplicate();
            copy.PrescriptionListViewModel = this.PrescriptionListViewModel.Duplicate();

            return copy;
        }

        private string _templateFilePath;
        public string TemplateFilePath
        {
            get { return _templateFilePath; }
            set {

                if (_templateFilePath == value) return;

                
                SetProperty<string> (ref _templateFilePath, value);
                Console.WriteLine($"TemplateFilePath ={TemplateFilePath} ");

                if (_templateFilePath != null && File.Exists(_templateFilePath))
                {
                    Console.WriteLine("Loading Template");
                    LoadFromTemplateFile(_templateFilePath);
                }
                else
                {
                    Console.WriteLine("TemplateFilePath is null or not found");
                }
            }
        }

        public void LoadFromTemplateFile(string templateFilePath)
        {
            Console.WriteLine($"loading from template file: {templateFilePath}...");

            string json = File.ReadAllText(templateFilePath);
            DoseLimitListEditorViewModel data = JsonConvert.DeserializeObject<DoseLimitListEditorViewModel>(json);

            // sort by priority
            data.DoseLimitListViewModel.DoseLimits = new ObservableCollection<DoseLimit>(data.DoseLimitListViewModel.DoseLimits.OrderBy(item => item.Priority));
            data.Plan = this.Plan;
            data.DoseLimitListViewModel.Evaluate();

            if (data != null)
            {
                this.Title = data.Title;
                this.DoseLimitListViewModel = data.DoseLimitListViewModel;
                this.PrescriptionListViewModel = data.PrescriptionListViewModel;
            }
        }

        public void Load()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                TemplateFilePath = openFileDialog.FileName;
            }
        }

        public void Save()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            if (saveFileDialog.ShowDialog() == true)
            {
                string path = saveFileDialog.FileName;
                Console.WriteLine($"Save to {path}");

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, json);
            }
        }

        public void Evaluate()
        {
            this.DoseLimitListViewModel.Evaluate();
        }



    }
}
