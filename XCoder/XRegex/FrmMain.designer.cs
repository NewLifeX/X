namespace NewLife.XRegex
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtOption = new System.Windows.Forms.TextBox();
            this.txtPattern = new System.Windows.Forms.TextBox();
            this.ptMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.正则ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.chkSingleline = new System.Windows.Forms.CheckBox();
            this.chkIgnorePatternWhitespace = new System.Windows.Forms.CheckBox();
            this.chkMultiline = new System.Windows.Forms.CheckBox();
            this.chkIgnoreCase = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtContent = new System.Windows.Forms.TextBox();
            this.txtMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.例子ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.button4 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.button5 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.lvMatch = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.splitContainer4 = new System.Windows.Forms.SplitContainer();
            this.lvGroup = new System.Windows.Forms.ListView();
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lvCapture = new System.Windows.Forms.ListView();
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader10 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.rtReplace = new System.Windows.Forms.RichTextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lbStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.groupBox1.SuspendLayout();
            this.ptMenu.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.txtMenu.SuspendLayout();
            this.panel1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.splitContainer4.Panel1.SuspendLayout();
            this.splitContainer4.Panel2.SuspendLayout();
            this.splitContainer4.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtOption);
            this.groupBox1.Controls.Add(this.txtPattern);
            this.groupBox1.Controls.Add(this.chkSingleline);
            this.groupBox1.Controls.Add(this.chkIgnorePatternWhitespace);
            this.groupBox1.Controls.Add(this.chkMultiline);
            this.groupBox1.Controls.Add(this.chkIgnoreCase);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(760, 149);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "正则表达式";
            // 
            // txtOption
            // 
            this.txtOption.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOption.Location = new System.Drawing.Point(421, 18);
            this.txtOption.Name = "txtOption";
            this.txtOption.ReadOnly = true;
            this.txtOption.Size = new System.Drawing.Size(333, 21);
            this.txtOption.TabIndex = 14;
            // 
            // txtPattern
            // 
            this.txtPattern.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPattern.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(224)))), ((int)(((byte)(192)))));
            this.txtPattern.ContextMenuStrip = this.ptMenu;
            this.txtPattern.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtPattern.Location = new System.Drawing.Point(6, 42);
            this.txtPattern.Multiline = true;
            this.txtPattern.Name = "txtPattern";
            this.txtPattern.Size = new System.Drawing.Size(748, 94);
            this.txtPattern.TabIndex = 13;
            // 
            // ptMenu
            // 
            this.ptMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.正则ToolStripMenuItem});
            this.ptMenu.Name = "ptMenu";
            this.ptMenu.Size = new System.Drawing.Size(101, 26);
            this.ptMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ptMenu_Opening);
            // 
            // 正则ToolStripMenuItem
            // 
            this.正则ToolStripMenuItem.Name = "正则ToolStripMenuItem";
            this.正则ToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.正则ToolStripMenuItem.Text = "正则";
            this.正则ToolStripMenuItem.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // chkSingleline
            // 
            this.chkSingleline.AutoSize = true;
            this.chkSingleline.Checked = true;
            this.chkSingleline.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSingleline.Location = new System.Drawing.Point(106, 20);
            this.chkSingleline.Name = "chkSingleline";
            this.chkSingleline.Size = new System.Drawing.Size(72, 16);
            this.chkSingleline.TabIndex = 6;
            this.chkSingleline.Text = "单行模式";
            this.toolTip1.SetToolTip(this.chkSingleline, "使用单行模式，其中句点 (.)匹配每个字符（而不是与除 \\n 之外的每个字符匹配）。 ");
            this.chkSingleline.UseVisualStyleBackColor = true;
            this.chkSingleline.Click += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // chkIgnorePatternWhitespace
            // 
            this.chkIgnorePatternWhitespace.AutoSize = true;
            this.chkIgnorePatternWhitespace.Checked = true;
            this.chkIgnorePatternWhitespace.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIgnorePatternWhitespace.Location = new System.Drawing.Point(280, 20);
            this.chkIgnorePatternWhitespace.Name = "chkIgnorePatternWhitespace";
            this.chkIgnorePatternWhitespace.Size = new System.Drawing.Size(120, 16);
            this.chkIgnorePatternWhitespace.TabIndex = 4;
            this.chkIgnorePatternWhitespace.Text = "忽略正则中的空白";
            this.toolTip1.SetToolTip(this.chkIgnorePatternWhitespace, "从模式中排除保留的空白并启用数字符号 ( #) 后的注释。");
            this.chkIgnorePatternWhitespace.UseVisualStyleBackColor = true;
            this.chkIgnorePatternWhitespace.Click += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // chkMultiline
            // 
            this.chkMultiline.AutoSize = true;
            this.chkMultiline.Checked = true;
            this.chkMultiline.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkMultiline.Location = new System.Drawing.Point(193, 20);
            this.chkMultiline.Name = "chkMultiline";
            this.chkMultiline.Size = new System.Drawing.Size(72, 16);
            this.chkMultiline.TabIndex = 3;
            this.chkMultiline.Text = "多线模式";
            this.toolTip1.SetToolTip(this.chkMultiline, "使用多线模式，其中 ^ 和 $ 匹配每行的开头和末尾（不是输入字符串的开头和末尾）。 ");
            this.chkMultiline.UseVisualStyleBackColor = true;
            this.chkMultiline.Click += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // chkIgnoreCase
            // 
            this.chkIgnoreCase.AutoSize = true;
            this.chkIgnoreCase.Checked = true;
            this.chkIgnoreCase.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIgnoreCase.Location = new System.Drawing.Point(7, 20);
            this.chkIgnoreCase.Name = "chkIgnoreCase";
            this.chkIgnoreCase.Size = new System.Drawing.Size(84, 16);
            this.chkIgnoreCase.TabIndex = 2;
            this.chkIgnoreCase.Text = "忽略大小写";
            this.toolTip1.SetToolTip(this.chkIgnoreCase, "使用不区分大小写的匹配");
            this.chkIgnoreCase.UseVisualStyleBackColor = true;
            this.chkIgnoreCase.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            this.chkIgnoreCase.Click += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtContent);
            this.groupBox2.Controls.Add(this.radioButton2);
            this.groupBox2.Controls.Add(this.radioButton1);
            this.groupBox2.Controls.Add(this.panel1);
            this.groupBox2.Controls.Add(this.button2);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(760, 191);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "数据源";
            // 
            // txtContent
            // 
            this.txtContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtContent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txtContent.ContextMenuStrip = this.txtMenu;
            this.txtContent.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtContent.Location = new System.Drawing.Point(6, 42);
            this.txtContent.Multiline = true;
            this.txtContent.Name = "txtContent";
            this.txtContent.Size = new System.Drawing.Size(748, 143);
            this.txtContent.TabIndex = 14;
            // 
            // txtMenu
            // 
            this.txtMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.例子ToolStripMenuItem});
            this.txtMenu.Name = "txtMenu";
            this.txtMenu.Size = new System.Drawing.Size(101, 26);
            this.txtMenu.Opening += new System.ComponentModel.CancelEventHandler(this.txtMenu_Opening);
            // 
            // 例子ToolStripMenuItem
            // 
            this.例子ToolStripMenuItem.Name = "例子ToolStripMenuItem";
            this.例子ToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.例子ToolStripMenuItem.Text = "例子";
            this.例子ToolStripMenuItem.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(63, 16);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(47, 16);
            this.radioButton2.TabIndex = 12;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "替换";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = true;
            this.radioButton1.Location = new System.Drawing.Point(9, 16);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(47, 16);
            this.radioButton1.TabIndex = 11;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "匹配";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.textBox2);
            this.panel1.Controls.Add(this.button4);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.button5);
            this.panel1.Location = new System.Drawing.Point(355, 13);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(284, 28);
            this.panel1.TabIndex = 10;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(139, 3);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(56, 21);
            this.textBox2.TabIndex = 5;
            this.textBox2.Text = "*.*";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(7, 2);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(75, 23);
            this.button4.TabIndex = 4;
            this.button4.Text = "打开目录";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(98, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 12);
            this.label1.TabIndex = 6;
            this.label1.Text = "过滤:";
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(203, 2);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(75, 23);
            this.button5.TabIndex = 7;
            this.button5.Text = "全部替换";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(132, 13);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "正则匹配";
            this.toolTip1.SetToolTip(this.button2, "如果正则中选中部分文本，则以选中部分文本作为匹配用正则。");
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.splitContainer3);
            this.groupBox3.Controls.Add(this.rtReplace);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox3.Location = new System.Drawing.Point(0, 0);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(760, 169);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "匹配结果";
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(3, 17);
            this.splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.lvMatch);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.splitContainer4);
            this.splitContainer3.Size = new System.Drawing.Size(754, 149);
            this.splitContainer3.SplitterDistance = 221;
            this.splitContainer3.TabIndex = 0;
            // 
            // lvMatch
            // 
            this.lvMatch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.lvMatch.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader8});
            this.lvMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvMatch.FullRowSelect = true;
            this.lvMatch.GridLines = true;
            this.lvMatch.HideSelection = false;
            this.lvMatch.Location = new System.Drawing.Point(0, 0);
            this.lvMatch.MultiSelect = false;
            this.lvMatch.Name = "lvMatch";
            this.lvMatch.Size = new System.Drawing.Size(221, 149);
            this.lvMatch.TabIndex = 0;
            this.lvMatch.UseCompatibleStateImageBehavior = false;
            this.lvMatch.View = System.Windows.Forms.View.Details;
            this.lvMatch.SelectedIndexChanged += new System.EventHandler(this.lvMatch_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "顺序";
            this.columnHeader1.Width = 36;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "匹配项";
            this.columnHeader2.Width = 118;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "位置";
            // 
            // splitContainer4
            // 
            this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer4.Location = new System.Drawing.Point(0, 0);
            this.splitContainer4.Name = "splitContainer4";
            // 
            // splitContainer4.Panel1
            // 
            this.splitContainer4.Panel1.Controls.Add(this.lvGroup);
            // 
            // splitContainer4.Panel2
            // 
            this.splitContainer4.Panel2.Controls.Add(this.lvCapture);
            this.splitContainer4.Size = new System.Drawing.Size(529, 149);
            this.splitContainer4.SplitterDistance = 314;
            this.splitContainer4.TabIndex = 0;
            // 
            // lvGroup
            // 
            this.lvGroup.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.lvGroup.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader7,
            this.columnHeader9});
            this.lvGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvGroup.FullRowSelect = true;
            this.lvGroup.GridLines = true;
            this.lvGroup.HideSelection = false;
            this.lvGroup.Location = new System.Drawing.Point(0, 0);
            this.lvGroup.MultiSelect = false;
            this.lvGroup.Name = "lvGroup";
            this.lvGroup.Size = new System.Drawing.Size(314, 149);
            this.lvGroup.TabIndex = 0;
            this.lvGroup.UseCompatibleStateImageBehavior = false;
            this.lvGroup.View = System.Windows.Forms.View.Details;
            this.lvGroup.SelectedIndexChanged += new System.EventHandler(this.lvGroup_SelectedIndexChanged);
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "顺序";
            this.columnHeader3.Width = 36;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "名称";
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "匹配项";
            this.columnHeader7.Width = 124;
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "位置";
            // 
            // lvCapture
            // 
            this.lvCapture.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.lvCapture.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader10});
            this.lvCapture.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvCapture.FullRowSelect = true;
            this.lvCapture.GridLines = true;
            this.lvCapture.HideSelection = false;
            this.lvCapture.Location = new System.Drawing.Point(0, 0);
            this.lvCapture.MultiSelect = false;
            this.lvCapture.Name = "lvCapture";
            this.lvCapture.Size = new System.Drawing.Size(211, 149);
            this.lvCapture.TabIndex = 0;
            this.lvCapture.UseCompatibleStateImageBehavior = false;
            this.lvCapture.View = System.Windows.Forms.View.Details;
            this.lvCapture.SelectedIndexChanged += new System.EventHandler(this.lvCapture_SelectedIndexChanged);
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "顺序";
            this.columnHeader5.Width = 36;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "匹配项";
            this.columnHeader6.Width = 109;
            // 
            // columnHeader10
            // 
            this.columnHeader10.Text = "位置";
            // 
            // rtReplace
            // 
            this.rtReplace.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.rtReplace.DetectUrls = false;
            this.rtReplace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtReplace.Font = new System.Drawing.Font("新宋体", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.rtReplace.HideSelection = false;
            this.rtReplace.Location = new System.Drawing.Point(3, 17);
            this.rtReplace.Name = "rtReplace";
            this.rtReplace.Size = new System.Drawing.Size(754, 149);
            this.rtReplace.TabIndex = 1;
            this.rtReplace.Text = "";
            this.rtReplace.Visible = false;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lbStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 540);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(784, 22);
            this.statusStrip1.TabIndex = 3;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lbStatus
            // 
            this.lbStatus.Name = "lbStatus";
            this.lbStatus.Size = new System.Drawing.Size(44, 17);
            this.lbStatus.Text = "已就绪";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(12, 12);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(760, 517);
            this.splitContainer1.SplitterDistance = 149;
            this.splitContainer1.TabIndex = 4;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.groupBox2);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.groupBox3);
            this.splitContainer2.Size = new System.Drawing.Size(760, 364);
            this.splitContainer2.SplitterDistance = 191;
            this.splitContainer2.TabIndex = 0;
            // 
            // folderBrowserDialog1
            // 
            this.folderBrowserDialog1.ShowNewFolderButton = false;
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 562);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip1);
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "新生命正则表达式工具";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmMain_FormClosing);
            this.Shown += new System.EventHandler(this.FrmMain_Shown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ptMenu.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.txtMenu.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            this.splitContainer3.ResumeLayout(false);
            this.splitContainer4.Panel1.ResumeLayout(false);
            this.splitContainer4.Panel2.ResumeLayout(false);
            this.splitContainer4.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lbStatus;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.CheckBox chkIgnorePatternWhitespace;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.CheckBox chkMultiline;
        private System.Windows.Forms.CheckBox chkIgnoreCase;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.ListView lvMatch;
        private System.Windows.Forms.SplitContainer splitContainer4;
        private System.Windows.Forms.ListView lvGroup;
        private System.Windows.Forms.ListView lvCapture;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.ContextMenuStrip ptMenu;
        private System.Windows.Forms.ContextMenuStrip txtMenu;
        private System.Windows.Forms.ToolStripMenuItem 正则ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 例子ToolStripMenuItem;
        private System.Windows.Forms.CheckBox chkSingleline;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.ColumnHeader columnHeader10;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RichTextBox rtReplace;
        private System.Windows.Forms.TextBox txtPattern;
        private System.Windows.Forms.TextBox txtContent;
        private System.Windows.Forms.TextBox txtOption;
    }
}

