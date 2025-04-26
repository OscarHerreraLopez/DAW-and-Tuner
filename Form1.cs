using NAudio.Wave;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Numerics;
using NAudio.Dsp;
using DAW;
using NAudio.Wave.SampleProviders;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;


namespace Test_Tuner
{
    public partial class GuitarTuner : Form
    {
        private WaveInEvent waveIn;
        private BufferedWaveProvider bufferedWaveProvider;
        private Label frequencyLabel;
        private Label noteLabel;
        private Panel meterPanel;
        private float currentFrequency=0;
        private ComboBox microphoneComboBox;
        private Panel waveformPanel;
        private Button startButton;
        private Button stopButton;
        private bool audioInitialized = false;
        private float[] lastWaveformData;
        private bool isRecording = false;
        private const int SampleRate = 44100; // Sample rate for audio input
        private const int BufferSize = 2048; // Buffer size for audio input
        private Timer timer; // Timer for periodic UI updates

        //daw stuff
        private DAW.WaveIn waveInForm;
        private string recordedFilePathDAW;

        //needle
        private double _needlePosition;



        private DAW.DAW _daw;
        private Button recordButtonDAW;
        private Button stopButtonDAW;
        private Button playButtonDAW;
        private TabPage dawTabPage;
        private WaveFileWriter writer;
        private WaveInEvent dawWaveIn;
        private MixingSampleProvider mixer;
        private WaveOutEvent waveOut;
        private bool isRecordingDAW = false;
        


        public GuitarTuner()
        {
            waveInForm = new DAW.WaveIn();
            waveInForm.FormClosed += WaveInForm_FormClosed;
            recordedFilePathDAW = string.Empty;

            InitializeComponent();
            InitializeUI();
            ApplyDarkTheme();
            PopulateMicrophoneComboBox();

            timer = new Timer();
            timer.Interval = 50;
            timer.Enabled = true;
            timer.Tick += Timer_Tick;

            _daw = new DAW.DAW();
            InitializeDAWUI();

        }
   
       
        private void InitializeDAWUI()
        {

            // Create and configure the Record button for the DAW  
            recordButtonDAW = new Button();
            recordButtonDAW.Text = "DAW Record";
            recordButtonDAW.Location = new Point(10, 10);
            recordButtonDAW.Size = new Size(100, 30);
            recordButtonDAW.Click += RecordButtonDAW_Click_Integrated;  

            // Create and configure the Stop button for the DAW  
            stopButtonDAW = new Button();
            stopButtonDAW.Text = "DAW Stop";
            stopButtonDAW.Location = new Point(120, 10);
            stopButtonDAW.Size = new Size(100, 30);
            stopButtonDAW.Click += StopButtonDAW_Click_Integrated;  


        }
        private float calculatedFrequency = 0;
       
        private float smoothedFrequency = 0;
        private const float SmoothingFactor = 0.35f;
        private float ApplySmoothing(float currentFrequency)
        {
            if (smoothedFrequency == 0)
            {
                smoothedFrequency = currentFrequency;
            }
            else
            {
                smoothedFrequency = SmoothingFactor * currentFrequency + (1 - SmoothingFactor) * smoothedFrequency;
            }
            return smoothedFrequency;
        }


        private const float SmoothingFactorFast = 0.80f;
        private Queue<float> recentFrequencies = new Queue<float>(10); 
        private const int StableFrequencyCount = 5;
        private const float StabilityToleranceFast = 2.0f;
        const int FFT_LENGTH = 4096;
        private string _stableNote = "";
        private int _stableNoteCounter = 0;
        private const int _stableNoteThreshold = 1; 
        private double _currentCentsDifference = 0; 


        private void Timer_Tick(object sender, EventArgs e)
        {
            if (isRecording && bufferedWaveProvider != null && bufferedWaveProvider.BufferedBytes >= BufferSize * 2)
            {
                byte[] byteBuffer = new byte[BufferSize];
                int bytesRead = bufferedWaveProvider.Read(byteBuffer, 0, byteBuffer.Length);

                if (bytesRead > 0)
                {
                    // Convert byte buffer to float buffer for frequency calculation
                    float[] floatBufferForFrequency = ConvertByteToFloat(byteBuffer);
                    float calculatedFrequencyRaw = CalculateFrequency(floatBufferForFrequency, waveIn.WaveFormat.SampleRate);
                    float calculatedFrequencySmoothed = calculatedFrequencyRaw > 0 ? ApplySmoothing(calculatedFrequencyRaw, SmoothingFactorFast) : 0;
                    

                    // Convert byte buffer to short buffer for waveform data
                    short[] sampleBuffer = new short[bytesRead / 2];
                    Buffer.BlockCopy(byteBuffer, 0, sampleBuffer, 0, bytesRead);

                    // Convert short buffer to float buffer for waveform display
                    float[] floatBufferForWaveform = sampleBuffer.Select(s => s / 32768.0f).ToArray();
                    lastWaveformData = floatBufferForWaveform.Clone() as float[];

                    if (calculatedFrequencySmoothed > 0)
                    {
                        recentFrequencies.Enqueue(calculatedFrequencySmoothed);
                        if (recentFrequencies.Count > 10) 
                        {
                            recentFrequencies.Dequeue();
                        }

                        float currentSmoothedFrequency = recentFrequencies.Average(); 
                        var noteOctave = GetNoteAndOctave(currentSmoothedFrequency);
                        string noteName = noteOctave.Item1;
                        int octave = noteOctave.Item2;
                        string fullNoteName = noteName + octave.ToString();

                        double cents = 0;
                  
                        if (targetFrequencies.ContainsKey(fullNoteName))
                        {
                            cents = GetCentsDifference(currentSmoothedFrequency, (float)targetFrequencies[fullNoteName]);
                            currentCentsDifference = cents;
                            UpdateTunerUI(cents);
                           
                        }
                        else
                        {
                            currentCentsDifference = 0;
                            ResetTunerUI();
                        }

                    

                        try
                        {
                            BeginInvoke((MethodInvoker)delegate
                            {
                                frequencyLabel.Text = $"Frequency: {currentSmoothedFrequency:F2} Hz";
                                noteLabel.Text = $"Note: {fullNoteName}"; 
                                meterPanel.Invalidate();
                                waveformPanel.Invalidate();
                            });
                        }
                        catch (Exception ex) { Console.WriteLine($"BeginInvoke Error (UI Update): {ex.Message}"); }
                    }
                    else
                    {
                        try
                        {
                            BeginInvoke((MethodInvoker)delegate
                            {
                                frequencyLabel.Text = "Frequency: -";
                                noteLabel.Text = "Note: -";
                                meterPanel.Invalidate();
                                waveformPanel.Invalidate();
                            });
                        }
                        catch (Exception ex) { Console.WriteLine($"BeginInvoke Error (Reset UI): {ex.Message}"); }
                    }
                }
            }
        }
        private float ApplySmoothing(float currentFrequency, float factor)
        {
            if (smoothedFrequency == 0)
            {
                smoothedFrequency = currentFrequency;
            }
            else
            {
                smoothedFrequency = factor * currentFrequency + (1 - factor) * smoothedFrequency;
            }
            return smoothedFrequency;
        }
        private float[] ConvertByteToFloat(byte[] buffer)
        {
            float[] floatBuffer = new float[buffer.Length / 2];
            for (int i = 0; i < buffer.Length / 2; i++)
            {
                short sample = (short)(buffer[i * 2] | (buffer[i * 2 + 1] << 8));
                floatBuffer[i] = sample / 32768f;
            }
            return floatBuffer;
        }



        public static float CalculateFrequency(float[] samples, int sampleRate)
        {
            const int FFT_LENGTH = 4096;
            int numSamples = Math.Min(samples.Length, FFT_LENGTH);
            Complex[] complexBuffer = new Complex[FFT_LENGTH];

            // 1. Apply Hann window and copy to complex buffer
            for (int i = 0; i < numSamples; i++)
            {
                double window = 0.5f * (1 - (float)Math.Cos((2 * Math.PI * i) / (numSamples - 1)));
                complexBuffer[i] = new Complex { X = (float)(samples[i] * window), Y = 0.0f };
            }

            // 2. Pad with zeros
            for (int i = numSamples; i < FFT_LENGTH; i++)
            {
                complexBuffer[i] = new Complex { X = 0.0f, Y = 0.0f };
            }

            // 3. Perform FFT
            FastFourierTransform.FFT(true, (int)Math.Log(FFT_LENGTH, 2.0), complexBuffer);

            float detectedFrequency = 0;
            float bestMagnitude = 0;
            int bestIndex = 0;

            float minFrequency = 80;
            float maxFrequency = 400;
            int minIndex = (int)(minFrequency * FFT_LENGTH / (double)sampleRate);
            int maxIndex = (int)(maxFrequency * FFT_LENGTH / (double)sampleRate);

            // Find the initial peak magnitude
            for (int i = minIndex; i < maxIndex; i++)
            {
                if (i > 0 && i < FFT_LENGTH / 2)
                {
                    double real = complexBuffer[i].X;
                    double imaginary = complexBuffer[i].Y;
                    float magnitude = (float)Math.Sqrt(real * real + imaginary * imaginary);
                    if (magnitude > bestMagnitude)
                    {
                        bestMagnitude = magnitude;
                        bestIndex = i;
                    }
                }
            }

            float interpolatedIndex = bestIndex;

            // Peak Interpolation (Parabolic)
            if (bestIndex > 0 && bestIndex < FFT_LENGTH / 2 - 1)
            {
                double leftReal = complexBuffer[bestIndex - 1].X;
                double leftImaginary = complexBuffer[bestIndex - 1].Y;
                float leftMagnitude = (float)Math.Sqrt(leftReal * leftReal + leftImaginary * leftImaginary);

                double centerReal = complexBuffer[bestIndex].X;
                double centerImaginary = complexBuffer[bestIndex].Y;
                float centerMagnitude = bestMagnitude;

                double rightReal = complexBuffer[bestIndex + 1].X;
                double rightImaginary = complexBuffer[bestIndex + 1].Y;
                float rightMagnitude = (float)Math.Sqrt(rightReal * rightReal + rightImaginary * rightImaginary);

                float alpha = leftMagnitude;
                float beta = centerMagnitude;
                float gamma = rightMagnitude;

                float denominator = alpha - 2 * beta + gamma;
                if (denominator != 0)
                {
                    float p = 0.5f * (alpha - gamma) / denominator;
                    interpolatedIndex = bestIndex + p;
                }
            }

            
            float threshold = bestMagnitude * 0.6f; 
            bestMagnitude = 0;
            bestIndex = 0;
            for (int i = minIndex; i < maxIndex; i++)
            {
                if (i > 0 && i < FFT_LENGTH / 2)
                {
                    double real = complexBuffer[i].X;
                    double imaginary = complexBuffer[i].Y;
                    float magnitude = (float)Math.Sqrt(real * real + imaginary * imaginary);
                    if (magnitude > threshold && magnitude > bestMagnitude)
                    {
                        bestMagnitude = magnitude;
                        bestIndex = i;
                    }
                }
            }

            if (bestIndex > 0)
            {
                interpolatedIndex = bestIndex;
                // Re-interpolate based on the potentially new bestIndex
                if (bestIndex > 0 && bestIndex < FFT_LENGTH / 2 - 1 && bestMagnitude > 0)
                {
                    double leftReal = complexBuffer[bestIndex - 1].X;
                    double leftImaginary = complexBuffer[bestIndex - 1].Y;
                    float leftMagnitude = (float)Math.Sqrt(leftReal * leftReal + leftImaginary * leftImaginary);

                    double centerReal = complexBuffer[bestIndex].X;
                    double centerImaginary = complexBuffer[bestIndex].Y;
                    float centerMagnitude = bestMagnitude;

                    double rightReal = complexBuffer[bestIndex + 1].X;
                    double rightImaginary = complexBuffer[bestIndex + 1].Y;
                    float rightMagnitude = (float)Math.Sqrt(rightReal * rightReal + rightImaginary * rightImaginary);

                    float alpha = leftMagnitude;
                    float beta = centerMagnitude;
                    float gamma = rightMagnitude;

                    float denominator = alpha - 2 * beta + gamma;
                    if (denominator != 0)
                    {
                        float p = 0.5f * (alpha - gamma) / denominator;
                        interpolatedIndex = bestIndex + p;
                    }
                }
                detectedFrequency = (float)(interpolatedIndex * (double)sampleRate / FFT_LENGTH);
            }

            return detectedFrequency;
        }

        private BufferedWaveProvider BufferedWaveProvider;

        private void InitializeAudio()
        {
        

            try
            {
                waveIn = new WaveInEvent();
                waveIn.WaveFormat = new WaveFormat(SampleRate, 16, 1);

                //increase buffer time size
                int bufferDurationSeconds = 45;
                
                bufferedWaveProvider = new BufferedWaveProvider(waveIn.WaveFormat);
                bufferedWaveProvider.BufferLength = waveIn.WaveFormat.SampleRate * waveIn.WaveFormat.Channels * (waveIn.WaveFormat.BitsPerSample / 8) * bufferDurationSeconds;

                waveIn.DataAvailable += WaveIn_DataAvailable_ToBuffer;



                if (microphoneComboBox.SelectedItem is MicrophoneItem selectedMicrophone)
                {
                    waveIn.DeviceNumber = selectedMicrophone.DeviceId;
                }
                else
                {
                    MessageBox.Show("No microphone selected.");
                    return;
                }
                waveIn.DataAvailable += WaveIn_DataAvailable_Debug;
                audioInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing audio: {ex.Message}");

            }
        }
        private void WaveIn_DataAvailable_ToBuffer(object sender, WaveInEventArgs e)
        {
            if (bufferedWaveProvider != null)
            {
                bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
            }
        }
        private void WaveIn_DataAvailable_Debug(object sender, WaveInEventArgs e)
        {
            short[] shortBuffer = new short[e.BytesRecorded / 2]; 
            Buffer.BlockCopy(e.Buffer, 0, shortBuffer, 0, e.BytesRecorded);
            float[] sampleBufferDebug = new float[shortBuffer.Length];
            for (int i = 0; i < shortBuffer.Length; i++)
            {
                sampleBufferDebug[i] = shortBuffer[i] / 32768.0f; 
            }

            bool nanFound = sampleBufferDebug.Any(float.IsNaN);
            

            bool negativeFound = sampleBufferDebug.Any(f => f < 0);
            
        }
     
        private void startButton_Click(object sender, EventArgs e)
        {
            if (!audioInitialized)
            {
                InitializeAudio();
            }

            if (waveIn != null && !isRecording)
            {
                try
                {
                    waveIn.StartRecording();
                    isRecording = true;
                    timer.Start();
                    frequencyLabel.Text = "Frequency: Listening...";
                    noteLabel.Text = "Note: Listening...";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error starting recording: {ex.Message}");
                }
            }
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            if (waveIn != null && isRecording)
            {
                waveIn.StopRecording();
                isRecording = false;
                timer.Stop();
                frequencyLabel.Text = "Frequency: Stopped";
                noteLabel.Text = "Note: Stopped";
                MessageBox.Show("Recording has stopped.", "Recording Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ApplyDarkTheme()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);
            frequencyLabel.ForeColor = Color.White;
            noteLabel.ForeColor = Color.White;
            meterPanel.BackColor = Color.FromArgb(45, 45, 45);
            waveformPanel.BackColor = Color.FromArgb(45, 45, 45);
            microphoneComboBox.BackColor = Color.FromArgb(45, 45, 45);
            microphoneComboBox.ForeColor = Color.White;
            startButton.BackColor = Color.FromArgb(60, 60, 60);
            startButton.ForeColor = Color.White;
            stopButton.BackColor = Color.FromArgb(60, 60, 60);
            stopButton.ForeColor = Color.White;
        }



        private float GetTargetFrequency(string noteNameWithOctave)
        {
            if (string.IsNullOrEmpty(noteNameWithOctave) || noteNameWithOctave == "-") return 0;

            string noteBase = noteNameWithOctave.Substring(0, noteNameWithOctave.Length - 1);
            if (!int.TryParse(noteNameWithOctave.Substring(noteNameWithOctave.Length - 1), out int octave)) return 0;

            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            int n = Array.IndexOf(noteNames, noteBase);
            if (n == -1) return 0;

            int a4NoteNumber = 9; // A is the 9th note
            int a4Octave = 4;
            double frequency = 440.0 * Math.Pow(2, (octave - a4Octave + (double)(n - a4NoteNumber) / 12.0));
            return (float)frequency;
        }

        private void MeterPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            int middleX = meterPanel.Width / 2;
            int meterWidth = 100;
            int needleWidth = 10;
            int needleHeight = 40;
            int meterY = 10;

            g.FillRectangle(Brushes.LightGray, middleX - meterWidth / 2, meterY, meterWidth, 30);

            float needleOffset = (float)_needlePosition;
            int needleX = middleX + (int)needleOffset - needleWidth / 2;

            needleX = Math.Max(middleX - meterWidth / 2, Math.Min(middleX + meterWidth / 2 - needleWidth, needleX));

            g.FillRectangle(Brushes.Red, needleX, meterY - needleHeight / 2 + 30 / 2, needleWidth, needleHeight);

            for (int i = -50; i <= 50; i += 10)
            {
                int markX = middleX + (int)(i * 1.5);
                if (i == 0)
                {
                    g.DrawLine(Pens.Black, markX, meterY, markX, meterY + 35);
                }
                else
                {
                    g.DrawLine(Pens.Gray, markX, meterY + 10, markX, meterY + 25);
                }
            }
        }
        private string displayedNote = "-";
        private float displayedNoteFrequency = 0;
        private const float HysteresisThreshold = 5.0f;

        private void UpdateUI()
        {
            var noteOctave = GetNoteAndOctave(currentFrequency);
            string noteName = noteOctave.Item1;
            int octave = noteOctave.Item2;
            string fullNoteName = noteName + octave.ToString();
            frequencyLabel.Text = $"Frequency: {currentFrequency:F2} Hz";
            noteLabel.Text = $"Note: {fullNoteName}";

            if (targetFrequencies.ContainsKey(fullNoteName))
            {
                double targetFrequency = targetFrequencies[fullNoteName];
                double cents = GetCentsDifference(currentFrequency, (float)targetFrequency);

                currentCentsDifference = cents;
                UpdateTunerUI(cents);
            }
            else
            {
                currentCentsDifference = 0;
                ResetTunerUI();
            }

            waveformPanel.Invalidate();
            meterPanel.Invalidate();
        }
        private double currentCentsDifference = 0;
        private Tuple<string, int> GetNoteAndOctave(double frequency)
        {
            double c0Frequency = 16.35;
            string[] noteNamesSharp = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            if (frequency <= 0) return Tuple.Create("-", -1);

            double semitonesFromC0 = 12 * Math.Log(frequency / c0Frequency, 2);
            int noteNumber = (int)Math.Round(semitonesFromC0);
            int octave = noteNumber / 12;
            int noteIndex = noteNumber % 12;

            if (noteIndex < 0)
            {
                noteIndex += 12;
                octave--; 
            }

            return Tuple.Create(noteNamesSharp[noteIndex], octave);
        }

        private int GetOctave(double frequency)
        {
            double c0Frequency = 16.35; // Frequency of C0
            double semitoneRatio = Math.Pow(2, 1.0 / 12.0);
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            if (frequency <= 0) return -1;

            double semitonesFromC0 = 12 * Math.Log(frequency / c0Frequency, 2);
            
            return (int)Math.Round(semitonesFromC0 / 12.0);
        }


        private void UpdateTunerUI(double cents)
        {
            _needlePosition = cents * 1.5;

        }

        private Brush _needleColor = Brushes.Gray;
        private string _tuningStatus = "";

        private void ResetTunerUI()
        {
            _needlePosition = 0;

        }

        private void WaveformPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(waveformPanel.BackColor);

            if (audioInitialized && lastWaveformData != null && lastWaveformData.Length > 0)
            {
                try
                {
                    int numSamples = lastWaveformData.Length;
                    int panelWidth = waveformPanel.Width;
                    int pointsToDraw = panelWidth; 

                    if (pointsToDraw > 1)
                    {
                        Point[] points = new Point[pointsToDraw];
                        float yScale = waveformPanel.Height / 2f;
                        float yOffset = waveformPanel.Height / 2f;
                        float xIncrement = (float)numSamples / panelWidth;

                        for (int i = 0; i < pointsToDraw; i++)
                        {
                            int sampleIndex = (int)(i * xIncrement);
                            
                            // Ensure sampleIndex is within bounds
                            sampleIndex = Math.Min(sampleIndex, numSamples - 1);

                            int y = (int)(yOffset - lastWaveformData[sampleIndex] * yScale * 10); 
                            y = Math.Max(0, Math.Min(waveformPanel.Height - 1, y));
                            points[i] = new Point(i, y);
                        }

                        g.DrawLines(Pens.Lime, points);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WaveformPanel_Paint Error: {ex.Message}");
                }
            }
        }

        private string GetNoteName(double frequency)
        {
            double c0Frequency = 16.35; // Frequency of C0
            string[] noteNamesSharp = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            double semitonesFromC0 = 12 * Math.Log(frequency / c0Frequency) / Math.Log(2);
            int noteNumber = (int)Math.Round(semitonesFromC0);
            int noteIndex = noteNumber % 12;

            // Handle negative index from module
            if (noteIndex < 0)
            {
                noteIndex += 12;
            }

            return noteNamesSharp[noteIndex];
        }

        private static Dictionary<string, double> targetFrequencies = new Dictionary<string, double>()
            {
                    {"C0", 16.35}, {"C#0", 17.32}, {"D0", 18.35}, {"D#0", 19.45}, {"E0", 20.60}, {"F0", 21.83}, {"F#0", 23.12}, {"G0", 24.50}, {"G#0", 25.96}, {"A0", 27.50}, {"A#0", 29.14}, {"B0", 30.87},
                    {"C1", 32.70}, {"C#1", 34.65}, {"D1", 36.71}, {"D#1", 38.89}, {"E1", 41.20}, {"F1", 43.65}, {"F#1", 46.25}, {"G1", 49.00}, {"G#1", 51.91}, {"A1", 55.00}, {"A#1", 58.27}, {"B1", 61.74},
                    {"C2", 65.41}, {"C#2", 69.30}, {"D2", 73.42}, {"D#2", 77.78}, {"E2", 82.41}, {"F2", 87.31}, {"F#2", 92.50}, {"G2", 98.00}, {"G#2", 103.83}, {"A2", 110.00}, {"A#2", 116.54}, {"B2", 123.47},
                    {"C3", 130.81}, {"C#3", 138.59}, {"D3", 146.83}, {"D#3", 155.56}, {"E3", 164.81}, {"F3", 174.61}, {"F#3", 185.00}, {"G3", 196.00}, {"G#3", 207.65}, {"A3", 220.00}, {"A#3", 233.08}, {"B3", 246.94},
                    {"C4", 261.63}, {"C#4", 277.18}, {"D4", 293.66}, {"D#4", 311.13}, {"E4", 329.63}, {"F4", 349.23}, {"F#4", 369.99}, {"G4", 392.00}, {"G#4", 415.30}, {"A4", 440.00}, {"A#4", 466.16}, {"B4", 493.88},
    
            };

        private void InitializeUI()
        {
            this.Text = "Guitar Tuner";
            this.Size = new Size(650, 550);

            TabControl tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            this.Controls.Add(tabControl);

            // creates tuner tab
            TabPage tunerTabPage = new TabPage("Tuner");
            tunerTabPage.BackColor = Color.FromArgb(30, 30, 30);
            tunerTabPage.ForeColor = Color.White;

            // labels
            frequencyLabel = new Label();
            frequencyLabel.Location = new Point(10, 50);
            frequencyLabel.Size = new Size(180, 30);
            frequencyLabel.Text = "Frequency: 0.00 Hz";
            frequencyLabel.ForeColor = Color.White;
            tunerTabPage.Controls.Add(frequencyLabel);

            noteLabel = new Label();
            noteLabel.Location = new Point(10, 80);
            noteLabel.Size = new Size(150, 30);
            noteLabel.Text = "Note: -";
            noteLabel.ForeColor = Color.White;
            tunerTabPage.Controls.Add(noteLabel);

            meterPanel = new Panel();
            meterPanel.Location = new Point(10, 120);
            meterPanel.Size = new Size(580, 50);
            meterPanel.BorderStyle = BorderStyle.FixedSingle;
            meterPanel.BackColor = Color.FromArgb(45, 45, 45);
            meterPanel.Paint += MeterPanel_Paint;
            tunerTabPage.Controls.Add(meterPanel);

            startButton = new Button();
            startButton.Location = new Point(10, 190);
            startButton.Size = new Size(80, 30);
            startButton.Text = "Start";
            startButton.BackColor = Color.FromArgb(60, 60, 60);
            startButton.ForeColor = Color.White;
            startButton.Click += startButton_Click;
            tunerTabPage.Controls.Add(startButton);

            stopButton = new Button();
            stopButton.Location = new Point(100, 190);
            stopButton.Size = new Size(80, 30);
            stopButton.Text = "Stop";
            stopButton.BackColor = Color.FromArgb(60, 60, 60);
            stopButton.ForeColor = Color.White;
            stopButton.Click += stopButton_Click;
            tunerTabPage.Controls.Add(stopButton);

            microphoneComboBox = new ComboBox();
            microphoneComboBox.Location = new Point(10, 10);
            microphoneComboBox.Size = new Size(250, 25);
            microphoneComboBox.BackColor = Color.FromArgb(45, 45, 45);
            microphoneComboBox.ForeColor = Color.White;
            tunerTabPage.Controls.Add(microphoneComboBox);

            waveformPanel = new Panel();
            waveformPanel.Location = new Point(10, 250);
            waveformPanel.Size = new Size(580, 150);
            waveformPanel.BorderStyle = BorderStyle.FixedSingle;
            waveformPanel.BackColor = Color.FromArgb(45, 45, 45);
            waveformPanel.Paint += WaveformPanel_Paint;
            tunerTabPage.Controls.Add(waveformPanel);

            tunerTabPage.Controls.AddRange(new Control[] { frequencyLabel, noteLabel, meterPanel, startButton, stopButton, microphoneComboBox, waveformPanel });
            tabControl.TabPages.Add(tunerTabPage);

            // DAW TabPage
            dawTabPage = new TabPage("DAW");
            dawTabPage.BackColor = Color.FromArgb(30, 30, 30);
            dawTabPage.ForeColor = Color.White;

            // Initialize DAW Buttons
            recordButtonDAW = new Button { Text = "Record", Location = new Point(10, 10), Size = new Size(80, 30), BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            recordButtonDAW.FlatAppearance.BorderSize = 0;
            recordButtonDAW.Click += RecordButtonDAW_Click_Integrated;

            stopButtonDAW = new Button { Text = "Stop", Location = new Point(100, 10), Size = new Size(80, 30), BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            stopButtonDAW.FlatAppearance.BorderSize = 0;
            stopButtonDAW.Click += StopButtonDAW_Click_Integrated; 

            playButtonDAW = new Button { Text = "Play", Location = new Point(190, 10), Size = new Size(80, 30), BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            playButtonDAW.FlatAppearance.BorderSize = 0;
            playButtonDAW.Click += PlayButtonDAW_Click_Integrated; 

            dawTabPage.Controls.AddRange(new Control[] { recordButtonDAW, stopButtonDAW, playButtonDAW });
            tabControl.TabPages.Add(dawTabPage);
        }


        private void PopulateMicrophoneComboBox()
        {
            try
            {
                var waveInDevices = WaveInEvent.DeviceCount;  
                for (int n = 0; n < waveInDevices; n++)
                {
                    var capabilities = WaveInEvent.GetCapabilities(n);
                    microphoneComboBox.Items.Add(new MicrophoneItem { DeviceId = n, ProductName = capabilities.ProductName });
                }

                if (microphoneComboBox.Items.Count > 0)
                {
                    microphoneComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error populating microphone list: {ex.Message}");
            }
        }

        private void RecordButtonDAW_Click_Integrated(object sender, EventArgs e)
        {
            waveInForm.ShowDialog();
            UpdateDAWButtonStatesIntegrated();

            MessageBox.Show("DAW recording started.", "DAW Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void StopButtonDAW_Click_Integrated(object sender, EventArgs e)
        {
            waveInForm.Close();
            UpdateDAWButtonStatesIntegrated();
            MessageBox.Show("DAW recording stopped.", "DAW Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void PlayButtonDAW_Click_Integrated(object sender, EventArgs e)
        {
            if (File.Exists(recordedFilePathDAW))
            {
                try
                {
                    using (var audioFile = new AudioFileReader(recordedFilePathDAW))
                    using (var outputDevice = new WaveOutEvent())
                    {
                        outputDevice.Init(audioFile);
                        outputDevice.Play();
                        MessageBox.Show($"Playing: {recordedFilePathDAW}", "DAW Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        outputDevice.PlaybackStopped += (s, args) => outputDevice.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error playing file: {ex.Message}", "DAW Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No recording available to play.", "DAW Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        

        private void WaveInForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            
            if (!string.IsNullOrEmpty(waveInForm.OutputFilename))
            {
                recordedFilePathDAW = waveInForm.OutputFilename;
            }
            UpdateDAWButtonStatesIntegrated();
            MessageBox.Show("WaveIn form has been closed.", "Form Closed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void UpdateDAWButtonStatesIntegrated()
        {
            recordButtonDAW.Enabled = !waveInForm.IsRecording; 
            stopButtonDAW.Enabled = waveInForm.IsRecording;   
            playButtonDAW.Enabled = File.Exists(recordedFilePathDAW) && !waveInForm.IsRecording;
        }

        private double GetCentsDifference(float frequency, float targetFrequency)
        {
            if (frequency <= 0 || targetFrequency <= 0)
            {
                return 0;
            }

            return 1200 * Math.Log(frequency / targetFrequency, 2);
        }
    }
}

  
internal class MicrophoneItem
{
    public int DeviceId { get; set; }
    public string ProductName { get; set; }

    public override string ToString()
    {
        return $"{ProductName} (ID: {DeviceId})";
    }
}
