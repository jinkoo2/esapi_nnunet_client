using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using nnunet_client.models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace nnunet_client.viewmodels
{
    public class DoseLimitListViewModel : INotifyPropertyChanged
    {
        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }

        private ObservableCollection<DoseLimit> _doseLimits;
        public ObservableCollection<DoseLimit> DoseLimits
        {
            get => _doseLimits;
            set
            {
                _doseLimits = value;
                OnPropertyChanged();
            }
        }


        private ObservableCollection<Contour> _contours;
        public ObservableCollection<Contour> Contours
        {
            get => _contours;
            set
            {
                _contours = value;
                OnPropertyChanged();
            }
        }


        private ObservableCollection<Prescription> _prescriptions;
        public ObservableCollection<Prescription> Prescriptions
        {
            get => _prescriptions;
            set
            {
                _prescriptions = value;
                OnPropertyChanged();
            }
        }

        private DoseLimit _selectedDoseLimit;
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

        public DoseLimitListViewModel()
        {
            DoseLimits = new ObservableCollection<DoseLimit>();

            var cont1 = new Contour { Id = "PTV" };
            var cont2 = new Contour { Id = "Bowel" };

            Contours = new ObservableCollection<Contour>();
            Contours.Add(cont1);
            Contours.Add(cont2);

            // Add some initial dummy data
            DoseLimits.Add(new DoseLimit { Id = "DL001", Contour = cont1, ContourType = DoseLimitContourType.Target });
            DoseLimits.Add(new DoseLimit { Id = "DL002", Contour = cont2, ContourType = DoseLimitContourType.OAR });

            Prescriptions = new ObservableCollection<Prescription>();
            Prescriptions.Add(new Prescription { Id = "Default", TotalDose = 3000 });

            LoadCommand = new RelayCommand(LoadDoseLimits);
            SaveCommand = new RelayCommand(SaveDoseLimits);
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

        private void LoadDoseLimits()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(openFileDialog.FileName);
                    var loadedDoseLimits = JsonConvert.DeserializeObject<ObservableCollection<DoseLimit>>(json);

                    DoseLimits.Clear();
                    if (loadedDoseLimits != null)
                    {
                        foreach (var d in loadedDoseLimits)
                        {
                            DoseLimits.Add(d);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading dose limits: {ex.Message}");
                }
            }
        }

        private void SaveDoseLimits()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            saveFileDialog.FileName = "doselimits";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(DoseLimits, Formatting.Indented);
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