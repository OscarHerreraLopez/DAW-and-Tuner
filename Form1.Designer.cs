using System;
using System.Drawing;
using System.Windows.Forms;

namespace Test_Tuner
{
    partial class GuitarTuner : Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.frequencyLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // frequencyLabel
            // 
            this.frequencyLabel.ForeColor = System.Drawing.Color.Black;
            this.frequencyLabel.Location = new System.Drawing.Point(0, 0);
            this.frequencyLabel.Name = "frequencyLabel";
            this.frequencyLabel.Size = new System.Drawing.Size(100, 23);
            this.frequencyLabel.TabIndex = 0;
            // 
            // GuitarTuner
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(543, 413);
            this.Name = "GuitarTuner";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }
        
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
       
        }


        #endregion
    }
}

