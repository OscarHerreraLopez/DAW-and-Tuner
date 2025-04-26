using NAudio.Wave;
using System;
using System.IO;
using System.Windows.Forms;

namespace DAW
{
    public partial class WaveIn : Form
    {
        private WaveInEvent waveSource = null;
        private WaveFileWriter waveWriter = null;
        private string outputFilename = "output.wav";
        public string OutputFilename { get; private set; }
        public bool IsRecording { get; private set; } = false;

        public WaveIn()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "WaveIn";
        }

        private void StartRecording_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Wave Files (*.wav)|*.wav";
            saveFileDialog.Title = "Save Recorded Audio";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                outputFilename = saveFileDialog.FileName;
                OutputFilename = outputFilename;
                IsRecording = true;

                try
                {
                    waveSource = new WaveInEvent();
                    waveSource.WaveFormat = new WaveFormat(44100, 1);
                    waveSource.DataAvailable += WaveSource_DataAvailable;
                    waveSource.RecordingStopped += WaveSource_RecordingStopped;
                    waveWriter = new WaveFileWriter(outputFilename, waveSource.WaveFormat);
                    waveSource.StartRecording();
                   
                    recordButton.Enabled = false;
                    stopButton.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error starting recording: {ex.Message}");
                    IsRecording = false;
                    
                    if (waveSource != null)
                    {
                        waveSource.Dispose();
                        waveSource = null;
                    }
                    if (waveWriter != null)
                    {
                        waveWriter.Dispose();
                        waveWriter = null;
                    }
                }
            }
        }

        private void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveWriter != null)
            {
                waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
                waveWriter.Flush();
            }
        }

        private void WaveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (waveSource != null)
            {
                waveSource.Dispose();
                waveSource = null;
            }
            if (waveWriter != null)
            {
                waveWriter.Dispose();
                waveWriter = null;
            }
            IsRecording = false;
            recordButton.Enabled = true;
            stopButton.Enabled = false;
        }

        private void StopRecording_Click_1(object sender, EventArgs e)
        {
            if (waveSource != null)
            {
                try
                {
                    waveSource.StopRecording();
                }
                catch (InvalidOperationException)
                {
                    MessageBox.Show("No recording in progress.");
                }
            }
            else
            {
                MessageBox.Show("No recording in progress.");
            }
        }

        private Button recordButton;
        private Button stopButton;

        private void InitializeComponent()
        {
            this.recordButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // recordButton
            // 
            this.recordButton.Location = new System.Drawing.Point(23, 90);
            this.recordButton.Name = "recordButton";
            this.recordButton.Size = new System.Drawing.Size(102, 45);
            this.recordButton.TabIndex = 0;
            this.recordButton.Text = "Record";
            this.recordButton.UseVisualStyleBackColor = true;
            this.recordButton.Click += new System.EventHandler(this.recordButton_Click);
            // 
            // stopButton
            // 
            this.stopButton.Location = new System.Drawing.Point(163, 90);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(98, 45);
            this.stopButton.TabIndex = 1;
            this.stopButton.Text = "Stop";
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // WaveIn
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.recordButton);
            this.Name = "WaveIn";
            this.Text = "WaveIn";
            this.ResumeLayout(false);

        }
        private void recordButton_Click(object sender, EventArgs e)
        {
            StartRecording_Click(sender, e);
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            StopRecording_Click_1(sender, e);
        }

        
    }
}