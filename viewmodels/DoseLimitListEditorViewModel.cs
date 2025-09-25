using Microsoft.Win32;
using Newtonsoft.Json;
using nnunet_client.models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace nnunet_client.viewmodels
{
    [JsonObject(MemberSerialization.OptIn)] // only include explicitly marked properties
    public class DoseLimitListEditorViewModel : BaseViewModel
    {
        [JsonIgnore]  // not include in JSON
        public ICommand LoadCommand { get; }
        [JsonIgnore]  // not include in JSON
        public ICommand SaveCommand { get; }

        private string _title = "Dose Limit Set";
        [JsonProperty]  // include in JSON
        public string Title { 
            get=>_title;
            set=>SetProperty<string>(ref _title, value);
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
                if (value != _plan)
                {
                    Console.WriteLine($"DoseLimitListEditorViewModel - Setting a new plan...{_plan?.Id}");
                    
                    SetProperty<VMS.TPS.Common.Model.API.PlanningItem>(ref _plan, value);

                    DoseLimitListViewModel.Plan = _plan;
                }
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
        }


        private void Load()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string path = openFileDialog.FileName;
                Console.WriteLine($"loading from {path}...");

                string json = File.ReadAllText(path);
                DoseLimitListEditorViewModel data = JsonConvert.DeserializeObject<DoseLimitListEditorViewModel>(json);

                if (data != null)
                {
                    this.Title = data.Title;
                    this.DoseLimitListViewModel = data.DoseLimitListViewModel;
                    this.PrescriptionListViewModel = data.PrescriptionListViewModel;
                }

            }
        }

        private void Save()
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

        

    }
}
