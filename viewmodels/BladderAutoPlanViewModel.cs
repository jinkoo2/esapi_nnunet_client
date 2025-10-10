using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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

namespace nnunet_client.viewmodels
{
    public class BladderAutoPlanViewModel : BaseViewModel
    {
        public ICommand MakePTVCommand { get; }
        public ICommand ObtimizeCommand { get; }


        // construction
        public BladderAutoPlanViewModel()
        {
            DoseLimitListEditorViewModel = new DoseLimitListEditorViewModel()
            {
                Title="Dose Limits"
            };
            

            MakePTVCommand = new RelayCommand(MakePTV);
            ObtimizeCommand = new RelayCommand(Optimize);
        }

        // dose limit editor
        private DoseLimitListEditorViewModel _doseLimitListEditorViewModel;
        public DoseLimitListEditorViewModel DoseLimitListEditorViewModel
        {
            get => _doseLimitListEditorViewModel;
            set => SetProperty<DoseLimitListEditorViewModel>(ref _doseLimitListEditorViewModel, value);
        }

        private VMSPatient _patient = null;
        public VMSPatient Patient
        {
            get => _patient;
            set
            {
                if (_patient == value) 
                    return;

                SetProperty<VMSPatient>(ref _patient, value);
            }
        }

        private VMSCourse _course;
        public VMSCourse Course
        {
            get => _course;
            set => SetProperty<VMSCourse>(ref _course, value);
        }

        string bladder_id = "Bladder";
        string bowel_id = "Bowel";
        string rectum_id = "Rectum";

        VMSImage _image = null;
        public VMSImage Image
        {
            get =>_image;
            set
            {
                if (_image == value)
                    return;
                
                SetProperty<VMSImage>(ref _image, value);

                // update bladder/rectum/
                if (_image != null)
                {
                    List<VMSStructureSet> list = esapi.esapi.sset_list_of_image_id_FOR(_image.Id, _image.FOR, _patient);
                    if (list.Count == 1)
                    {
                        StructureSet = list[0];
                        helper.log($"StructureSet found ({StructureSet.Id})");
                    }
                    else
                    {
                        helper.log("Image has no structure set.");
                    }
                }
                else
                {
                    Bladder = null;
                    Rectum = null;
                    Bowel= null;
                }
            }
        }

        VMSStructureSet _structureSet = null;
        public VMSStructureSet StructureSet
        {
            get => _structureSet;
            set
            {
                if(_structureSet == value) return;

                SetProperty(ref _structureSet, value);

                if (_structureSet != null)
                {
                    Bladder = esapi.esapi.s_of_id(bladder_id, _structureSet, false);
                    Rectum = esapi.esapi.s_of_id(rectum_id, _structureSet, false);
                    Bowel = esapi.esapi.s_of_id(bowel_id, _structureSet, false);
                }
                else
                {
                    Bladder = null;
                    Rectum = null;
                    Bowel = null;
                }
            }
        }

        private VMSStructure _bladder;
        public VMSStructure Bladder
        {
            get => _bladder;
            set => SetProperty<VMSStructure>(ref _bladder, value);
        }

        private VMSStructure _bowel;
        public VMSStructure Bowel
        {
            get => _bowel;
            set => SetProperty<VMSStructure>(ref _bowel, value);
        }

        private VMSStructure _rectum;
        public VMSStructure Rectum
        {
            get => _rectum;
            set => SetProperty<VMSStructure>(ref _rectum, value);
        }

        private VMSStructure _ptv;
        public VMSStructure PTV
        {
            get => _ptv;
            set => SetProperty<VMSStructure>(ref _ptv, value);
        }

        private VMSPlanSetup _plan;
        public VMSPlanSetup Plan
        {
            get => _plan;
            set => SetProperty<VMSPlanSetup>(ref _plan, value);
        }

        
        private double _cropByBodyInnerMargin = 3.0; // 3mm
        public double CropByBodyInnerMargin
        {
            get => _cropByBodyInnerMargin;
            set => SetProperty<double>(ref _cropByBodyInnerMargin, value);
        }



        private double _pTVMargin1All = 5.0; 
        public double PTVMargin1All
        {
            get => _pTVMargin1All;
            set => SetProperty<double>(ref _pTVMargin1All, value);
        }


        private double _pTVMargin2Inf = 5.0; // 5mm inf
        public double PTVMargin2Inf
        {
            get => _pTVMargin2Inf;
            set => SetProperty<double>(ref _pTVMargin2Inf, value);
        }

        private string _pTVId = "ptv_5_10";
        public string PTVId
        {
            get => _pTVId;
            set => SetProperty<string>(ref _pTVId, value);
        }
       
        
        
        string _optiRectumId = "_opti_rectum";
        string _optiBowelId = "_opti_bowel";

        Color ptv_color = Color.FromRgb(255, 0, 0);

        int num_fxs = 20;
        double dose_per_fx = 275.0;

        private void MakePTV()
        {
            helper.log("MakePTV()");

            // course selected?
            VMSCourse course = _course;
            if (course == null)
            {
                helper.show_error_msg_box("Please select a course!");
                return;
            }

            VMSPatient pt = _patient;
            helper.log($"Patient={pt.Id}");

            VMSImage ct = _image;
            helper.log($"Image={ct.Id}");
            List<VMSStructureSet> sset_list = sset_list_of_image_id_FOR(ct.Id, ct.FOR, pt);
            if (sset_list.Count == 0)
            {
                helper.show_error_msg_box($"StructureSet not found for image (Id={ct.Id}, FOR={ct.FOR})");
                return;
            }
            else if (sset_list.Count > 1)
            {
                helper.show_error_msg_box($"More than 1 StructureSet found for image (Id={ct.Id}, FOR={ct.FOR})");
                return;
            }
            VMSStructureSet sset = sset_list[0];
            helper.log($"StructureSet={sset.Id}");

            // ptv already existing
            VMSStructure ptv = esapi.esapi.s_of_id(_pTVId, sset);
            if (ptv != null)
            {
                helper.show_warning_msg_box($"PTV already exitsts (Id={_pTVId})");
                this.PTV = ptv;
                helper.log($"PTV={PTV.Id}");
                return;
            }

            // check bowel exists
            VMSStructure bowel = esapi.esapi.s_of_id(bowel_id, sset);
            if (bowel == null)
            {
                helper.show_error_msg_box($"Bowel not found (Id={bowel_id})");
                return;
            }

            // check rectum exists
            VMSStructure rectum = esapi.esapi.s_of_id(rectum_id, sset);
            if (rectum == null)
            {
                helper.show_error_msg_box($"Rectum not found (Id={rectum_id})");
                return;
            }

            // check bladder exists
            VMSStructure bladder = esapi.esapi.s_of_id(bladder_id, sset);
            if (bladder == null)
            {
                helper.show_error_msg_box($"Bladder not found (Id={bladder_id})");
                return;
            }

            _patient.BeginModifications();

            this.PTV = bladder_art.make_ptv(
                pt,
                sset,
                ct,
                bladder,
                rectum,
                bowel,
                _cropByBodyInnerMargin,
                _pTVMargin1All,
                _pTVMargin2Inf,
                _pTVId,
                ptv_color);

            helper.log("ptv created!");

            helper.log("creating rectum/bowel optimization contours!");
            var result = bladder_art.make_opti_contours(
                sset,
                this.PTV,
                rectum,
                bowel,
                _optiRectumId,
                _optiBowelId);


            global.vmsApplication.SaveModifications();

            helper.log("opti contours created!");
        }

        private async void Optimize()
        {
            VMSPatient pt = this.Patient;
            VMSImage ct = this.Image;
            VMSStructureSet sset = this.StructureSet;
            VMSCourse course = this.Course;
            
            if (pt == null || ct == null || sset == null || course == null)
            {
                helper.show_error_msg_box("Missing required inputs.");
                return;
            }

            pt.BeginModifications();

            try
            {
                helper.log("OptimizeButton_Click() - begin");

                string today = DateTime.Now.ToString("MMdd");
                int n = 0;
                string ps_id = $"art_{today}_{n}";
                while (ps_of_id(ps_id, pt) != null)
                {
                    n++;
                    ps_id = $"art_{today}_{n}";
                }
                helper.log($"plan_id={ps_id}");

                bladder_art.create_bladder_plan3(
                    pt,
                    sset,
                    course,
                    ps_id,
                    _pTVId,
                    bladder_id,
                    rectum_id,
                    _optiRectumId,
                    bowel_id,
                    _optiBowelId,
                    num_fxs,
                    dose_per_fx
                );

                Plan = esapi.esapi.ps_of_id(ps_id, pt);

                helper.log("optimization - done");
            }
            catch (Exception ex)
            {
                helper.error($"Error: {ex.Message}");
            }


            global.vmsApplication.SaveModifications();
        }

    }
}
