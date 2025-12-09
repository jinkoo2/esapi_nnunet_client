using esapi;
using nnunet_client.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Media.Effects;

using System.Windows;

using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using VMSApplication = VMS.TPS.Common.Model.API.Application;
using VMSCourse = VMS.TPS.Common.Model.API.Course;
using VMSHospital = VMS.TPS.Common.Model.API.Hospital;
using VMSImage = VMS.TPS.Common.Model.API.Image;
using VMSPatient = VMS.TPS.Common.Model.API.Patient;
using VMSReferencePoint = VMS.TPS.Common.Model.API.ReferencePoint;
using VMSRegistration = VMS.TPS.Common.Model.API.Registration;
using VMSSeries = VMS.TPS.Common.Model.API.Series;
using VMSStructure = VMS.TPS.Common.Model.API.Structure;
using VMSStructureSet = VMS.TPS.Common.Model.API.StructureSet;
using VMSStudy = VMS.TPS.Common.Model.API.Study;
using System.Windows.Documents.DocumentStructures;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.10")]
[assembly: AssemblyFileVersion("1.0.0.10")]
[assembly: AssemblyInformationalVersion("1.0.0.10")]
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

                //nnunet_client.models.DoseLimitEvaluator.RunTests();

                using (VMSApplication app = VMSApplication.CreateApplication())
                {
                    Execute(app);
                }
            }
            catch (Exception e)
            {
                // This catches exceptions thrown during VMSApplication setup or Execute() call.
                Console.Error.WriteLine("--- ESAPI Main Exception ---");
                Console.Error.WriteLine(e.ToString());
            }
        }


        static void Execute(VMS.TPS.Common.Model.API.Application vmsApp)
        {

            try
            {
                global.load_config();

                //VMSPatient pt = vmsApp.OpenPatientById("30013645");
                //VMSImage img = esapi.esapi.image_of_id("CBCT_9", pt);
                //esapi.exporter.export_image(img, @"U:\temp2", "cbct_0");
                //vmsApp.ClosePatient();


                global.vmsApplication = vmsApp;
                
                
                var wpfApp = new App();

                // ---------------------------------------------------------------------
                // CRITICAL FIX: Add a handler for exceptions occurring on the WPF UI thread (Dispatcher)
                // This catches exceptions inside button clicks, data bindings, etc.
                // ---------------------------------------------------------------------
                wpfApp.DispatcherUnhandledException += (sender, e) =>
                {
                    // Mark the exception as handled to prevent the application from crashing immediately.
                    e.Handled = true;

                    Console.Error.WriteLine("--- WPF Dispatcher Exception!!! ---");
                    Console.Error.WriteLine(e.Exception.ToString());

                    // Optionally show a user-friendly error message in a standard WPF MessageBox
                     MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}", 
                                     "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
                };

                //var window = new nnunet_client.DoseLimitEditorWindow();
                //var window = new nnunet_client.DoseLimitChecker(vmsApp);
                var window = new BladderART(vmsApp);
                //var window = new AutoContourWindow();


                wpfApp.Run(window);

                Console.WriteLine("done");
            }
            catch (Exception e)
            {
                // This catches exceptions thrown during the Execute method's setup phase, 
                // but before the WPF Dispatcher takes over.
                Console.Error.WriteLine("--- ESAPI Execute Exception ---");
                Console.Error.WriteLine(e.ToString());

                // Re-throw the exception so the main try/catch can log it if necessary
                throw;
            }
        }

    }
}
