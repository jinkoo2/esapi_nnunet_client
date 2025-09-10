using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace nnunet_client.models
{
    // The Prescription class must implement INotifyPropertyChanged
    // to allow a listener (like the UI or a debug method) to be
    // notified when a property's value changes.
    public class Contour : INotifyPropertyChanged
    {
        private string _id;

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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // debug
            Console.WriteLine("Prescription changed:" + propertyName);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"Id: {Id}";
        }

    }
}
