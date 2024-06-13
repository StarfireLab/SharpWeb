using System;
using System.IO;
using System.Security.Principal;
using System.Collections.Generic;
using System.Web.Script.Serialization;

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

        public static void WriteCSV(string[] header, List<string[]> data, string fileName)
        {
            try
            {
                string file = String.Format("{0}.csv", fileName);
                string directoryPath = Path.GetDirectoryName(file);

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                using (StreamWriter writer = new StreamWriter(file, false))
                {
                    writer.WriteLine(string.Join(",", header));

                    for (int i = 0; i < data.Count; i++)
                    {
                        writer.WriteLine(string.Join(",", data[i]));
                    }
                }

                Console.WriteLine("CSV file written successfully to: " + file);
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

        public static void WriteJson(string[] names, List<string[]> values, string filePath)
        {
            try
            {
                string file = String.Format("{0}.json", filePath);

                string directoryPath = Path.GetDirectoryName(file);

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                List<Dictionary<string, object>> cookies = new List<Dictionary<string, object>>();

                foreach (var innerArray in values)
                {
                    Dictionary<string, object> cookie = new Dictionary<string, object>();

                    for (int j = 0; j < names.Length; j++)
                    {
                        string currentValue = innerArray[j];
                        if (string.IsNullOrEmpty(currentValue))
                        {
                            cookie[names[j]] = string.Empty;
                        }
                        else
                        {
                            if (bool.TryParse(currentValue, out bool boolValue))
                            {
                                cookie[names[j]] = boolValue;
                            }
                            else if (double.TryParse(currentValue, out double doubleValue) && !names[j].Equals("storeId") && !names[j].Equals("value"))
                            {
                                cookie[names[j]] = doubleValue;
                            }
                            else
                            {
                                cookie[names[j]] = currentValue;
                            }
                        }
                    }

                    cookies.Add(cookie);
                }

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string jsonData = serializer.Serialize(cookies);
                var formattedJsonString = FormatJson(jsonData);
                File.WriteAllText(file, formattedJsonString);
                Console.WriteLine("JSON file written successfully to: " + file);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing JSON file: " + ex.Message);
            }
        }

        public static string FormatJson(string json)
        {
            int indentation = 0;
            int quoteCount = 0;
            var result = new StringWriter();
            for (int i = 0; i < json.Length; i++)
            {
                var ch = json[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        result.Write(ch);
                        result.Write(Environment.NewLine);
                        result.Write(new string(' ', ++indentation * 4));
                        break;
                    case '}':
                    case ']':
                        result.Write(Environment.NewLine);
                        result.Write(new string(' ', --indentation * 4));
                        result.Write(ch);
                        break;
                    case '"':
                        result.Write(ch);
                        quoteCount++;
                        break;
                    case ',':
                        result.Write(ch);
                        if (quoteCount % 2 == 0)
                        {
                            result.Write(Environment.NewLine);
                            result.Write(new string(' ', indentation * 4));
                        }
                        break;
                    case ':':
                        result.Write(ch);
                        result.Write(' ');
                        break;
                    default:
                        result.Write(ch);
                        break;
                }
            }
            return result.ToString();
        }

        public static string TryParsesameSite(string sameSite)
        {
            int intsameSite = -1;
            int.TryParse(sameSite, out intsameSite);

            string sameSiteString = "";
            switch (intsameSite)
            {
                case -1:
                    sameSiteString = "unspecified";
                    break;
                case 0:
                    sameSiteString = "no_restriction";
                    break;
                case 1:
                    sameSiteString = "lax";
                    break;
                case 2:
                    sameSiteString = "strict";
                    break;
                default:
                    throw new Exception($"Unexpected SameSite value {sameSite}");
            }
            return sameSiteString;
        }
    }
}
