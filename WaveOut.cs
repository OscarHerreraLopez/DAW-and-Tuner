using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAW
{
    public class WaveOut
    {
        public WaveOut()
        {
            Play();
        }

        public void Play()
        {
            var waveOut = new NAudio.Wave.WaveOut();
            waveOut.DeviceNumber = 0;
            string pathToDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);


            string filePath = null;
            AudioFileReader reader = new AudioFileReader(filePath);
            waveOut.Init(reader);
            waveOut.Play();
        }
    }
}