using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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

namespace nnunet_client.views
{
    /// <summary>
    /// Interaction logic for AutoPlanControl.xaml
    /// </summary>
    public partial class AutoPlanControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        VMSPatient _patient = null;
        public VMSPatient Patient
        {
            get
            {
                return _patient;
            }

            set {
                if (_patient != value)
                {
                    _SetPatient(value);
                    OnPropertyChanged(nameof(Image));
                }
            }
        }


        VMSImage _image = null;
        public VMSImage Image
        {
            get { return _image; }
            set
            {
                if (_image != value)
                {
                    _SetImage(value);
                    OnPropertyChanged(nameof(Image));
                }
            }
        }

        VMSStructureSet _sset = null;
        public VMSStructureSet StructureSet
        {
            get => _sset;   
        }

        private VMSStructure _bladder;
        public VMSStructure Bladder
        {
            get => _bladder;
            set
            {
                if (_bladder != value)
                {
                    _bladder = value;
                    OnPropertyChanged(nameof(Bladder));
                }
            }
        }

        private VMSStructure _bowel;
        public VMSStructure Bowel
        {
            get => _bowel;
            set
            {
                if (_bowel != value)
                {
                    _bowel = value;
                    OnPropertyChanged(nameof(Bowel));
                }
            }
        }

        private VMSStructure _rectum;
        public VMSStructure Rectum
        {
            get => _rectum;
            set
            {
                if (_rectum != value)
                {
                    _rectum = value;
                    OnPropertyChanged(nameof(Rectum));
                }
            }
        }

        private VMSStructure _ptv;
        public VMSStructure PTV
        {
            get => _ptv;
            set
            {
                if (_ptv != value)
                {
                    _ptv = value;
                    OnPropertyChanged(nameof(PTV));
                }
            }
        }

        private VMSPlanSetup _plan;
        public VMSPlanSetup Plan
        {
            get => _plan;
            set
            {
                if (_plan != value)
                {
                    _plan = value;
                    OnPropertyChanged(nameof(Plan));
                }
            }
        }

        string bladder_id = "Bladder";
        string bowel_id = "Bowel";
        string rectum_id = "Rectum";
        double crop_by_body_inner_margin = 3.0; // 3mm
        double ptv_margin1_all = 5.0; // 5mm all around
        double ptv_margin2_inf = 5.0; // 5mm inf
        string ptv_id = "ptv_5_10";
        string opti_rectum_id = "_opti_rectum";
        string opti_bowel_id = "_opti_bowel";

        Color ptv_color = Color.FromRgb(255, 0, 0);

        int num_fxs = 20;
        double dose_per_fx = 275.0;

        public AutoPlanControl()
        {
            InitializeComponent();

            this.DataContext = this;

            CourseComboBoxControl.SelectedItemChanged += (s, course) =>
            {
                if (course != null)
                {
                    helper.log($"Course selected: {course.Id}");
                    // Do something with the selected course
                }
            };
        }

        private void _SetPatient(VMSPatient patient)
        {
            if(_patient == patient) return;

            _patient = patient;

            CourseComboBoxControl.SetCourses(_patient.Courses);
        }

        private void _SetImage(VMSImage image)
        {
            if (_image == image)
            {
                return;
            }

            _image = image;

            List<VMSStructureSet> list = esapi.esapi.sset_list_of_image_id_FOR(image.Id, image.FOR, _patient);
            if(list.Count == 1)
            {
                _sset = list[0];
                OnPropertyChanged("StructureSet");

                helper.log($"StructureSet found ({StructureSet.Id})");


                Bladder = esapi.esapi.s_of_id(bladder_id, _sset, false);
                helper.log($"Bladder found ({Bladder.Id})");

                Rectum = esapi.esapi.s_of_id(rectum_id, _sset, false);
                helper.log($"Rectum found ({Rectum.Id})");

                Bowel = esapi.esapi.s_of_id(bowel_id, _sset, false);
                helper.log($"Bowel found ({Bowel.Id})");
            }
        }


        
        private void MakePTVButton_Click(object sender, RoutedEventArgs e)
        {
            helper.log("MakePTVButton_Click()");

            // course selected?
            VMSCourse course = CourseComboBoxControl.GetSelectedCourse();
            if (course == null)
            {
                helper.show_error_msg_box("Please select a course!");
                return;
            }

            VMSPatient pt = global.vmsPatient;
            helper.log($"Patient={pt.Id}");

            VMSImage ct = _image;
            helper.log($"Image={ct.Id}");
            List<VMSStructureSet> sset_list = sset_list_of_image_id_FOR(ct.Id, ct.FOR, pt);
            if(sset_list.Count == 0)
            {
                helper.show_error_msg_box($"StructureSet not found for image (Id={ct.Id}, FOR={ct.FOR})");
                return;
            }
            else if(sset_list.Count > 1)
            {
                helper.show_error_msg_box($"More than 1 StructureSet found for image (Id={ct.Id}, FOR={ct.FOR})");
                return;
            }
            VMSStructureSet sset = sset_list[0];
            helper.log($"StructureSet={sset.Id}");

            // ptv already existing
            VMSStructure ptv = esapi.esapi.s_of_id(ptv_id, sset);
            if (ptv != null)
            {
                helper.show_warning_msg_box($"PTV already exitsts (Id={ptv_id})");
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


            global.vmsPatient.BeginModifications();
            
            this.PTV = bladder_art.make_ptv(
                pt,
                sset,
                ct,
                bladder,
                rectum,
                bowel,
                crop_by_body_inner_margin,
                ptv_margin1_all,
                ptv_margin2_inf, 
                ptv_id,
                ptv_color);

            var result = bladder_art.make_opti_contours(
                sset,
                this.PTV,
                rectum,
                bowel,
                opti_rectum_id,
                opti_bowel_id);
            

            global.vmsApplication.SaveModifications();

            helper.log("make_ptv_and_opti_contours() - done");
        }




        private async void OptimizeButton_Click(object sender, RoutedEventArgs e)
        {
            VMSPatient pt = this.Patient;
            VMSImage ct = this.Image;
            VMSStructureSet sset = this.StructureSet;
            VMSCourse course = CourseComboBoxControl.GetSelectedCourse();

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
                    ptv_id,
                    bladder_id,
                    rectum_id,
                    opti_rectum_id,
                    bowel_id,
                    opti_bowel_id,
                    num_fxs,
                    dose_per_fx
                );

                
                Plan = esapi.esapi.ps_of_id(ps_id, pt);

                helper.log("OptimizeButton_Click() - done");


                // evaluate 



            }
            catch (Exception ex)
            {
                helper.error($"Error: {ex.Message}");
            }


            global.vmsApplication.SaveModifications();
        }

    }
}
