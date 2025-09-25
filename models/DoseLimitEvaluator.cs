using System;
using System.Globalization;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.Types;

namespace nnunet_client.models
{
    public enum Result
    {
        Pass,
        Fail,
        Acceptable
    }

    public static class DoseLimitEvaluator
    {
        /// <summary>
        /// Parses a string to extract volume and dose parameters. This method is designed for
        /// V-type dose limits, which specify a dose value first and then a volume value.
        /// e.g., "V100%<90cc"
        /// </summary>
        /// <param name="input">The input string to parse.</param>
        /// <returns>A tuple containing the parsed parts.</returns>
        /// <exception cref="Exception">Thrown if the input string does not match the required format.</exception>
        public static (double? DNumber, string DUnit, string Operator, double? VNumber1, double? VNumber2, string VUnit) VolumeMatch(string input)
        {
            var pattern = @"^V(?<dnum>\d+(\.\d+)?)(?<dunit>%|cGy|Gy)(?<op>[<>=])(?<vnum1>\d+(\.\d+)?)(?:-(?<vnum2>\d+(\.\d+)?))?(?<vunit>%|cc)$";
            var regex = new Regex(pattern);
            var match = regex.Match(input);

            if (!match.Success)
            {
                throw new Exception($"The input string '{input}' does not match the required format for a V-type dose limit (e.g., V100%<90cc).");
            }

            double? dNumber = null;
            double? vNumber1 = null;
            double? vNumber2 = null;

            double.TryParse(match.Groups["dnum"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedDNum);
            dNumber = parsedDNum;

            double.TryParse(match.Groups["vnum1"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedVNum1);
            vNumber1 = parsedVNum1;

            if (!string.IsNullOrEmpty(match.Groups["vnum2"].Value))
            {
                double.TryParse(match.Groups["vnum2"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedVNum2);
                vNumber2 = parsedVNum2;
            }

            string dUnit = match.Groups["dunit"].Value;
            string op = match.Groups["op"].Value;
            string vUnit = match.Groups["vunit"].Value;

            return (dNumber, dUnit, op, vNumber1, vNumber2, vUnit);
        }

        /// <summary>
        /// Parses a string to extract volume and dose parameters. This method is designed for
        /// D-type dose limits, which specify a volume value first and then a dose value.
        /// e.g., "D20cc<100Gy"
        /// </summary>
        /// <param name="input">The input string to parse.</param>
        /// <returns>A tuple containing the parsed parts.</returns>
        /// <exception cref="Exception">Thrown if the input string does not match the required format.</exception>
        public static (double? VNumber, string VUnit, string Operator, double? DNumber1, double? DNumber2, string DUnit) DoseMatch(string input)
        {
            var pattern = @"^D(?<vnum>\d+(\.\d+)?)(?<vunit>%|cc)(?<op>[<>=])(?<dnum1>\d+(\.\d+)?)(?:-(?<dnum2>\d+(\.\d+)?))?(?<dunit>%|cGy|Gy)$";
            var regex = new Regex(pattern);
            var match = regex.Match(input);

            if (!match.Success)
            {
                throw new Exception($"The input string '{input}' does not match the required format for a D-type dose limit (e.g., D20cc<100Gy).");
            }

            double? vNumber = null;
            double? dNumber1 = null;
            double? dNumber2 = null;

            double.TryParse(match.Groups["vnum"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedVNum);
            vNumber = parsedVNum;

            double.TryParse(match.Groups["dnum1"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedDNum1);
            dNumber1 = parsedDNum1;

            if (!string.IsNullOrEmpty(match.Groups["dnum2"].Value))
            {
                double.TryParse(match.Groups["dnum2"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedDNum2);
                dNumber2 = parsedDNum2;
            }

            string vUnit = match.Groups["vunit"].Value;
            string op = match.Groups["op"].Value;
            string dUnit = match.Groups["dunit"].Value;

            return (vNumber, vUnit, op, dNumber1, dNumber2, dUnit);
        }

        /// <summary>
        /// Parses a string to extract statistical dose parameters (e.g., Min, Max, Mean).
        /// </summary>
        /// <param name="input">The input string to parse.</param>
        /// <returns>A tuple containing the parsed parts.</returns>
        /// <exception cref="Exception">Thrown if the input string does not match the required format.</exception>
        public static (string StatType, string Operator, double? DNumber1, double? DNumber2, string DUnit) MinMaxMeanDoseMatch(string input)
        {
            var pattern = @"^(?<stat>Min|Max|Mean)(?<op>[<>=])(?<dnum1>\d+(\.\d+)?)(?:-(?<dnum2>\d+(\.\d+)?))?(?<dunit>%|cGy|Gy)$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = regex.Match(input);

            if (!match.Success)
            {
                throw new Exception($"The input string '{input}' does not match the required format for a Min, Max, or Mean dose limit (e.g., Max>100Gy).");
            }

            double? dNumber1 = null;
            double? dNumber2 = null;

            double.TryParse(match.Groups["dnum1"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedDNum1);
            dNumber1 = parsedDNum1;

            if (!string.IsNullOrEmpty(match.Groups["dnum2"].Value))
            {
                double.TryParse(match.Groups["dnum2"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedDNum2);
                dNumber2 = parsedDNum2;
            }

            string statType = match.Groups["stat"].Value;
            string op = match.Groups["op"].Value;
            string dUnit = match.Groups["dunit"].Value;

            return (statType, op, dNumber1, dNumber2, dUnit);
        }

        /// <summary>
        /// A helper method to test if a given value passes, fails, or is acceptable based on a specified operator and thresholds.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <param name="op">The operator to use ('<', '>', '=').</param>
        /// <param name="th1">The primary threshold.</param>
        /// <param name="th2">The secondary, optional threshold for acceptable results.</param>
        /// <returns>A Result enum value (Pass, Fail, or Acceptable).</returns>
        /// <exception cref="Exception">Thrown if an invalid operator is provided.</exception>
        public static Result Test(double value, string op, double? th1, double? th2)
        {
            if (th2 == null)
            {
                if (op == ">") return value > th1 ? Result.Pass : Result.Fail;
                if (op == "<") return value < th1 ? Result.Pass : Result.Fail;
                if (op == "=") return value == th1 ? Result.Pass : Result.Fail;
                throw new Exception($"Invalid operator:{op}");
            }
            else
            {
                if (op == ">")
                {
                    if (value > th2) return Result.Pass;
                    if (value > th1) return Result.Acceptable;
                    return Result.Fail;
                }
                if (op == "<")
                {
                    if (value < th1) return Result.Pass;
                    if (value < th2) return Result.Acceptable;
                    return Result.Fail;
                }
                if (op == "=")
                {
                    if (value > th1 && value < th2) return Result.Pass;
                    return Result.Fail;
                }
                throw new Exception($"Invalid operator:{op}");
            }
        }

        /// <summary>
        /// Calculates the absolute dose from a given percentage and prescription.
        /// </summary>
        /// <param name="dose_percent">The dose in percent.</param>
        /// <param name="Prescription">The prescription object containing total dose and unit.</param>
        /// <returns>A DoseValue object with the calculated absolute dose.</returns>
        private static DoseValue CalculateAbsoluteDose(double dose_percent, models.Prescription Prescription)
        {
            // convert to absolute dose using prescription
            double dose_absolute = (dose_percent / 100.0) * Prescription.TotalDose;

            if (Prescription.Unit == "cGy")
                return new DoseValue(dose_absolute, DoseValue.DoseUnit.cGy);
            else
                return new DoseValue(dose_absolute, DoseValue.DoseUnit.Gy);
        }

        public static (double value, string unit, Result result) Evaluate(
            string limitString,
            VMS.TPS.Common.Model.API.PlanningItem Plan,
            VMS.TPS.Common.Model.API.Structure Structure,
            models.Prescription Prescription)
        {
            if (Plan == null) throw new Exception("Plan is null");
            if (Structure == null) throw new Exception("Structure is null");
            if (limitString == null) throw new Exception("limitString is null");
            if (limitString.Trim().Length == 0) throw new Exception("limitString is empty");

            if (limitString.StartsWith("Min") || limitString.StartsWith("Max") || limitString.StartsWith("Mean"))
            {
                (string StatType, string Operator, double? DNumber1, double? DNumber2, string DUnit) = MinMaxMeanDoseMatch(limitString);

                DoseValue dv;

                if (StatType == "Min")
                {
                    dv = Plan.GetDVHCumulativeData(
                        Structure,
                        VMS.TPS.Common.Model.Types.DoseValuePresentation.Absolute,
                        VMS.TPS.Common.Model.Types.VolumePresentation.AbsoluteCm3,
                        0.1).MinDose;
                }
                else if (StatType == "Max")
                {
                    dv = Plan.GetDVHCumulativeData(
                            Structure,
                            VMS.TPS.Common.Model.Types.DoseValuePresentation.Absolute,
                            VMS.TPS.Common.Model.Types.VolumePresentation.AbsoluteCm3,
                            0.1).MaxDose;
                }
                else if (StatType == "Mean")
                {
                    dv = Plan.GetDVHCumulativeData(
                            Structure,
                            VMS.TPS.Common.Model.Types.DoseValuePresentation.Absolute,
                            VMS.TPS.Common.Model.Types.VolumePresentation.AbsoluteCm3,
                            0.1).MeanDose;
                }
                else
                {
                    throw new Exception($"Invalid StatType: {StatType}");
                }

                if (DUnit == "%")
                {
                    double dose_percent = dv.Dose / Prescription.TotalDose * 100.0;
                    Result result = Test(dose_percent, Operator, DNumber1, DNumber2);
                    return (dose_percent, "%", result);
                }
                else
                {
                    double dose_absolute = dv.Dose;
                    Result result = Test(dose_absolute, Operator, DNumber1, DNumber2);
                    // Corrected: Return the actual dose unit (Gy or cGy)
                    return (dose_absolute, dv.UnitAsString, result);
                }
            }
            else if (limitString.StartsWith("D"))
            {
                (double? VNumber, string VUnit, string Operator, double? DNumber1, double? DNumber2, string DUnit) = DoseMatch(limitString);

                double volume_absolut;
                if (VUnit == "%")
                    volume_absolut = (VNumber.Value / 100.0) * Structure.Volume;
                else
                    volume_absolut = VNumber.Value;

                DoseValue dv = Plan.GetDoseAtVolume(
                        Structure,
                        volume_absolut,
                        VMS.TPS.Common.Model.Types.VolumePresentation.AbsoluteCm3,
                        DoseValuePresentation.Absolute);

                if (DUnit == "%")
                {
                    double dose_percent = dv.Dose / Prescription.TotalDose * 100.0;
                    Result result = Test(dose_percent, Operator, DNumber1, DNumber2);
                    return (dose_percent, "%", result);
                }
                else
                {
                    double dose_absolute = dv.Dose;
                    Result result = Test(dose_absolute, Operator, DNumber1, DNumber2);
                    // Corrected: Return the actual dose unit (Gy or cGy)
                    return (dose_absolute, dv.UnitAsString, result);
                }
            }
            else if (limitString.StartsWith("V"))
            {
                (double? DNumber, string DUnit, string Operator, double? VNumber1, double? VNumber2, string VUnit) = VolumeMatch(limitString);

                DoseValue dv;
                if (DUnit == "%")
                {
                    dv = CalculateAbsoluteDose(DNumber.Value, Prescription);
                }
                else if (DUnit == "cGy")
                {
                    dv = new DoseValue(DNumber.Value, DoseValue.DoseUnit.cGy);
                }
                else if (DUnit == "Gy")
                {
                    dv = new DoseValue(DNumber.Value, DoseValue.DoseUnit.Gy);
                }
                else
                {
                    throw new Exception($"Invalid Dose Unit:{DUnit}");
                }

                if (VUnit == "%")
                {
                    double volume_percent = Plan.GetVolumeAtDose(
                           Structure,
                           dv,
                           VolumePresentation.Relative);

                    Result result = Test(volume_percent, Operator, VNumber1, VNumber2);
                    return (volume_percent, "%", result);
                }
                else
                {
                    double volume_cc = Plan.GetVolumeAtDose(
                           Structure,
                           dv,
                           VolumePresentation.AbsoluteCm3);

                    Result result = Test(volume_cc, Operator, VNumber1, VNumber2);
                    return (volume_cc, "cc", result);
                }
            }
            else
            {
                throw new Exception("Invalid limitString format");
            }

        }

        public static void RunTests()
        {
            Console.WriteLine("--- Testing V-Type Matches ---");
            string[] vTestCases = {
                "V100%<90%",
                "V99cGy>10cc",
                "V90%<90-100cc",
                "V100Gy<10-20%",
                "V50.5>=50.5-60.5cc",
                "A90%<10cc" // Expected to fail
            };

            foreach (var input in vTestCases)
            {
                try
                {
                    var result = VolumeMatch(input);
                    Console.WriteLine($"Input: {input}");
                    Console.WriteLine($"  DNumber: {result.DNumber}");
                    Console.WriteLine($"  DUnit: {result.DUnit}");
                    Console.WriteLine($"  Operator: {result.Operator}");
                    Console.WriteLine($"  VNumber1: {result.VNumber1}");
                    Console.WriteLine($"  VNumber2: {result.VNumber2}");
                    Console.WriteLine($"  VUnit: {result.VUnit}");
                    Console.WriteLine("------------------------------");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Input: {input}");
                    Console.WriteLine($"  Exception: {ex.Message}");
                    Console.WriteLine("------------------------------");
                }
            }

            Console.WriteLine("\n--- Testing D-Type Matches ---");
            string[] dTestCases = {
                "D20cc>100Gy",
                "D10%<50.5-60.5cGy",
                "D50cc>=50.5Gy",
                "V100%<90%", // Expected to fail
                "D10%<50.5-60.5" // Expected to fail due to missing dose unit
            };

            foreach (var input in dTestCases)
            {
                try
                {
                    var result = DoseMatch(input);
                    Console.WriteLine($"Input: {input}");
                    Console.WriteLine($"  VNumber: {result.VNumber}");
                    Console.WriteLine($"  VUnit: {result.VUnit}");
                    Console.WriteLine($"  Operator: {result.Operator}");
                    Console.WriteLine($"  DNumber1: {result.DNumber1}");
                    Console.WriteLine($"  DNumber2: {result.DNumber2}");
                    Console.WriteLine($"  DUnit: {result.DUnit}");
                    Console.WriteLine("------------------------------");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Input: {input}");
                    Console.WriteLine($"  Exception: {ex.Message}");
                    Console.WriteLine("------------------------------");
                }
            }

            Console.WriteLine("\n--- Testing Min/Max/Mean Dose Matches ---");
            string[] sTestCases = {
                "Max>100Gy",
                "Mean<50.5-60.5cGy",
                "Min>=20%",
                "Avg=100Gy", // Expected to fail
                "D20cc>100Gy" // Expected to fail
            };

            foreach (var input in sTestCases)
            {
                try
                {
                    var result = MinMaxMeanDoseMatch(input);
                    Console.WriteLine($"Input: {input}");
                    Console.WriteLine($"  StatType: {result.StatType}");
                    Console.WriteLine($"  Operator: {result.Operator}");
                    Console.WriteLine($"  DNumber1: {result.DNumber1}");
                    Console.WriteLine($"  DNumber2: {result.DNumber2}");
                    Console.WriteLine($"  DUnit: {result.DUnit}");
                    Console.WriteLine("------------------------------");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Input: {input}");
                    Console.WriteLine($"  Exception: {ex.Message}");
                    Console.WriteLine("------------------------------");
                }
            }
        }
    }
}
