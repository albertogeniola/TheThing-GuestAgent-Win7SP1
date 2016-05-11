using ResultAnalyzerUtility;

namespace ResultAnalyzerUtility
{
    partial class Form1
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadReportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bulkAnalysisToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.fileName = new System.Windows.Forms.Label();
            this.newApps = new System.Windows.Forms.LinkLabel();
            this.label9 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.err = new System.Windows.Forms.LinkLabel();
            this.Out = new System.Windows.Forms.LinkLabel();
            this.uibotLog = new System.Windows.Forms.LinkLabel();
            this.uiBot = new System.Windows.Forms.Label();
            this.injectorResult = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.vmResult = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.duration = new System.Windows.Forms.Label();
            this.jobId = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new MyPicturebox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.openDialog = new System.Windows.Forms.OpenFileDialog();
            this.label8 = new System.Windows.Forms.Label();
            this.screenN = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.totScreens = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.fileBtn = new System.Windows.Forms.Button();
            this.regBtn = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.bulkAnalysisToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(825, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadReportToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadReportToolStripMenuItem
            // 
            this.loadReportToolStripMenuItem.Name = "loadReportToolStripMenuItem";
            this.loadReportToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.loadReportToolStripMenuItem.Text = "Load report...";
            this.loadReportToolStripMenuItem.Click += new System.EventHandler(this.loadReportToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            // 
            // bulkAnalysisToolStripMenuItem
            // 
            this.bulkAnalysisToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setDirectoryToolStripMenuItem});
            this.bulkAnalysisToolStripMenuItem.Name = "bulkAnalysisToolStripMenuItem";
            this.bulkAnalysisToolStripMenuItem.Size = new System.Drawing.Size(86, 20);
            this.bulkAnalysisToolStripMenuItem.Text = "Bulk analysis";
            // 
            // setDirectoryToolStripMenuItem
            // 
            this.setDirectoryToolStripMenuItem.Name = "setDirectoryToolStripMenuItem";
            this.setDirectoryToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
            this.setDirectoryToolStripMenuItem.Text = "Set Directory...";
            this.setDirectoryToolStripMenuItem.Click += new System.EventHandler(this.setDirectoryToolStripMenuItem_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.fileName);
            this.groupBox1.Controls.Add(this.newApps);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.err);
            this.groupBox1.Controls.Add(this.Out);
            this.groupBox1.Controls.Add(this.uibotLog);
            this.groupBox1.Controls.Add(this.uiBot);
            this.groupBox1.Controls.Add(this.injectorResult);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.vmResult);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.duration);
            this.groupBox1.Controls.Add(this.jobId);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 27);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(621, 90);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Experiment INFO";
            // 
            // fileName
            // 
            this.fileName.AutoEllipsis = true;
            this.fileName.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.fileName.Location = new System.Drawing.Point(109, 16);
            this.fileName.Name = "fileName";
            this.fileName.Size = new System.Drawing.Size(153, 23);
            this.fileName.TabIndex = 18;
            this.fileName.Text = "N/A";
            // 
            // newApps
            // 
            this.newApps.Location = new System.Drawing.Point(587, 16);
            this.newApps.Name = "newApps";
            this.newApps.Size = new System.Drawing.Size(34, 13);
            this.newApps.TabIndex = 17;
            this.newApps.TabStop = true;
            this.newApps.Text = "N/A";
            this.newApps.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.newApps_LinkClicked);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(526, 16);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(59, 13);
            this.label9.TabIndex = 16;
            this.label9.Text = "New Apps:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(580, 38);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(9, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "|";
            // 
            // err
            // 
            this.err.AutoSize = true;
            this.err.Location = new System.Drawing.Point(587, 38);
            this.err.Name = "err";
            this.err.Size = new System.Drawing.Size(30, 13);
            this.err.TabIndex = 15;
            this.err.TabStop = true;
            this.err.Text = "ERR";
            this.err.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.err_LinkClicked);
            // 
            // Out
            // 
            this.Out.AutoSize = true;
            this.Out.Location = new System.Drawing.Point(551, 38);
            this.Out.Name = "Out";
            this.Out.Size = new System.Drawing.Size(30, 13);
            this.Out.TabIndex = 14;
            this.Out.TabStop = true;
            this.Out.Text = "OUT";
            this.Out.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.Out_LinkClicked);
            // 
            // uibotLog
            // 
            this.uibotLog.AutoSize = true;
            this.uibotLog.Location = new System.Drawing.Point(551, 62);
            this.uibotLog.Name = "uibotLog";
            this.uibotLog.Size = new System.Drawing.Size(62, 13);
            this.uibotLog.TabIndex = 13;
            this.uibotLog.TabStop = true;
            this.uibotLog.Text = "UI Bot LOG";
            this.uibotLog.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.uibotLog_LinkClicked);
            // 
            // uiBot
            // 
            this.uiBot.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiBot.Location = new System.Drawing.Point(373, 62);
            this.uiBot.Name = "uiBot";
            this.uiBot.Size = new System.Drawing.Size(172, 19);
            this.uiBot.TabIndex = 12;
            this.uiBot.Text = "N/A";
            // 
            // injectorResult
            // 
            this.injectorResult.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.injectorResult.Location = new System.Drawing.Point(373, 38);
            this.injectorResult.Name = "injectorResult";
            this.injectorResult.Size = new System.Drawing.Size(172, 24);
            this.injectorResult.TabIndex = 10;
            this.injectorResult.Text = "N/A";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(241, 62);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(126, 19);
            this.label7.TabIndex = 11;
            this.label7.Text = "UIBot Result:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(214, 38);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(153, 19);
            this.label6.TabIndex = 9;
            this.label6.Text = "Injector Result:";
            // 
            // vmResult
            // 
            this.vmResult.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.vmResult.Location = new System.Drawing.Point(373, 16);
            this.vmResult.Name = "vmResult";
            this.vmResult.Size = new System.Drawing.Size(138, 22);
            this.vmResult.TabIndex = 8;
            this.vmResult.Text = "N/A";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(268, 16);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(99, 19);
            this.label5.TabIndex = 7;
            this.label5.Text = "VM Result:";
            // 
            // duration
            // 
            this.duration.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.duration.Location = new System.Drawing.Point(109, 58);
            this.duration.Name = "duration";
            this.duration.Size = new System.Drawing.Size(99, 23);
            this.duration.TabIndex = 6;
            this.duration.Text = "N/A";
            // 
            // jobId
            // 
            this.jobId.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.jobId.Location = new System.Drawing.Point(109, 38);
            this.jobId.Name = "jobId";
            this.jobId.Size = new System.Drawing.Size(99, 20);
            this.jobId.TabIndex = 5;
            this.jobId.Text = "N/A";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(13, 57);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 19);
            this.label3.TabIndex = 4;
            this.label3.Text = "Duration:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(31, 38);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 19);
            this.label2.TabIndex = 2;
            this.label2.Text = "Job ID:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(49, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 19);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Cursor = System.Windows.Forms.Cursors.Cross;
            this.pictureBox1.Location = new System.Drawing.Point(12, 140);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(800, 577);
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(639, 33);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(86, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "< Prev Screen";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(731, 33);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(85, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "Next Screen >";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(639, 91);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(177, 23);
            this.button4.TabIndex = 0;
            this.button4.Text = "Accept";
            this.button4.UseVisualStyleBackColor = true;
            // 
            // openDialog
            // 
            this.openDialog.FileName = "report.xml";
            this.openDialog.Filter = "XML Report|report.xml";
            this.openDialog.ShowReadOnly = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(13, 124);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(47, 13);
            this.label8.TabIndex = 7;
            this.label8.Text = "Screen: ";
            // 
            // screenN
            // 
            this.screenN.AutoSize = true;
            this.screenN.Location = new System.Drawing.Point(66, 124);
            this.screenN.Name = "screenN";
            this.screenN.Size = new System.Drawing.Size(13, 13);
            this.screenN.TabIndex = 8;
            this.screenN.Text = "0";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(86, 124);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(12, 13);
            this.label10.TabIndex = 9;
            this.label10.Text = "/";
            // 
            // totScreens
            // 
            this.totScreens.AutoSize = true;
            this.totScreens.Location = new System.Drawing.Point(105, 124);
            this.totScreens.Name = "totScreens";
            this.totScreens.Size = new System.Drawing.Size(13, 13);
            this.totScreens.TabIndex = 10;
            this.totScreens.Text = "0";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(125, 123);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(687, 14);
            this.progressBar1.TabIndex = 11;
            // 
            // fileBtn
            // 
            this.fileBtn.Location = new System.Drawing.Point(639, 62);
            this.fileBtn.Name = "fileBtn";
            this.fileBtn.Size = new System.Drawing.Size(86, 23);
            this.fileBtn.TabIndex = 12;
            this.fileBtn.Text = "Files...";
            this.fileBtn.UseVisualStyleBackColor = true;
            this.fileBtn.Click += new System.EventHandler(this.button3_Click);
            // 
            // regBtn
            // 
            this.regBtn.Location = new System.Drawing.Point(732, 62);
            this.regBtn.Name = "regBtn";
            this.regBtn.Size = new System.Drawing.Size(84, 23);
            this.regBtn.TabIndex = 13;
            this.regBtn.Text = "Registry...";
            this.regBtn.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(825, 729);
            this.Controls.Add(this.regBtn);
            this.Controls.Add(this.fileBtn);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.totScreens);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.screenN);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Result analyzer";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label duration;
        private System.Windows.Forms.Label jobId;
        private System.Windows.Forms.Label injectorResult;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label vmResult;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.LinkLabel err;
        private System.Windows.Forms.LinkLabel Out;
        private System.Windows.Forms.LinkLabel uibotLog;
        private System.Windows.Forms.Label uiBot;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label4;
        private MyPicturebox pictureBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.ToolStripMenuItem loadReportToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openDialog;
        private System.Windows.Forms.LinkLabel newApps;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label screenN;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label totScreens;
        private System.Windows.Forms.Label fileName;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button fileBtn;
        private System.Windows.Forms.Button regBtn;
        private System.Windows.Forms.ToolStripMenuItem bulkAnalysisToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setDirectoryToolStripMenuItem;
    }
}

