using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using esapi;

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
using System.Windows.Media.Effects;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]
[assembly: ESAPIScript(IsWriteable = true)]

// TODO: Uncomment the following line if the script requires write access.
// [assembly: ESAPIScript(IsWriteable = true)]

namespace nnunet_client
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                using (VMSApplication app = VMSApplication.CreateApplication())
                {
                    Execute(app);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }



        static void Execute(Application vmsApp)
        {


            //VMSPatient pt = vmsApp.OpenPatientById("30013645");
            //VMSImage img = esapi.esapi.image_of_id("CBCT_9", pt);
            //esapi.exporter.export_image(img, @"U:\temp2", "cbct_0");
            //vmsApp.ClosePatient();


            global.vmsApplication = vmsApp;


            string dataDir = System.Configuration.ConfigurationManager.AppSettings["data_dir"];
            string templateDir = System.IO.Path.Combine(dataDir, "seg", "templates");
            TemplateManager templateManager = new TemplateManager();
            templateManager.LoadTemplates(templateDir);



            var wpfApp = new System.Windows.Application();

            var window = new ART(vmsApp);
            wpfApp.Run(window);

            Console.WriteLine("done");
        }

    }
}
