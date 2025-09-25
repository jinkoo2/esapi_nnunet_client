using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.Types;

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

    [JsonObject(MemberSerialization.OptIn)] // only include explicitly marked properties
    public class DoseLimit : BaseModel
    {

        private string _id;
        [JsonProperty]
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private Contour _contour;
        [JsonProperty]
        public Contour Contour
        {
            get => _contour;
            set {
                if (_contour != value)
                {
                    SetProperty(ref _contour, value);
                    Evaluate();
                }
            }
        }

        /// <summary>
        ///  hiding for now, not sure where it is being used....
        /// </summary>
        //private DoseLimitContourType _contourType;
        //public DoseLimitContourType ContourType
        //{
        //    get => _contourType;
        //    set => SetProperty(ref _contourType, value);
        //}

        private Prescription _prescription;
        [JsonProperty]
        public Prescription Prescription
        {
            get => _prescription;
            set
            {
                if (_prescription != value)
                {
                    SetProperty(ref _prescription, value);
                    Evaluate();
                }
            }
        }

        private string _limit;
        [JsonProperty]
        public string Limit
        {
            get => _limit;
            set
            {
                if (_limit != value)
                {
                    SetProperty(ref _limit, value?.Trim());

                    Evaluate();
                }
            }
        }

        [JsonIgnore]  // not include in JSON
        public string LimitType
        {
            get
            {
                if (_limit == null || _limit.Trim() == "")
                    return "None";
                else if (_limit.StartsWith("Min"))
                    return "Min";
                else if (_limit.StartsWith("Max"))
                    return "Max";
                else if (_limit.StartsWith("Mean"))
                    return "Mean";
                else if (_limit.StartsWith("V"))
                    return "Volume";
                else if (_limit.StartsWith("D"))
                    return "Dose";
                else return "None";
            }
        }

        private string _errorMessage;
        [JsonIgnore]  // not include in JSON
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value?.Trim());
        }

        [JsonIgnore]  // not include in JSON
        public IEnumerable<string> LimitValueStrings
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Limit))
                {
                    return Enumerable.Empty<string>();
                }

                // This regular expression finds any number (integer or decimal)
                // that is immediately followed by one or more letters or a percent sign.
                // (\d+(\.\d+)?): Matches the number part.
                // ([a-zA-Z%]+): Matches the unit part.
                return Regex.Matches(Limit, @"\d+(\.\d+)?[a-zA-Z%]+")
                            .Cast<Match>()
                            .Select(m => m.Value);
            }
        }

        [JsonIgnore]  // not include in JSON
        public bool LimitValid {
            get {
                ErrorMessage = "";

                string[] patterns = Array.Empty<string>();

                switch (LimitType)
                {
                    case "Volume":
                        patterns = new[]
                        {
                @"^V\d+(cGy|%)<\d+(cc|%)?$",
                @"^V\d+(cGy|%)<\d+-\d+(cc|%)?$",
                @"^V\d+(cGy|%)=\d+(cc|%)?$",
                @"^V\d+(cGy|%)=\d+-\d+(cc|%)?$",
                @"^V\d+(cGy|%)>\d+(cc|%)?$",
                @"^V\d+(cGy|%)>\d+-\d+(cc|%)?$",
                @"^V\d+(cGy|%)@(%|cc)$",
                @"^V\d+(cGy|%)<~\d+(cc|%)?$",
                @"^V\d+(cGy|%)<=~\d+(cc|%)?$"
            };
                        break;

                    case "Dose":
                        patterns = new[]
                        {
                @"^D\d+(\.\d+)?(cc|%)<\d+(cGy|%)$",
                @"^D\d+(\.\d+)?(cc|%)>\d+(cGy|%)$",
                @"^D\d+(\.\d+)?(cc|%)=\d+(cGy|%)$",
                @"^D\d+(\.\d+)?(cc|%)<\d+-\d+(cGy|%)$",
                @"^D\d+(\.\d+)?(cc|%)>\d+-\d+(cGy|%)$",
                @"^D\d+(\.\d+)?(cc|%)=\d+-\d+(cGy|%)$",
                @"^D\d+(\.\d+)?(cc|%)@(%|cGy)$",
                @"^D\d+(\.\d+)?(cc|%)<~\d+(cGy|%)$",
                @"^D\d+(\.\d+)?(cc|%)<=~\d+(cGy|%)$"
            };
                        break;

                    case "Mean":
                        patterns = new[]
                        {
                @"^Mean[<>=]\d+(cGy|%)$",
                @"^Mean[<>=]\d+-\d+(cGy|%)$",
                @"^Mean@(%|cGy)$",
                @"^Mean<~\d+(cGy|%)$",
                @"^Mean<=~\d+(cGy|%)$"
            };
                        break;

                    case "Min":
                        patterns = new[]
                        {
                @"^Min[<>=]\d+(cGy|%)$",
                @"^Min[<>=]\d+-\d+(cGy|%)$",
                @"^Min@(%|cGy)$",
                @"^Min<~\d+(cGy|%)$",
                @"^Min<=~\d+(cGy|%)$"
            };
                        break;

                    case "Max":
                        patterns = new[]
                        {
                @"^Max[<>=]\d+(cGy|%)$",
                @"^Max[<>=]\d+-\d+(cGy|%)$",
                @"^Max@(%|cGy)$",
                @"^Max<~\d+(cGy|%)$",
                @"^Max<=~\d+(cGy|%)$"
            };
                        break;

                    default:
                        ErrorMessage = $"Unknown dose limit type: {LimitType}";
                        return false;
                }

                foreach (var pattern in patterns)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(Limit, pattern))
                        return true;
                }

                ErrorMessage = $"Invalid format for {LimitType} dose limit type: \"{Limit}\"";
                return false;
            }
        }

        private VMS.TPS.Common.Model.API.PlanningItem _plan;
        [JsonIgnore]  // not include in JSON
        public VMS.TPS.Common.Model.API.PlanningItem Plan
        {
            get { return _plan; }
            set
            {
                if (_plan != value)
                {
                    Console.WriteLine($"DoseLimit: Setting a new plan... {_plan?.Id}");

                    SetProperty<VMS.TPS.Common.Model.API.PlanningItem>(ref _plan, value);

                    Evaluate();
                }
            }
        }

        private void Evaluate()
        {
            Console.WriteLine("Evaluating dose limt...");
            if (_plan == null)
            {
                Console.WriteLine("Plan is null");
                return;
            }

            if (_contour == null)
            {
                Console.WriteLine("Contour is null");
                return;
            }

            VMS.TPS.Common.Model.API.Structure s = esapi.esapi.s_of_id(_contour.Id, _plan.StructureSet);
            if(s == null)
            {
                Console.WriteLine($"Structure not found for Contour.Id={_contour.Id}");
                return;
            }

            if (!LimitValid)
            {
                Console.WriteLine($"Limit string [{Limit}] is not valid");
                return;
            }

            // evaluate
            (double value, string unit, Result result) = DoseLimitEvaluator.Evaluate(this.Limit, _plan, s, _prescription);

            // result
            this.Value = new DoubleWithUnit() { Value = value, Unit = unit };
            this.Result = result.ToString();


            //Console.WriteLine($"LimitType={LimitType}");
            //switch (LimitType)
            //{
            //    case "Volume":
            //        EvaluteVolumeLimitType();
            //        break;
            //    case "Dose":
            //        EvaluteDoseLimitType();
            //        break;
            //    default:
            //        Console.WriteLine($"LimitType [{LimitType}] not handled.");
            //        break;
            //}
            
        }

        private void EvaluteVolumeLimitType()
        {
            if (this.LimitValueStrings == null)
            {
                Console.WriteLine("Internal error: LimitValueStrings is null!");
                return;
            }

            string[] valueStrings = this.LimitValueStrings.ToArray();
            if ((valueStrings.Length != 2))
            {
                Console.WriteLine($"Internal error: LimitValueStrings must have 2 elements, but it has {valueStrings.Length} elements!");
                return;
            }

            string doseString = valueStrings[0];
            string volumeLimitString = valueStrings[1];
            Console.WriteLine($"doseString={doseString}");
            Console.WriteLine($"volumeStirng={volumeLimitString}");

            DoubleWithUnit dwuDose = new DoubleWithUnit(doseString);
            DoubleWithUnit dwuVolumeLimit = new DoubleWithUnit(volumeLimitString);

            Console.WriteLine($"dwuDose={dwuDose}");
            Console.WriteLine($"dwuVolumeLimit={dwuVolumeLimit}");


            // if doseString in %, convert it to an absolute dose using the given prescription (cGy)
            if (dwuDose.Unit == "%")
            {
                if (this._prescription == null)
                {
                    Console.WriteLine("Dose is in relative value, but the reference Prescription is not set");
                    return;
                }

                // the underlying dose unit is cGy
                double rx = this._prescription.TotalDose;
                Console.WriteLine($"Rx={rx}");

                // dose in cGy
                double dose_cgy = rx * dwuDose.Value / 100.0;

                dwuDose = new DoubleWithUnit { Value = dose_cgy, Unit = "cGy" };
            }

            // find structure
            VMS.TPS.Common.Model.API.Structure s = esapi.esapi.s_of_id(_contour.Id, _plan.StructureSet, false);
            if (s == null)
            {
                Console.WriteLine($"Plan structure not found with Id=[{_contour.Id}]");
                return;
            }
            Console.WriteLine("Structure found.");

            // set value
            if (dwuVolumeLimit.Unit == "%")
            {
                // get volume value in percent
                double volume_percent = _plan.GetVolumeAtDose(s, esapi.esapi.s2D(dwuDose.Display), VMS.TPS.Common.Model.Types.VolumePresentation.Relative);
                this.Value = new DoubleWithUnit($"{volume_percent}%");

                Console.WriteLine($"Value = {this.Value}");

                if (_limit.Contains('>'))
                {
                    Console.WriteLine($">");

                    if (volume_percent > dwuVolumeLimit.Value)
                    {
                        this.Result = "Pass";
                    }
                    else
                    {
                        this.Result = "Fail";
                    }
                }
                else if (_limit.Contains('<'))
                {
                    Console.WriteLine($"<");

                    if (volume_percent < dwuVolumeLimit.Value)
                    {
                        this.Result = "Pass";
                    }
                    else
                    {
                        this.Result = "Fail";
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid operator. Only supports < or > for now.");
                }
                Console.WriteLine($"Result = {this.Result}");

            }
            else
            {
                // get volume value in cc
                double volume_cc = _plan.GetVolumeAtDose(s, esapi.esapi.s2D(dwuDose.Display), VMS.TPS.Common.Model.Types.VolumePresentation.AbsoluteCm3);
                this.Value = new DoubleWithUnit($"{volume_cc}cc");

                Console.WriteLine($"Value = {this.Value}");

                if (_limit.Contains('>'))
                {
                    Console.WriteLine($">");

                    if (volume_cc > dwuVolumeLimit.Value)
                    {
                        this.Result = "Pass";
                    }
                    else
                    {
                        this.Result = "Fail";
                    }
                }
                else if (_limit.Contains('<'))
                {
                    Console.WriteLine($"<");

                    if (volume_cc < dwuVolumeLimit.Value)
                    {
                        this.Result = "Pass";
                    }
                    else
                    {
                        this.Result = "Fail";
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid operator. Only supports < or > for now.");
                }

                Console.WriteLine($"Result = {this.Result}");
            }
        }


        private void EvaluteDoseLimitType()
        {
            if (this.LimitValueStrings == null)
            {
                ErrorMessage = "Limit is empty";
                return;
            }

            string[] valueStrings = this.LimitValueStrings.ToArray();
            if ((valueStrings.Length != 2))
            {
                ErrorMessage = $"Invalid Limit format - it must have 2 number elements, but it has {valueStrings.Length}!";
                return;
            }

            string volumeString = valueStrings[0];
            string doseLimitString = valueStrings[1];
            Console.WriteLine($"volumeString={volumeString}");
            Console.WriteLine($"doseLimitString={doseLimitString}");

            DoubleWithUnit dwuVolume = new DoubleWithUnit(volumeString);
            DoubleWithUnit dwuDoseLimit = new DoubleWithUnit(doseLimitString);

            Console.WriteLine($"dwuVolume={dwuVolume}");
            Console.WriteLine($"dwuDoseLimit={dwuDoseLimit}");

            // find structure
            VMS.TPS.Common.Model.API.Structure s = esapi.esapi.s_of_id(_contour.Id, _plan.StructureSet, false);
            if (s == null)
            {
                Console.WriteLine($"Plan structure not found with Id=[{_contour.Id}]");
                return;
            }

            DoseValue dose_absolute;
            if (dwuVolume.Unit == "%")
            {
                dose_absolute = _plan.GetDoseAtVolume(
                       s,
                       dwuVolume.Value,
                       VolumePresentation.Relative,
                       DoseValuePresentation.Absolute);
            }
            else // volume in cc
            {
                dose_absolute = _plan.GetDoseAtVolume(
                       s,
                       dwuVolume.Value,
                       VolumePresentation.AbsoluteCm3,
                       DoseValuePresentation.Absolute);
            }

            // if target dose is in %, convert it using the selected prescription
            if (dwuDoseLimit.Unit == "%")
            {
                if (this._prescription == null)
                {
                    Console.WriteLine("Dose is in relative value, but the reference Prescription is not set");
                    return;
                }

                // the underlying dose unit is cGy
                double rx = this._prescription.TotalDose;
                Console.WriteLine($"Rx={rx}");

                // dose in cGy
                double dose_percent = rx * dose_absolute.Dose / 100.0;

                this.Value = new DoubleWithUnit($"{dose_percent}%");
            }
            else
            {
                this.Value = new DoubleWithUnit($"{dose_absolute.Dose} {dose_absolute.UnitAsString}"); 
            }

            // set result
            Console.WriteLine($"this.Value={this.Value}");
            DoubleWithUnit dwuDose = this.Value;
            Console.WriteLine($"dwuDose={dwuDose}");

            if (_limit.Contains('>'))
                {
                    Console.WriteLine($">");

                    if (dwuDose.Value > dwuDoseLimit.Value)
                    {
                        this.Result = "Pass";
                    }
                    else
                    {
                        this.Result = "Fail";
                    }
                }
                else if (_limit.Contains('<'))
                {
                    Console.WriteLine($"<");

                    if (dwuDose.Value < dwuDoseLimit.Value)
                    {
                        this.Result = "Pass";
                    }
                    else
                    {
                        this.Result = "Fail";
                    }
                }
                else
                {
                    ErrorMessage = $"Invalid operator. Only supports < or > for now.";
                }

                Console.WriteLine($"Result = {this.Result}");
            
        }

        // evaluated value
        private DoubleWithUnit _value;
        [JsonProperty]
        public DoubleWithUnit Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        // evaluated value
        private string _result;
        [JsonProperty]
        public string Result
        {
            get => _result;
            set => SetProperty(ref _result, value);
        }


        private string _comments;
        [JsonProperty]
        public string Comments
        {
            get => _comments;
            set => SetProperty(ref _comments, value);
        }

        public DoseLimit Duplicate() => new DoseLimit() 
        { 
            Id = this.Id, 
            Limit = this.Limit,
            Plan = this.Plan,
        };


        public override string ToString()
        {
            return $"{Id}";
        }
    }
}