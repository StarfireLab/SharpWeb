using System;
using System.Linq;
using SharpWeb.Utilities;
using static SharpWeb.Browsers.Chromium;
using static SharpWeb.Browsers.FireFox;
using static SharpWeb.Browsers.IE;

namespace SharpWeb
{
    class Program
    {
        public static bool aZip = false, Show = false, All = false;

        public static void Banner()
        {
            Console.WriteLine(@"
   _____ __                   _       __     __  
  / ___// /_  ____ __________| |     / /__  / /_ 
  \__ \/ __ \/ __ `/ ___/ __ \ | /| / / _ \/ __ \
 ___/ / / / / /_/ / /  / /_/ / |/ |/ /  __/ /_/ /
/____/_/ /_/\__,_/_/  / .___/|__/|__/\___/_.___/ 
                     /_/                         
                                        
");
        }

        static void Main(string[] args)
        {
            Banner();
            string argBrowsers = "";
            string argPath = "";
            foreach (var entry in args.Select((value, index) => new { index, value }))
            {
                string argument = entry.value.ToUpper();

                switch (argument)
                {
                    case "-B":
                    case "/B":
                        argBrowsers = args[entry.index + 1];
                        break;
                    case "-P":
                    case "/P":
                        argPath = args[entry.index + 1];
                        break;
                    case "-ZIP":
                    case "/ZIP":
                        aZip = true;
                        break;
                    case "-SHOW":
                    case "/SHOW":
                        Show = true;
                        break;
                    case "-ALL":
                    case "/ALL":
                        All = true;
                        break;
                }
            }

            if (args == null || !args.Any() || args.Length <= 1 && (args[0].Equals("-h") || args[0].Equals("-help")))
            {
                Console.WriteLine(@"Export all browingdata(password/cookie/history/download/bookmark) from browser
By @lele8

  -all           Obtain all browser data
  -b             Available browsers: chromium/firefox/ie
  -p             Custom profile dir path, get with chrome://version
  -show          Output the results on the command line
  -zip           Compress result to zip (default: false)

  Usage: 
       SharpWeb.exe -all
       SharpWeb.exe -all -zip
       SharpWeb.exe -all -show
       SharpWeb.exe -b firefox
       SharpWeb.exe -b chromium -p ""C:\Users\test\AppData\Local\Google\Chrome\User Data\Default""
");
            }
            else if (!string.IsNullOrEmpty(argBrowsers))
            {
                if (argBrowsers.Equals("chromium", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(argPath))
                    Specify_path(argBrowsers, argPath);
                else if (argBrowsers.Equals("firefox", StringComparison.OrdinalIgnoreCase))
                    GetFireFox();
                else if(argBrowsers.Equals("chromium", StringComparison.OrdinalIgnoreCase))
                    Chromium_kernel();
                else if (argBrowsers.Equals("ie", StringComparison.OrdinalIgnoreCase))
                    GetIE();
            }
            else if (All)
            {
                Chromium_kernel();
                GetFireFox();
                GetIE();
            }
            if (aZip)
                Zip.saveZip();
        }
    }

}