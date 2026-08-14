using System;
using System.IO;
using System.Threading.Tasks;

namespace AssistantAi.Helpers
{
    public static class FileHelper
    {
        /// <summary>
        /// Deletes a file if it exists, logging and reporting failures rather than
        /// throwing — callers use this for cleanup where a failure shouldn't abort the flow.
        /// </summary>
        public static Task DeleteAsync(string? filePath, ErrorLog log)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    File.Delete(filePath);
            }

            catch (Exception ex)
            {
                log.Write(ex);
                System.Windows.MessageBox.Show($"Delete File Exception: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
