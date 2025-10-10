using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Media;
using VMS.TPS.Common.Model.Types;

using static esapi.esapi;
using VMSCourse = VMS.TPS.Common.Model.API.Course;
using VMSHospital = VMS.TPS.Common.Model.API.Hospital;
using VMSImage = VMS.TPS.Common.Model.API.Image;
using VMSPatient = VMS.TPS.Common.Model.API.Patient;
using VMSPlanSetup = VMS.TPS.Common.Model.API.PlanSetup;
using VMSReferencePoint = VMS.TPS.Common.Model.API.ReferencePoint;
using VMSRegistration = VMS.TPS.Common.Model.API.Registration;
using VMSSeries = VMS.TPS.Common.Model.API.Series;
using VMSStructure = VMS.TPS.Common.Model.API.Structure;
using VMSStructureSet = VMS.TPS.Common.Model.API.StructureSet;
using VMSStudy = VMS.TPS.Common.Model.API.Study;

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

                    OnPropertyChanged(nameof(LimitValid));
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
                if (_plan == value) return;
                
                Console.WriteLine($"DoseLimit: Setting a new plan... {_plan?.Id}");
                SetProperty<VMS.TPS.Common.Model.API.PlanningItem>(ref _plan, value);

                // if contour is not set, set one if found from the contour list
                if(_contour == null)
                {
                    if (_plan != null && _plan.StructureSet != null)
                    {
                        VMSStructure s_found = _plan.StructureSet.Structures.FirstOrDefault(s=> s.Id == Id);
                        if (s_found != null)
                        {
                            Contour = new Contour() { Id = s_found.Id };
                        }
                    }
                }

                if(_plan != null)
                    Evaluate();
                
            }
        }

        private void Evaluate()
        {
            Console.WriteLine("Evaluating dose limit...");
            if (_plan == null)
            {
                this.ErrorMessage = "Plan is set.";
                return;
            }

            if (_contour == null)
            {
                this.ErrorMessage = "Contour is not set";
                return;
            }

            VMS.TPS.Common.Model.API.Structure s = esapi.esapi.s_of_id(_contour.Id, _plan.StructureSet);
            if(s == null)
            {
                this.ErrorMessage = $"Structure not found - Id={_contour.Id}";
                return;
            }

            // is the limit string valid
            if (!LimitValid)
            {
                this.ErrorMessage = $"Limit string [{Limit}] is not valid";
                return;
            }

            // evaluate
            (double value, string unit, Result result) = DoseLimitEvaluator.Evaluate(this.Limit, _plan, s, _prescription);

            // result
            this.Value = new DoubleWithUnit() { Value = value, Unit = unit };
            this.Result = result.ToString();         
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
            set
            {
                if (_result == value) return;

                SetProperty(ref _result, value);
            }
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