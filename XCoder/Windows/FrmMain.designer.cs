namespace XCoder
{
	partial class FrmMain
	{
		/// <summary>必需的设计器变量。</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>清理所有正在使用的资源。</summary>
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
            this.cbCutTableName = new System.Windows.Forms.CheckBox();
            this.cbUseID = new System.Windows.Forms.CheckBox();
            this.frmItems = new System.Windows.Forms.Button();
            this.cbRenderGenEntity = new System.Windows.Forms.CheckBox();
            this.btnRelease = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.txtBaseClass = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.btnOpenOutputDir = new System.Windows.Forms.Button();
            this.checkBox5 = new System.Windows.Forms.CheckBox();
            this.richTextBox2 = new System.Windows.Forms.RichTextBox();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.cbFixWord = new System.Windows.Forms.CheckBox();
            this.cbCutPrefix = new System.Windows.Forms.CheckBox();
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
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.label9 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.文件ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.oracle客户端运行时检查ToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.退出XToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.模型ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.模型管理MToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.导出模型EToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.架构管理SToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sQL查询器QToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.关于ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.组件手册ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.表名字段名命名规范ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.博客ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.qQ群1600800ToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.检查更新ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.关于ToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.qQ群1600800ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gbConnect.SuspendLayout();
            this.gbTable.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.gbConfig.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // bt_Connection
            // 
            this.bt_Connection.Location = new System.Drawing.Point(218, 42);
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
            this.gbConnect.Location = new System.Drawing.Point(3, 30);
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
            this.cbConn.SelectionChangeCommitted += new System.EventHandler(this.cbConn_SelectionChangeCommitted);
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
            this.gbTable.Controls.Add(this.bt_GenAll);
            this.gbTable.Controls.Add(this.bt_GenTable);
            this.gbTable.Controls.Add(this.cbTableList);
            this.gbTable.Controls.Add(this.label5);
            this.gbTable.Enabled = false;
            this.gbTable.Location = new System.Drawing.Point(2, 71);
            this.gbTable.Name = "gbTable";
            this.gbTable.Size = new System.Drawing.Size(725, 49);
            this.gbTable.TabIndex = 14;
            this.gbTable.TabStop = false;
            // 
            // bt_GenAll
            // 
            this.bt_GenAll.Location = new System.Drawing.Point(330, 19);
            this.bt_GenAll.Name = "bt_GenAll";
            this.bt_GenAll.Size = new System.Drawing.Size(75, 23);
            this.bt_GenAll.TabIndex = 21;
            this.bt_GenAll.Text = "生成所有表";
            this.bt_GenAll.UseVisualStyleBackColor = true;
            this.bt_GenAll.Click += new System.EventHandler(this.bt_GenAll_Click);
            // 
            // bt_GenTable
            // 
            this.bt_GenTable.Location = new System.Drawing.Point(249, 19);
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
            this.statusStrip1.Location = new System.Drawing.Point(0, 346);
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
            this.gbConfig.Controls.Add(this.cbCutTableName);
            this.gbConfig.Controls.Add(this.cbUseID);
            this.gbConfig.Controls.Add(this.frmItems);
            this.gbConfig.Controls.Add(this.cbRenderGenEntity);
            this.gbConfig.Controls.Add(this.btnRelease);
            this.gbConfig.Controls.Add(this.label3);
            this.gbConfig.Controls.Add(this.txtBaseClass);
            this.gbConfig.Controls.Add(this.label10);
            this.gbConfig.Controls.Add(this.btnOpenOutputDir);
            this.gbConfig.Controls.Add(this.checkBox5);
            this.gbConfig.Controls.Add(this.richTextBox2);
            this.gbConfig.Controls.Add(this.checkBox4);
            this.gbConfig.Controls.Add(this.checkBox3);
            this.gbConfig.Controls.Add(this.cbFixWord);
            this.gbConfig.Controls.Add(this.cbCutPrefix);
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
            this.gbConfig.Location = new System.Drawing.Point(2, 126);
            this.gbConfig.Name = "gbConfig";
            this.gbConfig.Size = new System.Drawing.Size(725, 215);
            this.gbConfig.TabIndex = 26;
            this.gbConfig.TabStop = false;
            // 
            // cbCutTableName
            // 
            this.cbCutTableName.AutoSize = true;
            this.cbCutTableName.Location = new System.Drawing.Point(11, 187);
            this.cbCutTableName.Name = "cbCutTableName";
            this.cbCutTableName.Size = new System.Drawing.Size(132, 16);
            this.cbCutTableName.TabIndex = 52;
            this.cbCutTableName.Text = "去除字段前面的表名";
            this.cbCutTableName.UseVisualStyleBackColor = true;
            // 
            // cbUseID
            // 
            this.cbUseID.AutoSize = true;
            this.cbUseID.Location = new System.Drawing.Point(261, 165);
            this.cbUseID.Name = "cbUseID";
            this.cbUseID.Size = new System.Drawing.Size(84, 16);
            this.cbUseID.TabIndex = 51;
            this.cbUseID.Text = "强制使用ID";
            this.cbUseID.UseVisualStyleBackColor = true;
            // 
            // frmItems
            // 
            this.frmItems.Location = new System.Drawing.Point(282, 131);
            this.frmItems.Name = "frmItems";
            this.frmItems.Size = new System.Drawing.Size(99, 23);
            this.frmItems.TabIndex = 50;
            this.frmItems.Text = "扩展属性编辑";
            this.toolTip1.SetToolTip(this.frmItems, "模版中通过Config.Items[name]使用。");
            this.frmItems.UseVisualStyleBackColor = true;
            this.frmItems.Click += new System.EventHandler(this.frmItems_Click);
            // 
            // cbRenderGenEntity
            // 
            this.cbRenderGenEntity.AutoSize = true;
            this.cbRenderGenEntity.Location = new System.Drawing.Point(178, 79);
            this.cbRenderGenEntity.Name = "cbRenderGenEntity";
            this.cbRenderGenEntity.Size = new System.Drawing.Size(108, 16);
            this.cbRenderGenEntity.TabIndex = 49;
            this.cbRenderGenEntity.Text = "生成泛型实体类";
            this.cbRenderGenEntity.UseVisualStyleBackColor = true;
            // 
            // btnRelease
            // 
            this.btnRelease.Location = new System.Drawing.Point(285, 20);
            this.btnRelease.Name = "btnRelease";
            this.btnRelease.Size = new System.Drawing.Size(88, 23);
            this.btnRelease.TabIndex = 48;
            this.btnRelease.Text = "释放内置模版";
            this.toolTip1.SetToolTip(this.btnRelease, "释放内置的模版到Template目录，作为参考供建立模版使用。");
            this.btnRelease.UseVisualStyleBackColor = true;
            this.btnRelease.Click += new System.EventHandler(this.btnRelease_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(176, 197);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(143, 12);
            this.label3.TabIndex = 29;
            this.label3.Text = ".Net技术交流群：1600800";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // txtBaseClass
            // 
            this.txtBaseClass.Location = new System.Drawing.Point(71, 77);
            this.txtBaseClass.Name = "txtBaseClass";
            this.txtBaseClass.Size = new System.Drawing.Size(97, 21);
            this.txtBaseClass.TabIndex = 47;
            this.txtBaseClass.Text = "Entity";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(11, 81);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(65, 12);
            this.label10.TabIndex = 46;
            this.label10.Text = "实体基类：";
            // 
            // btnOpenOutputDir
            // 
            this.btnOpenOutputDir.Location = new System.Drawing.Point(283, 104);
            this.btnOpenOutputDir.Name = "btnOpenOutputDir";
            this.btnOpenOutputDir.Size = new System.Drawing.Size(75, 23);
            this.btnOpenOutputDir.TabIndex = 45;
            this.btnOpenOutputDir.Text = "打开目录";
            this.btnOpenOutputDir.UseVisualStyleBackColor = true;
            this.btnOpenOutputDir.Click += new System.EventHandler(this.btnOpenOutputDir_Click);
            // 
            // checkBox5
            // 
            this.checkBox5.AutoSize = true;
            this.checkBox5.Location = new System.Drawing.Point(393, 26);
            this.checkBox5.Name = "checkBox5";
            this.checkBox5.Size = new System.Drawing.Size(126, 16);
            this.checkBox5.TabIndex = 42;
            this.checkBox5.Text = "使用.cs文件头模版";
            this.toolTip1.SetToolTip(this.checkBox5, "出现在每一个cs文件头部");
            this.checkBox5.UseVisualStyleBackColor = true;
            // 
            // richTextBox2
            // 
            this.richTextBox2.Location = new System.Drawing.Point(387, 49);
            this.richTextBox2.Name = "richTextBox2";
            this.richTextBox2.Size = new System.Drawing.Size(338, 160);
            this.richTextBox2.TabIndex = 40;
            this.richTextBox2.Text = resources.GetString("richTextBox2.Text");
            // 
            // checkBox4
            // 
            this.checkBox4.AutoSize = true;
            this.checkBox4.Location = new System.Drawing.Point(565, 24);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(72, 16);
            this.checkBox4.TabIndex = 27;
            this.checkBox4.Text = "调试模版";
            this.toolTip1.SetToolTip(this.checkBox4, "输出模版编译的中间文件");
            this.checkBox4.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Location = new System.Drawing.Point(178, 107);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(84, 16);
            this.checkBox3.TabIndex = 39;
            this.checkBox3.Text = "中文文件名";
            this.checkBox3.UseVisualStyleBackColor = true;
            // 
            // cbFixWord
            // 
            this.cbFixWord.AutoSize = true;
            this.cbFixWord.Location = new System.Drawing.Point(151, 165);
            this.cbFixWord.Name = "cbFixWord";
            this.cbFixWord.Size = new System.Drawing.Size(108, 16);
            this.cbFixWord.TabIndex = 38;
            this.cbFixWord.Text = "更正名称大小写";
            this.cbFixWord.UseVisualStyleBackColor = true;
            // 
            // cbCutPrefix
            // 
            this.cbCutPrefix.AutoSize = true;
            this.cbCutPrefix.Location = new System.Drawing.Point(11, 165);
            this.cbCutPrefix.Name = "cbCutPrefix";
            this.cbCutPrefix.Size = new System.Drawing.Size(138, 16);
            this.cbCutPrefix.TabIndex = 37;
            this.cbCutPrefix.Text = "去除前缀（以_为准）";
            this.cbCutPrefix.UseVisualStyleBackColor = true;
            // 
            // txtPrefix
            // 
            this.txtPrefix.Location = new System.Drawing.Point(71, 133);
            this.txtPrefix.Name = "txtPrefix";
            this.txtPrefix.Size = new System.Drawing.Size(184, 21);
            this.txtPrefix.TabIndex = 36;
            this.txtPrefix.Text = "X_;N_;tbl";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 137);
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
            this.txt_OutPath.Location = new System.Drawing.Point(71, 105);
            this.txt_OutPath.Name = "txt_OutPath";
            this.txt_OutPath.Size = new System.Drawing.Size(97, 21);
            this.txt_OutPath.TabIndex = 29;
            this.txt_OutPath.Text = "Dist";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(11, 109);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(65, 12);
            this.label7.TabIndex = 28;
            this.label7.Text = "输出目录：";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(11, 25);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 12);
            this.label6.TabIndex = 27;
            this.label6.Text = "模版：";
            // 
            // cb_Template
            // 
            this.cb_Template.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_Template.FormattingEnabled = true;
            this.cb_Template.Location = new System.Drawing.Point(58, 21);
            this.cb_Template.Name = "cb_Template";
            this.cb_Template.Size = new System.Drawing.Size(142, 20);
            this.cb_Template.TabIndex = 26;
            this.toolTip1.SetToolTip(this.cb_Template, "*开头的是内置系统模版。");
            // 
            // btnImport
            // 
            this.btnImport.Location = new System.Drawing.Point(280, 42);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(80, 23);
            this.btnImport.TabIndex = 30;
            this.btnImport.Text = "导入模型";
            this.toolTip1.SetToolTip(this.btnImport, "把数据库架构信息导出到xml文件，或者从xml文件导入");
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Filter = "架构文件|*.xml";
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.Filter = "架构文件|*.xml";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(388, 39);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(65, 12);
            this.label9.TabIndex = 32;
            this.label9.Text = "两种用法：";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(446, 60);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(167, 12);
            this.label11.TabIndex = 33;
            this.label11.Text = "2，导入模型，得到数据表信息";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(446, 39);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(179, 12);
            this.label12.TabIndex = 34;
            this.label12.Text = "1，连接数据库，得到数据表信息";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.文件ToolStripMenuItem,
            this.模型ToolStripMenuItem,
            this.关于ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(730, 25);
            this.menuStrip1.TabIndex = 35;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 文件ToolStripMenuItem
            // 
            this.文件ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.oracle客户端运行时检查ToolStripMenuItem1,
            this.退出XToolStripMenuItem});
            this.文件ToolStripMenuItem.Name = "文件ToolStripMenuItem";
            this.文件ToolStripMenuItem.Size = new System.Drawing.Size(58, 21);
            this.文件ToolStripMenuItem.Text = "文件(&F)";
            // 
            // oracle客户端运行时检查ToolStripMenuItem1
            // 
            this.oracle客户端运行时检查ToolStripMenuItem1.Name = "oracle客户端运行时检查ToolStripMenuItem1";
            this.oracle客户端运行时检查ToolStripMenuItem1.Size = new System.Drawing.Size(210, 22);
            this.oracle客户端运行时检查ToolStripMenuItem1.Text = "Oracle客户端运行时检查";
            this.oracle客户端运行时检查ToolStripMenuItem1.Click += new System.EventHandler(this.oracle客户端运行时检查ToolStripMenuItem1_Click);
            // 
            // 退出XToolStripMenuItem
            // 
            this.退出XToolStripMenuItem.Name = "退出XToolStripMenuItem";
            this.退出XToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.退出XToolStripMenuItem.Text = "退出(&X)";
            this.退出XToolStripMenuItem.Click += new System.EventHandler(this.退出XToolStripMenuItem_Click);
            // 
            // 模型ToolStripMenuItem
            // 
            this.模型ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.模型管理MToolStripMenuItem,
            this.导出模型EToolStripMenuItem,
            this.架构管理SToolStripMenuItem,
            this.sQL查询器QToolStripMenuItem});
            this.模型ToolStripMenuItem.Name = "模型ToolStripMenuItem";
            this.模型ToolStripMenuItem.Size = new System.Drawing.Size(64, 21);
            this.模型ToolStripMenuItem.Text = "模型(&M)";
            this.模型ToolStripMenuItem.Visible = false;
            // 
            // 模型管理MToolStripMenuItem
            // 
            this.模型管理MToolStripMenuItem.Name = "模型管理MToolStripMenuItem";
            this.模型管理MToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.模型管理MToolStripMenuItem.Text = "模型管理(&M)";
            this.模型管理MToolStripMenuItem.Click += new System.EventHandler(this.模型管理MToolStripMenuItem_Click);
            // 
            // 导出模型EToolStripMenuItem
            // 
            this.导出模型EToolStripMenuItem.Name = "导出模型EToolStripMenuItem";
            this.导出模型EToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.导出模型EToolStripMenuItem.Text = "导出模型(&E)";
            this.导出模型EToolStripMenuItem.Click += new System.EventHandler(this.导出模型EToolStripMenuItem_Click);
            // 
            // 架构管理SToolStripMenuItem
            // 
            this.架构管理SToolStripMenuItem.Name = "架构管理SToolStripMenuItem";
            this.架构管理SToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.架构管理SToolStripMenuItem.Text = "架构管理(&S)";
            this.架构管理SToolStripMenuItem.Visible = false;
            this.架构管理SToolStripMenuItem.Click += new System.EventHandler(this.架构管理SToolStripMenuItem_Click);
            // 
            // sQL查询器QToolStripMenuItem
            // 
            this.sQL查询器QToolStripMenuItem.Name = "sQL查询器QToolStripMenuItem";
            this.sQL查询器QToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.sQL查询器QToolStripMenuItem.Text = "SQL查询器(&Q)";
            this.sQL查询器QToolStripMenuItem.Click += new System.EventHandler(this.sQL查询器QToolStripMenuItem_Click);
            // 
            // 关于ToolStripMenuItem
            // 
            this.关于ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.组件手册ToolStripMenuItem,
            this.表名字段名命名规范ToolStripMenuItem,
            this.toolStripMenuItem2,
            this.博客ToolStripMenuItem,
            this.qQ群1600800ToolStripMenuItem1,
            this.检查更新ToolStripMenuItem,
            this.关于ToolStripMenuItem1,
            this.qQ群1600800ToolStripMenuItem});
            this.关于ToolStripMenuItem.Name = "关于ToolStripMenuItem";
            this.关于ToolStripMenuItem.Size = new System.Drawing.Size(61, 21);
            this.关于ToolStripMenuItem.Text = "帮助(&H)";
            // 
            // 组件手册ToolStripMenuItem
            // 
            this.组件手册ToolStripMenuItem.Name = "组件手册ToolStripMenuItem";
            this.组件手册ToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.组件手册ToolStripMenuItem.Text = "组件手册(&X)";
            this.组件手册ToolStripMenuItem.Click += new System.EventHandler(this.组件手册ToolStripMenuItem_Click);
            // 
            // 表名字段名命名规范ToolStripMenuItem
            // 
            this.表名字段名命名规范ToolStripMenuItem.Name = "表名字段名命名规范ToolStripMenuItem";
            this.表名字段名命名规范ToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.表名字段名命名规范ToolStripMenuItem.Text = "表名字段名命名规范(&N)";
            this.表名字段名命名规范ToolStripMenuItem.Click += new System.EventHandler(this.表名字段名命名规范ToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(199, 6);
            // 
            // 博客ToolStripMenuItem
            // 
            this.博客ToolStripMenuItem.Name = "博客ToolStripMenuItem";
            this.博客ToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.博客ToolStripMenuItem.Text = "博客";
            this.博客ToolStripMenuItem.Click += new System.EventHandler(this.博客ToolStripMenuItem_Click);
            // 
            // qQ群1600800ToolStripMenuItem1
            // 
            this.qQ群1600800ToolStripMenuItem1.Name = "qQ群1600800ToolStripMenuItem1";
            this.qQ群1600800ToolStripMenuItem1.Size = new System.Drawing.Size(202, 22);
            this.qQ群1600800ToolStripMenuItem1.Text = "QQ群1600800";
            // 
            // 检查更新ToolStripMenuItem
            // 
            this.检查更新ToolStripMenuItem.Name = "检查更新ToolStripMenuItem";
            this.检查更新ToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.检查更新ToolStripMenuItem.Text = "检查更新(&U)";
            this.检查更新ToolStripMenuItem.Click += new System.EventHandler(this.检查更新ToolStripMenuItem_Click);
            // 
            // 关于ToolStripMenuItem1
            // 
            this.关于ToolStripMenuItem1.Name = "关于ToolStripMenuItem1";
            this.关于ToolStripMenuItem1.Size = new System.Drawing.Size(202, 22);
            this.关于ToolStripMenuItem1.Text = "关于(&A)";
            this.关于ToolStripMenuItem1.Click += new System.EventHandler(this.关于ToolStripMenuItem1_Click);
            // 
            // qQ群1600800ToolStripMenuItem
            // 
            this.qQ群1600800ToolStripMenuItem.Name = "qQ群1600800ToolStripMenuItem";
            this.qQ群1600800ToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.qQ群1600800ToolStripMenuItem.Text = "QQ群1600800";
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(730, 368);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.btnImport);
            this.Controls.Add(this.gbConfig);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.gbTable);
            this.Controls.Add(this.gbConnect);
            this.Controls.Add(this.bt_Connection);
            this.MainMenuStrip = this.menuStrip1;
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
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
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
        private System.Windows.Forms.CheckBox cbCutPrefix;
        private System.Windows.Forms.ComboBox cbConn;
        private System.Windows.Forms.CheckBox cbFixWord;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.RichTextBox richTextBox2;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.CheckBox checkBox5;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnOpenOutputDir;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.TextBox txtBaseClass;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button btnRelease;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.CheckBox cbRenderGenEntity;
        private System.Windows.Forms.Button frmItems;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 文件ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 模型ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 关于ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 退出XToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 模型管理MToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 导出模型EToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 架构管理SToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sQL查询器QToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 表名字段名命名规范ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem 检查更新ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 关于ToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem 组件手册ToolStripMenuItem;
        private System.Windows.Forms.CheckBox cbUseID;
        private System.Windows.Forms.CheckBox cbCutTableName;
        private System.Windows.Forms.ToolStripMenuItem qQ群1600800ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 博客ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem oracle客户端运行时检查ToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem qQ群1600800ToolStripMenuItem1;
	}
}

