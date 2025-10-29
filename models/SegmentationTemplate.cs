using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;

namespace nnunet_client.models
{

    public class SegmentationTemplate : BaseModel, INamedTemplate
    {
        // constructor
        public SegmentationTemplate() {


        }

        private string _name;
        private string _description;
        private ObservableCollection<ContourItem> _contourList = new ObservableCollection<ContourItem>();

        public string Name
        {
            get => _name;
            // Use SetProperty to set the backing field and raise PropertyChanged
            set => SetProperty(ref _name, value, nameof(Name));
        }

        public string Description
        {
            get => _description;
            // Use SetProperty to set the backing field and raise PropertyChanged
            set => SetProperty(ref _description, value, nameof(Description));
        }

        private ObservableCollection<string> _contourTypes = new ObservableCollection<string>() {
                "ORGAN",
                "PTV",
                "CTV",
                "GTV",
                "BODY",
                "None"
            };
        public ObservableCollection<string> ContourTypes
        {
            get => _contourTypes;
            set => SetProperty<ObservableCollection<string>>(ref _contourTypes, value);
        }

        public ObservableCollection<ContourItem> ContourList
        {
            get => _contourList;
            // Use SetProperty to set the backing field and raise PropertyChanged
            set => SetProperty(ref _contourList, value, nameof(ContourList));
        }


        public class ContourItem : BaseModel
        {
            private string _id;
            private string _type;
            private Color _color = Colors.Transparent;
            private bool _highResolution;
            private string _modelId;
            private string _modelLabelName;
            private int _modelLabelNumber;
            private string _status = "Unknown";

            public string Id
            {
                get => _id;
                set => SetProperty(ref _id, value, nameof(Id));
            }

            public string Type
            {
                get => _type;
                set => SetProperty(ref _type, value, nameof(Type));
            }

            [JsonIgnore]
            public Color Color
            {
                get => _color;
                set
                {
                    // Use SetProperty for the Color field
                    if (SetProperty(ref _color, value, nameof(Color)))
                    {
                        // Crucially, when Color changes, we must also notify that 
                        // the dependent properties ColorString and ColorBrush have changed.
                        OnPropertyChanged(nameof(ColorString));
                        OnPropertyChanged(nameof(ColorBrush));
                    }
                }
            }

            [JsonProperty("Color")]
            public string ColorString
            {
                get
                {
                    var converter = new ColorConverter();
                    return converter.ConvertToString(Color); // Reads the Color property (which reads _color)
                }

                set
                {
                    try
                    {
                        // Convert the incoming string value to a Color
                        var newColor = ColorConverter.ConvertFromString(value);
                        // Set the Color property, which correctly updates the backing field
                        // and raises the necessary PropertyChanged events (Color, ColorString, ColorBrush).
                        Color = (Color)newColor;
                    }
                    catch
                    {
                        Color = Colors.Transparent;
                    }
                }
            }


            public bool HighResolution
            {
                get => _highResolution;
                set => SetProperty(ref _highResolution, value, nameof(HighResolution));
            }

            public string ModelId
            {
                get => _modelId;
                set => SetProperty(ref _modelId, value, nameof(ModelId));
            }

            public string ModelLabelName
            {
                get => _modelLabelName;
                set => SetProperty(ref _modelLabelName, value, nameof(ModelLabelName));
            }

            public int ModelLabelNumber
            {
                get => _modelLabelNumber;
                set => SetProperty(ref _modelLabelNumber, value, nameof(ModelLabelNumber));
            }

            [JsonIgnore]
            public string Status
            {
                get => _status;
                set
                {
                    if (_status == value) return; 

                    SetProperty(ref _status, value, nameof(Status));
                
                    OnPropertyChanged(nameof(StatusCode));
                }
            }
            
            [JsonIgnore]
            public string StatusCode
            {
                get
                {
                    if (_status != null && _status.ToLower().Contains("error"))
                        return "ERROR";
                    else
                        return "";
                }
            }


            [JsonIgnore]
            // This is a computed property, it doesn't need a backing field or SetProperty.
            // It uses the Color property and relies on the Color setter to call OnPropertyChanged(nameof(ColorBrush)).
            public SolidColorBrush ColorBrush => new SolidColorBrush(Color);


        }
    }
}