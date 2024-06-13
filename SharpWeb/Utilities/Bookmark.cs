using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using static SharpWeb.Display.OutputFormatting;
using static SharpWeb.Browsers.Chromium;
using static SharpWeb.Utilities.Current;

namespace SharpWeb.Utilities
{
    public class Child
    {
        public string date_added { get; set; }
        public string guid { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string url { get; set; }
        public List<Child> children { get; set; } // 添加用于处理子文件夹的属性
    }

    public class RootObject
    {
        public string checksum { get; set; }
        public Dictionary<string, Children> roots { get; set; }
        public int version { get; set; }
    }

    public class Children
    {
        public List<Child> children { get; set; }
    }
    class Bookmark
    {
        static void TraverseFolders(List<Child> children, int depth, List<string[]> data)
        {
            string indentation = new string(' ', depth * 4);

            foreach (var child in children)
            {
                PrintSuccess($"{indentation}NAME: {child.name}", 1);

                if (child.url != null)
                {
                    PrintSuccess($"{indentation}URL: {child.url}", 1);
                    data.Add(new string[] { child.name, child.url });
                }

                if (child.children != null && child.children.Count > 0)
                {
                    PrintSuccess($"{indentation}Subfolder:", 1);
                    TraverseFolders(child.children, depth + 1, data);
                }
            }
        }

        public static void Treat(string path)
        {
            string[] header = new string[] { "NAME", "URL" };
            List<string[]> data = new List<string[]> { };

            JavaScriptSerializer serializer = new JavaScriptSerializer();

            var jsonObject = serializer.Deserialize<RootObject>(path);
            var roots = jsonObject.roots;

            foreach (var root in roots)
            {
                TraverseFolders(root.Value.children, 1, data);
            }
            string fileName = Path.Combine("out", browser_name + "_bookmark");
            if (Program.format.Equals("json", StringComparison.OrdinalIgnoreCase))
                WriteJson(header, data, fileName);
            else
                WriteCSV(header, data, fileName);
        }

    }
}
