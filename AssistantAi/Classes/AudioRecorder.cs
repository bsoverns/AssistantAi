using NAudio.Wave;
using System;
using System.IO;

namespace YourNamespace
{
    public class AudioRecorder : IDisposable
    {
        private WaveInEvent waveSource = null;
        private WaveFileWriter waveFile = null;
        private bool isDisposed = false;
        private string outputFolderPath;

        public AudioRecorder(string folderPath)
        {
            outputFolderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
            Directory.CreateDirectory(outputFolderPath); // Ensure the directory exists
        }

        public void StartRecording(string fileName)
        {
            if (waveSource != null || waveFile != null)
                throw new InvalidOperationException("Recording is already in progress.");

            string outputFilePath = Path.Combine(outputFolderPath, fileName);

            waveSource = new WaveInEvent();
            waveSource.WaveFormat = new WaveFormat(44100, 1); // CD quality audio, mono
            waveSource.DataAvailable += WaveSource_DataAvailable;
            waveSource.RecordingStopped += WaveSource_RecordingStopped;

            waveFile = new WaveFileWriter(outputFilePath, waveSource.WaveFormat);
            waveSource.StartRecording();
        }

        public void StopRecording()
        {
            if (waveSource != null)
            {
                waveSource.StopRecording();
            }
        }

        private void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (!isDisposed && waveFile != null)
            {
                waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                waveFile.Flush();
            }
        }

        private void WaveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            Dispose();
            if (e.Exception != null)
            {
                Console.WriteLine("An error occurred during recording: " + e.Exception.Message);
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                waveFile?.Dispose();
                waveFile = null;
                waveSource?.Dispose();
                waveSource = null;
            }
        }
    }
}