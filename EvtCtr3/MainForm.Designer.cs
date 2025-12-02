using SkiaSharp.Views.Desktop;

namespace EvtCtr3
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            eventsui1 = new EventsUI();
            SuspendLayout();
            // 
            // eventsui1
            // 
            eventsui1.BackColor = Color.Black;
            eventsui1.Location = new Point(13, 13);
            eventsui1.Margin = new Padding(4);
            eventsui1.Name = "eventsui1";
            eventsui1.PixelScale = 2;
            eventsui1.Size = new Size(609, 444);
            eventsui1.TabIndex = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(635, 468);
            Controls.Add(eventsui1);
            Name = "MainForm";
            Text = "MainForm";
            ResumeLayout(false);
        }

        #endregion
        private EventsUI eventsui1;
    }
}
