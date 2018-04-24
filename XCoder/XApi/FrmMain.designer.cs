namespace XApi
{
    partial class FrmMain
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.gbReceive = new System.Windows.Forms.GroupBox();
            this.txtReceive = new System.Windows.Forms.RichTextBox();
            this.menuReceive = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi日志着色 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.mi显示应用日志 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi显示编码日志 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi显示统计信息 = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSend = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mi清空2 = new System.Windows.Forms.ToolStripMenuItem();
            this.btnConnect = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.fontDialog1 = new System.Windows.Forms.FontDialog();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.lbAddr = new System.Windows.Forms.Label();
            this.cbMode = new System.Windows.Forms.ComboBox();
            this.cbAddr = new System.Windows.Forms.ComboBox();
            this.pnlSetting = new System.Windows.Forms.Panel();
            this.cbAction = new System.Windows.Forms.ComboBox();
            this.gbSend = new System.Windows.Forms.GroupBox();
            this.numThreads = new System.Windows.Forms.NumericUpDown();
            this.numSleep = new System.Windows.Forms.NumericUpDown();
            this.txtSend = new System.Windows.Forms.RichTextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.numMutilSend = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.numPort = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.gbReceive.SuspendLayout();
            this.menuReceive.SuspendLayout();
            this.menuSend.SuspendLayout();
            this.pnlSetting.SuspendLayout();
            this.gbSend.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numThreads)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSleep)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMutilSend)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).BeginInit();
            this.SuspendLayout();
            // 
            // gbReceive
            // 
            this.gbReceive.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbReceive.Controls.Add(this.txtReceive);
            this.gbReceive.Location = new System.Drawing.Point(18, 156);
            this.gbReceive.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.gbReceive.Name = "gbReceive";
            this.gbReceive.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.gbReceive.Size = new System.Drawing.Size(1304, 398);
            this.gbReceive.TabIndex = 4;
            this.gbReceive.TabStop = false;
            this.gbReceive.Text = "接收区：已接收0字节";
            // 
            // txtReceive
            // 
            this.txtReceive.ContextMenuStrip = this.menuReceive;
            this.txtReceive.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtReceive.HideSelection = false;
            this.txtReceive.Location = new System.Drawing.Point(6, 34);
            this.txtReceive.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.txtReceive.Name = "txtReceive";
            this.txtReceive.Size = new System.Drawing.Size(1292, 358);
            this.txtReceive.TabIndex = 1;
            this.txtReceive.Text = "";
            // 
            // menuReceive
            // 
            this.menuReceive.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.menuReceive.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.mi日志着色,
            this.toolStripMenuItem3,
            this.mi显示应用日志,
            this.mi显示编码日志,
            this.mi显示统计信息});
            this.menuReceive.Name = "menuSend";
            this.menuReceive.Size = new System.Drawing.Size(233, 190);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(232, 36);
            this.toolStripMenuItem1.Text = "清空";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.mi清空_Click);
            // 
            // mi日志着色
            // 
            this.mi日志着色.Name = "mi日志着色";
            this.mi日志着色.Size = new System.Drawing.Size(232, 36);
            this.mi日志着色.Text = "日志着色";
            this.mi日志着色.Click += new System.EventHandler(this.miCheck_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(229, 6);
            // 
            // mi显示应用日志
            // 
            this.mi显示应用日志.Name = "mi显示应用日志";
            this.mi显示应用日志.Size = new System.Drawing.Size(232, 36);
            this.mi显示应用日志.Text = "显示应用日志";
            this.mi显示应用日志.Click += new System.EventHandler(this.miCheck_Click);
            // 
            // mi显示编码日志
            // 
            this.mi显示编码日志.Name = "mi显示编码日志";
            this.mi显示编码日志.Size = new System.Drawing.Size(232, 36);
            this.mi显示编码日志.Text = "显示编码日志";
            this.mi显示编码日志.Click += new System.EventHandler(this.miCheck_Click);
            // 
            // mi显示统计信息
            // 
            this.mi显示统计信息.Name = "mi显示统计信息";
            this.mi显示统计信息.Size = new System.Drawing.Size(232, 36);
            this.mi显示统计信息.Text = "显示统计信息";
            this.mi显示统计信息.Click += new System.EventHandler(this.miCheck_Click);
            // 
            // menuSend
            // 
            this.menuSend.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.menuSend.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mi清空2});
            this.menuSend.Name = "menuSend";
            this.menuSend.Size = new System.Drawing.Size(137, 40);
            // 
            // mi清空2
            // 
            this.mi清空2.Name = "mi清空2";
            this.mi清空2.Size = new System.Drawing.Size(136, 36);
            this.mi清空2.Text = "清空";
            this.mi清空2.Click += new System.EventHandler(this.mi清空2_Click);
            // 
            // btnConnect
            // 
            this.btnConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConnect.Location = new System.Drawing.Point(997, 16);
            this.btnConnect.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(134, 58);
            this.btnConnect.TabIndex = 3;
            this.btnConnect.Text = "打开";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 300;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 19);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 24);
            this.label1.TabIndex = 6;
            this.label1.Text = "模式：";
            // 
            // lbAddr
            // 
            this.lbAddr.AutoSize = true;
            this.lbAddr.Location = new System.Drawing.Point(498, 19);
            this.lbAddr.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lbAddr.Name = "lbAddr";
            this.lbAddr.Size = new System.Drawing.Size(82, 24);
            this.lbAddr.TabIndex = 7;
            this.lbAddr.Text = "地址：";
            // 
            // cbMode
            // 
            this.cbMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMode.FormattingEnabled = true;
            this.cbMode.Items.AddRange(new object[] {
            "服务端",
            "客户端"});
            this.cbMode.Location = new System.Drawing.Point(96, 15);
            this.cbMode.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.cbMode.Name = "cbMode";
            this.cbMode.Size = new System.Drawing.Size(182, 32);
            this.cbMode.TabIndex = 9;
            // 
            // cbAddr
            // 
            this.cbAddr.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbAddr.FormattingEnabled = true;
            this.cbAddr.Location = new System.Drawing.Point(592, 15);
            this.cbAddr.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.cbAddr.Name = "cbAddr";
            this.cbAddr.Size = new System.Drawing.Size(328, 32);
            this.cbAddr.TabIndex = 10;
            // 
            // pnlSetting
            // 
            this.pnlSetting.Controls.Add(this.numPort);
            this.pnlSetting.Controls.Add(this.label5);
            this.pnlSetting.Controls.Add(this.cbAddr);
            this.pnlSetting.Controls.Add(this.label1);
            this.pnlSetting.Controls.Add(this.lbAddr);
            this.pnlSetting.Controls.Add(this.cbMode);
            this.pnlSetting.Location = new System.Drawing.Point(18, 16);
            this.pnlSetting.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.pnlSetting.Name = "pnlSetting";
            this.pnlSetting.Size = new System.Drawing.Size(944, 62);
            this.pnlSetting.TabIndex = 13;
            // 
            // cbAction
            // 
            this.cbAction.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAction.FormattingEnabled = true;
            this.cbAction.Location = new System.Drawing.Point(102, 95);
            this.cbAction.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.cbAction.Name = "cbAction";
            this.cbAction.Size = new System.Drawing.Size(626, 32);
            this.cbAction.TabIndex = 12;
            this.cbAction.Visible = false;
            this.cbAction.SelectedIndexChanged += new System.EventHandler(this.cbAction_SelectedIndexChanged);
            // 
            // gbSend
            // 
            this.gbSend.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbSend.Controls.Add(this.numThreads);
            this.gbSend.Controls.Add(this.numSleep);
            this.gbSend.Controls.Add(this.txtSend);
            this.gbSend.Controls.Add(this.btnSend);
            this.gbSend.Controls.Add(this.numMutilSend);
            this.gbSend.Controls.Add(this.label2);
            this.gbSend.Controls.Add(this.label7);
            this.gbSend.Location = new System.Drawing.Point(18, 566);
            this.gbSend.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.gbSend.Name = "gbSend";
            this.gbSend.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.gbSend.Size = new System.Drawing.Size(1304, 168);
            this.gbSend.TabIndex = 15;
            this.gbSend.TabStop = false;
            this.gbSend.Text = "发送区：已发送0字节";
            // 
            // numThreads
            // 
            this.numThreads.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.numThreads.Location = new System.Drawing.Point(1186, 44);
            this.numThreads.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.numThreads.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numThreads.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numThreads.Name = "numThreads";
            this.numThreads.Size = new System.Drawing.Size(104, 35);
            this.numThreads.TabIndex = 18;
            this.numThreads.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.toolTip1.SetToolTip(this.numThreads, "模拟多客户端发送，用于压力测试！");
            this.numThreads.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // numSleep
            // 
            this.numSleep.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.numSleep.Location = new System.Drawing.Point(1076, 108);
            this.numSleep.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.numSleep.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numSleep.Name = "numSleep";
            this.numSleep.Size = new System.Drawing.Size(104, 35);
            this.numSleep.TabIndex = 16;
            this.numSleep.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numSleep.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // txtSend
            // 
            this.txtSend.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSend.ContextMenuStrip = this.menuSend;
            this.txtSend.HideSelection = false;
            this.txtSend.Location = new System.Drawing.Point(0, 38);
            this.txtSend.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.txtSend.Name = "txtSend";
            this.txtSend.Size = new System.Drawing.Size(990, 114);
            this.txtSend.TabIndex = 2;
            this.txtSend.Text = "";
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.Location = new System.Drawing.Point(1192, 98);
            this.btnSend.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(100, 60);
            this.btnSend.TabIndex = 1;
            this.btnSend.Text = "发送";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // numMutilSend
            // 
            this.numMutilSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.numMutilSend.Location = new System.Drawing.Point(1076, 44);
            this.numMutilSend.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.numMutilSend.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numMutilSend.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMutilSend.Name = "numMutilSend";
            this.numMutilSend.Size = new System.Drawing.Size(104, 35);
            this.numMutilSend.TabIndex = 14;
            this.numMutilSend.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numMutilSend.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(1006, 116);
            this.label2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 24);
            this.label2.TabIndex = 17;
            this.label2.Text = "间隔：";
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(1006, 52);
            this.label7.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(82, 24);
            this.label7.TabIndex = 15;
            this.label7.Text = "次数：";
            // 
            // numPort
            // 
            this.numPort.Location = new System.Drawing.Point(360, 14);
            this.numPort.Margin = new System.Windows.Forms.Padding(6);
            this.numPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numPort.Name = "numPort";
            this.numPort.Size = new System.Drawing.Size(126, 35);
            this.numPort.TabIndex = 16;
            this.numPort.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numPort.Value = new decimal(new int[] {
            777,
            0,
            0,
            0});
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(290, 19);
            this.label5.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(82, 24);
            this.label5.TabIndex = 17;
            this.label5.Text = "端口：";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(20, 99);
            this.label3.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(82, 24);
            this.label3.TabIndex = 16;
            this.label3.Text = "服务：";
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1334, 758);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cbAction);
            this.Controls.Add(this.gbSend);
            this.Controls.Add(this.pnlSetting);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.gbReceive);
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Api调试";
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.gbReceive.ResumeLayout(false);
            this.menuReceive.ResumeLayout(false);
            this.menuSend.ResumeLayout(false);
            this.pnlSetting.ResumeLayout(false);
            this.pnlSetting.PerformLayout();
            this.gbSend.ResumeLayout(false);
            this.gbSend.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numThreads)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSleep)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMutilSend)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox gbReceive;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ContextMenuStrip menuSend;
        private System.Windows.Forms.ToolStripMenuItem mi清空2;
        private System.Windows.Forms.RichTextBox txtReceive;
        private System.Windows.Forms.FontDialog fontDialog1;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lbAddr;
        private System.Windows.Forms.ComboBox cbMode;
        private System.Windows.Forms.ComboBox cbAddr;
        private System.Windows.Forms.Panel pnlSetting;
        private System.Windows.Forms.ContextMenuStrip menuReceive;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.GroupBox gbSend;
        private System.Windows.Forms.NumericUpDown numSleep;
        private System.Windows.Forms.RichTextBox txtSend;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.NumericUpDown numMutilSend;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ToolStripMenuItem mi显示统计信息;
        private System.Windows.Forms.NumericUpDown numThreads;
        private System.Windows.Forms.ToolStripMenuItem mi显示应用日志;
        private System.Windows.Forms.ToolStripMenuItem mi显示编码日志;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ComboBox cbAction;
        private System.Windows.Forms.ToolStripMenuItem mi日志着色;
        private System.Windows.Forms.NumericUpDown numPort;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label3;
    }
}

