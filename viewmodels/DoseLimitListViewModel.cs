using Microsoft.Win32;
using Newtonsoft.Json;
using nnunet_client.models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows;
using System.Threading;

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
    public class DoseLimitListViewModel : BaseViewModel
    {
        [JsonIgnore]
        public ICommand AddCommand { get; }
        [JsonIgnore]
        public ICommand RemoveCommand { get; }

        [JsonIgnore]
        public ICommand DuplicateCommand { get; }

        [JsonIgnore]
        public ICommand LoadCommand { get; }
        [JsonIgnore]
        public ICommand SaveCommand { get; }

        private Visibility _saveLoadButtonsVisibilty = Visibility.Visible;
        public Visibility SaveLoadButtonsVisibility
        {
            get { return _saveLoadButtonsVisibilty; }
            set
            {
                if (_saveLoadButtonsVisibilty != value)
                {
                    _saveLoadButtonsVisibilty = value;

                    OnPropertyChanged(nameof(SaveLoadButtonsVisibility));

                    Console.WriteLine($"SaveLoadButtonsVisible={_saveLoadButtonsVisibilty}");
                }
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
                if (_plan == value)
                {
                    Console.WriteLine($"DoseLimitListViewModel - Given plan is the same as the current plan, so returning...{_plan?.Id}");
                    return;
                }

                Console.WriteLine($"DoseLimitListViewModel - Setting a new plan...{value?.Id}");
                SetProperty<VMS.TPS.Common.Model.API.PlanningItem>(ref _plan, value, nameof(Plan));

                if (_plan != null)
                {
                    // set plan to the dose limits
                    foreach (DoseLimit doseLimit in this.DoseLimits)
                    {
                        doseLimit.Plan = _plan;
                    }

                    // update Contours
                    if (_plan.StructureSet != null)
                        Contours = new ObservableCollection<models.Contour>(_plan.StructureSet.Structures.Select(s => new models.Contour() { Id = s.Id }));
                    else
                        Contours = new ObservableCollection<models.Contour>();
                }
                else
                {
                    Console.WriteLine($"DoseLimitListViewModel - plan is null. clearning Contours list and setting null to DoseLimits...");

                    Contours = new ObservableCollection<models.Contour>();

                    // reset plan
                    foreach (DoseLimit doseLimit in this.DoseLimits)
                    {
                        doseLimit.Plan = null;
                    }
                }
            }
        }

        private ObservableCollection<Prescription> _prescriptions;
        [JsonProperty]  // ✅ include in JSON
        public ObservableCollection<Prescription> Prescriptions
        {
            get => _prescriptions;
            set => SetProperty<ObservableCollection<Prescription>>(ref _prescriptions, value);
        }

        private ObservableCollection<DoseLimit> _doseLimits;
        [JsonProperty]  // ✅ include in JSON
        public ObservableCollection<DoseLimit> DoseLimits
        {
            get => _doseLimits;
            set => SetProperty<ObservableCollection<DoseLimit>>(ref _doseLimits, value);
        }

        public void Evaluate()
        {
            if (_doseLimits == null) return;

            foreach (DoseLimit doseLimit in _doseLimits)
            {
                doseLimit.Evaluate();
             }
        }

        private DoseLimit _selectedDoseLimit;
        [JsonIgnore]  // not include in JSON
        public DoseLimit SelectedDoseLimit
        {
            get => _selectedDoseLimit;
            set
            {
                if (value != _selectedDoseLimit)
                {
                    SetProperty<DoseLimit>(ref _selectedDoseLimit, value);
                   
                    ((RelayCommand)RemoveCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DuplicateCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private ObservableCollection<models.Contour> _contours;

        [JsonIgnore]  // not include in JSON
        public ObservableCollection<models.Contour> Contours
        {
            get => _contours;
            set => SetProperty<ObservableCollection<models.Contour>>(ref _contours, value);
        }


        //private string _comments;
        //[JsonProperty]  // ✅ include in JSON
        //public string Comments
        //{
        //    get => _comments;
        //    set
        //    {
        //        _comments = value;
        //        OnPropertyChanged();
        //    }
        //}

        // constructor
        public DoseLimitListViewModel()
        {
            DoseLimits = new ObservableCollection<DoseLimit>();

            //var cont1 = new Contour { Id = "PTV" };
            //var cont2 = new Contour { Id = "Bowel" };

         
            //Contours.Add(cont1);
            //Contours.Add(cont2);

            // Add some initial dummy data
            //DoseLimits.Add(new DoseLimit { Id = "DL001", Contour = cont1, ContourType = DoseLimitContourType.Target });
            //DoseLimits.Add(new DoseLimit { Id = "DL002", Contour = cont2, ContourType = DoseLimitContourType.OAR });

            //DoseLimits.Add(new DoseLimit { Id = "DL001", Contour = cont1 });
            //DoseLimits.Add(new DoseLimit { Id = "DL002", Contour = cont2 });

            Prescriptions = new ObservableCollection<Prescription>();


            //Prescriptions.Add(new Prescription { Id = "Default", TotalDose = 3000 });

            LoadCommand = new RelayCommand(Load);
            SaveCommand = new RelayCommand(Save);
            AddCommand = new RelayCommand(AddItem);
            RemoveCommand = new RelayCommand(RemoveItem, IsItemSelected);
            DuplicateCommand = new RelayCommand(DuplicateItem, IsItemSelected);
        }

        private void AddItem()
        {
            DoseLimit doselimit = new DoseLimit { Id = "New", Plan=Plan };

            // add the first prescirption
            if (_prescriptions?.Count()>0)
                doselimit.Prescription = _prescriptions[0];

            DoseLimits.Add(doselimit);

            SelectedDoseLimit = DoseLimits.Last();
        }

        private void RemoveItem()
        {
            if (SelectedDoseLimit != null)
            {
                DoseLimits.Remove(SelectedDoseLimit);
            }
        }

        private void DuplicateItem()
        {
            if (SelectedDoseLimit != null)
            {
                DoseLimits.Add(SelectedDoseLimit.Duplicate());
            }
        }


        private bool IsItemSelected()
        {
            return SelectedDoseLimit != null;
        }

        public DoseLimitListViewModel Duplicate()
        {
            DoseLimitListViewModel copy = new DoseLimitListViewModel();
            // dose limites
            copy.DoseLimits.Clear();
            if (this.DoseLimits != null)
            {
                foreach (var d in this.DoseLimits)
                {
                    copy.DoseLimits.Add(d.Duplicate());
                }
            }

            // prescriptions
            copy.Prescriptions.Clear();
            if (this.Prescriptions != null)
            {
                foreach (var d in this.Prescriptions)
                {
                    copy.Prescriptions.Add(d.Duplicate());
                }
            }

            // contours
            copy.Contours.Clear();
            if (this.Contours != null)
            {
                foreach (var d in this.Contours)
                {
                    copy.Contours.Add(d.Duplicate());
                }
            }

            return copy;
        }

        public void Load()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            { 
                try
                {
                    string json = File.ReadAllText(openFileDialog.FileName);
                    Console.WriteLine(json);

                    DoseLimitListViewModel loaded = JsonConvert.DeserializeObject<DoseLimitListViewModel>(json);

                    // dose limites
                    DoseLimits.Clear();
                    if (loaded.DoseLimits != null)
                    {
                        foreach (var d in loaded.DoseLimits)
                        {
                            Console.WriteLine("Adding Dose Limit:");
                            Console.WriteLine(d.ToString());
                            DoseLimits.Add(d);
                        }
                    }

                    // prescriptions
                    Prescriptions.Clear();
                    if (loaded.Prescriptions != null)
                    {
                        foreach (var d in loaded.Prescriptions)
                        {
                            Prescriptions.Add(d);
                        }
                    }

                    // contours
                    Contours.Clear();
                    if (loaded.Contours != null)
                    {
                        foreach (var d in loaded.Contours)
                        {
                            Contours.Add(d);
                        }
                    }

                    // comments
                    // this.Comments = loaded.Comments;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading dose limits:\n{ex.Message}",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        public void Save()
        {
            
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                saveFileDialog.FileName = "doselimits";

               

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                    System.IO.File.WriteAllText(saveFileDialog.FileName, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving dose limits: {ex.Message}");
                }
            }
        }

        

       
    }
}