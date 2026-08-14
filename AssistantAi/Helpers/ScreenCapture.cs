using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace AssistantAi.Helpers
{
    public static class ScreenCapture
    {
        /// <summary>
        /// Saves a PNG of the primary screen to <paramref name="outputPath"/>,
        /// creating the directory if needed. Returns the path written.
        /// </summary>
        public static string CaptureFullScreen(string outputPath)
        {
            string? directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            Rectangle bounds = System.Windows.Forms.Screen.GetBounds(Point.Empty);

            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }

                bitmap.Save(outputPath, ImageFormat.Png);
            }

            return outputPath;
        }
    }
}
