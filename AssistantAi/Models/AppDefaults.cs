namespace AssistantAi.Models
{
    /// <summary>
    /// Default selections and starting field values applied on startup.
    /// </summary>
    public static class AppDefaults
    {
        /// <summary>Model pre-selected in cmbModel.</summary>
        public const string ChatModel = "gpt-5-mini";

        /// <summary>Reserved for the not-yet-implemented realtime mode.</summary>
        public const string RealtimeModel = "gpt-4o-mini-realtime-preview";

        /// <summary>Endpoint pre-selected in cmbVoice.</summary>
        public const string WhisperEndPoint = "transcriptions";

        /// <summary>Speech-to-text model used for all transcription/translation calls.</summary>
        public const string WhisperModel = "gpt-4o-mini-transcribe";

        /// <summary>Voice pre-selected in cmbAudioVoice.</summary>
        public const string AudioVoice = "onyx";

        /// <summary>Model pre-selected in cmbVoiceModel.</summary>
        public const string TtsModel = "gpt-4o-mini-tts";

        /// <summary>Fallback used when the selected chat model can't accept image input.</summary>
        public const string VisionModel = "gpt-5-mini";

        // dall-e-2 / dall-e-3 were shut down on the API May 12, 2026 -- they now return 400.
        // Replacements: gpt-image-2 (latest), gpt-image-1.5, gpt-image-1, gpt-image-1-mini (cheapest).
        /// <summary>Model used by the "Create Image" checkbox.</summary>
        public const string ImageGenerationModel = "gpt-image-2";

        /// <summary>Generated image resolution.</summary>
        public const string ImageSize = "1024x1024";

        /// <summary>Generated image quality: low | medium | high | auto.</summary>
        public const string ImageQuality = "auto";

        /// <summary>Starting value for txtMaxTokens.</summary>
        public const string MaxTokens = "2048";

        /// <summary>Starting value for txtMaxDollars.</summary>
        public const string MaxDollars = "0.50";

        // https://platform.openai.com/docs/guides/text-generation/reproducible-outputs
        /// <summary>Starting value for txtTemperature.</summary>
        public const string Temperature = "0.5";

        /// <summary>Starting value for txtUserId. Not used by any request yet.</summary>
        public const string UserId = "1";

        /// <summary>Seconds recorded in "Translate/Transcribe" mode before auto-stop.</summary>
        public const int StandardListeningSeconds = 30;

        /// <summary>Seconds per chunk in "Continuous STT" mode.</summary>
        public const int ContinuousListeningSeconds = 5;

        /// <summary>How often the OpenAI status light refreshes, in milliseconds.</summary>
        public const int ApiStatusCheckIntervalMs = 30000;

        /// <summary>Placeholder dropped into txtQuestion when Image Review Mode is enabled.</summary>
        public const string ImageReviewInstructions =
            @"You have selected to upload a list of images for an AI to review.  Please replace this text with your request.  This request will be the same for each image.  An example request is 'Attached is a review sheet that I completed.  Can you please review my answers for mistakes, and provide the correct answers if possible as well as a description for why that answer is correct";
    }
}
