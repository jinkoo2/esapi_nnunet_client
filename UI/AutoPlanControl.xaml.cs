using System;
using System.Collections.Generic;
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

using static esapi.esapi;

namespace nnunet_client.UI
{
    /// <summary>
    /// Interaction logic for AutoPlanControl.xaml
    /// </summary>
    public partial class AutoPlanControl : UserControl
    {
        VMSPatient _patient = null;
        VMSImage _image = null;


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

        public AutoPlanControl()
        {
            InitializeComponent();

            CourseComboBoxControl.SelectedItemChanged += (s, course) =>
            {
                if (course != null)
                {
                    helper.log($"Course selected: {course.Id}");
                    // Do something with the selected course
                }
            };
        }

        public void SetPatient(VMSPatient patient)
        {
            _patient = patient;

            CourseComboBoxControl.SetCourses(_patient.Courses);
        }

        public void SetImage(VMSImage image)
        {
            _image = image;
        }

        private void MakePTVButton_Click(object sender, RoutedEventArgs e)
        {
            helper.log("MakePTVButton_Click()");

            VMSCourse course = CourseComboBoxControl.GetSelectedCourse();
            if (course == null)
            {
                // ask user to select a source
                throw new Exception("Please selecte a course.");
                return;
            }


            // Add margins to PTV
            VMSPatient pt = global.vmsPatient;
            helper.log($"Patient={pt.Id}");

            VMSImage ct = _image;
            helper.log($"Image={ct.Id}");
            List<VMSStructureSet> sset_list = sset_list_of_image_id_FOR(ct.Id, ct.FOR, pt);
            if(sset_list.Count == 0)
            {
                throw new Exception($"StructureSet not found for image (Id={ct.Id}, FOR={ct.FOR})");
            }
            else if(sset_list.Count > 1)
            {
                throw new Exception($"More than 1 StructureSet found for image (Id={ct.Id}, FOR={ct.FOR})");
            }



            VMSStructureSet sset = sset_list[0];

            helper.log($"StructureSet={sset.Id}");

            global.vmsPatient.BeginModifications();
            
            
            bladder_art.make_ptv_and_opti_contours(
                pt,
                sset,
                ct,
                bladder_id,
                rectum_id,
                bowel_id,
                crop_by_body_inner_margin,
                ptv_margin1_all,
                ptv_margin2_inf, ptv_id,
                ptv_color,
                opti_rectum_id,
                opti_bowel_id);
            global.vmsApplication.SaveModifications();

            helper.log("make_ptv_and_opti_contours() - done");
        }

        private void OptimizeButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
