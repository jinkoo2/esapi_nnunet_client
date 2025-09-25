using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Navigation;

namespace nnunet_client.models
{
    // The Prescription class must implement INotifyPropertyChanged
    // to allow a listener (like the UI or a debug method) to be
    // notified when a property's value changes.
    public class Person : BaseModel
    {

        private string _id;
        public string Id
        {
            get => _id;
            set
            {
                SetProperty<string>(ref _id, value);
            }
        }

        private double _totalDose;
        public double TotalDose
        {
            get => _totalDose;
            set
            {
                SetProperty<double>(ref _totalDose, value);
            }
        }

        public override string ToString()
        {
            return $"ID: {Id}, TotalDose: {TotalDose}";
        }

    }
}
