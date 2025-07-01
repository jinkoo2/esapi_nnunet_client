using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nnunet_client
{
    public static class helper
    {
        public static void log(string message)
        {
            Console.WriteLine(message);
        }

        public static void print(string message)
        {
            Console.WriteLine(message);
        }

        public static void error(string message)
        {
            Console.WriteLine(message);
        }



        public static string join(string path1, string path2)
        {
            return System.IO.Path.Combine(path1, path2);
        }


        public static Dictionary<string, object> json2dict(string jsonString)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
        }

        public static Dictionary<string, string> json2strstrdict(string jsonString)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
        }

        public static List<object> json2list(string jsonString)
        {
            return JsonConvert.DeserializeObject<List<object>>(jsonString);
        }


    }
}
