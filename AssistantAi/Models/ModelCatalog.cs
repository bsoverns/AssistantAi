using System.Collections.Generic;

namespace AssistantAi.Models
{
    /// <summary>
    /// The model, endpoint and voice identifiers offered in the UI drop-downs.
    /// Adding support for a new model should only require editing a list here.
    /// </summary>
    public static class ModelCatalog
    {
        /// <summary>Chat/vision models shown in cmbModel.</summary>
        public static IReadOnlyList<string> ChatModels { get; } = new[]
        {
            "gpt-5.4", "gpt-5.4-mini", "gpt-5.4-nano",
            "gpt-5", "gpt-5-mini",
            "gpt-4.1", "gpt-4.1-mini", "gpt-4.1-nano",
            "gpt-4o", "gpt-4o-mini",
            "o3", "o3-pro", "o3-mini", "o4-mini"
        };

        /// <summary>Reserved for the not-yet-implemented realtime mode.</summary>
        public static IReadOnlyList<string> RealtimeModels { get; } = new[]
        {
            "gpt-4o-realtime-preview", "gpt-4o-mini-realtime-preview"
        };

        /// <summary>Whisper endpoints shown in cmbVoice — appended to /v1/audio/.</summary>
        public static IReadOnlyList<string> WhisperEndPoints { get; } = new[]
        {
            "transcriptions", "translations"
        };

        /// <summary>Text-to-speech models shown in cmbVoiceModel.</summary>
        public static IReadOnlyList<string> TtsModels { get; } = new[]
        {
            "tts-1", "tts-1-hd", "gpt-4o-mini-tts"
        };

        /// <summary>Speech voices shown in cmbAudioVoice.</summary>
        public static IReadOnlyList<string> Voices { get; } = new[]
        {
            "alloy", "ash", "ballad", "coral", "echo", "fable", "onyx",
            "nova", "sage", "shimmer", "verse", "marin", "cedar"
        };

        /// <summary>
        /// True when the model can be used against the chat/completions endpoint.
        /// Used to decide whether to fall back to <see cref="AppDefaults.VisionModel"/>.
        /// </summary>
        public static bool IsChatModel(string model)
        {
            foreach (var m in ChatModels)
            {
                if (m == model)
                    return true;
            }
            return false;
        }
    }
}
