using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;
using System.Collections.ObjectModel;
using nnunet_client.models;

namespace nnunet_client.viewmodels
{
    public class DoseLimitListEditorControlViewModel
    {
        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }

        public PrescriptionListViewModel PrescriptionListViewModel { get; set; }
        public DoseLimitListViewModel DoseLimitListViewModel { get; set; }

        // constructor
        public DoseLimitListEditorControlViewModel() { 
            
            // prescription and contours
            ObservableCollection<Prescription> prescriptions = new ObservableCollection<Prescription>();
            ObservableCollection<Contour> contours = new ObservableCollection<Contour>();

            // prescription view model
            this.PrescriptionListViewModel = new PrescriptionListViewModel();
            this.PrescriptionListViewModel.Prescriptions = prescriptions;
            
            // doselimit list view model
            this.DoseLimitListViewModel = new DoseLimitListViewModel();
            this.DoseLimitListViewModel.Prescriptions = prescriptions;
            this.DoseLimitListViewModel.Contours = contours;
        }
        

    }
}
