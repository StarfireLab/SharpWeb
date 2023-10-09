using System;
using System.IO;
using System.Security.Principal;
using System.Collections.Generic;

namespace SharpWeb.Utilities
{
    class Current
    {
        public static bool IsHighIntegrity()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string is_true_false(string value)
        {
            if (value == "1")
            {
                return "True";
            }
            else
            {
                return "False";
            }
        }

        public static bool FileExists(string[] paths)
        {
            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    return true;
                }
            }
            return false;
        }

        public static void WriteCSV(List<string[]> data, string fileName)
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(fileName);

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                using (StreamWriter writer = new StreamWriter(fileName, false))
                {
                    string[] header = data[0];
                    writer.WriteLine(string.Join(",", header));

                    for (int i = 1; i < data.Count; i++)
                    {
                        writer.WriteLine(string.Join(",", data[i]));
                    }
                }

                Console.WriteLine("CSV file written successfully to: " + fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing CSV file: " + ex.Message);
            }
        }

        public static string CreateTmpFile(string dbfile)
        {
            try
            {
                string tempFile = Path.GetTempFileName();
                File.Copy(dbfile, tempFile, true);
                return tempFile;
            }
            catch
            {
                return null;
            }
        }

        public static string PatchWALDatabase(string tempDBFile)
        {
            // I couldn't figure out a safe way to open WAL enabled sqlite DBs (https://github.com/metacore/csharp-sqlite/issues/112)
            // So we'll "patch" temporary DB files we're reading to disable WAL journaling
            // Patch idea from here (https://stackoverflow.com/a/5476850)
            // WARNING - Don't use this patch on live/production sqlite DB files, always create temp duplicates first then patch the copy
            var offsets = new List<int> { 0x12, 0x13 };

            foreach (var n in offsets)

                using (var fs = new FileStream(tempDBFile, FileMode.Open, FileAccess.ReadWrite))
                {
                    fs.Position = n;
                    fs.WriteByte(Convert.ToByte(0x1));
                }
            return tempDBFile;
        }
    }
}
