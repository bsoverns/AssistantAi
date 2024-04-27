using NAudio.Wave;
using System;
using System.IO;

namespace YourNamespace
{
    public class AudioRecorder
    {
        private WaveInEvent waveSource = null;
        private WaveFileWriter waveFile = null;
        private string outputFolderPath;

        public AudioRecorder()
        {
        }

        public AudioRecorder(string folderPath)
        {
            outputFolderPath = folderPath;
            Directory.CreateDirectory(outputFolderPath); // Ensure the directory exists
        }

        public void StartRecording(string fileName)
        {
            waveSource = new WaveInEvent();
            waveSource.WaveFormat = new WaveFormat(44100, 1); // CD quality audio, mono

            waveSource.DataAvailable += WaveSource_DataAvailable;
            waveSource.RecordingStopped += WaveSource_RecordingStopped;

            string outputFilePath = fileName;
            waveFile = new WaveFileWriter(outputFilePath, waveSource.WaveFormat);

            waveSource.StartRecording();
        }

        public void StopRecording()
        {
            if (waveSource != null)
            {
                waveSource.StopRecording();
            }

            if (waveFile != null)
            {
                waveFile.Close(); // Explicitly close the file
                waveFile.Dispose();
                waveFile = null;
            }

            if (waveSource != null)
            {
                waveSource.Dispose();
                waveSource = null;
            }
        }

        private void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveFile != null)
            {
                waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                waveFile.Flush();
            }
        }

        private void WaveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (waveFile != null)
            {
                waveFile.Dispose();
                waveFile = null;
            }

            if (waveSource != null)
            {
                waveSource.Dispose();
                waveSource = null;
            }

            // If there was an exception during recording, expose it here
            if (e.Exception != null)
            {
                throw e.Exception;
            }
        }
    }
}
