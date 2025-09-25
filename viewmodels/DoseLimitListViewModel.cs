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
                if (value != _plan)
                {
                    Console.WriteLine($"DoseLimitListViewModel - Setting a new plan...{_plan?.Id}");

                    SetProperty<VMS.TPS.Common.Model.API.PlanningItem>(ref _plan, value);

                    OnPropertyChanged(nameof(Contours));

                    // set plan to the dose limits
                    foreach (DoseLimit doseLimit in this.DoseLimits)
                    {
                        doseLimit.Plan = value;
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

        [JsonIgnore]  // not include in JSON
        public ObservableCollection<models.Contour> Contours
        {
            get
            {
                if (_plan != null)
                    return new ObservableCollection<models.Contour>(_plan.StructureSet.Structures.Select(s => new models.Contour() { Id = s.Id }));
                else
                    return new ObservableCollection<models.Contour>();
            }
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
            AddCommand = new RelayCommand(Add);
            RemoveCommand = new RelayCommand(Remove, IsItemSelected);
            DuplicateCommand = new RelayCommand(Duplicate, IsItemSelected);
        }

        private void Add()
        {
            DoseLimit doselimit = new DoseLimit { Id = "New" };

            if (_prescriptions?.Count()>0)
                doselimit.Prescription = _prescriptions[0];

            DoseLimits.Add(doselimit);

            SelectedDoseLimit = DoseLimits.Last();
        }

        private void Remove()
        {
            if (SelectedDoseLimit != null)
            {
                DoseLimits.Remove(SelectedDoseLimit);
            }
        }

        private void Duplicate()
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

        public void Load()
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            { 
                try
                {
                    string json = File.ReadAllText(openFileDialog.FileName);
                    Console.WriteLine("==== json =========");
                    Console.WriteLine(json);
                    Console.WriteLine("==== end of json =========");

                    DoseLimitListViewModel loaded = JsonConvert.DeserializeObject<DoseLimitListViewModel>(json);

                    // dose limites
                    Console.WriteLine("Clerning DoseLimites....");
                    DoseLimits.Clear();

                    Console.WriteLine("Loaded Data===>");
                    Console.WriteLine(loaded.ToString());
                    Console.WriteLine("<==== Loaded Data");

                    Console.WriteLine($"number of loaded dose limites: {loaded.DoseLimits.Count}");

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