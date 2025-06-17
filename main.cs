using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

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
            Console.WriteLine("Hello~~~");

            var wpfApp = new System.Windows.Application();

            try
            {
                var window = new ART(esapiApp);
                wpfApp.Run(window);
            }
            catch (Exception ex)
            {
                // Log the exception and continue
                System.Diagnostics.Debug.WriteLine("Unhandled UI exception: " + ex.ToString());
                Console.WriteLine("Error occurred, but app will continue running.");
            }

            Console.WriteLine("done");
        }

    }
}
