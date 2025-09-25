using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Navigation;

namespace nnunet_client.models
{
    // The Prescription class must implement INotifyPropertyChanged
    // to allow a listener (like the UI or a debug method) to be
    // notified when a property's value changes.
    public class Prescription : BaseModel
    {
        private string _id;
        private double _totalDose;

        public string Id
        {
            get => _id;
            set => SetProperty<string>(ref _id, value);
        }

        public double TotalDose
        {
            get => _totalDose;
            set => SetProperty<double>(ref _totalDose, value);
        }

        public string Unit
        {
            get => "cGy";
        }

        public override string ToString() => $"ID: {Id}, TotalDose: {TotalDose} {Unit}";
       
        public Prescription Duplicate()=> new Prescription() {Id = this.Id, TotalDose = this.TotalDose };
        
    }
}
