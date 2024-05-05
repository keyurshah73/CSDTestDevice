using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTestDevice.LogData
{
    public static class LogAction
    {
        private static object _Locklogfile = new object(); 
        private static object _LockCSVfile = new object(); 

        public static void LogDebug(string LogData)
        {
            lock (_Locklogfile)
            {
                if (!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("DebugLog");
                }
                // Format: DebugLog_MM_YYYY.txt  Example: DebugLog_04_2024.txt
                StreamWriter StreamWriter = new StreamWriter(@"DebugLog\" + "DebugLog_" + DateTime.Now.ToString("MM") + "_" + DateTime.Now.ToString("yyyy") + ".txt", true);
                StreamWriter.WriteLine(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ": " + LogData);
                StreamWriter.Close();
            }
        }

        public static void LogCSV(string LogData)
        {
            lock (_LockCSVfile)
            {
                if (!Directory.Exists("Output"))
                {
                    Directory.CreateDirectory("Output");
                }
                if (!File.Exists(@"Output\" + "Result_" + DateTime.Now.ToString("MM") + "_" + DateTime.Now.ToString("yyyy") + ".csv"))
                {
                    StreamWriter StreamWriter = new StreamWriter(@"Output\" + "Result_" + DateTime.Now.ToString("MM") + "_" + DateTime.Now.ToString("yyyy") + ".csv", true);
                    StreamWriter.WriteLine("Date Time,Battery Barcode, Program No, Test Pressure[mbar], Filling Pressure[mbar], Test Decay[mbar], Outcome, Tester No");
                    StreamWriter.WriteLine(LogData);
                    StreamWriter.Close();
                }
                else
                {
                    // Format: Result_MM_YYYY.csv  Example: Result_04_2024.csv
                    StreamWriter StreamWriter = new StreamWriter(@"Output\" + "Result_"+ DateTime.Now.ToString("MM") + "_" + DateTime.Now.ToString("yyyy") + ".csv", true);
                    StreamWriter.WriteLine(LogData);
                    StreamWriter.Close();
                }
            }
        }
    }
}
