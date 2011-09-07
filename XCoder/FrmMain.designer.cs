namespace XCoder
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            this.bt_Connection = new System.Windows.Forms.Button();
            this.gbConnect = new System.Windows.Forms.GroupBox();
            this.cbConn = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.gbTable = new System.Windows.Forms.GroupBox();
            this.button1 = new System.Windows.Forms.Button();
            this.bt_GenAll = new System.Windows.Forms.Button();
            this.bt_GenTable = new System.Windows.Forms.Button();
            this.cbTableList = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lb_Status = new System.Windows.Forms.ToolStripStatusLabel();
            this.pg_Process = new System.Windows.Forms.ToolStripProgressBar();
            this.proc_percent = new System.Windows.Forms.ToolStripStatusLabel();
            this.bw = new System.ComponentModel.BackgroundWorker();
            this.gbConfig = new System.Windows.Forms.GroupBox();
            this.btnOpenOutputDir = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.checkBox5 = new System.Windows.Forms.CheckBox();
            this.richTextBox2 = new System.Windows.Forms.RichTextBox();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.txtPrefix = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txt_ConnName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txt_NameSpace = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txt_OutPath = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.cb_Template = new System.Windows.Forms.ComboBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnImport = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.label3 = new System.Windows.Forms.Label();
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.gbConnect.SuspendLayout();
            this.gbTable.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.gbConfig.SuspendLayout();
            this.SuspendLayout();
            // 
            // bt_Connection
            // 
            this.bt_Connection.Location = new System.Drawing.Point(217, 12);
            this.bt_Connection.Name = "bt_Connection";
            this.bt_Connection.Size = new System.Drawing.Size(52, 23);
            this.bt_Connection.TabIndex = 6;
            this.bt_Connection.Text = "连接";
            this.toolTip1.SetToolTip(this.bt_Connection, "数据库结构带有缓存，如不能获取最新结构，请稍候重试！");
            this.bt_Connection.UseVisualStyleBackColor = true;
            this.bt_Connection.Click += new System.EventHandler(this.bt_Connection_Click);
            // 
            // gbConnect
            // 
            this.gbConnect.Controls.Add(this.cbConn);
            this.gbConnect.Controls.Add(this.label4);
            this.gbConnect.Location = new System.Drawing.Point(2, 0);
            this.gbConnect.Name = "gbConnect";
            this.gbConnect.Size = new System.Drawing.Size(205, 38);
            this.gbConnect.TabIndex = 7;
            this.gbConnect.TabStop = false;
            // 
            // cbConn
            // 
            this.cbConn.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbConn.FormattingEnabled = true;
            this.cbConn.Location = new System.Drawing.Point(58, 13);
            this.cbConn.Name = "cbConn";
            this.cbConn.Size = new System.Drawing.Size(141, 20);
            this.cbConn.TabIndex = 13;
            this.cbConn.SelectedIndexChanged += new System.EventHandler(this.cbConn_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(11, 17);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 12);
            this.label4.TabIndex = 12;
            this.label4.Text = "连接：";
            // 
            // gbTable
            // 
            this.gbTable.Controls.Add(this.button1);
            this.gbTable.Controls.Add(this.bt_GenAll);
            this.gbTable.Controls.Add(this.bt_GenTable);
            this.gbTable.Controls.Add(this.cbTableList);
            this.gbTable.Controls.Add(this.label5);
            this.gbTable.Enabled = false;
            this.gbTable.Location = new System.Drawing.Point(2, 45);
            this.gbTable.Name = "gbTable";
            this.gbTable.Size = new System.Drawing.Size(725, 49);
            this.gbTable.TabIndex = 14;
            this.gbTable.TabStop = false;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(500, 19);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(97, 23);
            this.button1.TabIndex = 22;
            this.button1.Text = "导出映射文件";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // bt_GenAll
            // 
            this.bt_GenAll.Location = new System.Drawing.Point(387, 19);
            this.bt_GenAll.Name = "bt_GenAll";
            this.bt_GenAll.Size = new System.Drawing.Size(75, 23);
            this.bt_GenAll.TabIndex = 21;
            this.bt_GenAll.Text = "生成所有表";
            this.bt_GenAll.UseVisualStyleBackColor = true;
            this.bt_GenAll.Click += new System.EventHandler(this.bt_GenAll_Click);
            // 
            // bt_GenTable
            // 
            this.bt_GenTable.Location = new System.Drawing.Point(283, 19);
            this.bt_GenTable.Name = "bt_GenTable";
            this.bt_GenTable.Size = new System.Drawing.Size(75, 23);
            this.bt_GenTable.TabIndex = 19;
            this.bt_GenTable.Text = "生成该表";
            this.bt_GenTable.UseVisualStyleBackColor = true;
            this.bt_GenTable.Click += new System.EventHandler(this.bt_GenTable_Click);
            // 
            // cbTableList
            // 
            this.cbTableList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTableList.FormattingEnabled = true;
            this.cbTableList.Location = new System.Drawing.Point(58, 20);
            this.cbTableList.Name = "cbTableList";
            this.cbTableList.Size = new System.Drawing.Size(173, 20);
            this.cbTableList.TabIndex = 17;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(11, 24);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 12);
            this.label5.TabIndex = 16;
            this.label5.Text = "数据表：";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lb_Status,
            this.pg_Process,
            this.proc_percent});
            this.statusStrip1.Location = new System.Drawing.Point(0, 328);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(730, 22);
            this.statusStrip1.TabIndex = 23;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lb_Status
            // 
            this.lb_Status.Name = "lb_Status";
            this.lb_Status.Size = new System.Drawing.Size(287, 17);
            this.lb_Status.Spring = true;
            this.lb_Status.Text = "状态";
            this.lb_Status.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pg_Process
            // 
            this.pg_Process.Name = "pg_Process";
            this.pg_Process.Size = new System.Drawing.Size(400, 16);
            // 
            // proc_percent
            // 
            this.proc_percent.ForeColor = System.Drawing.Color.Red;
            this.proc_percent.Name = "proc_percent";
            this.proc_percent.Size = new System.Drawing.Size(26, 17);
            this.proc_percent.Text = "0%";
            // 
            // bw
            // 
            this.bw.WorkerReportsProgress = true;
            this.bw.WorkerSupportsCancellation = true;
            this.bw.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bw_DoWork);
            this.bw.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.bw_ProgressChanged);
            this.bw.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.bw_RunWorkerCompleted);
            // 
            // gbConfig
            // 
            this.gbConfig.Controls.Add(this.btnOpenOutputDir);
            this.gbConfig.Controls.Add(this.label9);
            this.gbConfig.Controls.Add(this.webBrowser1);
            this.gbConfig.Controls.Add(this.checkBox5);
            this.gbConfig.Controls.Add(this.richTextBox2);
            this.gbConfig.Controls.Add(this.checkBox4);
            this.gbConfig.Controls.Add(this.checkBox3);
            this.gbConfig.Controls.Add(this.checkBox2);
            this.gbConfig.Controls.Add(this.checkBox1);
            this.gbConfig.Controls.Add(this.txtPrefix);
            this.gbConfig.Controls.Add(this.label2);
            this.gbConfig.Controls.Add(this.txt_ConnName);
            this.gbConfig.Controls.Add(this.label1);
            this.gbConfig.Controls.Add(this.txt_NameSpace);
            this.gbConfig.Controls.Add(this.label8);
            this.gbConfig.Controls.Add(this.txt_OutPath);
            this.gbConfig.Controls.Add(this.label7);
            this.gbConfig.Controls.Add(this.label6);
            this.gbConfig.Controls.Add(this.cb_Template);
            this.gbConfig.Location = new System.Drawing.Point(2, 100);
            this.gbConfig.Name = "gbConfig";
            this.gbConfig.Size = new System.Drawing.Size(725, 224);
            this.gbConfig.TabIndex = 26;
            this.gbConfig.TabStop = false;
            // 
            // btnOpenOutputDir
            // 
            this.btnOpenOutputDir.Location = new System.Drawing.Point(283, 75);
            this.btnOpenOutputDir.Name = "btnOpenOutputDir";
            this.btnOpenOutputDir.Size = new System.Drawing.Size(75, 23);
            this.btnOpenOutputDir.TabIndex = 45;
            this.btnOpenOutputDir.Text = "打开目录";
            this.btnOpenOutputDir.UseVisualStyleBackColor = true;
            this.btnOpenOutputDir.Click += new System.EventHandler(this.btnOpenOutputDir_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(491, 19);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(125, 12);
            this.label9.TabIndex = 44;
            this.label9.Text = "出现在每一个文件头部";
            // 
            // webBrowser1
            // 
            this.webBrowser1.AllowWebBrowserDrop = false;
            this.webBrowser1.IsWebBrowserContextMenuEnabled = false;
            this.webBrowser1.Location = new System.Drawing.Point(13, 156);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.ScrollBarsEnabled = false;
            this.webBrowser1.Size = new System.Drawing.Size(360, 60);
            this.webBrowser1.TabIndex = 43;
            this.webBrowser1.Url = new System.Uri("", System.UriKind.Relative);
            // 
            // checkBox5
            // 
            this.checkBox5.AutoSize = true;
            this.checkBox5.Location = new System.Drawing.Point(387, 18);
            this.checkBox5.Name = "checkBox5";
            this.checkBox5.Size = new System.Drawing.Size(84, 16);
            this.checkBox5.TabIndex = 42;
            this.checkBox5.Text = "文件头模版";
            this.checkBox5.UseVisualStyleBackColor = true;
            // 
            // richTextBox2
            // 
            this.richTextBox2.Location = new System.Drawing.Point(387, 40);
            this.richTextBox2.Name = "richTextBox2";
            this.richTextBox2.Size = new System.Drawing.Size(338, 176);
            this.richTextBox2.TabIndex = 40;
            this.richTextBox2.Text = resources.GetString("richTextBox2.Text");
            // 
            // checkBox4
            // 
            this.checkBox4.AutoSize = true;
            this.checkBox4.Location = new System.Drawing.Point(178, 22);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(48, 16);
            this.checkBox4.TabIndex = 27;
            this.checkBox4.Text = "调试";
            this.checkBox4.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Location = new System.Drawing.Point(178, 78);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(84, 16);
            this.checkBox3.TabIndex = 39;
            this.checkBox3.Text = "中文文件名";
            this.checkBox3.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point(178, 134);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(132, 16);
            this.checkBox2.TabIndex = 38;
            this.checkBox2.Text = "自动更正名称大小写";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(13, 134);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(162, 16);
            this.checkBox1.TabIndex = 37;
            this.checkBox1.Text = "自动去除前缀（以_为准）";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // txtPrefix
            // 
            this.txtPrefix.Location = new System.Drawing.Point(71, 107);
            this.txtPrefix.Name = "txtPrefix";
            this.txtPrefix.Size = new System.Drawing.Size(184, 21);
            this.txtPrefix.TabIndex = 36;
            this.txtPrefix.Text = "X_;N_;tbl";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 111);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 12);
            this.label2.TabIndex = 35;
            this.label2.Text = "删除前缀：";
            // 
            // txt_ConnName
            // 
            this.txt_ConnName.Location = new System.Drawing.Point(276, 49);
            this.txt_ConnName.Name = "txt_ConnName";
            this.txt_ConnName.Size = new System.Drawing.Size(97, 21);
            this.txt_ConnName.TabIndex = 34;
            this.txt_ConnName.Text = "default";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(216, 53);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 33;
            this.label1.Text = "连接名：";
            // 
            // txt_NameSpace
            // 
            this.txt_NameSpace.Location = new System.Drawing.Point(71, 49);
            this.txt_NameSpace.Name = "txt_NameSpace";
            this.txt_NameSpace.Size = new System.Drawing.Size(134, 21);
            this.txt_NameSpace.TabIndex = 32;
            this.txt_NameSpace.Text = "XData";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(11, 53);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(65, 12);
            this.label8.TabIndex = 31;
            this.label8.Text = "命名空间：";
            // 
            // txt_OutPath
            // 
            this.txt_OutPath.Location = new System.Drawing.Point(71, 76);
            this.txt_OutPath.Name = "txt_OutPath";
            this.txt_OutPath.Size = new System.Drawing.Size(97, 21);
            this.txt_OutPath.TabIndex = 29;
            this.txt_OutPath.Text = "Dist";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(10, 80);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(65, 12);
            this.label7.TabIndex = 28;
            this.label7.Text = "输出目录：";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(11, 24);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 12);
            this.label6.TabIndex = 27;
            this.label6.Text = "模版：";
            // 
            // cb_Template
            // 
            this.cb_Template.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_Template.FormattingEnabled = true;
            this.cb_Template.Location = new System.Drawing.Point(58, 20);
            this.cb_Template.Name = "cb_Template";
            this.cb_Template.Size = new System.Drawing.Size(90, 20);
            this.cb_Template.TabIndex = 26;
            // 
            // btnImport
            // 
            this.btnImport.Location = new System.Drawing.Point(279, 12);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(80, 23);
            this.btnImport.TabIndex = 30;
            this.btnImport.Text = "导入架构";
            this.toolTip1.SetToolTip(this.btnImport, "把数据库架构信息导出到xml文件，或者从xml文件导入");
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(387, 29);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(143, 12);
            this.linkLabel1.TabIndex = 28;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "http://nnhy.cnblogs.com";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(387, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(149, 12);
            this.label3.TabIndex = 29;
            this.label3.Text = ".Net技术交流群：10193406";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // timer2
            // 
            this.timer2.Enabled = true;
            this.timer2.Interval = 2000;
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Filter = "架构文件|*.xml";
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.Filter = "架构文件|*.xml";
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(730, 350);
            this.Controls.Add(this.btnImport);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.gbConfig);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.gbTable);
            this.Controls.Add(this.gbConnect);
            this.Controls.Add(this.bt_Connection);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "FrmMain";
            this.RightToLeftLayout = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "新生命代码生成器";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmMain_FormClosing);
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.Shown += new System.EventHandler(this.FrmMain_Shown);
            this.gbConnect.ResumeLayout(false);
            this.gbConnect.PerformLayout();
            this.gbTable.ResumeLayout(false);
            this.gbTable.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.gbConfig.ResumeLayout(false);
            this.gbConfig.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button bt_Connection;
		private System.Windows.Forms.GroupBox gbConnect;
		private System.Windows.Forms.GroupBox gbTable;
		private System.Windows.Forms.Button bt_GenTable;
		private System.Windows.Forms.ComboBox cbTableList;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button bt_GenAll;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.ToolStripStatusLabel lb_Status;
		private System.Windows.Forms.ToolStripProgressBar pg_Process;
		private System.ComponentModel.BackgroundWorker bw;
        private System.Windows.Forms.ToolStripStatusLabel proc_percent;
        private System.Windows.Forms.GroupBox gbConfig;
        private System.Windows.Forms.TextBox txt_ConnName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txt_NameSpace;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txt_OutPath;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cb_Template;
        private System.Windows.Forms.TextBox txtPrefix;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.ComboBox cbConn;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.RichTextBox richTextBox2;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.CheckBox checkBox5;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Timer timer2;
        private System.Windows.Forms.WebBrowser webBrowser1;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button btnOpenOutputDir;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
	}
}

