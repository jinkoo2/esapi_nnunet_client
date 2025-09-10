using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;

namespace nnunet_client.viewmodels
{
    using Newtonsoft.Json;
    using nnunet_client.models;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;
    using System.Windows.Shapes;

    [JsonObject(MemberSerialization.OptIn)] // only include explicitly marked properties
    public class PrescriptionListViewModel : INotifyPropertyChanged
    {

        // Commands that the UI buttons will bind to.
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

        private Prescription _selectedPrescription;
        [JsonIgnore]  // not include in JSON
        public Prescription SelectedPrescription
        {
            get => _selectedPrescription;
            set
            {
                _selectedPrescription = value;
                OnPropertyChanged();
                //OnPropertyChanged(nameof(SelectedPrescriptionTotalDose));

                //Notify the RemoveCommand that it should re-evaluate its state
                ((RelayCommand)RemoveCommand).RaiseCanExecuteChanged();
            }
        }

        // A computed property to show the TotalDose of the selected prescription
        public double? SelectedPrescriptionTotalDose => _selectedPrescription?.TotalDose;

        public PrescriptionListViewModel()
        {
            Prescriptions = new ObservableCollection<Prescription>();
            //Prescriptions.Add(new Prescription { Id = "Default", TotalDose = 3000 });

            // Initialize commands, linking them to the logic methods.
            LoadCommand = new RelayCommand(Load);
            SaveCommand = new RelayCommand(Save);

            // 🎯 Initialize the new commands
            AddCommand = new RelayCommand(AddPrescription);
            RemoveCommand = new RelayCommand(RemovePrescription, CanRemovePrescription);
        }

        private void AddPrescription()
        {
            // Add a new, empty prescription to the list
            Prescriptions.Add(new Prescription { Id = "New", TotalDose = 0.0 });
            // You can select the new item for immediate editing
            SelectedPrescription = Prescriptions.Last();
        }

        private void RemovePrescription()
        {
            if (SelectedPrescription != null)
            {
                Prescriptions.Remove(SelectedPrescription);
            }
        }

        private bool CanRemovePrescription()
        {
            // The remove button is only enabled when an item is selected
            return SelectedPrescription != null;
        }



        private void Load()
        {
            
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

                


            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                   

                    string json = File.ReadAllText(openFileDialog.FileName);
                    PrescriptionListViewModel data = JsonConvert.DeserializeObject<PrescriptionListViewModel>(json);

                    // Clear the existing list and add the loaded items.
                    Prescriptions.Clear();
                    if (data.Prescriptions != null)
                    {
                        foreach (var p in data.Prescriptions)
                        {
                            Prescriptions.Add(p);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle potential errors during file reading or deserialization.
                    Console.WriteLine($"Error loading prescriptions: {ex.Message}");
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
