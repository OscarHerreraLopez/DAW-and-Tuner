using System.Windows.Forms;

partial class WaveIn : System.Windows.Forms.Form 
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;


    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        //
        // WaveIn
        //
        this.ClientSize = new System.Drawing.Size(284, 261);
        this.Name = "WaveIn";
        this.Text = "WaveIn";
        this.ResumeLayout(false);
       
        //
    }

    #endregion
}
