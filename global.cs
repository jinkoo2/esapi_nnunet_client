using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VMSApplication = VMS.TPS.Common.Model.API.Application;
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

namespace nnunet_client
{

    internal static class global
    {
        public static VMSPatient vmsPatient = null;
        public static VMSApplication vmsApplication = null;

        public static string data_root_secure = @"G:\data_secure";
        public static bool make_export_log = false; // do not make export_log.

        public static string nnunet_requester_id = "esapi_nnunet_client@varianEclipseTest";
        public static string nnunet_request_user_name = "Jinkoo Kim";
        public static string nnunet_request_user_email = "jinkoo.kim@stonybrookmedicine.edu";
        public static string nnunet_request_user_institution = "Stony Brook";

        public static string app_data_dir = @"C:\Users\jkim20\Documents\Eclipse Scripting API\Projects\esapi_nnunet_client\_data";
        public static string nnunet_server_url = "http://127.0.0.1:8000";

    }
}
