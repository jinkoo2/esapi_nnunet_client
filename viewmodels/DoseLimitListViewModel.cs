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

namespace nnunet_client.viewmodels
{
    [JsonObject(MemberSerialization.OptIn)] // only include explicitly marked properties
    public class DoseLimitListViewModel : INotifyPropertyChanged
    {
        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }

        private ObservableCollection<Prescription> _prescriptions;
        [JsonProperty]  // ✅ include in JSON
        public ObservableCollection<Prescription> Prescriptions
        {
            get => _prescriptions;
            set
            {
                _prescriptions = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<DoseLimit> _doseLimits;
        [JsonProperty]  // ✅ include in JSON
        public ObservableCollection<DoseLimit> DoseLimits
        {
            get => _doseLimits;
            set
            {
                _doseLimits = value;
                OnPropertyChanged();
            }
        }

        private DoseLimit _selectedDoseLimit;
        [JsonIgnore]  // not include in JSON
        public DoseLimit SelectedDoseLimit
        {
            get => _selectedDoseLimit;
            set
            {
                _selectedDoseLimit = value;
                OnPropertyChanged();
                ((RelayCommand)RemoveCommand).RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<Contour> _contours;
        [JsonProperty]  // ✅ include in JSON
        public ObservableCollection<Contour> Contours
        {
            get => _contours;
            set
            {
                _contours = value;
                OnPropertyChanged();
            }
        }

        private string _comments;
        [JsonProperty]  // ✅ include in JSON
        public string Comments
        {
            get => _comments;
            set
            {
                _comments = value;
                OnPropertyChanged();
            }
        }


        // constructor
        public DoseLimitListViewModel()
        {
            DoseLimits = new ObservableCollection<DoseLimit>();

            //var cont1 = new Contour { Id = "PTV" };
            //var cont2 = new Contour { Id = "Bowel" };

            Contours = new ObservableCollection<Contour>();
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
            AddCommand = new RelayCommand(AddDoseLimit);
            RemoveCommand = new RelayCommand(RemoveDoseLimit, CanRemoveDoseLimit);
        }

        private void AddDoseLimit()
        {
            DoseLimits.Add(new DoseLimit {Id = "New"});
            SelectedDoseLimit = DoseLimits.Last();
        }

        private void RemoveDoseLimit()
        {
            if (SelectedDoseLimit != null)
            {
                DoseLimits.Remove(SelectedDoseLimit);
            }
        }

        private bool CanRemoveDoseLimit()
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
                    this.Comments = loaded.Comments;
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}