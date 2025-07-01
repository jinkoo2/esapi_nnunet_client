using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nnunet_client
{
    public class filesystem
    {
        public static string encode_path(string path0)
        {
            return path0.Replace(" ", "[sp]")
                .Replace("#", "[srp]")
                .Replace(".", "[dot]")
                .Replace("/", "[slsh]")
                .Replace("\\", "[bslsh]")
                .Replace("-", "[mns]")
                .Replace("%", "[prcnt]")
                .Replace(",", "[cmma]")
                .Replace("+", "[pls]")
                .Replace(":", "[cln]");
        }

        public static string decode_path(string path0)
        {
            return path0.Replace("[sp]", " ")
                .Replace("[srp]", "#")
                .Replace("[dot]", ".")
                .Replace("[slsh]", "/")
                .Replace("[bslsh]", "\\")
                .Replace("[mns]", "-")
                .Replace("[prcnt]", "%")
                .Replace("[cmma]", ",")
                .Replace("[pls]", "+")
                .Replace("[cln]", ":");

        }

        public static string join(string path1, string path2, bool make_dir = false)
        {
            string path = System.IO.Path.Combine(path1, path2);
            if (make_dir)
                mkdir(path);
            return path;
        }

        public static void mkdir(string dir)
        {
            if (System.IO.Directory.Exists(dir))
                return;

            System.IO.Directory.CreateDirectory(dir);
        }

        public static bool file_exists(string path)
        {
            return System.IO.File.Exists(path);
        }

        public static bool dir_exists(string path)
        {
            return System.IO.Directory.Exists(path);
        }

        public static bool move_file(string src, string dst)
        {
            if (file_exists(dst))
                System.IO.File.Delete(dst);

            System.IO.File.Move(src, dst);

            return true;
        }

        public static void write(string path, string txt)
        {
            System.IO.File.WriteAllText(path, txt);
        }

        public static void append(string path, string txt)
        {
            System.IO.File.AppendAllText(path, txt);
        }


        public static void writeline(string path, string line)
        {
            System.IO.File.WriteAllLines(path, new string[] { line });
        }
        public static void appendline(string path, string line)
        {
            System.IO.File.AppendAllLines(path, new string[] { line });
        }

        public static long get_dir_size(string folderPath)
        {
            if (!dir_exists(folderPath))
                return 0;

            long size = 0;

            // Create a DirectoryInfo object for the specified folder path
            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);

            // Get the size of all files in the current directory
            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                size += file.Length;
            }

            // Recursively get the size of all subdirectories
            foreach (DirectoryInfo subDirectory in directoryInfo.GetDirectories())
            {
                size += get_dir_size(subDirectory.FullName);
            }

            return size;
        }


        public static void copy_dir(string sourceDir, string targetDir)
        {
            // Create target directory if it doesn't exist
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Copy files
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                if(!file_exists(targetFile))
                    File.Copy(file, targetFile, false);
            }

            // Recursively copy subdirectories
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string targetSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
                copy_dir(subDir, targetSubDir);
            }

        }
    
        public static void write_all_text(string path, string text)
        {
            System.IO.File.WriteAllText(path, text);
        }

        private static void _err(String msg)
        {
            Console.WriteLine("Error:" + msg);
            throw new Exception("Error:" + msg);
        }

        public static void file_must_exist(string path)
        {
            if (!filesystem.file_exists(path))
                _err($"file not found:{path}");
        }
        public static void dir_must_exist(string path)
        {
            if (!filesystem.dir_exists(path))
                _err($"folder not found:{path}");
        }

    }
}
