namespace InstallerAnalyzer1_Guest
{
    partial class AnalyzerMainWindow
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AnalyzerMainWindow));
            this.consoleBox = new System.Windows.Forms.RichTextBox();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.monitoredPids = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.elapsedTime = new System.Windows.Forms.Label();
            this.timeout = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.servicePids = new System.Windows.Forms.Label();
            this.logRateBox = new System.Windows.Forms.Label();
            this.busy = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // consoleBox
            // 
            this.consoleBox.BackColor = System.Drawing.Color.Black;
            this.consoleBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.consoleBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.consoleBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.consoleBox.HideSelection = false;
            this.consoleBox.Location = new System.Drawing.Point(88, 12);
            this.consoleBox.Name = "consoleBox";
            this.consoleBox.ReadOnly = true;
            this.consoleBox.Size = new System.Drawing.Size(297, 58);
            this.consoleBox.TabIndex = 2;
            this.consoleBox.Text = "";
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "notifyIcon1";
            this.notifyIcon1.Visible = true;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(70, 58);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(391, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Logging rate:";
            // 
            // label3
            // 
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(391, 57);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(87, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Monitored PIDs:";
            // 
            // monitoredPids
            // 
            this.monitoredPids.Cursor = System.Windows.Forms.Cursors.No;
            this.monitoredPids.ForeColor = System.Drawing.Color.Yellow;
            this.monitoredPids.Location = new System.Drawing.Point(484, 57);
            this.monitoredPids.Name = "monitoredPids";
            this.monitoredPids.Size = new System.Drawing.Size(363, 22);
            this.monitoredPids.TabIndex = 6;
            this.monitoredPids.Text = "...";
            this.monitoredPids.Click += new System.EventHandler(this.monitoredPids_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(761, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(86, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Interaction Time:";
            // 
            // elapsedTime
            // 
            this.elapsedTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.elapsedTime.Location = new System.Drawing.Point(853, 9);
            this.elapsedTime.Name = "elapsedTime";
            this.elapsedTime.Size = new System.Drawing.Size(77, 13);
            this.elapsedTime.TabIndex = 8;
            this.elapsedTime.Text = "00:00:00";
            // 
            // timeout
            // 
            this.timeout.Location = new System.Drawing.Point(936, 9);
            this.timeout.Name = "timeout";
            this.timeout.Size = new System.Drawing.Size(87, 13);
            this.timeout.TabIndex = 9;
            // 
            // label4
            // 
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(391, 34);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(87, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Session 0 PIDs:";
            // 
            // servicePids
            // 
            this.servicePids.Cursor = System.Windows.Forms.Cursors.No;
            this.servicePids.ForeColor = System.Drawing.Color.Yellow;
            this.servicePids.Location = new System.Drawing.Point(484, 34);
            this.servicePids.Name = "servicePids";
            this.servicePids.Size = new System.Drawing.Size(363, 22);
            this.servicePids.TabIndex = 11;
            this.servicePids.Text = "...";
            this.servicePids.Click += new System.EventHandler(this.servicePids_Click);
            // 
            // logRateBox
            // 
            this.logRateBox.ForeColor = System.Drawing.Color.Yellow;
            this.logRateBox.Location = new System.Drawing.Point(484, 9);
            this.logRateBox.Name = "logRateBox";
            this.logRateBox.Size = new System.Drawing.Size(268, 23);
            this.logRateBox.TabIndex = 12;
            this.logRateBox.Text = "...";
            // 
            // busy
            // 
            this.busy.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.busy.ForeColor = System.Drawing.Color.Red;
            this.busy.Location = new System.Drawing.Point(761, 34);
            this.busy.Name = "busy";
            this.busy.Size = new System.Drawing.Size(138, 22);
            this.busy.TabIndex = 13;
            // 
            // AnalyzerMainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(1035, 82);
            this.ControlBox = false;
            this.Controls.Add(this.busy);
            this.Controls.Add(this.logRateBox);
            this.Controls.Add(this.servicePids);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.timeout);
            this.Controls.Add(this.elapsedTime);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.monitoredPids);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.consoleBox);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.ForeColor = System.Drawing.Color.Lime;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "AnalyzerMainWindow";
            this.Opacity = 0.8D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "WKWatcher";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.RichTextBox consoleBox;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label monitoredPids;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label elapsedTime;
        private System.Windows.Forms.Label timeout;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label servicePids;
        private System.Windows.Forms.Label logRateBox;
        private System.Windows.Forms.Label busy;
    }
}