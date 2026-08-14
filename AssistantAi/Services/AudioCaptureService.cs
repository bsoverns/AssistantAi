using System;
using System.Collections.Generic;
using System.IO;
using AssistantAi.Models;

namespace AssistantAi.Services
{
    /// <summary>
    /// Owns the live <see cref="AudioRecorder"/> instances and the queue of captured
    /// files waiting to be transcribed.
    /// </summary>
    public class AudioCaptureService
    {
        private readonly List<AudioRecorder> _activeRecorders = new List<AudioRecorder>();
        private readonly List<string> _queue = new List<string>();

        /// <summary>Files captured and not yet transcribed, oldest first.</summary>
        public IReadOnlyList<string> Queue => _queue;

        /// <summary>Path of the most recently started recording.</summary>
        public string? CurrentRecordingPath { get; private set; }

        /// <summary>
        /// Starts a new recording in <paramref name="recordingsDirectory"/>, queues it,
        /// and returns its path.
        /// </summary>
        public string StartRecording(string recordingsDirectory)
        {
            Directory.CreateDirectory(recordingsDirectory);

            string path = AppPaths.TimestampedFile(recordingsDirectory, "Recording", "wav");
            CurrentRecordingPath = path;
            _queue.Add(path);

            var recorder = new AudioRecorder(recordingsDirectory);
            recorder.StartRecording(path);
            _activeRecorders.Add(recorder);

            return path;
        }

        /// <summary>
        /// Stops and disposes every active recorder. All recorders are processed even
        /// if one throws; the failures are then reported together.
        /// </summary>
        public void StopAll()
        {
            List<Exception>? failures = null;

            foreach (var recorder in _activeRecorders)
            {
                try
                {
                    recorder.StopRecording();
                    recorder.Dispose();
                }

                catch (Exception ex)
                {
                    (failures ??= new List<Exception>()).Add(ex);
                }
            }

            _activeRecorders.Clear();

            if (failures != null)
                throw new AggregateException("One or more recorders failed to stop.", failures);
        }

        public void ClearQueue()
        {
            _queue.Clear();
        }

        public void RemoveFromQueue(int index)
        {
            _queue.RemoveAt(index);
        }
    }
}
