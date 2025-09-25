using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace nnunet_client.models
{
    public class DoubleWithUnit : BaseModel
    {
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;

        private int _subDecimalPoint  = 1;
        public int SubDecimalPoint
        {
            get => _subDecimalPoint;
            set
            {
                if (_subDecimalPoint == value) return;

                SetProperty<int>(ref _subDecimalPoint, value);

                OnPropertyChanged(nameof(Display));
            }
        }

        public string Display => Value.ToString($"F{SubDecimalPoint}", CultureInfo.InvariantCulture) + Unit;

        public DoubleWithUnit() { }

        public DoubleWithUnit(string stringValue)
        {
            (Value, Unit) = ExtractNumberAndUnit(stringValue);
        }

        public static (double Number, string Unit) ExtractNumberAndUnit(string input)
        {
            Console.WriteLine($"ExtractNumberAndUnit({input})");

            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input cannot be null or empty.", nameof(input));

            // Match number (integer or decimal) followed by letters or %
            var match = Regex.Match(input, @"^(?<num>\d+(\.\d+)?)\s*(?<unit>[a-zA-Z%]+)?$");
            if (!match.Success)
            {
                Console.WriteLine($"Invalid format: '{input}'");
                throw new FormatException($"Invalid format: '{input}'");
            }

            double number = double.Parse(match.Groups["num"].Value, CultureInfo.InvariantCulture);
            Console.WriteLine($"number: '{number}'");
            
            string unit = match.Groups["unit"].Success ? match.Groups["unit"].Value : string.Empty;
            Console.WriteLine($"unit: '{unit}'");

            return (number, unit);
        }

        public override string ToString() => Display;

        // Optional: convenience method for creating directly
        public static DoubleWithUnit Parse(string input) => new DoubleWithUnit(input);
    }
}
