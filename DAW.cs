using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio;
using NAudio.Wave;

namespace DAW
{
    public class DAW
    {
        private WaveFileWriter Writer;
        private NAudio.Wave.WaveIn wave;

        public DAW()
        {
            Record();
        }

        public void Record()
        {
            wave = new NAudio.Wave.WaveIn();
            wave.DeviceNumber = 0;
            wave.WaveFormat = new WaveFormat(44100, 16, 1);
            wave.DataAvailable += Wave_DataAvailable;
            wave.RecordingStopped += Wave_RecordingStopped;

            string pathToDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = pathToDesktop + "\\ExampleRecording.wav";
            Writer = new WaveFileWriter(filePath, wave.WaveFormat);

            wave.StartRecording();
        }

        public void StopRecording()
        {
            wave.StopRecording();
        }

        private void Wave_RecordingStopped(object sender, StoppedEventArgs e)
        {
            Writer.Dispose();
        }

        private void Wave_DataAvailable(object sender, WaveInEventArgs e)
        {
            Writer.Write(e.Buffer, 0, e.BytesRecorded);
        }
    }
}