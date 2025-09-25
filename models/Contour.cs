using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace nnunet_client.models
{
    // The Prescription class must implement INotifyPropertyChanged
    // to allow a listener (like the UI or a debug method) to be
    // notified when a property's value changes.
    public class Contour : BaseModel
    {
        private string _id;

        public string Id
        {
            get => _id;
            set => SetProperty<string>(ref _id, value);
        }

        public override string ToString()=> $"Id: {Id}";

    }
}
