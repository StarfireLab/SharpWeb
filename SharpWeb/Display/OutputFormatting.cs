using System;
using static SharpWeb.Utilities.Enums;

namespace SharpWeb.Display
{
    internal class OutputFormatting
    {
        public static void PrintSuccess(string msg, int indentation = 0)
        {
            if (Program.Show)
            {
                Print(OUTPUT_TYPE.SUCCESS, msg, indentation);
            }
        }

        public static void PrintNorma(string msg, int indentation = 0)
        {
            if (Program.Show)
            {
                Print(OUTPUT_TYPE.Normal, msg, indentation);
            }
        }

        public static void PrintVerbose(string msg, int indentation = 0)
        {
            Print(OUTPUT_TYPE.VERBOSE, msg, indentation);
        }

        public static void PrintError(string msg, int indentation = 0)
        {
            Print(OUTPUT_TYPE.ERROR, msg, indentation);
        }

        public static void PrintFail(string msg, int indentation = 0)
        {
            Print(OUTPUT_TYPE.Fail, msg, indentation);
        }

        private static void Print(OUTPUT_TYPE msgType, string msg, int indentation = 0)
        {
            if (indentation != 0)
            {
                string tabs = new String('\t', indentation);
                Console.WriteLine("{0}[{1}] {2}", tabs, (char)msgType, msg);
            }
            else if(msgType== OUTPUT_TYPE.Normal)
            {
                Console.WriteLine("{0}",msg);
            }
            else
            {
                Console.WriteLine("[{0}] {1}", (char)msgType, msg);
            }
        }
    }
}
