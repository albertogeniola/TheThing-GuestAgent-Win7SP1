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
            this.logbox = new System.Windows.Forms.RichTextBox();
            this.consoleBox = new System.Windows.Forms.RichTextBox();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.localSockAddr = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.userFriendlyPath = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.followedPath = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // logbox
            // 
            this.logbox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logbox.BackColor = System.Drawing.Color.Black;
            this.logbox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.logbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.logbox.ForeColor = System.Drawing.Color.Yellow;
            this.logbox.Location = new System.Drawing.Point(268, 12);
            this.logbox.Name = "logbox";
            this.logbox.ReadOnly = true;
            this.logbox.Size = new System.Drawing.Size(755, 67);
            this.logbox.TabIndex = 0;
            this.logbox.Text = "";
            // 
            // consoleBox
            // 
            this.consoleBox.BackColor = System.Drawing.Color.Black;
            this.consoleBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.consoleBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.consoleBox.ForeColor = System.Drawing.Color.Gold;
            this.consoleBox.HideSelection = false;
            this.consoleBox.Location = new System.Drawing.Point(12, 12);
            this.consoleBox.Name = "consoleBox";
            this.consoleBox.ReadOnly = true;
            this.consoleBox.Size = new System.Drawing.Size(250, 67);
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
            // panel1
            // 
            this.panel1.Controls.Add(this.localSockAddr);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.userFriendlyPath);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.followedPath);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Location = new System.Drawing.Point(12, 85);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1011, 48);
            this.panel1.TabIndex = 3;
            // 
            // localSockAddr
            // 
            this.localSockAddr.AutoSize = true;
            this.localSockAddr.ForeColor = System.Drawing.Color.DarkOrange;
            this.localSockAddr.Location = new System.Drawing.Point(96, 28);
            this.localSockAddr.Name = "localSockAddr";
            this.localSockAddr.Size = new System.Drawing.Size(10, 13);
            this.localSockAddr.TabIndex = 8;
            this.localSockAddr.Text = "-";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Local Socket:";
            // 
            // userFriendlyPath
            // 
            this.userFriendlyPath.AutoSize = true;
            this.userFriendlyPath.ForeColor = System.Drawing.Color.DarkOrange;
            this.userFriendlyPath.Location = new System.Drawing.Point(96, 15);
            this.userFriendlyPath.Name = "userFriendlyPath";
            this.userFriendlyPath.Size = new System.Drawing.Size(10, 13);
            this.userFriendlyPath.TabIndex = 6;
            this.userFriendlyPath.Text = "-";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(3, 15);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Path:";
            // 
            // followedPath
            // 
            this.followedPath.AutoSize = true;
            this.followedPath.ForeColor = System.Drawing.Color.DarkOrange;
            this.followedPath.Location = new System.Drawing.Point(96, 0);
            this.followedPath.Name = "followedPath";
            this.followedPath.Size = new System.Drawing.Size(10, 13);
            this.followedPath.TabIndex = 4;
            this.followedPath.Text = "-";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(87, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "FollowedPath:";
            // 
            // AnalyzerMainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(1035, 139);
            this.ControlBox = false;
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.consoleBox);
            this.Controls.Add(this.logbox);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.ForeColor = System.Drawing.Color.Lime;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "AnalyzerMainWindow";
            this.Opacity = 0.8D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Watcher";
            this.TopMost = true;
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox logbox;
        private System.Windows.Forms.RichTextBox consoleBox;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label followedPath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label userFriendlyPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label localSockAddr;
    }
}