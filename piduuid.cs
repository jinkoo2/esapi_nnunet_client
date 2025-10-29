using System.Collections.Generic;
using System.Linq;

namespace nnunet_client
{
    public static class piduuid
    {
        static Dictionary<string, string> dict_uuid2pid = null;
        static Dictionary<string, string> dict_pid2uuid = null;

        public static void init_db()
        {
            string fn = "piduuid.init_db()";
            helper.print(fn);

            dict_uuid2pid = new Dictionary<string, string>();
            dict_pid2uuid = new Dictionary<string, string>();

            string file = System.IO.Path.Combine(global.appConfig.data_root_secure, "id2uuid.txt");
            string[] lines = System.IO.File.ReadAllLines(file);
            foreach (string line in lines)
            {
                if (line.Trim() == "")
                    continue;

                string[] parts = line.Split('=');
                string _pid = parts[0].Trim(); // pid
                string _uuid = parts[1].Trim(); // uuid

                // pid2uuid dictionary
                if (dict_pid2uuid.ContainsKey(_pid))
                {
                    // this should not happen
                    helper.error(string.Format("Duplite PID found! uuid={0}, pid={1}! So, skipping line={2}", _uuid, _pid, line));
                }
                else
                {
                    dict_pid2uuid.Add(_pid, _uuid);
                }

                // uuid2pid dictionary
                if (dict_uuid2pid.ContainsKey(_uuid))
                {
                    // this should not happen
                    helper.error(string.Format("Duplite UUID found! uuid={0}, pid={1}! So, skipping line={2}", _uuid, _pid, line));
                }
                else
                {
                    dict_uuid2pid.Add(_uuid, _pid);
                }
            }
        }

        public static string uuid2pid(string uuid)
        {
            if (dict_uuid2pid == null)
                init_db();

            if (!dict_uuid2pid.ContainsKey(uuid))
                return "";

            return dict_uuid2pid[uuid];
        }
        public static string pid2uuid(string pid)
        {
            if (dict_uuid2pid == null)
                init_db();

            if (!dict_pid2uuid.ContainsKey(pid))
                return "";

            return dict_pid2uuid[pid];
        }

        public static string[] get_pid_list()
        {
            if (dict_uuid2pid == null)
                init_db();

            return dict_pid2uuid.Keys.ToArray<string>();
        }

        public static string[] get_uuid_list()
        {
            if (dict_uuid2pid == null)
                init_db();

            return dict_uuid2pid.Keys.ToArray<string>();
        }
    }


}