using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using esapi;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

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
                using (Application app = Application.CreateApplication())
                {
                    Execute(app);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }



        static void Execute(Application esapiApp)
        {
            string dataDir = System.Configuration.ConfigurationManager.AppSettings["data_dir"];
            string templateDir = System.IO.Path.Combine(dataDir, "seg", "templates");
            TemplateManager templateManager = new TemplateManager();
            templateManager.LoadTemplates(templateDir);



            var wpfApp = new System.Windows.Application();

            var window = new ART(esapiApp);
            wpfApp.Run(window);

            Console.WriteLine("done");
        }

    }
}
