using System;
using System.IO;
using System.Reflection;

namespace AssistantAi.Models
{
    /// <summary>
    /// Every directory and file the app reads or writes, resolved once from the
    /// executable location. Nothing else should build these paths by hand.
    /// </summary>
    public class AppPaths
    {
        public AppPaths(string? programLocation = null)
        {
            ProgramLocation = programLocation
                ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? AppContext.BaseDirectory;

            string files = Path.Combine(ProgramLocation, "Files");

            Recordings = Path.Combine(files, "Sound recordings", "Recordings");
            Speech = Path.Combine(files, "Sound recordings", "Speech");
            ImageCaptures = Path.Combine(files, "Images", "Captures");
            ImageCreations = Path.Combine(files, "Images", "Creations");
            ErrorLogs = Path.Combine(files, "ErrorLogs");
            ApiKeyFile = Path.Combine(files, "ApiKey.json");
            ConversationDatabase = Path.Combine(files, "conversations.db");
        }

        /// <summary>Directory the executable was loaded from.</summary>
        public string ProgramLocation { get; }

        /// <summary>Microphone captures awaiting transcription.</summary>
        public string Recordings { get; }

        /// <summary>Text-to-speech output, deleted after playback.</summary>
        public string Speech { get; }

        /// <summary>Screenshots taken by the "Get Image" button.</summary>
        public string ImageCaptures { get; }

        /// <summary>Images returned by the image generation endpoint.</summary>
        public string ImageCreations { get; }

        /// <summary>Root passed to LogWriter; it adds the year and date beneath.</summary>
        public string ErrorLogs { get; }

        /// <summary>JSON file holding the OpenAI key.</summary>
        public string ApiKeyFile { get; }

        /// <summary>SQLite database of saved conversations.</summary>
        public string ConversationDatabase { get; }

        /// <summary>Builds a timestamped path inside <paramref name="directory"/>.</summary>
        public static string TimestampedFile(string directory, string prefix, string extension)
        {
            return Path.Combine(directory, $"{prefix}_{DateTime.Now:yyyyMMddHHmmss}.{extension}");
        }
    }
}
