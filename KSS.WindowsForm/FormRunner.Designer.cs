namespace KSS.WindowsForm
{
    partial class FormRunner
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
            bRunner01 = new Button();
            SuspendLayout();
            // 
            // bRunner01
            // 
            bRunner01.Location = new Point(12, 12);
            bRunner01.Name = "bRunner01";
            bRunner01.Size = new Size(174, 67);
            bRunner01.TabIndex = 0;
            bRunner01.Text = "Runner 01";
            bRunner01.UseVisualStyleBackColor = true;
            bRunner01.Click += bRunner01_Click;
            // 
            // FormRunner
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(bRunner01);
            Name = "FormRunner";
            Text = "FormRunner";
            ResumeLayout(false);
        }

        #endregion

        private Button bRunner01;
    }
}