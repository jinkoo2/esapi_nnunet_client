using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace nnunet_client.models
{

    [JsonConverter(typeof(StringEnumConverter))]
    public enum DoseLimitContourType
    {
        Target,
        OAR
    }

    public static class EnumHelper
    {
        public static Array DoseLimitContourTypes => Enum.GetValues(typeof(DoseLimitContourType));
    }

    public class DoseLimit : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;



        private string _id;
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private Contour _contour;
        public Contour Contour
        {
            get => _contour;
            set => SetProperty(ref _contour, value);
        }

        private DoseLimitContourType _contourType;
        public DoseLimitContourType ContourType
        {
            get => _contourType;
            set => SetProperty(ref _contourType, value);
        }

        private Prescription _prescription;
        public Prescription Prescription
        {
            get => _prescription;
            set => SetProperty(ref _prescription, value);
        }

        private string _limit;
        public string Limit
        {
            get => _limit;
            set => SetProperty(ref _limit, value);
        }

        private string _comments;
        public string Comments
        {
            get => _comments;
            set => SetProperty(ref _comments, value);
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}