using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static nnunet_client.ConstraintSet.Constraint;
using Newtonsoft.Json.Converters;
using nnunet_client.models;

namespace nnunet_client
{
    public static class EnumHelper
    {
        public static Array ContourTypeValues => Enum.GetValues(typeof(ContourType));
        public static Array ConstraintTypeValues => Enum.GetValues(typeof(ConstraintType));
    }


    [JsonConverter(typeof(StringEnumConverter))]
    public enum ContourType
    {
        Target, OAR
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ConstraintType
    {
        Volume, Dose, Min, Max, Mean
    }


    public class ConstraintSet
    {
        
        public string PlanContourId { get; set; }


        public class Constraint
        {
            public ConstraintType Type { get; set; }

            public string Limit { get; set; }

            public string Comment { get; set; }

            public bool Validate(out string errorMessage)
            {
                errorMessage = null;

                string[] patterns = Array.Empty<string>();

                switch (Type)
                {
                    case ConstraintType.Volume:
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

                    case ConstraintType.Dose:
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

                    case ConstraintType.Mean:
                        patterns = new[]
                        {
                @"^Mean[<>=]\d+(cGy|%)$",
                @"^Mean[<>=]\d+-\d+(cGy|%)$",
                @"^Mean@(%|cGy)$",
                @"^Mean<~\d+(cGy|%)$",
                @"^Mean<=~\d+(cGy|%)$"
            };
                        break;

                    case ConstraintType.Min:
                        patterns = new[]
                        {
                @"^Min[<>=]\d+(cGy|%)$",
                @"^Min[<>=]\d+-\d+(cGy|%)$",
                @"^Min@(%|cGy)$",
                @"^Min<~\d+(cGy|%)$",
                @"^Min<=~\d+(cGy|%)$"
            };
                        break;

                    case ConstraintType.Max:
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
                        errorMessage = $"Unknown constraint type: {Type}";
                        return false;
                }

                foreach (var pattern in patterns)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(Limit, pattern))
                        return true;
                }

                errorMessage = $"Invalid format for {Type} constraint: \"{Limit}\"";
                return false;
            }
        }

        public class ContourConstraint
        {
            public string Id { get; set; }
            public string PlanContourId { get; set; }
            public ContourType Type { get; set; }
            public Prescription Prescription { get; set; }

            public Constraint[] Constraints { get; set; }
        }


        [JsonProperty]
        public string Title { get; set; }


        [JsonProperty]
        public Prescription[] Prescriptions { get; set; }

        [JsonProperty]
        public string[] PlanContourIds { get; set; }


        [JsonProperty]
        public ContourConstraint[] ContourConstraints { get; set; }



        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (ContourConstraints == null)
                return true;

            foreach (var cc in ContourConstraints)
            {
                if (cc.Constraints == null)
                    continue;

                foreach (var constraint in cc.Constraints)
                {
                    if (!constraint.Validate(out string error))
                    {
                        errors.Add($"Constraint '{constraint.Limit}' in ContourConstraint '{cc.Id}' is invalid: {error}");
                    }
                }
            }

            return errors.Count == 0;
        }


    }


}