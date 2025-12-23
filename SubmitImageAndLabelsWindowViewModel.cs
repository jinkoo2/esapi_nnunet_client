using nnunet_client.viewmodels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
using System.Windows;
using VMS.TPS.Common.Model.API;

namespace nnunet_client
{
    public class SubmitImageAndLabelsWindowViewModel : nnunet_client.viewmodels.BaseViewModel
    {
        public SubmitImageAndLabelsWindowViewModel()
        {
            _submitImageAndLabelsViewModel = new SubmitImageAndLabelsViewModel();
            _submitImageAndLabelsViewModel.PropertyChanged += SubmitImageAndLabelsViewModel_PropertyChanged;
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
                    helper.log($"New patient selected (Id={_patient.Id}). Populating structure sets...");
                    UpdateStructureSets();
                }
                else
                {
                    helper.log($"Clearing StructureSets...");
                    StructureSets = new ObservableCollection<VMSStructureSet>();
                    SelectedStructureSet = null;
                }
            }
        }

        private ObservableCollection<VMSStructureSet> _structureSets = new ObservableCollection<VMSStructureSet>();
        public ObservableCollection<VMSStructureSet> StructureSets
        {
            get { return _structureSets; }
            set
            {
                if (_structureSets == value) return;
                SetProperty(ref _structureSets, value, nameof(StructureSets));
            }
        }

        private VMSStructureSet _selectedStructureSet;
        public VMSStructureSet SelectedStructureSet
        {
            get { return _selectedStructureSet; }
            set
            {
                if (_selectedStructureSet == value) return;

                SetProperty(ref _selectedStructureSet, value, nameof(SelectedStructureSet));

                helper.log($"Structure set selected: {_selectedStructureSet?.Id}");

                // Pass the selected structure set to the SubmitImageAndLabelsViewModel
                if (_submitImageAndLabelsViewModel != null)
                {
                    _submitImageAndLabelsViewModel.StructureSet = _selectedStructureSet;
                }
            }
        }

        private void UpdateStructureSets()
        {
            if (_patient == null)
            {
                StructureSets = new ObservableCollection<VMSStructureSet>();
                return;
            }

            try
            {
                // Get all structure sets from all studies
                var allStructureSets = _patient.StructureSets.ToList();

                StructureSets = new ObservableCollection<VMSStructureSet>(allStructureSets);
                helper.log($"Found {StructureSets.Count} structure sets");

                // Select the first one if available
                if (StructureSets.Count > 0)
                {
                    SelectedStructureSet = StructureSets[0];
                }
            }
            catch (Exception ex)
            {
                helper.log($"Error updating structure sets: {ex.Message}");
                MessageBox.Show($"Error loading structure sets:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private viewmodels.SubmitImageAndLabelsViewModel _submitImageAndLabelsViewModel;
        public viewmodels.SubmitImageAndLabelsViewModel SubmitImageAndLabelsViewModel
        {
            get { return _submitImageAndLabelsViewModel; }
            set
            {
                if (_submitImageAndLabelsViewModel == value) return;

                if (_submitImageAndLabelsViewModel != null)
                {
                    _submitImageAndLabelsViewModel.PropertyChanged -= SubmitImageAndLabelsViewModel_PropertyChanged;
                }

                SetProperty(ref _submitImageAndLabelsViewModel, value, nameof(SubmitImageAndLabelsViewModel));

                if (_submitImageAndLabelsViewModel != null)
                {
                    _submitImageAndLabelsViewModel.PropertyChanged += SubmitImageAndLabelsViewModel_PropertyChanged;
                    // Update structure set when view model changes
                    _submitImageAndLabelsViewModel.StructureSet = _selectedStructureSet;
                }
            }
        }

        private void SubmitImageAndLabelsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Handle property changes from SubmitImageAndLabelsViewModel if needed
        }
    }
}

