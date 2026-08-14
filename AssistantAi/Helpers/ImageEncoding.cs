using System;
using System.IO;
using System.Threading.Tasks;

namespace AssistantAi.Helpers
{
    /// <summary>Converts image files into the base64 data URIs the API expects.</summary>
    public static class ImageEncoding
    {
        public static string ToBase64(string imagePath)
        {
            return Convert.ToBase64String(File.ReadAllBytes(imagePath));
        }

        public static async Task<string> ToBase64Async(string imagePath)
        {
            return Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath));
        }

        /// <summary>Maps a file extension to its MIME type.</summary>
        /// <exception cref="NotSupportedException">The extension isn't a supported image type.</exception>
        public static string GetMimeType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            return ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                _ => throw new NotSupportedException($"Unsupported image extension: {ext}")
            };
        }
    }
}
