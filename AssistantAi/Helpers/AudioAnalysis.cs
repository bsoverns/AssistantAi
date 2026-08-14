using System;
using NAudio.Wave;

namespace AssistantAi.Helpers
{
    public static class AudioAnalysis
    {
        /// <summary>Peak amplitude below this counts as silence.</summary>
        private const float SilenceThreshold = 0.01f;

        /// <summary>
        /// True when the recording peaks above the silence threshold. Used to skip
        /// uploading (and paying for) chunks that captured nothing but room noise.
        /// </summary>
        public static bool HasSpeech(string filePath)
        {
            using (var reader = new AudioFileReader(filePath))
            {
                float maxVolume = 0f;
                float[] buffer = new float[reader.WaveFormat.SampleRate];
                int read;

                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int n = 0; n < read; n++)
                    {
                        var abs = Math.Abs(buffer[n]);
                        if (abs > maxVolume)
                            maxVolume = abs;
                    }
                }

                Console.WriteLine("Max volume: " + maxVolume);
                return maxVolume > SilenceThreshold;
            }
        }
    }
}
