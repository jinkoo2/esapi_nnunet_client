using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace nnunet_client.models
{
    // The Prescription class must implement INotifyPropertyChanged
    // to allow a listener (like the UI or a debug method) to be
    // notified when a property's value changes.
    public class Prescription : INotifyPropertyChanged
    {
        private string _id;
        private double _totalDose;

        // INotifyPropertyChanged requires this event to be defined.
        public event PropertyChangedEventHandler PropertyChanged;

        public string Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        public double TotalDose
        {
            get => _totalDose;
            set
            {
                if (_totalDose != value)
                {
                    _totalDose = value;
                    // This is where we notify that the property has changed.
                    OnPropertyChanged();
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // debug
            Console.WriteLine("Prescription changed:" + propertyName);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"ID: {Id}, TotalDose: {TotalDose}";
        }

    }
}
