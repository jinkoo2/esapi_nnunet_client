using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;

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
        public Color Color { get; set; }

        [JsonProperty("color")]
        public string ColorString
        {
            get => Color.ToString(); // e.g., "#FF0000FF"
            set
            {
                if (!string.IsNullOrEmpty(value))
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
        }

        public bool HighResolution { get; set; }
        public string ModelId { get; set; }
        public string ModelLabel { get; set; }

        [JsonIgnore]
        public SolidColorBrush ColorBrush => new SolidColorBrush(Color);
    }
}
