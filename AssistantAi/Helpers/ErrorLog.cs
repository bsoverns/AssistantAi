using System;
using AssistantAi.Class;

namespace AssistantAi.Helpers
{
    /// <summary>
    /// Wraps <see cref="LogWriter"/> with the log directory bound once, so callers
    /// write "_log.Write(ex)" instead of repeating the directory at every catch block.
    /// </summary>
    public class ErrorLog
    {
        private readonly string _directory;

        public ErrorLog(string directory)
        {
            _directory = directory;
        }

        public void Write(string message)
        {
            new LogWriter().WriteLog(_directory, message);
        }

        public void Write(Exception ex)
        {
            Write(ex.ToString());
        }

        /// <summary>Logs an exception prefixed with the input that triggered it.</summary>
        public void Write(string context, Exception ex)
        {
            Write(context + ":\r\n " + ex.ToString());
        }
    }
}
