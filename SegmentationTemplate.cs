using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;

namespace esapi
{

    public class SegmentationTemplate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ContourItem> ContourList { get; set; } = new List<ContourItem>();

        public class ContourItem
        {
            public string Id { get; set; }
            public string Type { get; set; }

            [JsonIgnore]
            public Color Color { get; set; } = Colors.Transparent;

            [JsonProperty("Color")]
            public string ColorString
            {
                get
                {
                    var converter = new ColorConverter();
                    return converter.ConvertToString(Color);
                }

                set
                {
                    try
                    {
                        Color = (Color)ColorConverter.ConvertFromString(value);
                    }
                    catch
                    {
                        Color = Colors.Transparent;
                    }
                }
            }

            public bool HighResolution { get; set; }
            public string ModelId { get; set; }
            public string ModelLabelName { get; set; }

            public int ModelLabelNumber { get; set; }


            public string Status { get; set; } = "Unknown";

            [JsonIgnore]
            public SolidColorBrush ColorBrush => new SolidColorBrush(Color);


        }
    }


}