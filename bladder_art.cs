using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Management.Instrumentation;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Controls;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Media;
using System.Security.AccessControl;
using System.Runtime.Remoting.Contexts;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Xml;

using static esapi.esapi;


using VMSPatient = VMS.TPS.Common.Model.API.Patient;
using VMSStructureSet = VMS.TPS.Common.Model.API.StructureSet;
using VMSStructure = VMS.TPS.Common.Model.API.Structure;
using VMSImage = VMS.TPS.Common.Model.API.Image;
using VMSCourse = VMS.TPS.Common.Model.API.Course;
using VMSStudy = VMS.TPS.Common.Model.API.Study;
using VMSSeries = VMS.TPS.Common.Model.API.Series;
using VMSRegistration = VMS.TPS.Common.Model.API.Registration;
using VMSReferencePoint = VMS.TPS.Common.Model.API.ReferencePoint;
using VMSHospital = VMS.TPS.Common.Model.API.Hospital;
using System.Windows.Forms;

namespace nnunet_client
{
    public static class bladder_art
    {
        static VMS.TPS.Common.Model.API.Application vmsApp { get {
                return global.vmsApplication;
            } set { 
            global.vmsApplication = value;
            } }


        private static IProgress<string> Progress = null;


        private static void _err(string msg)
        {
            _info(msg);
            throw new Exception(msg);
        }

        private static void _info(string msg)
        {
            //Console.WriteLine(msg);
            helper.log(msg);
        }

        public static void crop_by_body2(VMS.TPS.Common.Model.API.Structure s, double body_inner_margin_mm, VMS.TPS.Common.Model.API.StructureSet sset)
        {
            // BODY
            Structure s_body = s_of_type_external(sset);
            if (s_body == null)
            {
                _err($"Could not find structure(Id=BODY)");
            }

            // shrink body
            SegmentVolume body_shrink = s_body.SegmentVolume.Margin(-body_inner_margin_mm);

            // crop
            s.SegmentVolume = s.SegmentVolume.And(body_shrink);
        }

        private static string _log_file=null;
        public static void make_cbct_art_plans_3mm(VMS.TPS.Common.Model.API.Application app)
        {
            vmsApp = app;
            global.appConfig.data_root_secure = @"G:\data_secure";
            global.appConfig.make_export_log = false; // do not make export_log.

            string nnunet_data_dir = @"G:\data_secure\_bladder_art\nnunet";
            string nnunet_raw_dir = filesystem.join(nnunet_data_dir, "raw", false);
            string dataset_name = "Dataset104_CBCTRectumBowel";
            string dataset_dir = filesystem.join(nnunet_raw_dir, dataset_name);

            DateTime t = DateTime.Now;
            _log_file = filesystem.join(dataset_dir, $"log_{t.Year}{t.Month:D2}{t.Day:D2}.txt");


            string export_dirname = "r2_imagesTs";
            string export_dir = filesystem.join(dataset_dir, export_dirname);
            string exports_file = filesystem.join(export_dir, "export_list.txt");

            _info($"export_file={exports_file}");
            filesystem.file_must_exist(exports_file);

            int ptv_margin_mm = 3;
            string bladder_id = "r2_bladder_3d_lowres";
            string rectum_bowel_id = "r2_3d_lowres_rectumbowel";

            int num_beams = 7;
            int gantr_interval = 50;
            int gantry_start = 180 + 50 / 2;
            double[] Gs = new double[num_beams];
            for (int g = 0; g < num_beams; g++)
            {
                double gantry_angle = gantry_start + gantr_interval * g;
                if (gantry_angle >= 360)
                    gantry_angle -= 360;

                Gs[g] = gantry_angle;
            }

            esapi.param p = new esapi.param(exports_file, "->", "||");
            string[] keys = p.get_keys();
            int[] skip = new int[] {11,12, 15, 50, 51, 52, 53, 54, 55, 56, 57, 58, 
                59, 60, 61, 62, 63, 86, 94, 136, 137, 139, 141, 143, 145, 158, 170, 171, 214, 225, 226, 
                278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 436, 437, 438, 
                439, 440, 441, 442, 443, 444, 445, 446, 447 };
            for(int i=0; i< keys.Length; i++)
            {
                if (skip.Contains(i))
                {
                    _info($"i[{i}] is in the skip array. so skipping...");
                    continue;
                }

                string key = keys[i];   
                if (key.StartsWith("-"))
                {
                    _info($"image name {key} starts with '-', so skipping...");
                    continue;
                }

                string[] values = p.get_value_as_array(key);

                string pid = values[0];
                string study_id = values[1];
                string image_id = values[2];
                string image_FOR = values[3];
                string creation_dt = values[4];

                _info($"[{i}/{keys.Length}] {key} - {pid} - {image_id} - {creation_dt} -  {image_FOR}");


                string cs_id = "_art";
                string ps_id= $"bldr_{ptv_margin_mm}_{i:D3}";                

                try
                {
                    create_bladder_plan(pid, image_id, image_FOR, bladder_id, rectum_bowel_id, ptv_margin_mm,Gs, cs_id, ps_id);
                }
                catch(Exception exn)
                {
                    _info($"{exn.Message}");
                    app.ClosePatient();
                }
                
            }
        }

        public static void run(VMS.TPS.Common.Model.API.Application app)
        {
            create_3mm_5mm_10mm_art_plans(app);
            //change_rx_of_art_plan(app);
            //create_plan0_ct_plans(app);
            //copy_beams_and_cal_dose_for_plans(app);

        }

        public static void copy_beams_and_cal_dose_for_plans(VMS.TPS.Common.Model.API.Application app)
        {
            vmsApp = app;

            string plan_src = "plan0_ct";
            string plan_dst = "plan0_set1";

            string art_3mm_plan_list_file = @"G:\data_secure\_bladder_art\art_3mm_set1_plans.csv";

            string[] lines = System.IO.File.ReadAllLines(art_3mm_plan_list_file);

            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n].Trim();

                if (line.StartsWith("#"))
                    continue;
                if (line == "")
                    continue;

                string[] elms = line.Split(',');

                int i = 0;
                string pid = elms[i++].Trim();
                string plan_id = elms[i++].Trim();
                string sset_id = elms[i++].Trim();
                string image_id = elms[i++].Trim();
                string image_FOR = elms[i++].Trim();

                string cs_id = "_art";

                if(pid == "00685933" || pid == "31057646")
                    copy_beams_and_cal_dose(pid, cs_id, plan_src, plan_dst);
                
            }
        }


        public static void copy_beams_and_cal_dose(
    string pid,
    string cs_id,
    string plan_src,
    string plan_dst
    )
        {
            _info($"copy_beams_and_cal_dose(): pid={pid}, cs_id={cs_id}, plan_src={plan_src}, plan_dst={plan_dst}");
            Patient pt = vmsApp.OpenPatientById(pid);
            if (pt == null)
            {
                _err($"Patient(Id={pid}) not found");
            }

            // check if the plan exists
            Course cs = cs_of_id(cs_id, pt);
            if (cs == null)
            {
                _err($"Course ({cs_id}) not found!");
            }

            ExternalPlanSetup ps_src = (ExternalPlanSetup)ps_of_id(plan_src, pt);
            if (ps_src == null)
            {
                _err($"Plan ({plan_src}) not found!");
            }

            ExternalPlanSetup ps_dst = (ExternalPlanSetup)ps_of_id(plan_dst, pt);
            if (ps_dst == null)
            {
                _err($"Plan ({plan_dst}) not found!");
            }

            pt.BeginModifications();

            // keep isocenter 
            VVector iso_dst = ps_dst.Beams.ToArray()[0].IsocenterPosition;

            //ExternalBeamMachineParameters machine = new ExternalBeamMachineParameters("SIL 21IX", "6X", 600, "STATIC", "");
            List<KeyValuePair<string, MetersetValue>> ms_src = new List<KeyValuePair<string, MetersetValue>>();

            for (int i = 0; i < ps_src.Beams.Count(); i++)
            {
               
                Beam bm_src = ps_src.Beams.ToArray()[i];
                Beam bm_dst = ps_dst.Beams.ToArray()[i];

                BeamParameters bp_src = bm_src.GetEditableParameters();
                BeamParameters bp_dst = bm_dst.GetEditableParameters();

                if (bp_dst.ControlPoints.Count() > 0 && bp_dst.ControlPoints.Count() == bp_src.ControlPoints.Count())
                {
                    for (int j = 0; j < bp_dst.ControlPoints.Count(); j++)
                    {
                        ControlPointParameters cpp_src = bp_src.ControlPoints.ElementAt(j);
                        ControlPointParameters cpp_dst = bp_dst.ControlPoints.ElementAt(j);

                        cpp_dst.LeafPositions = cpp_src.LeafPositions;
                        cpp_dst.JawPositions = cpp_src.JawPositions;
                    }
                    bm_dst.ApplyParameters(bp_dst);
                }

                KeyValuePair<string, MetersetValue> kv = new KeyValuePair<string, MetersetValue>(bm_src.Id, bm_src.Meterset);
                ms_src.Add(kv);
            }

            // public void SetCalculationModel(
            ps_dst.SetCalculationModel(CalculationType.PhotonOptimization, "PO_13623");
            ps_dst.SetCalculationModel(CalculationType.PhotonVolumeDose, "AAA_13623");
            ps_dst.SetCalculationModel(CalculationType.PhotonLeafMotions, "Smart LMC [13.6.23]");

            if (ps_dst.Beams.ElementAt(0).ControlPoints.Count() < 10 )
            {

                CalculationResult lmc_result = ps_dst.CalculateLeafMotions();

                if (!lmc_result.Success)
                {
                    _err("LMC Calculation Failed");
                }
            }

            ps_dst.CalculateDoseWithPresetValues(ms_src);
            
           
            vmsApp.SaveModifications();


            
            vmsApp.ClosePatient();
        }

        

        public static void change_rx_of_art_plan(VMS.TPS.Common.Model.API.Application app)
        {
            vmsApp = app;

            string[] plan_ids = new string[] { "art2_3mm_set1", "art2_5mm_set1", "art2_1cm_set1", "plan0_set1" };



            string art_3mm_plan_list_file = @"G:\data_secure\_bladder_art\art_3mm_set1_plans.csv";

            string[] lines = System.IO.File.ReadAllLines(art_3mm_plan_list_file);

            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n].Trim();

                if (line.StartsWith("#"))
                    continue;
                if (line == "")
                    continue;

                string[] elms = line.Split(',');

                int i = 0;
                string pid = elms[i++].Trim();
                string plan_id = elms[i++].Trim();
                string sset_id = elms[i++].Trim();
                string image_id = elms[i++].Trim();
                string image_FOR = elms[i++].Trim();

                int num_of_fxs = 20;
                double dose_per_fx = 275.0;
                string cs_id = "_art";

                foreach (string ps_id in plan_ids)
                {
                    set_plan_rx(pid, cs_id, ps_id, num_of_fxs, dose_per_fx);
                }
            }
        }

        public static void set_plan_rx(
            string pid,
            string cs_id,
            string ps_id,
            int num_fxs,
            double dose_per_fx
            )
        {
            _info($"set_plan_rx(): pid={pid}, cs_id={cs_id}, ps_id={ps_id}, num_fxs={num_fxs}, dose_per_fx={dose_per_fx}, total_dose={num_fxs * dose_per_fx}");
            Patient pt = vmsApp.OpenPatientById(pid);
            if (pt == null)
            {
                _err($"Patient(Id={pid}) not found");
            }

            double total_dose = num_fxs * dose_per_fx;

            // check if the plan exists
            Course cs = cs_of_id(cs_id, pt);
            if(cs == null)
            {
                _err($"Course ({cs_id}) not found!");
            }

            ExternalPlanSetup ps = (ExternalPlanSetup)ps_of_id(ps_id, pt);
            if (ps == null)
            {
                _err($"Plan ({ps_id}) not found!");
            }

            pt.BeginModifications();

            //new_ps.SetPrescription(ps.NumberOfFractions??0, ps.DosePerFraction, ps.TreatmentPercentage);
            ps.SetPrescription(num_fxs, new DoseValue(dose_per_fx, DoseValue.DoseUnit.cGy), 1.0);

            vmsApp.SaveModifications();
            vmsApp.ClosePatient();
        }

        public static void create_plan0_ct_plans(VMS.TPS.Common.Model.API.Application app)
        {
            vmsApp = app;

            //make_cbct_art_plans_3mm(app);

           

            // collect set1 cases (many of them were made manually, due to non-zero acquisition angles of CBCT, so here I collect the cases
            string art_3mm_plan_list_file = @"G:\data_secure\_bladder_art\art_3mm_set1_plans.csv";

            // make plans
            {
                int margin_mm = 10;
                string bladder_id = "Bladder";
                string[] bowel_ids = new string[] { "Bowel_Small", "Bowel_Large", "Colon_Sigmoid" };
                string rectum_id = "Rectum";
                string plan_id = "plan0_ct";
                string cs_id = "_art";
                optimize_plan0_ct_plans(art_3mm_plan_list_file, bladder_id, bowel_ids, rectum_id, margin_mm, plan_id, cs_id);
            }         
            
        }


        public static void create_3mm_5mm_10mm_art_plans(VMS.TPS.Common.Model.API.Application app)
        {
            vmsApp = app;

            //make_cbct_art_plans_3mm(app);

            List<string> art_plans = new List<string>();

            // collect set1 cases (many of them were made manually, due to non-zero acquisition angles of CBCT, so here I collect the cases
            string art_3mm_plan_list_file = @"G:\data_secure\_bladder_art\art_3mm_set1_plans.csv";
            //{
                //string pid_file = @"G:\data_secure\_bladder_art\bladder_16_pids.txt";
                //string plan_id = "art_3mm_set1";
                //find_plans(pid_file, plan_id, art_3mm_plan_list_file);
                //art_plans.Add(plan_id);
            //}

            // add 3mm plans
            {
                int margin_mm = 3;
                string bladder_id = "r2_bladder_3d_lowres";
                string rectum_bowel_id = "r2_3d_lowres_rectumbowel";
                string plan_id = "art2_3mm_set1";
                string cs_id = "_art";
                //add_art_plans2(art_3mm_plan_list_file, bladder_id, margin_mm, rectum_bowel_id, plan_id, cs_id);
                art_plans.Add(plan_id);
            }

            // add 5 mm plans
            {
                int margin_mm = 5;
                string bladder_id = "r2_bladder_3d_lowres";
                string rectum_bowel_id = "r2_3d_lowres_rectumbowel";
                string plan_id = "art2_5mm_set1";
                string cs_id = "_art";
                //add_art_plans2(art_3mm_plan_list_file, bladder_id, margin_mm, rectum_bowel_id, plan_id, cs_id);
                art_plans.Add(plan_id);
            }

            // add 10 mm plans
            {
                int margin_mm = 10;
                string bladder_id = "r2_bladder_3d_lowres";
                string rectum_bowel_id = "r2_3d_lowres_rectumbowel";
                string plan_id = "art2_1cm_set1";
                string cs_id = "_art";
                //add_art_plans2(art_3mm_plan_list_file, bladder_id, margin_mm, rectum_bowel_id, plan_id, cs_id);
                art_plans.Add(plan_id);
            }

            eval_dvh(art_plans.ToArray(), app);

        }

        public static void find_plans(string pid_file, string plan_id, string out_file)
        {
            global.appConfig.make_export_log = false; // do not make export_log.

            
            _log_file = out_file+".log";

            string result_csv = out_file;

            filesystem.writeline(result_csv, "pid, plan_id, sset_id, image_id, image_FOR");

            string[] lines = System.IO.File.ReadAllLines(pid_file);

            foreach (string line in lines)
            {
                _info(line);

                if (line.Trim() == "")
                    continue;

                if (line.Trim().StartsWith("#"))
                    continue;

                string pid = line.Trim();

                _info($"pid={pid}");

                Patient pt = vmsApp.OpenPatientById(pid);

                if (pt == null)
                {
                    _info("Could not open the patient");
                    continue;
                }

                // art
                PlanSetup plan = ps_of_id(plan_id, pt);
                if (plan == null)
                {
                    _info($"plan not found ({plan_id}).");
                    vmsApp.ClosePatient();
                    continue;
                }

                string out_line = $"{pid},{plan.Id},{plan.StructureSet.Id},{plan.StructureSet.Image.Id},{plan.StructureSet.Image.FOR}";

                _info(out_line);

                filesystem.appendline(result_csv, out_line);

                vmsApp.ClosePatient();
            }

            _info("done.");

        }

        public static string collect_dvhs(PlanSetup ps)
        {
            Dictionary<string, double> dvhs = new Dictionary<string, double>();

            // bladder
            Structure bldr = s_of_id("Bladder", ps.StructureSet);
            DVHData dvh = ps.GetDVHCumulativeData(bldr, DoseValuePresentation.Relative, VolumePresentation.Relative, 1);
            double bladder_Dmean = dvh.MeanDose.Dose;
            double bladder_D95 = ps.GetDoseAtVolume(bldr, 95.0, VolumePresentation.Relative, DoseValuePresentation.Relative).Dose;
            double bladder_Dmax = ps.GetDoseAtVolume(bldr, 0.03, VolumePresentation.AbsoluteCm3, DoseValuePresentation.Relative).Dose;

            // rectum bowel
            Structure rb = s_of_id("RectumBowel", ps.StructureSet);
            double rb_Dmax = ps.GetDoseAtVolume(rb, 0.035, VolumePresentation.AbsoluteCm3, DoseValuePresentation.Absolute).Dose;
            double rx = ps.TotalDose.Dose;
            double rb_V100 = ps.GetVolumeAtDose(rb, s2D($"{rx} cGy"), VolumePresentation.AbsoluteCm3);
            double rb_V90 = ps.GetVolumeAtDose(rb, s2D($"{rx*0.9} cGy"), VolumePresentation.AbsoluteCm3);
            double rb_V80 = ps.GetVolumeAtDose(rb, s2D($"{rx * 0.8} cGy"), VolumePresentation.AbsoluteCm3);
            double rb_V50 = ps.GetVolumeAtDose(rb, s2D($"{rx * 0.5} cGy"), VolumePresentation.AbsoluteCm3);

            return $"{bladder_Dmean},{bladder_D95}, {bladder_Dmax}, {rb_V100},{rb_V90},{rb_V80},{rb_V50}";
        }

        public static void eval_dvh(string[] art_plans, VMS.TPS.Common.Model.API.Application app)
        {
            global.appConfig.make_export_log = false; // do not make export_log.

            string pid_file = @"G:\data_secure\_bladder_art\bladder_16_pids.txt";
            string result_csv = @"G:\data_secure\_bladder_art\bladder_16_pids.3_plans.dvhs.3.csv";
            _log_file = result_csv + ".log";

            // cvs headerodse
            string head_line = "pid,bladder_Dmean,bladder_D95,bladder_Dmax,rb_V100,rb_V90,rb_V80,rb_V50";
            foreach(string art_plan in art_plans)
            {
                head_line += ",bladder_Dmean,bladder_D95,bladder_Dmax,rb_V100,rb_V90,rb_V80,rb_V50";
            }
            filesystem.writeline(result_csv, head_line);


            string[] lines = System.IO.File.ReadAllLines(pid_file);

            foreach(string line in lines)
            {
                _info(line);

                if (line.Trim() == "")
                    continue;

                if (line.Trim().StartsWith("#"))
                    continue;

                string pid = line.Trim();


                _info($"pid={pid}");

                Patient pt = app.OpenPatientById(pid);

                if (pt == null)
                {
                    _info("Could not open the patient");
                    continue;
                }
                // clincial plan
                string plan_1cm_id = "plan0_set1";
                PlanSetup plan_1cm = ps_of_id(plan_1cm_id, pt);
                if (plan_1cm == null)
                {
                    _info($"plan not found ({plan_1cm_id}) not found.");
                    app.ClosePatient();
                    continue;
                }
                string conv_dvhs = collect_dvhs(plan_1cm);

                // art
                string out_line = $"{pid}, {conv_dvhs}";
                foreach (string art_plan in art_plans)
                {
                    PlanSetup ps = ps_of_id(art_plan, pt);
                    if (ps == null)
                    {
                        _info($"plan({art_plan}) not found for {pt.Id}.");
                        vmsApp.ClosePatient();

                        // clear out_line
                        out_line = "";
                        break;
                    }

                    _info($"collecting dvhs for {art_plan}");

                    string art_dvhs = collect_dvhs(ps);

                    out_line += $",{art_dvhs}";
                }

                if(out_line != "")
                { 
                    _info(out_line);
                    filesystem.appendline(result_csv, out_line);
                }


                app.ClosePatient();
            }


            _info("done.");

        }

       

        public static void optimize_plan0_ct_plans(string base_plan_list_file, string bladder_id, string[] bowel_ids, string rectum_id, int ptv_margin_mm, string plan_id0, string cs_id)
        {
            string[] lines = System.IO.File.ReadAllLines(base_plan_list_file);

            int num_beams = 7;
            int gantr_interval = 50;
            int gantry_start = 180 + 50 / 2;
            double[] Gs = new double[num_beams];
            for (int g = 0; g < num_beams; g++)
            {
                double gantry_angle = gantry_start + gantr_interval * g;
                if (gantry_angle >= 360)
                    gantry_angle -= 360;

                Gs[g] = gantry_angle;
            }

            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n].Trim();

                if (line.StartsWith("#"))
                    continue;
                if (line == "")
                    continue;

                string[] elms = line.Split(',');

                int i = 0;
                string pid = elms[i++].Trim();
                string plan_id = elms[i++].Trim();
                string sset_id = elms[i++].Trim();
                string image_id = elms[i++].Trim();
                string image_FOR = elms[i++].Trim();

                create_plan0_ct(pid, image_id, image_FOR, bladder_id, bowel_ids, rectum_id, ptv_margin_mm, Gs, cs_id, plan_id0, 20, 275);


            }

        }


        public static void add_art_plans(string base_plan_list_file, string bladder_id ,  int ptv_margin_mm, string rectum_bowel_id, string new_plan_id, string cs_id)
        {
            string[] lines = System.IO.File.ReadAllLines(base_plan_list_file);

            int num_beams = 7;
            int gantr_interval = 50;
            int gantry_start = 180 + 50 / 2;
            double[] Gs = new double[num_beams];
            for (int g = 0; g < num_beams; g++)
            {
                double gantry_angle = gantry_start + gantr_interval * g;
                if (gantry_angle >= 360)
                    gantry_angle -= 360;

                Gs[g] = gantry_angle;
            }

            for(int n=0; n<lines.Length; n++)
            {
                string line = lines[n].Trim();

                if (line.StartsWith("#"))
                    continue;
                if (line=="")
                    continue;

                string[] elms = line.Split(',');

                int i = 0;
                string pid = elms[i++].Trim();
                string plan_id = elms[i++].Trim();
                string sset_id = elms[i++].Trim();
                string image_id = elms[i++].Trim();
                string image_FOR = elms[i++].Trim();

                create_bladder_plan(pid, image_id, image_FOR, bladder_id, rectum_bowel_id, ptv_margin_mm, Gs, cs_id, new_plan_id);


            }

        }


        public static void add_art_plans2(string base_plan_list_file, string bladder_id, int ptv_margin_mm, string rectum_bowel_id, string new_plan_id, string cs_id)
        {
            string[] lines = System.IO.File.ReadAllLines(base_plan_list_file);

            int num_beams = 7;
            int gantr_interval = 50;
            int gantry_start = 180 + 50 / 2;
            double[] Gs = new double[num_beams];
            for (int g = 0; g < num_beams; g++)
            {
                double gantry_angle = gantry_start + gantr_interval * g;
                if (gantry_angle >= 360)
                    gantry_angle -= 360;

                Gs[g] = gantry_angle;
            }

            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n].Trim();

                if (line.StartsWith("#"))
                    continue;
                if (line == "")
                    continue;

                string[] elms = line.Split(',');

                int i = 0;
                string pid = elms[i++].Trim();
                string plan_id = elms[i++].Trim();
                string sset_id = elms[i++].Trim();
                string image_id = elms[i++].Trim();
                string image_FOR = elms[i++].Trim();

                create_bladder_plan2(pid, image_id, image_FOR, bladder_id, rectum_bowel_id, ptv_margin_mm, Gs, cs_id, new_plan_id);


            }

        }


        public static void create_plan0_ct(
            string pid,
            string image_id,
            string image_FOR,
            string bladder_id,
            string[] bowel_ids,
            string rectum_id,
            int ptv_margin_mm,
            double[] Gs,
            string cs_id,
            string ps_id,
            int num_fxs = 20,
            double dose_per_fx = 275.0
            )
        {

            string[] done = {};

            // skip that have been done before
            if (Array.Exists(done, e => e==pid))
            {
                _info($"{pid} -> done.");
                return;
            }

            _info($"pid={pid}");

            Patient pt = vmsApp.OpenPatientById(pid);
            if (pt == null)
            {
                _err($"Patient(Id={pid}) not found");
            }

            ps_id = "plan0_ct";
            ExternalPlanSetup ps = (ExternalPlanSetup)ps_of_id(ps_id, pt);
            if (ps == null)
            {
                _err($"plan not found:{pid}->({ps_id}. skipping...");
                vmsApp.ClosePatient();
                return;
            }


            double total_dose = num_fxs * dose_per_fx;

            StringBuilder sb = new StringBuilder();
            //string sset_id = image_id; // image ID and structureset ID are same

            Color ptv_color = Color.FromRgb(255, 0, 0);
            double body_inner_margin_for_crop = 5.0;

            string ptv_Id = $"ptv_bldr_{ptv_margin_mm}mm";

            ExternalBeamMachineParameters machine = new ExternalBeamMachineParameters("SIL 21IX", "6X", 600, "STATIC", "");

            pt.BeginModifications();

            ///////
            // sset
            StructureSet sset = ps.StructureSet;

            if (sset.Image.Series.ImagingDeviceId == null || sset.Image.Series.ImagingDeviceId == "")
                sset.Image.Series.SetImagingDevice("CTAWP96967");

            //////////////////////////
            /// s_ctv is the bladder
            Structure s_ctv = s_of_id(bladder_id, sset);
            if (s_ctv == null)
            {
                _err($"Structure(Id={bladder_id}) not found");
            }
            sb.Append($",{s_ctv.Id}");

            ////////////
            /// rectum 
            Structure s_rectum = s_of_id(rectum_id, sset);
            if (s_rectum == null)
            {
                _err($"Structure(Id={rectum_id}) not found");
            }
            sb.Append($",{rectum_id}");


            //////////
            // bowel
            SegmentVolume sv_bowel = null;
            for (int i = 0; i < bowel_ids.Length; i++)
            {
                string s_id = bowel_ids[i];
                Structure s = s_of_id(s_id, sset, false);
                if (s == null)
                {
                    _info($"structure {s_id} is not found!. skipping...");
                    continue;
                }

                if (sv_bowel == null)
                    sv_bowel = s.SegmentVolume;
                else
                    sv_bowel = sv_bowel.Or(s.SegmentVolume);
            
            }
            Structure s_bowel = find_or_add_s("ORGAN", "bowel_sum", sset);
            s_bowel.SegmentVolume = sv_bowel;

            vmsApp.SaveModifications();


            /////////////////////
            //// rectum + bowel
            Structure s_rectumbowel = find_or_add_s("ORGAN", "rectumbowel", sset);
            s_rectumbowel.SegmentVolume = s_rectum.SegmentVolume.Or(s_bowel.SegmentVolume);
            vmsApp.SaveModifications();


            /////////
            /// Body
            Structure s_body = s_of_type_external(sset);
            if (s_body == null || s_body.IsEmpty)
            {
                _info("Body not found. Creating one...");
                SearchBodyParameters sbparam = sset.GetDefaultSearchBodyParameters();
                s_body = sset.CreateAndSearchBody(sbparam);
            }

            ////////
            // PTV
            Structure s_ptv = find_or_add_s("PTV", ptv_Id, sset);
            if (s_ptv == null)
            {
                _err($"Could not find or add structure(Id={ptv_Id})");
            }
            s_ptv.Color = ptv_color;
            sb.Append($",{s_ptv.Id}");

            // ptv = ctv + margin_mm
            s_ptv.SegmentVolume = s_ctv.SegmentVolume.Margin(ptv_margin_mm); // add margin
            s_ptv.SegmentVolume = s_ptv.SegmentVolume.Sub(s_rectumbowel.SegmentVolume); // subtract bowel + rectum
            s_ptv.SegmentVolume = s_ptv.SegmentVolume.Or(s_ctv.SegmentVolume); // add bladder back 
            sb.Append($",{ptv_margin_mm}");

            crop_by_body2(s_ptv, body_inner_margin_for_crop, sset); // cut ptv by body

            vmsApp.SaveModifications();

            ///////////////////////////////////////////////////
            //// rectumbowel_m_bladder = rectum_bowel - bladder
            Structure s_rectumbowel_m_bladder = find_or_add_s("ORGAN", "rectumbowel_m_bladder", sset);
            s_rectumbowel_m_bladder.SegmentVolume = s_rectumbowel.SegmentVolume.Sub(s_ctv.SegmentVolume);

            ///////////////////////////
            // rectum_bowel for opti = s_rectumbowel_m_bladder and ptv+3cm
            Structure s_opti_rectumbowel = find_or_add_s("ORGAN", "_opti_rectumbowel", sset);
            s_opti_rectumbowel.SegmentVolume = s_ptv.SegmentVolume.Margin(30.0).And(s_rectumbowel_m_bladder.SegmentVolume);

            ///////////////////////////////////////
            Course cs = find_or_add_cs(cs_id, pt);
            if (cs == null)
                _err($"Course(Id={cs_id}) not found nor create it.");

            ps.SetPrescription(num_fxs, new DoseValue(dose_per_fx, DoseValue.DoseUnit.cGy), 1.0);

            // fit collimators
            foreach(Beam b in ps.Beams)
            {
                // fit Jaw to the ptv
                double jaw_margin_mm = 20.0; // large enough for IMRT
                b.FitCollimatorToStructure(new FitToStructureMargins(jaw_margin_mm), s_ptv, true, true, false);
            }

            // public void SetCalculationModel(
            ps.SetCalculationModel(CalculationType.PhotonOptimization, "PO_13623");
            ps.SetCalculationModel(CalculationType.PhotonVolumeDose, "AAA_13623");
            ps.SetCalculationModel(CalculationType.PhotonLeafMotions, "Smart LMC [13.6.23]");

            // optimization
            OptimizationSetup os = ps.OptimizationSetup;

            // remove all existing objectives
            foreach(OptimizationObjective obj in os.Objectives)
            {
                os.RemoveObjective(obj);
            }

            vmsApp.SaveModifications();


            OptimizationNormalTissueParameter nto = os.AddAutomaticNormalTissueObjective(100);

            // dose values
            DoseValue dv_rx = new DoseValue(total_dose, DoseValue.DoseUnit.cGy);
            DoseValue dv_rx_110p = new DoseValue(total_dose * 1.1, DoseValue.DoseUnit.cGy);
            DoseValue dv_rx_50p = new DoseValue(total_dose * 0.5, DoseValue.DoseUnit.cGy);

            // PTV
            {
                os.AddPointObjective(s_ptv, OptimizationObjectiveOperator.Lower, dv_rx, 100.0, 100.0);
                os.AddPointObjective(s_ptv, OptimizationObjectiveOperator.Upper, dv_rx_110p, 0.0, 100.0);
            }

            // Rectum Bowel
            {
                os.AddPointObjective(s_opti_rectumbowel, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
                os.AddPointObjective(s_opti_rectumbowel, OptimizationObjectiveOperator.Upper, dv_rx_50p, 50, 100.0);
            }

            // Rectum
            {
                os.AddPointObjective(s_rectum, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
                os.AddPointObjective(s_rectum, OptimizationObjectiveOperator.Upper, dv_rx_50p, 25, 100.0);

            }

            //os.AddAutomaticNormalTissueObjective(100.0);

            OptimizationOptionsIMRT optm_Options = new OptimizationOptionsIMRT(
                300,//int maxIterations,
                OptimizationOption.RestartOptimization,
                OptimizationConvergenceOption.TerminateIfConverged,
                OptimizationIntermediateDoseOption.NoIntermediateDose,
                "" //string mlcId
            );

            vmsApp.SaveModifications();

            helper.log("optimizing...");
            Stopwatch stopwatch = new Stopwatch();
            // intial optimization
            stopwatch.Restart();
            OptimizerResult opt_result = ps.Optimize(optm_Options);
            stopwatch.Stop();
            helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

            //// push down rectum bowel slightly 
            //OptimizationOptionsIMRT optm_Options2 = new OptimizationOptionsIMRT(
            //    100,//int maxIterations,
            //    OptimizationOption.ContinueOptimization,
            //    OptimizationConvergenceOption.TerminateIfConverged,
            //    OptimizationIntermediateDoseOption.NoIntermediateDose,
            //    "" //string mlcId
            //);

            //// get the volume of the 50% dose
            //os.AddPointObjective(s_opti_rectumbowel, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
            //ps.Optimize(optm_Options2);


            if (!opt_result.Success)
            {
                vmsApp.ClosePatient();
                _err($"Optimization Failed: opt_result.ToString()");
            }

            stopwatch.Restart();

            // LMC
            helper.log("calculating LMC...");

            // method 1. 
            //bool fixedJaw = true;
            //LMCVOptions lmc_options = new LMCVOptions(fixedJaw);
            //CalculationResult lmc_result = ps.CalculateLeafMotions(lmc_options);

            // method 2.
            //bool fixedFieldBorders = true;
            //bool jawTracking = false;
            //SmartLMCOptions lmc_options =  new SmartLMCOptions(true, false);
            //ps.CalculateLeafMotions(lmc_options);


            // method 3.
            CalculationResult lmc_result = ps.CalculateLeafMotions();

            stopwatch.Stop();
            helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

            if (!lmc_result.Success)
            {
                vmsApp.ClosePatient();
                _err("LMC Calculation Failed");
            }

            vmsApp.SaveModifications();

            // Dose
            helper.log("calculating dose....");
            stopwatch.Restart();
            CalculationResult dose_result = ps.CalculateDose();
            stopwatch.Stop();
            helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

            if (!dose_result.Success)
            {
                vmsApp.ClosePatient();
                _err("Dose Calculation Failed");
            }

            vmsApp.SaveModifications();
            vmsApp.ClosePatient();

            // create a copy plan 
            //{
            //    string new_cs_Id = "_test1";
            //    string new_ps_Id = "plan2";
            //    string new_ptv_Id = "ptv_lump1_2mm_cropped";

            //    string clincial_info_file = filesystem.join(dir, "clinical_info.csv");
            //    string result_file = filesystem.join(dir, "create_plan_and_eval.result.csv");

            //    create_plan_and_eval(new_cs_Id, new_ps_Id,new_ptv_Id, clincial_info_file, result_file);
            //}

            // create a copy plan 
            //{
            //    string in_file = filesystem.join(dir, "create_plan_and_eval.result.csv");
            //    string out_file = in_file + ".with.dice.csv";
            //    append_dice(job_id, in_file, out_file);
            //}


            helper.log("done.");
        }

        public static VMSStructure make_ptv(VMSPatient pt, VMSStructureSet sset, VMSImage ct,
            VMSStructure s_bladder, VMSStructure s_rectum, VMSStructure s_bowel, double crop_by_body_inner_margin, double ptv_margin1_all, double ptv_margin2_inf, string ptv_id, Color ptv_color)
        {
            helper.log($"make_ptv()");

            /////////////////////////////////
            /// bladder (ctv), rectum, bowel
            Structure s_ctv = s_bladder;
            helper.print($"CTV={s_ctv.Id}");
            helper.print($"s_rectum={s_rectum.Id}");
            helper.print($"s_bowel{s_bowel.Id}");
            helper.print($"s_bladder={s_bladder.Id}");

            /////////////////
            // rectum + bowel
            Structure s_rectumbowel = find_or_add_s("ORGAN", "rectumbowel", sset, false);
            s_rectumbowel.SegmentVolume = s_rectum.SegmentVolume.Or(s_bowel.SegmentVolume);
            helper.print($"Created contour {s_rectumbowel.Id}");

            /////////
            /// Body
            Structure s_body = s_of_type_external(sset);
            if (s_body == null || s_body.IsEmpty)
            {
                _info("Body not found. Creating one...");
                SearchBodyParameters sbparam = sset.GetDefaultSearchBodyParameters();
                s_body = sset.CreateAndSearchBody(sbparam);
            }
            helper.print($"Found body {s_body.Id}");

            ////////
            // PTV
            Structure s_ptv = find_or_add_s("PTV", ptv_id, sset);
            if (s_ptv == null)
            {
                _err($"Could not find or add structure(Id={ptv_id})");
            }
            s_ptv.Color = ptv_color;
            helper.print($"PTV contour: {s_ptv.Id}");

            // ptv = ctv + margin_mm
            AxisAlignedMargins margin2 = new AxisAlignedMargins(StructureMarginGeometry.Outer, 
                0, // right 
                0, // ant
                ptv_margin2_inf, // inf
                0, // left
                0, // post
                0); // sup

            s_ptv.SegmentVolume = s_ctv.SegmentVolume
                .Margin(ptv_margin1_all)
                .AsymmetricMargin(margin2);
            helper.print($"PTV margin: added margins {ptv_margin1_all} mm all around and {ptv_margin2_inf} to INF.");

            // drop by body
            crop_by_body2(s_ptv, crop_by_body_inner_margin, sset);
            helper.print($"Cropped PTV by the body with inner marin of {crop_by_body_inner_margin}.");

            /// ptv = ptv - rectumbowel
            s_ptv.SegmentVolume = s_ptv.SegmentVolume.Sub(s_rectumbowel.SegmentVolume);
            helper.print($"Subtracted rectumbowel from PTV.");

            return s_ptv;
        }

        public static (Structure s_opti_rectum, Structure s_opti_bowel) make_opti_contours(VMSStructureSet sset, VMSStructure s_ptv, VMSStructure s_rectum, VMSStructure s_bowel, 
            string opti_rectum_id, string opti_bowel_id)
        {
            helper.log($"make_ptv_and_opti_contours()");

            // ptv+3cm
            SegmentVolume sv_ptv_3cm = s_ptv.SegmentVolume.Margin(30.0);

            ////////////////
            // _opti_rectum
            Structure s_opti_rectum = find_or_add_s("ORGAN", opti_rectum_id, sset);
            s_opti_rectum.SegmentVolume = sv_ptv_3cm.And(s_rectum.SegmentVolume);
            helper.print($"Created {s_opti_rectum.Id}.");

            ///////////////
            // _opti_bowel
            Structure s_opti_bowel = find_or_add_s("ORGAN", opti_bowel_id, sset);
            s_opti_bowel.SegmentVolume = sv_ptv_3cm.And(s_bowel.SegmentVolume);
            helper.print($"Created {s_opti_bowel.Id}.");

            return (s_opti_rectum, s_opti_bowel);
        }

        public static async Task create_bladder_plan3(
            VMSPatient pt,
            VMSStructureSet sset,
            VMSCourse course,
            string ps_id,
            string ptv_id,
            string bladder_id,
            string rectum_id,
            string opti_rectum_id, 
            string bowel_id,
            string opti_bowel_id,
            int num_fxs = 20,
            double dose_per_fx = 275.0
            )
        {
            _info("create_bladder_plan3()");

            if (pt == null)
            {
                _err($"Patient(Id={pt.Id}) is invalid");
                return;
            }

            if (sset == null)
            {
                _err($"StructureSet (Id={sset.Id}) is invalid");
                return;
            }

            VMSImage ct = sset.Image;
            if (ct == null)
            {
                _err($"ct (Id={ct.Id}) is invalid");
                return;
            }

            if (course == null)
            {
                _err($"Course (Id={course.Id}) is invalid");
                return;
            }

            double direction00 = ct.XDirection[0];
            double direction11 = ct.YDirection[1];
            double direction22 = ct.ZDirection[2];
            double prod = direction00 * direction11 * direction22;
            if (prod < 0.9999)
            {
                _err($"Image Tilted! direction product = {prod}");
                return;
            }

            double total_dose = num_fxs * dose_per_fx;

            ExternalBeamMachineParameters machine = new ExternalBeamMachineParameters("SIL 21IX", "6X", 600, "STATIC", "");

            
            // set default imaging device
            if (ct.Series.ImagingDeviceId == null || ct.Series.ImagingDeviceId == "")
                ct.Series.SetImagingDevice("CTAWP96967");

            //////////////
            /// ptv
            Structure s_ptv = s_of_id(ptv_id, sset);
            if (s_ptv == null)
            {
                _err($"Structure(Id={s_ptv}) not found");
            }
            _info($"s_ptv={s_ptv.Id}");


            //////////////
            /// bladder
            Structure s_bladder = s_of_id(bladder_id, sset);
            if (s_bladder == null)
            {
                _err($"Structure(Id={bladder_id}) not found");
            }
            _info($"s_ctv={s_bladder.Id}");

            ////////////
            /// rectum 
            Structure s_rectum = s_of_id(rectum_id, sset);
            if (s_rectum == null)
            {
                _err($"Structure(Id={rectum_id}) not found");
            }
            _info($"s_rectum={s_rectum.Id}");

            ////////////
            /// bowel
            Structure s_bowel = s_of_id(bowel_id, sset);
            if (s_bowel == null)
            {
                _err($"Structure(Id={rectum_id}) not found");
            }
            _info($"s_bowel={s_bowel.Id}");

           
            ///////////////////////
            /// rectum for opti
            Structure s_opti_rectum = find_or_add_s("ORGAN", opti_rectum_id, sset);
            if (s_opti_rectum == null)
            {
                _err($"Structure(Id=_opti_rectum) not found");
                return;
            }
            _info($"s_opti_rectum={s_opti_rectum.Id}");

            ///////////////////////
            /// bowel for opti
            Structure s_opti_bowel = find_or_add_s("ORGAN", opti_bowel_id, sset);
            if (s_opti_bowel == null)
            {
                _err($"Structure(Id=_opti_bowel) not found");
                return;
            }
            _info($"s_opti_bowel={s_opti_bowel.Id}");

            /////////
            /// Body
            Structure s_body = s_of_type_external(sset);
            if (s_body == null || s_body.IsEmpty)
            {
                _info("Body not found. Creating one...");
                SearchBodyParameters sbparam = sset.GetDefaultSearchBodyParameters();
                s_body = sset.CreateAndSearchBody(sbparam);
            }
            _info($"s_body={s_body.Id}");

            int num_beams = 7;
            int gantr_interval = 50;
            int gantry_start = 180 + 50 / 2;
            double[] Gs = new double[num_beams];
            for (int g = 0; g < num_beams; g++)
            {
                double gantry_angle = gantry_start + gantr_interval * g;
                if (gantry_angle >= 360)
                    gantry_angle -= 360;

                Gs[g] = gantry_angle;
            }

            ExternalPlanSetup ps = find_or_add_ext_ps(ps_id, sset, course);
            if (ps == null)
            {
                _err($"Plan (Id={ps_id}) - failed to create nor found.");
                return;
            }
            _info($"ps={ps.Id}");

            //new_ps.SetPrescription(ps.NumberOfFractions??0, ps.DosePerFraction, ps.TreatmentPercentage);
            ps.SetPrescription(num_fxs, new DoseValue(dose_per_fx, DoseValue.DoseUnit.cGy), 1.0);

            for (int b_index = 0; b_index < Gs.Length; b_index++)
            {
                VRect<double> jawPositions = new VRect<double>(-50, 50, -50, 50);
                double collimatorAngle = 30.0;
                double gantryAngle = Gs[b_index];
                double patientSupportAngle = 0.0;
                VVector isocenter = s_ptv.CenterPoint;

                string b_Id = $"{b_index}";
                //Beam new_b = new_ps.AddMLCBeam(machine, cpt0.LeafPositions, cpt0.JawPositions, cpt0.CollimatorAngle, cpt0.GantryAngle, cpt0.PatientSupportAngle, b.IsocenterPosition);
                Beam new_b = ps.AddStaticBeam(
                    machine,
                    jawPositions,
                    collimatorAngle,
                    gantryAngle,
                    patientSupportAngle,
                    isocenter
                    );
                new_b.Id = b_Id;

                //BeamParameters bp = new_b.GetEditableParameters();

                // fit Jaw to the new ptv
                double jaw_margin_mm = 20.0; // large enough for IMRT
                new_b.FitCollimatorToStructure(new FitToStructureMargins(jaw_margin_mm), s_ptv, true, true, false);

                // fit MLC to the new ptv
                //double mlc_margin_mm = 5.0;
                //new_b.FitMLCToStructure(new FitToStructureMargins(mlc_margin_mm), new_ptv, false, JawFitting.FitToStructure, OpenLeavesMeetingPoint.OpenLeavesMeetingPoint_Middle, ClosedLeavesMeetingPoint.ClosedLeavesMeetingPoint_BankOne);

                //new_b.Wedges.Add(new EnhancedDynamicWedge(new_b))
                // MU
                //mus.Add(b.Meterset.Value);
                //mu_list.Add(new KeyValuePair<string, MetersetValue>(new_b.Id, new MetersetValue(b.Meterset.Value, b.Meterset.Unit)));

                //m++;
            }

            // public void SetCalculationModel(
            ps.SetCalculationModel(CalculationType.PhotonOptimization, "PO_13623");
            ps.SetCalculationModel(CalculationType.PhotonVolumeDose, "AAA_13623");

            // optimization
            OptimizationSetup os = ps.OptimizationSetup;
            OptimizationNormalTissueParameter nto = os.AddAutomaticNormalTissueObjective(0.1);

            // dose values
            DoseValue dv_rx = new DoseValue(total_dose, DoseValue.DoseUnit.cGy);
            DoseValue dv_rx_110p = new DoseValue(total_dose * 1.1, DoseValue.DoseUnit.cGy);
            DoseValue dv_rx_50p = new DoseValue(total_dose * 0.5, DoseValue.DoseUnit.cGy);

            // PTV
            {
                os.AddPointObjective(s_ptv, OptimizationObjectiveOperator.Lower, dv_rx, 100.0, 100.0);
                os.AddPointObjective(s_ptv, OptimizationObjectiveOperator.Upper, dv_rx_110p, 0.0, 100.0);
            }

            // Rectum
            {
                os.AddPointObjective(s_opti_rectum, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
                os.AddPointObjective(s_opti_rectum, OptimizationObjectiveOperator.Upper, dv_rx_50p, 25.0, 100.0);
            }

            // Bowel
            {
                os.AddPointObjective(s_opti_bowel, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
                os.AddPointObjective(s_opti_bowel, OptimizationObjectiveOperator.Upper, dv_rx_50p, 50.0, 100.0);
            }

            os.AddAutomaticNormalTissueObjective(100.0);

            OptimizationOptionsIMRT optm_Options = new OptimizationOptionsIMRT(
                300,//int maxIterations,
                OptimizationOption.RestartOptimization,
                OptimizationConvergenceOption.TerminateIfConverged,
                OptimizationIntermediateDoseOption.NoIntermediateDose,
                "" //string mlcId
            );


            _info("optimizing...");

            Stopwatch stopwatch = new Stopwatch();
            // intial optimization
            stopwatch.Restart();
            OptimizerResult opt_result = ps.Optimize(optm_Options);
            stopwatch.Stop();
            _info($"finished. Optimization took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

            //// push down rectum bowel slightly 
            //OptimizationOptionsIMRT optm_Options2 = new OptimizationOptionsIMRT(
            //    100,//int maxIterations,
            //    OptimizationOption.ContinueOptimization,
            //    OptimizationConvergenceOption.TerminateIfConverged,
            //    OptimizationIntermediateDoseOption.NoIntermediateDose,
            //    "" //string mlcId
            //);

            //// get the volume of the 50% dose
            //os.AddPointObjective(s_opti_rectumbowel, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
            //ps.Optimize(optm_Options2);


            if (!opt_result.Success)
            {
                _err($"Optimization Failed: {opt_result.ToString()}");
                return;
            }

            // LMC
            _info("calculating LMC...");
            bool fixedJaw = true;
            LMCVOptions lmc_options = new LMCVOptions(fixedJaw);

            stopwatch.Restart();
            CalculationResult lmc_result = ps.CalculateLeafMotions(lmc_options);
            stopwatch.Stop();
            _info($"finished. LMC calculation took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

            if (!lmc_result.Success)
            {
                global.vmsApplication.ClosePatient();
                _err("LMC Calculation Failed");
            }

            // Dose
            _info("calculating dose....");
            stopwatch.Restart();
            CalculationResult dose_result = ps.CalculateDose();
            stopwatch.Stop();
            helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

            if (!dose_result.Success)
            {
                _err("Dose Calculation Failed");
            }

            // create a copy plan 
            //{
            //    string new_cs_Id = "_test1";
            //    string new_ps_Id = "plan2";
            //    string new_ptv_Id = "ptv_lump1_2mm_cropped";

            //    string clincial_info_file = filesystem.join(dir, "clinical_info.csv");
            //    string result_file = filesystem.join(dir, "create_plan_and_eval.result.csv");

            //    create_plan_and_eval(new_cs_Id, new_ps_Id,new_ptv_Id, clincial_info_file, result_file);
            //}

            // create a copy plan 
            //{
            //    string in_file = filesystem.join(dir, "create_plan_and_eval.result.csv");
            //    string out_file = in_file + ".with.dice.csv";
            //    append_dice(job_id, in_file, out_file);
            //}
        }

        

        public static async Task<ExternalPlanSetup> create_bladder_plan4(
            VMSPatient pt,
            VMSStructureSet sset,
            VMSCourse course,
            VMSReferencePoint primary_reference_point,
            ExternalBeamMachineParameters machine,
            string ps_id,
            string ptv_id,
            string bladder_id,
            string rectum_id,
            string opti_rectum_id,
            string bowel_id,
            string opti_bowel_id,
            int num_beams,
            int num_fxs,
            double dose_per_fx,
            string default_imaging_device_id,
            string optimization_model,
            string volume_dose_calculation_model,
            bool use_intermediate_dose_calculation,
            bool use_jaw_tracking,
            int task_delay_milliseconds
            )
        {
            _info("create_bladder_plan3()");

            if (pt == null)
            {
                _err($"Patient is null");
            }

            if (sset == null)
            {
                _err($"StructureSet is null");
            }

            VMSImage ct = sset.Image;
            if (ct == null)
            {
                _err($"image is null");
            }

            if (course == null)
            {
                _err($"Course is null");
                return null;
            }

            double direction00 = ct.XDirection[0];
            double direction11 = ct.YDirection[1];
            double direction22 = ct.ZDirection[2];
            double prod = direction00 * direction11 * direction22;
            if (prod < 0.9999)
            {
                _err($"Image Tilted! direction product = {prod}");
            }

            double total_dose = num_fxs * dose_per_fx;

            


            // set default imaging device
            if (ct.Series.ImagingDeviceId == null || ct.Series.ImagingDeviceId == "")
            {
                _info($"The image(series) does not have imaging device id set. Setting the given default imaging device id ({default_imaging_device_id})");
                ct.Series.SetImagingDevice(default_imaging_device_id);
            }

            //////////////
            /// ptv
            Structure s_ptv = s_of_id(ptv_id, sset);
            if (s_ptv == null)
            {
                _err($"Structure(Id={ptv_id}) not found");
            }
            _info($"s_ptv={s_ptv.Id}");


            //////////////
            /// bladder
            Structure s_bladder = s_of_id(bladder_id, sset);
            if (s_bladder == null)
            {
                _err($"Structure(Id={bladder_id}) not found");
            }
            _info($"s_bladder(ctv)={s_bladder.Id}");

            ////////////
            /// rectum 
            Structure s_rectum = s_of_id(rectum_id, sset);
            if (s_rectum == null)
            {
                _err($"Structure(Id={rectum_id}) not found");
            }
            _info($"s_rectum={s_rectum.Id}");

            ////////////
            /// bowel
            Structure s_bowel = s_of_id(bowel_id, sset);
            if (s_bowel == null)
            {
                _err($"Structure(Id={rectum_id}) not found");
            }
            _info($"s_bowel={s_bowel.Id}");


            ///////////////////////
            /// rectum for opti
            Structure s_opti_rectum = s_of_id(opti_rectum_id, sset);
            if (s_opti_rectum == null)
            {
                _err($"Structure(Id={opti_rectum_id}) not found");
                return null;
            }
            _info($"s_opti_rectum={s_opti_rectum.Id}");

            ///////////////////////
            /// bowel for opti
            Structure s_opti_bowel = s_of_id(opti_bowel_id, sset);
            if (s_opti_bowel == null)
            {
                _err($"Structure(Id={opti_bowel_id}) not found");
                return null;
            }
            _info($"s_opti_bowel={s_opti_bowel.Id}");

            /////////
            /// Body
            Structure s_body = s_of_type_external(sset);
            if (s_body == null || s_body.IsEmpty)
            {
                _info("Body not found. Creating one...");
                SearchBodyParameters sbparam = sset.GetDefaultSearchBodyParameters();
                s_body = sset.CreateAndSearchBody(sbparam);
            }
            _info($"s_body={s_body.Id}");


            // beams (gantry angles)
            int gantr_interval = (int)(360.0/num_beams);
            int gantry_start = 180 + (int)(gantr_interval/2);
            double[] Gs = new double[num_beams];
            for (int g = 0; g < num_beams; g++)
            {
                double gantry_angle = gantry_start - gantr_interval * g;
                if (gantry_angle < 0)
                    gantry_angle += 360;
                Gs[g] = gantry_angle;
            }
            _info($"Gantry angels={string.Join(", ", Gs)}");

            helper.log($"Adding ExternalPlanSetup[Id={ps_id}]");
            ExternalPlanSetup ps = course.AddExternalPlanSetup(sset, s_ptv, primary_reference_point );
            if (ps == null)
            {
                _err($"Plan (Id={ps_id}) - failed to create nor find plan.");
            }
            ps.Id = ps_id;

            _info($"ps={ps.Id}");
            //new_ps.SetPrescription(ps.NumberOfFractions??0, ps.DosePerFraction, ps.TreatmentPercentage);
            ps.SetPrescription(num_fxs, new DoseValue(dose_per_fx, DoseValue.DoseUnit.cGy), 1.0);

            
            VVector isocenter = s_ptv.CenterPoint;

            _info("Adding Beams...");
            for (int b_index = 0; b_index < Gs.Length; b_index++)
            {
                VRect<double> jawPositions = new VRect<double>(-50, -50, 50, 50);
                double collimatorAngle = 30.0;
                double gantryAngle = Gs[b_index];
                double patientSupportAngle = 0.0;

                string b_Id = $"{b_index}";
                //Beam new_b = new_ps.AddMLCBeam(machine, cpt0.LeafPositions, cpt0.JawPositions, cpt0.CollimatorAngle, cpt0.GantryAngle, cpt0.PatientSupportAngle, b.IsocenterPosition);
                Console.WriteLine($"b_Id={b_Id}");
                Console.WriteLine($"machine={machine}");
                Console.WriteLine($"jawPositions={jawPositions}");
                Console.WriteLine($"collimatorAngle={collimatorAngle}");
                Console.WriteLine($"gantryAngle={gantryAngle}");
                Console.WriteLine($"patientSupportAngle={patientSupportAngle}");
                Console.WriteLine($"isocenter=[{isocenter[0]},{isocenter[1]},{isocenter[2]}");
                Beam new_b = ps.AddStaticBeam(
                    machine,
                    jawPositions,
                    collimatorAngle,
                    gantryAngle,
                    patientSupportAngle,
                    isocenter
                    );
                new_b.Id = b_Id;

                //BeamParameters bp = new_b.GetEditableParameters();


                // fit Jaw to the new ptv
                double jaw_margin_mm = 10.0; // large enough for IMRT
                helper.log($"Fitting Jaw to the target with margin {jaw_margin_mm} mm");
                new_b.FitCollimatorToStructure(new FitToStructureMargins(jaw_margin_mm), s_ptv, true, true, false);

                // fit MLC to the new ptv
                //double mlc_margin_mm = 5.0;
                //new_b.FitMLCToStructure(new FitToStructureMargins(mlc_margin_mm), new_ptv, false, JawFitting.FitToStructure, OpenLeavesMeetingPoint.OpenLeavesMeetingPoint_Middle, ClosedLeavesMeetingPoint.ClosedLeavesMeetingPoint_BankOne);

                //new_b.Wedges.Add(new EnhancedDynamicWedge(new_b))
                // MU
                //mus.Add(b.Meterset.Value);
                //mu_list.Add(new KeyValuePair<string, MetersetValue>(new_b.Id, new MetersetValue(b.Meterset.Value, b.Meterset.Unit)));

                //m++;
            }

            // add setup beams
            helper.log("Adding setup beams...");
            add_setup_beam(ps, "CBCT", 0.0, isocenter, machine);
            add_setup_beam(ps, "AP", 0.0, isocenter, machine);
            add_setup_beam(ps, "PA", 180.0, isocenter, machine);
            add_setup_beam(ps, "LT", 90.0, isocenter, machine);
            add_setup_beam(ps, "RT", 270.0, isocenter, machine);

            // add drr
            helper.log("Adding DRRs...");
            foreach (Beam b in ps.Beams)
                add_bony_drr(b);


            // public void SetCalculationModel(
            //_info("Setting calculation models...");
            //ps.SetCalculationModel(CalculationType.PhotonOptimization, optimization_model);
            //ps.SetCalculationModel(CalculationType.PhotonVolumeDose, volume_dose_calculation_model);

            // just use default LMC (?)
            //ps.SetCalculationModel(CalculationType.PhotonLeafMotions, "Smart LMC [13.6.23]");

            _info("setting up optimization...");

            // optimization
            OptimizationSetup os = ps.OptimizationSetup;

            //_info($"use_jaw_tracking={use_jaw_tracking}");
            //os.UseJawTracking = use_jaw_tracking;
            
            //OptimizationNormalTissueParameter nto = os.AddAutomaticNormalTissueObjective(0.1);

            // dose values
            DoseValue dv_rx = new DoseValue(total_dose, DoseValue.DoseUnit.cGy);
            DoseValue dv_rx_115p = new DoseValue(total_dose * 1.15, DoseValue.DoseUnit.cGy);
            DoseValue dv_rx_50p = new DoseValue(total_dose * 0.5, DoseValue.DoseUnit.cGy);

            // PTV
            {
                os.AddPointObjective(s_ptv, OptimizationObjectiveOperator.Lower, dv_rx, 100.0, 100.0);
                os.AddPointObjective(s_ptv, OptimizationObjectiveOperator.Lower, dv_rx, 95.0, 200.0);
                os.AddPointObjective(s_ptv, OptimizationObjectiveOperator.Upper, dv_rx_115p, 0.0, 100.0);
            }

            // opti Rectum
            {
                os.AddPointObjective(s_opti_rectum, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
                os.AddPointObjective(s_opti_rectum, OptimizationObjectiveOperator.Upper, dv_rx_50p, 50.0, 50.0);
            }

            // opti Bowel
            {
                os.AddPointObjective(s_opti_bowel, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
                os.AddPointObjective(s_opti_bowel, OptimizationObjectiveOperator.Upper, dv_rx_50p, 50.0, 50.0);
            }

            os.AddAutomaticNormalTissueObjective(100.0);

            OptimizationOptionsIMRT optm_Options = null;
            if (use_intermediate_dose_calculation)
            {
                optm_Options = new OptimizationOptionsIMRT(
                    300,//int maxIterations,
                    OptimizationOption.RestartOptimization,
                    OptimizationConvergenceOption.TerminateIfConverged,
                    OptimizationIntermediateDoseOption.UseIntermediateDose,
                    "" //string mlcId
                        );
            }
            else
            {
                optm_Options = new OptimizationOptionsIMRT(
                    300,//int maxIterations,
                    OptimizationOption.RestartOptimization,
                    OptimizationConvergenceOption.TerminateIfConverged,
                    OptimizationIntermediateDoseOption.NoIntermediateDose,
                    "" //string mlcId
                        );
            }




            _info("optimizing...");
            await Task.Delay(task_delay_milliseconds);

            Stopwatch stopwatch = new Stopwatch();
            // intial optimization
            stopwatch.Restart();
            OptimizerResult opt_result = ps.Optimize(optm_Options);
            stopwatch.Stop();
            _info($"finished. Optimization took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");
            await Task.Delay(task_delay_milliseconds);

            //// push down rectum bowel slightly 
            //OptimizationOptionsIMRT optm_Options2 = new OptimizationOptionsIMRT(
            //    100,//int maxIterations,
            //    OptimizationOption.ContinueOptimization,
            //    OptimizationConvergenceOption.TerminateIfConverged,
            //    OptimizationIntermediateDoseOption.NoIntermediateDose,
            //    "" //string mlcId
            //);

            //// get the volume of the 50% dose
            //os.AddPointObjective(s_opti_rectumbowel, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
            //ps.Optimize(optm_Options2);

            if (!opt_result.Success)
            {
                _err($"Optimization Failed: {opt_result.ToString()}");
            }

            // LMC
            _info("calculating LMC...");
            await Task.Delay(task_delay_milliseconds);

            //bool fixedJaw = true;
            //LMCVOptions lmc_options = new LMCVOptions(fixedJaw);
            

            stopwatch.Restart();

            CalculationResult lmc_result;
            try
            {
                _info("trying SmartLMC options...");
                bool fixedFieldBorders = false;
                _info($"fixedFieldBorders={fixedFieldBorders}");
                _info($"use_jaw_tracking={use_jaw_tracking}");
                lmc_result = ps.CalculateLeafMotions(new SmartLMCOptions(fixedFieldBorders, use_jaw_tracking));
            }
            catch (Exception ex)
            {
                bool fixedJaws = !use_jaw_tracking;
                _info("didn't work. trying LMCVOptions...");
                _info($"fixedJaws={fixedJaws}");
                lmc_result = ps.CalculateLeafMotions(new LMCVOptions(fixedJaws));
            }

            stopwatch.Stop();
            _info($"finished. LMC calculation took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");
            await Task.Delay(task_delay_milliseconds);

            if (!lmc_result.Success)
            {
                //global.vmsApplication.ClosePatient();
                _err("LMC Calculation Failed");
            }

            // Dose
            _info("calculating dose....");
            await Task.Delay(task_delay_milliseconds);

            stopwatch.Restart();
            CalculationResult dose_result = ps.CalculateDose();
            stopwatch.Stop();
            helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");
            await Task.Delay(task_delay_milliseconds);

            if (!dose_result.Success)
            {
                _err("Dose Calculation Failed");
            }

            _info("Done.");
            return ps;
        }

        public static void create_bladder_plan2(
            string pid,
            string image_id,
            string image_FOR,
            string bladder_id,
            string rectum_bowel_id,
            int ptv_margin_mm,
            double[] Gs,
            string cs_id,
            string ps_id,
            int num_fxs = 20,
            double dose_per_fx = 275.0
            )
        {
            _info($"pid={pid}, ps_id={ps_id}");

            Patient pt = vmsApp.OpenPatientById(pid);
            if (pt == null)
            {
                _err($"Patient(Id={pid}) not found");
            }

            VMS.TPS.Common.Model.API.Image ct = image_of_id_FOR(image_id, image_FOR, pt);
            //VMS.TPS.Common.Model.API.Image ct = first_image_of_id(image_id, pt);

            if (ct == null)
            {
                _err($"image not found:{pid}->({image_id},{image_FOR}). skipping...");
            }

            double direction00 = ct.XDirection[0];
            double direction11 = ct.YDirection[1];
            double direction22 = ct.ZDirection[2];
            double prod = direction00 * direction11 * direction22;
            if (prod < 0.9999)
            {
                _err($"Image Tilted! direction product = {prod}");
            }



            if (ct.Series.ImagingDeviceId == null || ct.Series.ImagingDeviceId == "")
                ct.Series.SetImagingDevice("CTAWP96967");

            double total_dose = num_fxs * dose_per_fx;

            StringBuilder sb = new StringBuilder();
            //string sset_id = image_id; // image ID and structureset ID are same

            Color ptv_color = Color.FromRgb(255, 0, 0);
            double body_inner_margin_for_crop = 5.0;

            string ptv_Id = $"ptv_bldr_{ptv_margin_mm}mm";

            ExternalBeamMachineParameters machine = new ExternalBeamMachineParameters("SIL 21IX", "6X", 600, "STATIC", "");

            // check if the plan exists
            if (cs_of_id(cs_id, pt) != null && ps_of_id(ps_id, pt) != null)
            {
                _info($"plan already exists: {pt.Id}->{cs_id}->{ps_id}. skipping...");
                vmsApp.ClosePatient();
                return;
            }


            pt.BeginModifications();

            ///////
            // sset
            StructureSet[] sset_list = sset_list_of_image_id_FOR(image_id, image_FOR, pt).ToArray();
            if (sset_list.Length == 0)
            {
                _err($"StructureSet not found!");
            }
            StructureSet sset = sset_list[0];

            if (sset.Image.Series.ImagingDeviceId == null || sset.Image.Series.ImagingDeviceId == "")
                sset.Image.Series.SetImagingDevice("CTAWP96967");

            //////////////////////////
            /// s_ctv is the bladder
            Structure s_ctv = s_of_id(bladder_id, sset);
            if (s_ctv == null)
            {
                _err($"Structure(Id={bladder_id}) not found");
            }
            sb.Append($",{s_ctv.Id}");

            ///////////////////////
            /// rectum bowel
            Structure s_rectumbowel = s_of_id(rectum_bowel_id, sset);
            if (s_rectumbowel == null)
            {
                _err($"Structure(Id={rectum_bowel_id}) not found");
            }
            sb.Append($",{rectum_bowel_id}");


            ///////////////////////
            /// rectum for opti
            Structure s_opti_rectum = find_or_add_s("ORGAN", "_opti_rectum", sset);
            if (s_opti_rectum == null)
            {
                _err($"Structure(Id=_opti_rectum) not found");
            }
            AxisAlignedMargins margin_post_5cm = new AxisAlignedMargins(StructureMarginGeometry.Outer, 0, 0, 0, 0, 50, 0);
            SegmentVolume sv_10cm = s_ctv.SegmentVolume.AsymmetricMargin(margin_post_5cm).AsymmetricMargin(margin_post_5cm); // bladder + 10cm post
            SegmentVolume sv_opti_rectum = s_rectumbowel.SegmentVolume.And(sv_10cm); // rectum opti = recturm bladder in the bladder + 10cm post
            SegmentVolume sv_2cm = s_ctv.SegmentVolume.Margin(20.0); // bladder + 2cm
            s_opti_rectum.SegmentVolume = sv_opti_rectum.Sub(sv_2cm); // rectum opti = rectum opti - (bladder + 2cm)


            sb.Append($",_opti_rectum");

            vmsApp.SaveModifications();

            /////////
            /// Body
            Structure s_body = s_of_type_external(sset);
            if (s_body == null || s_body.IsEmpty)
            {
                _info("Body not found. Creating one...");
                SearchBodyParameters sbparam = sset.GetDefaultSearchBodyParameters();
                s_body = sset.CreateAndSearchBody(sbparam);
            }

            ////////
            // PTV
            Structure s_ptv = find_or_add_s("PTV", ptv_Id, sset);
            if (s_ptv == null)
            {
                _err($"Could not find or add structure(Id={ptv_Id})");
            }
            s_ptv.Color = ptv_color;
            sb.Append($",{s_ptv.Id}");

            // ptv = ctv + margin_mm
            s_ptv.SegmentVolume = s_ctv.SegmentVolume.Margin(ptv_margin_mm);
            sb.Append($",{ptv_margin_mm}");


            crop_by_body2(s_ptv, body_inner_margin_for_crop, sset);

            ///////////////////////////////////////////////////
            //// rectumbowel_m_bladder = rectum_bowel - bladder
            Structure s_rectumbowel_m_bladder = find_or_add_s("ORGAN", "rectumbowel_m_bladder", sset);
            s_rectumbowel_m_bladder.SegmentVolume = s_rectumbowel.SegmentVolume.Sub(s_ctv.SegmentVolume);

            ///////////////////////////
            // rectum_bowel for opti = s_rectumbowel_m_bladder and ptv+3cm
            Structure s_opti_rectumbowel = find_or_add_s("ORGAN", "_opti_rectumbowel", sset);
            s_opti_rectumbowel.SegmentVolume = s_ptv.SegmentVolume.Margin(30.0).And(s_rectumbowel_m_bladder.SegmentVolume);

            ///////////////////////////////////////
            /// subtract rectumbowel from the ptv
            /// ptv = ptv - rectumbowel_m_bladder
            s_ptv.SegmentVolume = s_ptv.SegmentVolume.Sub(s_rectumbowel_m_bladder.SegmentVolume);

            Course cs = find_or_add_cs(cs_id, pt);
            if (cs == null)
                _err($"Course(Id={cs_id}) not found nor create it.");

            ExternalPlanSetup ps = find_or_add_ext_ps(ps_id, sset, cs);

            //new_ps.SetPrescription(ps.NumberOfFractions??0, ps.DosePerFraction, ps.TreatmentPercentage);
            ps.SetPrescription(num_fxs, new DoseValue(dose_per_fx, DoseValue.DoseUnit.cGy), 1.0);

            for (int b_index = 0; b_index < Gs.Length; b_index++)
            {
                VRect<double> jawPositions = new VRect<double>(5, 5, 5, 5);
                double collimatorAngle = 30.0;
                double gantryAngle = Gs[b_index];
                double patientSupportAngle = 0.0;
                VVector isocenter = s_ctv.CenterPoint;

                string b_Id = $"{b_index}";
                //Beam new_b = new_ps.AddMLCBeam(machine, cpt0.LeafPositions, cpt0.JawPositions, cpt0.CollimatorAngle, cpt0.GantryAngle, cpt0.PatientSupportAngle, b.IsocenterPosition);
                Beam new_b = ps.AddStaticBeam(
                    machine,
                    jawPositions,
                    collimatorAngle,
                    gantryAngle,
                    patientSupportAngle,
                    isocenter
                    );
                new_b.Id = b_Id;

                //BeamParameters bp = new_b.GetEditableParameters();

                // fit Jaw to the new ptv
                double jaw_margin_mm = 20.0; // large enough for IMRT
                new_b.FitCollimatorToStructure(new FitToStructureMargins(jaw_margin_mm), s_ptv, true, true, false);

                // fit MLC to the new ptv
                //double mlc_margin_mm = 5.0;
                //new_b.FitMLCToStructure(new FitToStructureMargins(mlc_margin_mm), new_ptv, false, JawFitting.FitToStructure, OpenLeavesMeetingPoint.OpenLeavesMeetingPoint_Middle, ClosedLeavesMeetingPoint.ClosedLeavesMeetingPoint_BankOne);

                //new_b.Wedges.Add(new EnhancedDynamicWedge(new_b))
                // MU
                //mus.Add(b.Meterset.Value);
                //mu_list.Add(new KeyValuePair<string, MetersetValue>(new_b.Id, new MetersetValue(b.Meterset.Value, b.Meterset.Unit)));

                //m++;
            }

            // public void SetCalculationModel(
            ps.SetCalculationModel(CalculationType.PhotonOptimization, "PO_13623");
            ps.SetCalculationModel(CalculationType.PhotonVolumeDose, "AAA_13623");

            // optimization
            OptimizationSetup os = ps.OptimizationSetup;
            OptimizationNormalTissueParameter nto = os.AddAutomaticNormalTissueObjective(0.1);

            // dose values
            DoseValue dv_rx = new DoseValue(total_dose, DoseValue.DoseUnit.cGy);
            DoseValue dv_rx_110p = new DoseValue(total_dose * 1.1, DoseValue.DoseUnit.cGy);
            DoseValue dv_rx_50p = new DoseValue(total_dose * 0.5, DoseValue.DoseUnit.cGy);

            // PTV
            {
                os.AddPointObjective(s_ptv, OptimizationObjectiveOperator.Lower, dv_rx, 100.0, 100.0);
                os.AddPointObjective(s_ptv, OptimizationObjectiveOperator.Upper, dv_rx_110p, 0.0, 100.0);
            }
            // Rectum Bowel
            {
                os.AddPointObjective(s_opti_rectumbowel, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
            }

            // Rectum Bowel
            {
                os.AddPointObjective(s_opti_rectumbowel, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
                os.AddPointObjective(s_opti_rectumbowel, OptimizationObjectiveOperator.Upper, dv_rx_50p, 50.0, 100.0);
            }

            // Rectum
            {
                os.AddPointObjective(s_opti_rectum, OptimizationObjectiveOperator.Upper, dv_rx_50p, 25.0, 100.0);
            }


            os.AddAutomaticNormalTissueObjective(100.0);

            OptimizationOptionsIMRT optm_Options = new OptimizationOptionsIMRT(
                300,//int maxIterations,
                OptimizationOption.RestartOptimization,
                OptimizationConvergenceOption.TerminateIfConverged,
                OptimizationIntermediateDoseOption.NoIntermediateDose,
                "" //string mlcId
            );


            helper.log("optimizing...");
            Stopwatch stopwatch = new Stopwatch();
            // intial optimization
            stopwatch.Restart();
            OptimizerResult opt_result = ps.Optimize(optm_Options);
            stopwatch.Stop();
            helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

            //// push down rectum bowel slightly 
            //OptimizationOptionsIMRT optm_Options2 = new OptimizationOptionsIMRT(
            //    100,//int maxIterations,
            //    OptimizationOption.ContinueOptimization,
            //    OptimizationConvergenceOption.TerminateIfConverged,
            //    OptimizationIntermediateDoseOption.NoIntermediateDose,
            //    "" //string mlcId
            //);

            //// get the volume of the 50% dose
            //os.AddPointObjective(s_opti_rectumbowel, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
            //ps.Optimize(optm_Options2);


            if (!opt_result.Success)
            {
                vmsApp.ClosePatient();
                _err($"Optimization Failed: opt_result.ToString()");
            }

            // LMC
            helper.log("calculating LMC...");
            bool fixedJaw = true;
            LMCVOptions lmc_options = new LMCVOptions(fixedJaw);

            stopwatch.Restart();
            CalculationResult lmc_result = ps.CalculateLeafMotions(lmc_options);
            stopwatch.Stop();
            helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

            if (!lmc_result.Success)
            {
                vmsApp.ClosePatient();
                _err("LMC Calculation Failed");
            }

            vmsApp.SaveModifications();

            // Dose
            helper.log("calculating dose....");
            stopwatch.Restart();
            CalculationResult dose_result = ps.CalculateDose();
            stopwatch.Stop();
            helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

            if (!dose_result.Success)
            {
                vmsApp.ClosePatient();
                _err("Dose Calculation Failed");
            }

            vmsApp.SaveModifications();
            vmsApp.ClosePatient();

            // create a copy plan 
            //{
            //    string new_cs_Id = "_test1";
            //    string new_ps_Id = "plan2";
            //    string new_ptv_Id = "ptv_lump1_2mm_cropped";

            //    string clincial_info_file = filesystem.join(dir, "clinical_info.csv");
            //    string result_file = filesystem.join(dir, "create_plan_and_eval.result.csv");

            //    create_plan_and_eval(new_cs_Id, new_ps_Id,new_ptv_Id, clincial_info_file, result_file);
            //}

            // create a copy plan 
            //{
            //    string in_file = filesystem.join(dir, "create_plan_and_eval.result.csv");
            //    string out_file = in_file + ".with.dice.csv";
            //    append_dice(job_id, in_file, out_file);
            //}


            helper.log("done.");
        }



        public static void create_bladder_plan(
            string pid,
            string image_id,
            string image_FOR,
            string bladder_id,
            string rectum_bowel_id,
            int ptv_margin_mm,
            double[] Gs,
            string cs_id,
            string ps_id,
            int num_fxs=32,
            double dose_per_fx=200.0
            )
        {
            Patient pt = vmsApp.OpenPatientById(pid);
            if (pt == null)
            {
                _err($"Patient(Id={pid}) not found");
            }

            VMS.TPS.Common.Model.API.Image ct = image_of_id_FOR(image_id, image_FOR, pt);
            //VMS.TPS.Common.Model.API.Image ct = first_image_of_id(image_id, pt);

            if (ct == null)
            {
                _err($"image not found:{pid}->({image_id},{image_FOR}). skipping...");
            }

            double direction00 = ct.XDirection[0];
            double direction11 = ct.YDirection[1];
            double direction22 = ct.ZDirection[2];
            double prod = direction00 * direction11 * direction22 ;
            if (prod<0.9999)
            {
                _err($"Image Tilted! direction product = {prod}");
            }


            double total_dose = num_fxs * dose_per_fx;

            StringBuilder sb = new StringBuilder();
            //string sset_id = image_id; // image ID and structureset ID are same

            Color ptv_color = Color.FromRgb(255, 0, 0);
            double body_inner_margin_for_crop = 5.0;

            string ptv_Id = $"ptv_bldr_{ptv_margin_mm}mm";

            ExternalBeamMachineParameters machine = new ExternalBeamMachineParameters("SIL 21IX", "6X", 600, "STATIC", "");

            

            // check if the plan exists
            if (cs_of_id(cs_id, pt) !=null && ps_of_id(ps_id, pt) != null)
            {
                _info($"plan already exists: {pt.Id}->{cs_id}->{ps_id}. skipping...");
                vmsApp.ClosePatient();
                return;
            }


            pt.BeginModifications();

            ///////
            // sset
            StructureSet[] sset_list = sset_list_of_image_id_FOR(image_id, image_FOR, pt).ToArray();
            if (sset_list.Length == 0)
            {
                _err($"StructureSet not found!");
            }
            StructureSet sset = sset_list[0];

            if(sset.Image.Series.ImagingDeviceId == null || sset.Image.Series.ImagingDeviceId == "")
                sset.Image.Series.SetImagingDevice("CTAWP96967");
            
            //////////////////////////
            /// s_ctv is the bladder
            Structure s_ctv= s_of_id(bladder_id, sset);
            if (s_ctv == null)
            {
                _err($"Structure(Id={bladder_id}) not found");
            }
            sb.Append($",{s_ctv.Id}");

            ///////////////////////
            /// rectum bowel
            Structure s_rectumbowel = s_of_id(rectum_bowel_id, sset);
            if (s_rectumbowel == null)
            {
                _err($"Structure(Id={rectum_bowel_id}) not found");
            }
            sb.Append($",{rectum_bowel_id}");


            /////////
            /// Body
            Structure s_body = s_of_type_external(sset);
            if (s_body == null || s_body.IsEmpty)
            {
                _info("Body not found. Creating one...");
                SearchBodyParameters sbparam = sset.GetDefaultSearchBodyParameters();
                s_body = sset.CreateAndSearchBody(sbparam);
            }

            ////////
            // PTV
            Structure s_ptv = find_or_add_s("PTV", ptv_Id, sset);
            if (s_ptv == null)
            {
                _err($"Could not find or add structure(Id={ptv_Id})");
            }
            s_ptv.Color = ptv_color;
            sb.Append($",{s_ptv.Id}");

            // ptv = ctv + margin_mm
            s_ptv.SegmentVolume = s_ctv.SegmentVolume.Margin(ptv_margin_mm);
            sb.Append($",{ptv_margin_mm}");

           
            crop_by_body2(s_ptv, body_inner_margin_for_crop, sset);

            ///////////////////////////////////////////////////
            //// rectumbowel_m_bladder = rectum_bowel - bladder
            Structure s_rectumbowel_m_bladder = find_or_add_s("ORGAN", "rectumbowel_m_bladder", sset);
            s_rectumbowel_m_bladder.SegmentVolume = s_rectumbowel.SegmentVolume.Sub(s_ctv.SegmentVolume);

            ///////////////////////////
            // rectum_bowel for opti = s_rectumbowel_m_bladder and ptv+3cm
            Structure s_opti_rectumbowel = find_or_add_s("ORGAN", "_opti_rectumbowel", sset);
            s_opti_rectumbowel.SegmentVolume = s_ptv.SegmentVolume.Margin(30.0).And(s_rectumbowel_m_bladder.SegmentVolume);

            ///////////////////////////////////////
            /// subtract rectumbowel from the ptv
            /// ptv = ptv - rectumbowel_m_bladder
            s_ptv.SegmentVolume = s_ptv.SegmentVolume.Sub(s_rectumbowel_m_bladder.SegmentVolume);

            Course cs = find_or_add_cs(cs_id, pt);
            if (cs == null)
                _err($"Course(Id={cs_id}) not found nor create it.");

            ExternalPlanSetup ps = find_or_add_ext_ps(ps_id, sset, cs);

            //new_ps.SetPrescription(ps.NumberOfFractions??0, ps.DosePerFraction, ps.TreatmentPercentage);
            ps.SetPrescription(num_fxs, new DoseValue(dose_per_fx, DoseValue.DoseUnit.cGy), 1.0);

            for (int b_index=0; b_index<Gs.Length; b_index++)
            {
                VRect<double> jawPositions = new VRect<double>(5,5,5,5);
                double collimatorAngle = 30.0;
                double gantryAngle = Gs[b_index];
                double patientSupportAngle = 0.0;
                VVector isocenter = s_ctv.CenterPoint;

                string b_Id = $"{b_index}";
                //Beam new_b = new_ps.AddMLCBeam(machine, cpt0.LeafPositions, cpt0.JawPositions, cpt0.CollimatorAngle, cpt0.GantryAngle, cpt0.PatientSupportAngle, b.IsocenterPosition);
                Beam new_b = ps.AddStaticBeam(
                    machine, 
                    jawPositions, 
                    collimatorAngle, 
                    gantryAngle, 
                    patientSupportAngle, 
                    isocenter
                    );
                new_b.Id = b_Id;

                //BeamParameters bp = new_b.GetEditableParameters();

                // fit Jaw to the new ptv
                double jaw_margin_mm = 20.0; // large enough for IMRT
                new_b.FitCollimatorToStructure(new FitToStructureMargins(jaw_margin_mm), s_ptv, true, true, false);

                // fit MLC to the new ptv
                //double mlc_margin_mm = 5.0;
                //new_b.FitMLCToStructure(new FitToStructureMargins(mlc_margin_mm), new_ptv, false, JawFitting.FitToStructure, OpenLeavesMeetingPoint.OpenLeavesMeetingPoint_Middle, ClosedLeavesMeetingPoint.ClosedLeavesMeetingPoint_BankOne);

                //new_b.Wedges.Add(new EnhancedDynamicWedge(new_b))
                // MU
                //mus.Add(b.Meterset.Value);
                //mu_list.Add(new KeyValuePair<string, MetersetValue>(new_b.Id, new MetersetValue(b.Meterset.Value, b.Meterset.Unit)));

                //m++;
            }

            // public void SetCalculationModel(
            ps.SetCalculationModel(CalculationType.PhotonOptimization, "PO_13623");
            ps.SetCalculationModel(CalculationType.PhotonVolumeDose, "AAA_13623");

            // optimization
            OptimizationSetup os = ps.OptimizationSetup;
            OptimizationNormalTissueParameter nto = os.AddAutomaticNormalTissueObjective(0.1);

            // dose values
            DoseValue dv_rx = new DoseValue(total_dose, DoseValue.DoseUnit.cGy);
            DoseValue dv_rx_110p = new DoseValue(total_dose * 1.1, DoseValue.DoseUnit.cGy);
            DoseValue dv_rx_50p = new DoseValue(total_dose * 0.5, DoseValue.DoseUnit.cGy);

            // PTV
            {
                os.AddPointObjective(s_ptv, OptimizationObjectiveOperator.Lower, dv_rx, 100.0, 100.0);
                os.AddPointObjective(s_ptv, OptimizationObjectiveOperator.Upper, dv_rx_110p, 0.0, 100.0);
            }
            // Rectum Bowel
            {
                os.AddPointObjective(s_opti_rectumbowel, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
            }

            os.AddAutomaticNormalTissueObjective(100.0);

            OptimizationOptionsIMRT optm_Options = new OptimizationOptionsIMRT(
                300,//int maxIterations,
                OptimizationOption.RestartOptimization,
                OptimizationConvergenceOption.TerminateIfConverged,
                OptimizationIntermediateDoseOption.NoIntermediateDose,
                "" //string mlcId
            );

                
            helper.log("optimizing...");
            Stopwatch stopwatch = new Stopwatch();
            // intial optimization
            stopwatch.Restart();
            OptimizerResult opt_result = ps.Optimize(optm_Options);
            stopwatch.Stop();
            helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

            //// push down rectum bowel slightly 
            //OptimizationOptionsIMRT optm_Options2 = new OptimizationOptionsIMRT(
            //    100,//int maxIterations,
            //    OptimizationOption.ContinueOptimization,
            //    OptimizationConvergenceOption.TerminateIfConverged,
            //    OptimizationIntermediateDoseOption.NoIntermediateDose,
            //    "" //string mlcId
            //);

            //// get the volume of the 50% dose
            //os.AddPointObjective(s_opti_rectumbowel, OptimizationObjectiveOperator.Upper, dv_rx, 0.0, 100.0);
            //ps.Optimize(optm_Options2);


            if (!opt_result.Success)
            {
                vmsApp.ClosePatient();
                _err($"Optimization Failed: opt_result.ToString()");
            }

            // LMC
            helper.log("calculating LMC...");
            bool fixedJaw = true;
            LMCVOptions lmc_options = new LMCVOptions(fixedJaw);

            stopwatch.Restart();
            CalculationResult lmc_result = ps.CalculateLeafMotions(lmc_options);
            stopwatch.Stop();
            helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

            if (!lmc_result.Success)
            {
                vmsApp.ClosePatient();
                _err("LMC Calculation Failed");
            }

            vmsApp.SaveModifications();

            // Dose
            helper.log("calculating dose....");
            stopwatch.Restart();
            CalculationResult dose_result = ps.CalculateDose();
            stopwatch.Stop();
            helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

            if (!dose_result.Success)
            {
                vmsApp.ClosePatient();
                _err("Dose Calculation Failed");
            }

            vmsApp.SaveModifications();
            vmsApp.ClosePatient();

            // create a copy plan 
            //{
            //    string new_cs_Id = "_test1";
            //    string new_ps_Id = "plan2";
            //    string new_ptv_Id = "ptv_lump1_2mm_cropped";

            //    string clincial_info_file = filesystem.join(dir, "clinical_info.csv");
            //    string result_file = filesystem.join(dir, "create_plan_and_eval.result.csv");

            //    create_plan_and_eval(new_cs_Id, new_ps_Id,new_ptv_Id, clincial_info_file, result_file);
            //}

            // create a copy plan 
            //{
            //    string in_file = filesystem.join(dir, "create_plan_and_eval.result.csv");
            //    string out_file = in_file + ".with.dice.csv";
            //    append_dice(job_id, in_file, out_file);
            //}


            helper.log("done.");
        }


        public static void append_dice(string job_id, string in_file, string out_file)
        {
            string log_file = out_file + ".log.txt";

            string job_file = string.Format(@"U:\temp\{0}\worker_job.json", job_id);

            Dictionary<string, object> dict = helper.json2dict(System.IO.File.ReadAllText(job_file));
            string test = dict["Test"].ToString();
            Dictionary<string, object> dictTest = helper.json2dict(test);
            string results = dictTest["results"].ToString();

            List<Dictionary<string, object>> list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(results);

            Dictionary<string, double> dict_img_dice = new Dictionary<string, double>();
            foreach (var item in list)
            {
                if (item.ContainsKey("dice"))
                {
                    int n = Convert.ToInt32(item["n"]);
                    if (n == 12)
                    {
                        helper.log("stop here");
                    }
                    double diceValue = Convert.ToDouble(item["dice"]);
                    string img_file = (string)item["img_file"];
                    string[] elms = img_file.Split('/');
                    string image_id = elms[elms.Length - 2].Trim();

                    if (dict_img_dice.ContainsKey(image_id))
                        dict_img_dice[image_id] = diceValue;
                    else
                        dict_img_dice.Add(image_id, diceValue);

                    Console.WriteLine($"Dice Value: {diceValue}");
                }
            }


            string[] lines = System.IO.File.ReadAllLines(in_file);
            for (int i = 0; i < lines.Length; i++)
            {
                filesystem.appendline(log_file, $"i={i}");

                string line = lines[i];

                helper.log($"[{i}/{lines.Length}] - {line}");

                //pid, pt_uuid, mongo_img_id, contour_file, PatientSer, ImageId, StructureSetId
                string[] elms = line.Split(',');
                string pid = elms[0];
                string image_id = elms[2];
                double dice = Double.NaN;
                if (dict_img_dice.ContainsKey(image_id))
                    dice = dict_img_dice[image_id.Trim()];

                filesystem.appendline(out_file, $"{line},{dice}");
            }


        }

        public static void select_clincial_cd_plan_and_target_contours(string import_result_file, string out_file)
        {
            string log_file = out_file + ".log.txt";

            VMS.TPS.Common.Model.API.Application app = vmsApp;

            // the cases done already
            string done_cases = "";
            if (filesystem.file_exists(out_file))
            {
                done_cases = System.IO.File.ReadAllText(out_file);
            }

            string[] lines = System.IO.File.ReadAllLines(import_result_file);
            for (int i = 1; i < lines.Length; i++)
            {
                filesystem.appendline(log_file, $"i={i}");

                string line = lines[i];

                helper.log($"[{i}/{lines.Length}] - {line}");

                if (line.Contains("not_found"))
                {
                    helper.log("Import was not successful. Skipping...");
                    continue;
                }

                //pid, pt_uuid, mongo_img_id, contour_file, PatientSer, ImageId, StructureSetId
                string[] elms = line.Split(',');
                string pid = elms[0];
                string pt_uuid = elms[1];
                string sset_id = elms[6];

                if (done_cases != "" && done_cases.Contains(pt_uuid))
                {
                    helper.log("This case is already in the out_file. Skipping...");
                    continue;
                }

                filesystem.appendline(log_file, $"pid={pid}");
                filesystem.append(out_file, $"{line}");

                Patient pt = app.OpenPatientById(pid);

                if (pt == null)
                {
                    helper.error($"Patient(Id={pid}) not found");
                    filesystem.append(out_file, ",pt_not_found");
                    continue;
                }

                ///////
                // sset
                StructureSet sset = sset_of_id(sset_id, pt);
                if (sset == null)
                {
                    helper.error($"StructureSet(Id={sset_id}) not found");
                    filesystem.append(out_file, $",sset_not_found");
                    continue;
                }

                //////////////////
                // select CD Plan 
                helper.log($"==== select CD Plan ====");
                helper.log($"Enter -1 if you cannot find one");
                ExternalPlanSetup ps = user_select_ext_ps_of_sset(sset);
                if (ps == null)
                {
                    filesystem.append(out_file, ",cd_plan_not_found");
                    filesystem.appendline(log_file, "cd plan not found.");
                }
                else
                {
                    filesystem.append(out_file, $",{ps.Id}");
                    filesystem.appendline(log_file, $"selected cd_plan={ps.Id}");
                }

                // ptv & ctv
                Structure ptv = s_of_id("PTV_Lumpectomy", sset, false);
                Structure ctv = s_of_id("CTV_Lumpectomy", sset, false);

                if (ptv == null || ctv == null)
                {
                    helper.log($"==== select CTV Lumpectomy ====");
                    helper.log($"Enter -1 if you cannot find one");
                    ctv = user_select_s_of_sset(sset);

                    helper.log($"==== select PTV Lumpectomy ====");
                    helper.log($"Enter -1 if you cannot find one");
                    ptv = user_select_s_of_sset(sset);
                }

                if (ctv == null)
                {
                    filesystem.append(out_file, ",ctv_lump_not_found");
                    filesystem.appendline(log_file, "ctv not found.");
                }
                else
                {
                    filesystem.append(out_file, $",{ctv.Id}");
                    filesystem.appendline(log_file, $"selected ctv={ctv.Id}");
                }

                if (ptv == null)
                {
                    filesystem.append(out_file, ",ptv_lump_not_found");
                    filesystem.appendline(log_file, "ptv not found");
                }
                else
                {
                    filesystem.append(out_file, $",{ptv.Id}");
                    filesystem.appendline(log_file, $"selected ptv={ptv.Id}");
                }

                app.ClosePatient();
                filesystem.append(out_file, "\n");
            }

            // write cases to a file
            helper.log($"Saving file...{out_file}");
        }


        public static void create_plan_and_eval(string new_cs_Id, string new_ps_Id, string new_ptv_Id, string import_result_file, string out_file)
        {

            // Create a Stopwatch instance
            Stopwatch stopwatch = new Stopwatch();

            string log_file = out_file + ".log.txt";

            VMS.TPS.Common.Model.API.Application app = vmsApp;

            string finished_cases = "";
            if (filesystem.file_exists(out_file))
                finished_cases = System.IO.File.ReadAllText(out_file);

            string[] lines = System.IO.File.ReadAllLines(import_result_file);
            for (int i = 0; i < lines.Length; i++)
            {
                filesystem.appendline(log_file, $"i={i}");

                string line = lines[i];

                helper.log($"[{i}/{lines.Length}] - {line}");

                if (line.Contains("not_found"))
                {
                    helper.log("Import was not successful. Skipping...");
                    continue;
                }

                //pid, pt_uuid, mongo_img_id, contour_file, PatientSer, ImageId, StructureSetId
                string[] elms = line.Split(',');
                string pid = elms[0].PadLeft(8, '0');
                string pt_uuid = elms[1];
                string sset_id = elms[6];
                string cln_ps_id = elms[7];
                string cln_ctv_id = elms[8];
                string cln_ptv_id = elms[9];


                // if done already skip.
                if (finished_cases != "" && finished_cases.Contains(pt_uuid))
                {
                    filesystem.append(log_file, "this case was done already, skipping...");
                    helper.log("this case was done already, skipping...");
                    continue;
                }



                filesystem.appendline(log_file, $"pid={pid}");
                filesystem.append(out_file, $"{line}");

                Patient pt = app.OpenPatientById(pid);

                if (pt == null)
                {
                    helper.error($"Patient(Id={pid}) not found");
                    filesystem.append(out_file, $",pt_not_found");
                    continue;
                }

                ///////
                // sset
                StructureSet sset = sset_of_id(sset_id, pt);
                if (sset == null)
                {
                    helper.error($"StructureSet(Id={sset_id}) not found");
                    filesystem.append(out_file, $",sset_not_found");
                    continue;
                }
                filesystem.append(out_file, $",{sset.Id}");

                ExternalPlanSetup ps = (ExternalPlanSetup)ps_of_id(cln_ps_id, pt);
                if (ps == null)
                {
                    filesystem.appendline(log_file, $"clincial CD plan not found! plan.Id={cln_ps_id}");
                    filesystem.append(out_file, $",cd_plan_not_found");
                    continue;
                }
                filesystem.append(out_file, $",{ps.Id}");
                filesystem.appendline(log_file, $"cd ps={ps.Id}");

                pt.BeginModifications();

                //////////////
                /// copy plan
                remove_ps(new_ps_Id, pt);

                // make a plan copy

                Course new_cs = find_or_add_cs(new_cs_Id, pt);
                //ExternalPlanSetup ps_copy = (ExternalPlanSetup)new_cs.CopyPlanSetup(ps);
                //ps_copy.Id = new_ps_Id;

                if (new_cs == null)
                {
                    helper.error($"Course(Id={new_cs_Id}) not found nor create it.");
                    filesystem.append(out_file, $",new_cs_not_found");
                    continue;
                }
                filesystem.append(out_file, $",{new_cs.Id}");

                ExternalPlanSetup new_ps = find_or_add_ext_ps(new_ps_Id, ps.StructureSet, new_cs);

                //new_ps.SetPrescription(ps.NumberOfFractions??0, ps.DosePerFraction, ps.TreatmentPercentage);
                new_ps.SetPrescription(4, new DoseValue(250.0, DoseValue.DoseUnit.cGy), 1.0);
                /*
                int m = 1;
                List<KeyValuePair<string, MetersetValue>> mu_list = new List<KeyValuePair<string, MetersetValue>>();
                List<double> mus = new List<double>();
                */
                Structure new_ptv = s_of_id(new_ptv_Id, sset);

                foreach (Beam b in ps.Beams)
                {
                    if (b.IsSetupField)
                        continue;

                    Beam new_b = bm_of_ps(b.Id, new_ps);
                    if (new_b == null)
                    {
                        ExternalBeamMachineParameters machine = new ExternalBeamMachineParameters("SIL 21IX", "6X", 600, "STATIC", "");
                        ControlPoint cpt0 = b.ControlPoints[0];
                        //Beam new_b = new_ps.AddMLCBeam(machine, cpt0.LeafPositions, cpt0.JawPositions, cpt0.CollimatorAngle, cpt0.GantryAngle, cpt0.PatientSupportAngle, b.IsocenterPosition);
                        new_b = new_ps.AddStaticBeam(machine, cpt0.JawPositions, cpt0.CollimatorAngle, cpt0.GantryAngle, cpt0.PatientSupportAngle, b.IsocenterPosition);
                        new_b.Id = b.Id;
                    }
                    //BeamParameters bp = new_b.GetEditableParameters();

                    // fit Jaw to the new ptv
                    double jaw_margin_mm = 20.0; // large enough for IMRT
                    new_b.FitCollimatorToStructure(new FitToStructureMargins(jaw_margin_mm), new_ptv, true, true, false);

                    // fit MLC to the new ptv
                    //double mlc_margin_mm = 5.0;
                    //new_b.FitMLCToStructure(new FitToStructureMargins(mlc_margin_mm), new_ptv, false, JawFitting.FitToStructure, OpenLeavesMeetingPoint.OpenLeavesMeetingPoint_Middle, ClosedLeavesMeetingPoint.ClosedLeavesMeetingPoint_BankOne);

                    //new_b.Wedges.Add(new EnhancedDynamicWedge(new_b))
                    // MU
                    //mus.Add(b.Meterset.Value);
                    //mu_list.Add(new KeyValuePair<string, MetersetValue>(new_b.Id, new MetersetValue(b.Meterset.Value, b.Meterset.Unit)));

                    //m++;
                }

                // public void SetCalculationModel(
                new_ps.SetCalculationModel(CalculationType.PhotonOptimization, "PO_13623");
                new_ps.SetCalculationModel(CalculationType.PhotonVolumeDose, "AAA_13623");

                // optimization
                OptimizationSetup os = new_ps.OptimizationSetup;
                OptimizationNormalTissueParameter nto = os.AddAutomaticNormalTissueObjective(0.1);
                DoseValue dv_lower = new DoseValue(1000.0, DoseValue.DoseUnit.cGy);
                DoseValue dv_upper = new DoseValue(1100.0, DoseValue.DoseUnit.cGy);
                os.AddPointObjective(new_ptv, OptimizationObjectiveOperator.Lower, dv_lower, 100.0, 100.0);
                os.AddPointObjective(new_ptv, OptimizationObjectiveOperator.Upper, dv_upper, 0.0, 100.0);

                OptimizationOptionsIMRT optm_Options = new OptimizationOptionsIMRT(
                    10000000,//int maxIterations,
                    OptimizationOption.RestartOptimization,
                    OptimizationConvergenceOption.TerminateIfConverged,
                    OptimizationIntermediateDoseOption.NoIntermediateDose,
                    "" //string mlcId
                );

                helper.log("optimizing...");

                // Start the stopwatch
                stopwatch.Restart();
                OptimizerResult opt_result = new_ps.Optimize(optm_Options);
                stopwatch.Stop();
                helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

                if (!opt_result.Success)
                {
                    helper.error("Optimization Failed");
                    filesystem.append(out_file, ",opti_failed");
                    app.ClosePatient();
                    continue;
                }

                // LMC
                helper.log("calculating LMC...");
                bool fixedJaw = true;
                LMCVOptions lmc_options = new LMCVOptions(fixedJaw);

                stopwatch.Restart();
                CalculationResult lmc_result = new_ps.CalculateLeafMotions(lmc_options);
                stopwatch.Stop();
                helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

                if (!lmc_result.Success)
                {
                    helper.error("LMC Calculation Failed");
                    filesystem.append(out_file, ",lmc_calc_failed");
                    app.ClosePatient();
                    continue;
                }

                // Dose
                helper.log("calculating dose....");
                stopwatch.Restart();
                CalculationResult dose_result = new_ps.CalculateDose();
                stopwatch.Stop();
                helper.log($"finished. It took {(stopwatch.ElapsedMilliseconds / 1000.0 / 60.0).ToString("0.0")} minutes.");

                if (!dose_result.Success)
                {
                    helper.error("Dose Calculation Failed");
                    filesystem.append(out_file, ",dose_calc_failed");
                    app.ClosePatient();
                    continue;
                }

                // read target volume coverage 
                helper.log("evaluaing target coverages...");
                Structure s_cln_ctv = s_of_id(cln_ctv_id, sset);
                double dose_rx = 1000.0;
                {
                    Structure s = s_cln_ctv;
                    if (s != null && !s.IsEmpty)
                    {
                        double v100 = new_ps.GetVolumeAtDose(s, s2D($"{dose_rx} cGy"), VolumePresentation.Relative);
                        double v95 = new_ps.GetVolumeAtDose(s, s2D($"{dose_rx * 0.95} cGy"), VolumePresentation.Relative);
                        double v90 = new_ps.GetVolumeAtDose(s, s2D($"{dose_rx * 0.90} cGy"), VolumePresentation.Relative);

                        DVHData dvh = new_ps.GetDVHCumulativeData(s, DoseValuePresentation.Absolute, VolumePresentation.Relative, 1);
                        double mean_dose = dvh.MeanDose.Dose;

                        helper.log($"ctv - v100 = {v100.ToString("0.0")}");
                        helper.log($"ctv - v95 = {v95.ToString("0.0")}");
                        helper.log($"ctv - mean_dose = {mean_dose.ToString("0.0")}");

                        filesystem.append(out_file, $",{v100.ToString("0.0")},{v95.ToString("0.0")},{v90.ToString("0.0")},{mean_dose.ToString("0.0")}");
                    }
                    else
                    {
                        filesystem.appendline(log_file, $"clincial ctv(Id={s.Id} is empty");
                        helper.log("clincial ctv is empty");
                        filesystem.append(out_file, $",NaN,NaN,NaN,NaN");
                    }
                }

                Structure s_cln_ptv = s_of_id(cln_ptv_id, sset);
                {
                    Structure s = s_cln_ptv;
                    if (s != null && !s.IsEmpty)
                    {
                        double v100 = new_ps.GetVolumeAtDose(s, s2D($"{dose_rx} cGy"), VolumePresentation.Relative);
                        double v95 = new_ps.GetVolumeAtDose(s, s2D($"{dose_rx * 0.95} cGy"), VolumePresentation.Relative);
                        double v90 = new_ps.GetVolumeAtDose(s, s2D($"{dose_rx * 0.90} cGy"), VolumePresentation.Relative);

                        DVHData dvh = new_ps.GetDVHCumulativeData(s, DoseValuePresentation.Absolute, VolumePresentation.Relative, 1);
                        double mean_dose = dvh.MeanDose.Dose;

                        helper.log($"ptv - v100 = {v100.ToString("0.0")}");
                        helper.log($"ptv - v95 = {v95.ToString("0.0")}");
                        helper.log($"ptv - mean_dose = {mean_dose.ToString("0.0")}");

                        filesystem.append(out_file, $",{v100.ToString("0.0")},{v95.ToString("0.0")},{v90.ToString("0.0")},{mean_dose.ToString("0.0")}");
                    }
                    else
                    {
                        filesystem.appendline(log_file, $"clincial ptv(Id={s.Id} is empty");
                        helper.log("clincial ptv is empty");
                        filesystem.append(out_file, $",NaN,NaN,NaN,NaN");
                    }
                }

                app.SaveModifications();
                app.ClosePatient();

                filesystem.append(out_file, "\n");
            }

        }




        public static void crop_by_body(string s_Id, string s_cropped_Id, Color s_color, double body_inner_margin_mm, string import_result_file, string out_file)
        {

            string log_file = out_file + ".log.txt";

            VMS.TPS.Common.Model.API.Application app = vmsApp;

            StringBuilder sb = new StringBuilder();

            string[] lines = System.IO.File.ReadAllLines(import_result_file);
            for (int i = 1; i < lines.Length; i++)
            {
                filesystem.appendline(log_file, $"i={i}");

                string line = lines[i];

                helper.log($"[{i}/{lines.Length}] - {line}");

                if (line.Contains("not_found"))
                {
                    helper.log("Import was not successful. Skipping...");
                    continue;
                }

                //pid, pt_uuid, mongo_img_id, contour_file, PatientSer, ImageId, StructureSetId
                string[] elms = line.Split(',');
                string pid = elms[0];
                string sset_id = elms[6];

                filesystem.appendline(log_file, $"pid={pid}");

                sb.Append($"{pid}");

                Patient pt = app.OpenPatientById(pid);
                pt.BeginModifications();
                if (pt == null)
                {
                    helper.error($"Patient(Id={pid}) not found");
                    sb.Append($",pt_not_found");
                    continue;
                }

                ///////
                // sset
                StructureSet sset = sset_of_id(sset_id, pt);
                if (sset == null)
                {
                    helper.error($"StructureSet(Id={sset_id}) not found");
                    sb.Append($",sset_not_found");
                    continue;
                }
                sb.Append($",{sset.Id}");

                ///////
                /// s
                Structure s = s_of_id(s_Id, sset);
                if (s == null)
                {
                    helper.error($"Structure(Id={s_Id}) not found");
                    sb.Append($",s_not_found");
                    continue;
                }
                sb.Append($",{s.Id}");

                //////////
                // BODY
                Structure s_body = s_of_type_external(sset);
                if (s_body == null)
                {
                    helper.error($"Could not find structure(Id=BODY)");
                    sb.Append($",BODY_not_found");
                    continue;
                }

                /////////////////
                /// new structure
                Structure s_new = find_or_add_s("ORGAN", s_cropped_Id, sset);
                s_new.Color = s_color;
                SegmentVolume body_shrink = s_body.SegmentVolume.Margin(-body_inner_margin_mm);
                s_new.SegmentVolume = s.SegmentVolume.And(body_shrink);

                app.SaveModifications();
                app.ClosePatient();

                sb.Append("\n");
            }

            // write cases to a file
            helper.log($"Saving file...{out_file}");
            filesystem.write_all_text(out_file, sb.ToString());
        }


        public static void add_margin(string ctv_Id, string ptv_Id, Color ptv_color, double margin_mm, string import_result_file, string out_file)
        {
            VMS.TPS.Common.Model.API.Application app = vmsApp;

            StringBuilder sb = new StringBuilder();

            string[] lines = System.IO.File.ReadAllLines(import_result_file);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];

                helper.log($"[{i}/{lines.Length}] - {line}");

                if (line.Contains("not_found"))
                {
                    helper.log("Import was not successful. Skipping...");
                    continue;
                }

                //pid, pt_uuid, mongo_img_id, contour_file, PatientSer, ImageId, StructureSetId
                string[] elms = line.Split(',');
                string pid = elms[0];
                string sset_id = elms[6];

                sb.Append($"{pid}");

                Patient pt = app.OpenPatientById(pid);
                pt.BeginModifications();
                if (pt == null)
                {
                    helper.error($"Patient(Id={pid}) not found");
                    sb.Append($",pt_not_found");
                    continue;
                }

                ///////
                // sset
                StructureSet sset = sset_of_id(sset_id, pt);
                if (sset == null)
                {
                    helper.error($"StructureSet(Id={sset_id}) not found");
                    sb.Append($",sset_not_found");
                    continue;
                }
                sb.Append($",{sset.Id}");


                ///////
                /// s
                Structure s_ctv = s_of_id(ctv_Id, sset);
                if (s_ctv == null)
                {
                    helper.error($"Structure(Id={ctv_Id}) not found");
                    sb.Append($",s_not_found");
                    continue;
                }
                sb.Append($",{s_ctv.Id}");

                // add ptv
                Structure s_ptv = find_or_add_s("ORGAN", ptv_Id, sset);
                if (s_ptv == null)
                {
                    helper.error($"Could not find or add structure(Id={ptv_Id})");
                    sb.Append($",s_create_failed");
                    continue;
                }
                s_ptv.Color = ptv_color;
                sb.Append($",{s_ptv.Id}");

                // ptv = ctv + margin_mm
                s_ptv.SegmentVolume = s_ctv.SegmentVolume.Margin(margin_mm);
                sb.Append($",{margin_mm}");

                app.SaveModifications();
                app.ClosePatient();

                sb.Append("\n");
            }

            // write cases to a file
            helper.log($"Saving file...{out_file}");
            filesystem.write_all_text(out_file, sb.ToString());
        }

        //public static void import_from_cont_files(string job_id, string cont_Id, string img_uuid_list_file, string out_file)
        //{
        //    VMS.TPS.Common.Model.API.Application app = _vmsApp;
        //    StringBuilder sb = new StringBuilder();
        //    sb.AppendLine("pid, pt_uuid, mongo_img_id, contour_file, PatientSer, ImageId, StructureSetId");

        //    int i = 0;
        //    string[] lines = System.IO.File.ReadAllLines(img_uuid_list_file);
        //    foreach (string line in lines)
        //    {
        //        helper.log($"[{i}/{lines.Length}] - {line}");

        //        string img_id = line.Split(',')[0];
        //        string pt_uuid = line.Split(',')[1];
        //        string pid = piduuid.uuid2pid(pt_uuid);
        //        string s_Id = cont_Id;
        //        string seg_dir = string.Format(@"U:\temp\{0}\test\seg\{1}", job_id, img_id);
        //        string cont_json_file = helper.join(seg_dir, "img.mha.points.w.json");

        //        sb.Append($"{pid},{pt_uuid},{img_id},{cont_json_file}");

        //        ///////////////////////////////////
        //        // copy file data from the clinial system
        //        // sometime PatientSer is used for folder name, other times pid is used in making the path.
        //        variandb.varian db = new variandb.varian("variandb", "PhysicsSB", "PhysicsSB09");
        //        db.open_connection();
        //        string PatientSer = db.get_PatientSer_by_PatientId(pid);
        //        db.close_connection();
        //        copy_data_PatientSer(PatientSer);
        //        copy_data_pid(pid);

        //        sb.Append($",{PatientSer}");

        //        import_contours(img_id, pid, cont_json_file, s_Id, app, sb);

        //        sb.Append('\n');

        //        i++;
        //    }

        //    // write cases to a file
        //    helper.log($"Saving file...{out_file}");
        //    filesystem.write_all_text(out_file, sb.ToString());


        //}


        static void copy_data_PatientSer(string PatientSer)
        {
            string src_dir = $@"\\varianfs\VA_DATA$\PATIENTS\_{PatientSer}";
            if (!filesystem.dir_exists(src_dir))
            {
                helper.error($"Source dir not found! dir={src_dir}");
                return;
            }

            string dst_dir = $@"\\variantest\VA_DATA$\PATIENTS\_{PatientSer}";

            // check if the destination dir is up to date
            long src_dir_size = filesystem.get_dir_size(src_dir);
            long dst_dir_size = filesystem.get_dir_size(dst_dir);

            if (src_dir_size == dst_dir_size)
            {
                helper.log("dst folder is up to date.");
                return;
            }

            // copy folder
            filesystem.copy_dir(src_dir, dst_dir);

        }

        static void copy_data_pid(string pid)
        {
            string src_dir = $@"\\varianfs\VA_DATA$\PATIENTS\{pid}";
            if (!filesystem.dir_exists(src_dir))
            {
                helper.error($"Source dir not found! dir={src_dir}");
                return;
            }

            string dst_dir = $@"\\variantest\VA_DATA$\PATIENTS\{pid}";

            // check if the destination dir is up to date
            long src_dir_size = filesystem.get_dir_size(src_dir);
            long dst_dir_size = filesystem.get_dir_size(dst_dir);

            if (src_dir_size == dst_dir_size)
            {
                helper.log("dst folder is up to date.");
                return;
            }

            // copy folder
            filesystem.copy_dir(src_dir, dst_dir);

        }

        //static string api_url = "http://roweb3.uhmc.sbuh.stonybrook.edu:5001/api";

        //static string vms_image_Id_from_mongo_image_id(string mango_image_id)
        //{
        //    string images_url = string.Format("{0}/images/{1}", api_url, mango_image_id);
        //    webservcie.ImagesDataProvider idp = new webservcie.ImagesDataProvider(images_url);
        //    Dictionary<string, string> image_db = idp.get_one(mango_image_id);
        //    if (image_db == null)
        //    {
        //        helper.error(string.Format("Failed retriveing image object for image_id={0}", mango_image_id));
        //        return null;
        //    }
        //    return image_db["Id"];
        //}

        //static void import_contours(string img_id, string pid, string cont_json_file, string cont_Id, VMS.TPS.Common.Model.API.Application app, StringBuilder sb)
        //{
        //    //////////////
        //    /// vms pt
        //    Patient vms_pt = app.OpenPatientById(pid);
        //    if (vms_pt == null)
        //    {
        //        sb.Append(",pt_not_found");
        //        return;
        //    }
        //    vms_pt.BeginModifications();

        //    ///////////////
        //    // vms Image
        //    string ImageId = vms_image_Id_from_mongo_image_id(img_id);
        //    VMS.TPS.Common.Model.API.Image vms_img = image_of_id(ImageId, vms_pt);
        //    if (vms_img == null)
        //    {
        //        helper.error($"VMS Image not found for the Id={ImageId}");
        //        sb.Append(",vms_img_not_found");
        //        app.ClosePatient();
        //        return;
        //    }
        //    sb.Append($",{ImageId}");

        //    //////////////////////
        //    // vms StructureSet
        //    List<StructureSet> sset_list = sset_list_of_image_id(vms_img.Id, vms_pt);
        //    if (sset_list.Count == 0)
        //    {
        //        helper.error($"There is no sset for image Id = {vms_img.Id}!");
        //        return;
        //    }
        //    if (sset_list.Count > 1)
        //    {
        //        helper.log($"WARING - There are more than 1 sset for image Id = {vms_img.Id}! Using the first one.");
        //    }
        //    StructureSet vms_sset = sset_list[0];
        //    helper.log(vms_sset.Id);
        //    sb.Append($",{vms_sset.Id}");

        //    //////////////////
        //    // vms Structure
        //    Structure vms_s = find_or_add_s("ORGAN", cont_Id, vms_sset);
        //    if (vms_s == null)
        //    {
        //        helper.error($"Could not find or add a new strucdture {cont_Id} for {pid}. Please check if the course is 'Completed'");
        //        sb.Append(",could_not_add_structure");
        //        app.ClosePatient();
        //        return;
        //    }

        //    if (!vms_s.IsEmpty)
        //        s_clear_all_slices(vms_s, vms_img.ZSize);

        //    // load contour data
        //    s_load_contour_data_from_cont_json_file(vms_s, cont_json_file);

        //    app.SaveModifications();
        //    helper.log(vms_pt.ToString());
        //    app.ClosePatient();
        //}

    }


}
