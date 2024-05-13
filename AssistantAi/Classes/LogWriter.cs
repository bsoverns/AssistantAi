using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AssistantAi.Class
{
    class LogWriter
    {
        public void WriteLog(string path, string message)
        {
            string PATH = path;
            string PATH_FILE_combine = System.IO.Path.Combine(PATH, DateTime.Today.ToString("yyyy"), DateTime.Today.ToString("yyyyMMdd") + ".log");

            //*Create data directory if needed
            DirectoryInfo data = Directory.CreateDirectory(PATH_FILE_combine.Replace(DateTime.Today.ToString("yyyyMMdd") + ".log", ""));

            try
            {
                while (IsFileLocked(PATH_FILE_combine))
                {
                    System.Threading.Thread.Sleep(100);
                }

                using (StreamWriter Writer = new StreamWriter(PATH_FILE_combine, true))
                {
                    //while (FileLocked(PATH_FILE_combine))
                    //while (IsFileLocked(PATH_FILE_combine))
                    //{
                    //    System.Threading.Thread.Sleep(100);
                    //}

                    Writer.WriteLine(message.ToString() + "\n");
                }
            }

            catch (Exception ex)
            {
                string errorPath = @"@error_logs\";
                using (StreamWriter errorWriter = new StreamWriter(System.IO.Path.Combine(errorPath, "ErrorLog_" + DateTime.Today.ToString("yyyyMMdd") + ".log"), true))
                {
                    errorWriter.WriteLine(PATH_FILE_combine.ToString() + " - " + ex.ToString() + "\n");
                }
            }
        }

        public static bool FileLocked(string FileName)
        {
            if (File.Exists(FileName))
            {
                FileStream fs = null;

                try
                {
                    // NOTE: This doesn't handle situations where file is opened for writing by another process but put into write shared mode, it will not throw an exception and won't show it as write locked
                    fs = File.Open(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None); // If we can't open file for reading and writing then it's locked by another process for writing
                }

                catch (UnauthorizedAccessException) // https://msdn.microsoft.com/en-us/library/y973b725(v=vs.110).aspx
                {
                    // This is because the file is Read-Only and we tried to open in ReadWrite mode, now try to open in Read only mode
                    try
                    {
                        fs = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.None);
                    }

                    catch (Exception)
                    {
                        return true; // This file has been locked, we can't even open it to read
                    }
                }

                catch (Exception)
                {
                    return true; // This file has been locked
                }

                finally
                {
                    if (fs != null)
                    {
                        fs.Close();
                    }
                }
            }

            return false;
        }

        public bool IsFileLocked(string filename)
        {
            bool Locked = false;

            try
            {
                FileStream fs = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                fs.Close();
            }

            catch
            {
                Locked = true;
            }

            return Locked;
        }
    }
}
