namespace XCom
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
            this.cbParity = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.cbStopBit = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cbDataBit = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cbBaundrate = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbName = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.gbSet2 = new System.Windows.Forms.GroupBox();
            this.chkRTS = new System.Windows.Forms.CheckBox();
            this.chkBreak = new System.Windows.Forms.CheckBox();
            this.chkDTR = new System.Windows.Forms.CheckBox();
            this.gbStatus = new System.Windows.Forms.GroupBox();
            this.chkRLSD = new System.Windows.Forms.CheckBox();
            this.chkRing = new System.Windows.Forms.CheckBox();
            this.chkDSR = new System.Windows.Forms.CheckBox();
            this.chkCTS = new System.Windows.Forms.CheckBox();
            this.numMutilSend = new System.Windows.Forms.NumericUpDown();
            this.gbReceive = new System.Windows.Forms.GroupBox();
            this.txtReceive = new System.Windows.Forms.TextBox();
            this.menuReceive = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mi清空 = new System.Windows.Forms.ToolStripMenuItem();
            this.miHEX编码 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi字符串编码 = new System.Windows.Forms.ToolStripMenuItem();
            this.gbSend = new System.Windows.Forms.GroupBox();
            this.txtSend = new System.Windows.Forms.TextBox();
            this.menuSend = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mi清空2 = new System.Windows.Forms.ToolStripMenuItem();
            this.miHEX编码2 = new System.Windows.Forms.ToolStripMenuItem();
            this.label7 = new System.Windows.Forms.Label();
            this.btnSend = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.pnlSet = new System.Windows.Forms.Panel();
            this.miHex不换行 = new System.Windows.Forms.ToolStripMenuItem();
            this.miHex自动换行 = new System.Windows.Forms.ToolStripMenuItem();
            this.gbSet2.SuspendLayout();
            this.gbStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMutilSend)).BeginInit();
            this.gbReceive.SuspendLayout();
            this.menuReceive.SuspendLayout();
            this.gbSend.SuspendLayout();
            this.menuSend.SuspendLayout();
            this.pnlSet.SuspendLayout();
            this.SuspendLayout();
            // 
            // cbParity
            // 
            this.cbParity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbParity.FormattingEnabled = true;
            this.cbParity.Location = new System.Drawing.Point(274, 194);
            this.cbParity.Name = "cbParity";
            this.cbParity.Size = new System.Drawing.Size(53, 20);
            this.cbParity.TabIndex = 9;
            this.cbParity.Visible = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(237, 198);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 12);
            this.label5.TabIndex = 8;
            this.label5.Text = "校验：";
            this.label5.Visible = false;
            // 
            // cbStopBit
            // 
            this.cbStopBit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbStopBit.FormattingEnabled = true;
            this.cbStopBit.Location = new System.Drawing.Point(176, 194);
            this.cbStopBit.Name = "cbStopBit";
            this.cbStopBit.Size = new System.Drawing.Size(53, 20);
            this.cbStopBit.TabIndex = 7;
            this.cbStopBit.Visible = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(127, 198);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 12);
            this.label4.TabIndex = 6;
            this.label4.Text = "停止位：";
            this.label4.Visible = false;
            // 
            // cbDataBit
            // 
            this.cbDataBit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDataBit.FormattingEnabled = true;
            this.cbDataBit.Location = new System.Drawing.Point(82, 195);
            this.cbDataBit.Name = "cbDataBit";
            this.cbDataBit.Size = new System.Drawing.Size(38, 20);
            this.cbDataBit.TabIndex = 5;
            this.cbDataBit.Visible = false;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(32, 198);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 4;
            this.label3.Text = "数据位：";
            this.label3.Visible = false;
            // 
            // cbBaundrate
            // 
            this.cbBaundrate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbBaundrate.FormattingEnabled = true;
            this.cbBaundrate.Location = new System.Drawing.Point(259, 3);
            this.cbBaundrate.Name = "cbBaundrate";
            this.cbBaundrate.Size = new System.Drawing.Size(62, 20);
            this.cbBaundrate.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(211, 7);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "波特率：";
            // 
            // cbName
            // 
            this.cbName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbName.FormattingEnabled = true;
            this.cbName.Location = new System.Drawing.Point(44, 3);
            this.cbName.Name = "cbName";
            this.cbName.Size = new System.Drawing.Size(160, 20);
            this.cbName.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "端口：";
            // 
            // gbSet2
            // 
            this.gbSet2.Controls.Add(this.chkRTS);
            this.gbSet2.Controls.Add(this.chkBreak);
            this.gbSet2.Controls.Add(this.chkDTR);
            this.gbSet2.Location = new System.Drawing.Point(72, 53);
            this.gbSet2.Name = "gbSet2";
            this.gbSet2.Size = new System.Drawing.Size(157, 70);
            this.gbSet2.TabIndex = 1;
            this.gbSet2.TabStop = false;
            this.gbSet2.Text = "线路控制";
            this.gbSet2.Visible = false;
            // 
            // chkRTS
            // 
            this.chkRTS.AutoSize = true;
            this.chkRTS.Location = new System.Drawing.Point(14, 42);
            this.chkRTS.Name = "chkRTS";
            this.chkRTS.Size = new System.Drawing.Size(42, 16);
            this.chkRTS.TabIndex = 2;
            this.chkRTS.Text = "RTS";
            this.chkRTS.UseVisualStyleBackColor = true;
            // 
            // chkBreak
            // 
            this.chkBreak.AutoSize = true;
            this.chkBreak.Location = new System.Drawing.Point(81, 20);
            this.chkBreak.Name = "chkBreak";
            this.chkBreak.Size = new System.Drawing.Size(54, 16);
            this.chkBreak.TabIndex = 1;
            this.chkBreak.Text = "Break";
            this.chkBreak.UseVisualStyleBackColor = true;
            // 
            // chkDTR
            // 
            this.chkDTR.AutoSize = true;
            this.chkDTR.Location = new System.Drawing.Point(14, 20);
            this.chkDTR.Name = "chkDTR";
            this.chkDTR.Size = new System.Drawing.Size(42, 16);
            this.chkDTR.TabIndex = 0;
            this.chkDTR.Text = "DTR";
            this.chkDTR.UseVisualStyleBackColor = true;
            // 
            // gbStatus
            // 
            this.gbStatus.Controls.Add(this.chkRLSD);
            this.gbStatus.Controls.Add(this.chkRing);
            this.gbStatus.Controls.Add(this.chkDSR);
            this.gbStatus.Controls.Add(this.chkCTS);
            this.gbStatus.ForeColor = System.Drawing.Color.Red;
            this.gbStatus.Location = new System.Drawing.Point(292, 110);
            this.gbStatus.Name = "gbStatus";
            this.gbStatus.Size = new System.Drawing.Size(157, 72);
            this.gbStatus.TabIndex = 2;
            this.gbStatus.TabStop = false;
            this.gbStatus.Text = "线路状态（只读）";
            this.gbStatus.Visible = false;
            // 
            // chkRLSD
            // 
            this.chkRLSD.AutoSize = true;
            this.chkRLSD.Enabled = false;
            this.chkRLSD.Location = new System.Drawing.Point(81, 42);
            this.chkRLSD.Name = "chkRLSD";
            this.chkRLSD.Size = new System.Drawing.Size(48, 16);
            this.chkRLSD.TabIndex = 6;
            this.chkRLSD.Text = "RLSD";
            this.chkRLSD.UseVisualStyleBackColor = true;
            // 
            // chkRing
            // 
            this.chkRing.AutoSize = true;
            this.chkRing.Enabled = false;
            this.chkRing.Location = new System.Drawing.Point(14, 42);
            this.chkRing.Name = "chkRing";
            this.chkRing.Size = new System.Drawing.Size(48, 16);
            this.chkRing.TabIndex = 5;
            this.chkRing.Text = "RING";
            this.chkRing.UseVisualStyleBackColor = true;
            // 
            // chkDSR
            // 
            this.chkDSR.AutoSize = true;
            this.chkDSR.Enabled = false;
            this.chkDSR.Location = new System.Drawing.Point(81, 20);
            this.chkDSR.Name = "chkDSR";
            this.chkDSR.Size = new System.Drawing.Size(42, 16);
            this.chkDSR.TabIndex = 4;
            this.chkDSR.Text = "DSR";
            this.chkDSR.UseVisualStyleBackColor = true;
            // 
            // chkCTS
            // 
            this.chkCTS.AutoSize = true;
            this.chkCTS.Enabled = false;
            this.chkCTS.Location = new System.Drawing.Point(14, 20);
            this.chkCTS.Name = "chkCTS";
            this.chkCTS.Size = new System.Drawing.Size(42, 16);
            this.chkCTS.TabIndex = 3;
            this.chkCTS.Text = "CTS";
            this.chkCTS.UseVisualStyleBackColor = true;
            // 
            // numMutilSend
            // 
            this.numMutilSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.numMutilSend.Location = new System.Drawing.Point(502, 23);
            this.numMutilSend.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numMutilSend.Name = "numMutilSend";
            this.numMutilSend.Size = new System.Drawing.Size(42, 21);
            this.numMutilSend.TabIndex = 14;
            this.numMutilSend.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numMutilSend.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // gbReceive
            // 
            this.gbReceive.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbReceive.Controls.Add(this.cbParity);
            this.gbReceive.Controls.Add(this.label5);
            this.gbReceive.Controls.Add(this.cbStopBit);
            this.gbReceive.Controls.Add(this.label4);
            this.gbReceive.Controls.Add(this.cbDataBit);
            this.gbReceive.Controls.Add(this.label3);
            this.gbReceive.Controls.Add(this.gbStatus);
            this.gbReceive.Controls.Add(this.gbSet2);
            this.gbReceive.Controls.Add(this.txtReceive);
            this.gbReceive.Location = new System.Drawing.Point(9, 43);
            this.gbReceive.Name = "gbReceive";
            this.gbReceive.Size = new System.Drawing.Size(550, 241);
            this.gbReceive.TabIndex = 4;
            this.gbReceive.TabStop = false;
            this.gbReceive.Text = "接收区：已接收0字节";
            // 
            // txtReceive
            // 
            this.txtReceive.BackColor = System.Drawing.Color.White;
            this.txtReceive.ContextMenuStrip = this.menuReceive;
            this.txtReceive.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtReceive.Location = new System.Drawing.Point(3, 17);
            this.txtReceive.Multiline = true;
            this.txtReceive.Name = "txtReceive";
            this.txtReceive.ReadOnly = true;
            this.txtReceive.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtReceive.Size = new System.Drawing.Size(544, 221);
            this.txtReceive.TabIndex = 0;
            // 
            // menuReceive
            // 
            this.menuReceive.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mi清空,
            this.miHEX编码,
            this.mi字符串编码});
            this.menuReceive.Name = "contextMenuStrip1";
            this.menuReceive.Size = new System.Drawing.Size(153, 92);
            // 
            // mi清空
            // 
            this.mi清空.Name = "mi清空";
            this.mi清空.Size = new System.Drawing.Size(152, 22);
            this.mi清空.Text = "清空";
            this.mi清空.Click += new System.EventHandler(this.mi清空_Click);
            // 
            // miHEX编码
            // 
            this.miHEX编码.CheckOnClick = true;
            this.miHEX编码.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miHex不换行,
            this.miHex自动换行});
            this.miHEX编码.Name = "miHEX编码";
            this.miHEX编码.Size = new System.Drawing.Size(152, 22);
            this.miHEX编码.Text = "HEX编码";
            this.miHEX编码.Click += new System.EventHandler(this.miHEX编码_Click);
            // 
            // mi字符串编码
            // 
            this.mi字符串编码.CheckOnClick = true;
            this.mi字符串编码.Name = "mi字符串编码";
            this.mi字符串编码.Size = new System.Drawing.Size(152, 22);
            this.mi字符串编码.Text = "字符串编码";
            this.mi字符串编码.Click += new System.EventHandler(this.mi字符串编码_Click);
            // 
            // gbSend
            // 
            this.gbSend.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbSend.Controls.Add(this.txtSend);
            this.gbSend.Controls.Add(this.label7);
            this.gbSend.Controls.Add(this.btnSend);
            this.gbSend.Controls.Add(this.numMutilSend);
            this.gbSend.Location = new System.Drawing.Point(9, 290);
            this.gbSend.Name = "gbSend";
            this.gbSend.Size = new System.Drawing.Size(550, 84);
            this.gbSend.TabIndex = 5;
            this.gbSend.TabStop = false;
            this.gbSend.Text = "发送区：已发送0字节";
            // 
            // txtSend
            // 
            this.txtSend.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSend.BackColor = System.Drawing.Color.White;
            this.txtSend.ContextMenuStrip = this.menuSend;
            this.txtSend.Location = new System.Drawing.Point(0, 20);
            this.txtSend.Multiline = true;
            this.txtSend.Name = "txtSend";
            this.txtSend.Size = new System.Drawing.Size(451, 59);
            this.txtSend.TabIndex = 0;
            // 
            // menuSend
            // 
            this.menuSend.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mi清空2,
            this.miHEX编码2});
            this.menuSend.Name = "menuSend";
            this.menuSend.Size = new System.Drawing.Size(125, 48);
            // 
            // mi清空2
            // 
            this.mi清空2.Name = "mi清空2";
            this.mi清空2.Size = new System.Drawing.Size(124, 22);
            this.mi清空2.Text = "清空";
            this.mi清空2.Click += new System.EventHandler(this.mi清空2_Click);
            // 
            // miHEX编码2
            // 
            this.miHEX编码2.CheckOnClick = true;
            this.miHEX编码2.Name = "miHEX编码2";
            this.miHEX编码2.Size = new System.Drawing.Size(124, 22);
            this.miHEX编码2.Text = "HEX编码";
            this.miHEX编码2.Click += new System.EventHandler(this.miHEX编码2_Click);
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(457, 27);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(41, 12);
            this.label7.TabIndex = 15;
            this.label7.Text = "次数：";
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.Location = new System.Drawing.Point(457, 50);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(87, 29);
            this.btnSend.TabIndex = 1;
            this.btnSend.Text = "发送";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(346, 8);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(67, 29);
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
            // pnlSet
            // 
            this.pnlSet.Controls.Add(this.cbBaundrate);
            this.pnlSet.Controls.Add(this.label2);
            this.pnlSet.Controls.Add(this.cbName);
            this.pnlSet.Controls.Add(this.label1);
            this.pnlSet.Location = new System.Drawing.Point(12, 8);
            this.pnlSet.Name = "pnlSet";
            this.pnlSet.Size = new System.Drawing.Size(324, 29);
            this.pnlSet.TabIndex = 10;
            // 
            // miHex不换行
            // 
            this.miHex不换行.Checked = true;
            this.miHex不换行.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miHex不换行.Name = "miHex不换行";
            this.miHex不换行.Size = new System.Drawing.Size(152, 22);
            this.miHex不换行.Tag = "false";
            this.miHex不换行.Text = "不换行";
            this.miHex不换行.Click += new System.EventHandler(this.自动换行ToolStripMenuItem_Click);
            // 
            // miHex自动换行
            // 
            this.miHex自动换行.Name = "miHex自动换行";
            this.miHex自动换行.Size = new System.Drawing.Size(152, 22);
            this.miHex自动换行.Tag = "true";
            this.miHex自动换行.Text = "自动换行";
            this.miHex自动换行.Click += new System.EventHandler(this.自动换行ToolStripMenuItem_Click);
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(565, 386);
            this.Controls.Add(this.pnlSet);
            this.Controls.Add(this.gbSend);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.gbReceive);
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "串口调试工具";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmMain_FormClosing);
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.gbSet2.ResumeLayout(false);
            this.gbSet2.PerformLayout();
            this.gbStatus.ResumeLayout(false);
            this.gbStatus.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMutilSend)).EndInit();
            this.gbReceive.ResumeLayout(false);
            this.gbReceive.PerformLayout();
            this.menuReceive.ResumeLayout(false);
            this.gbSend.ResumeLayout(false);
            this.gbSend.PerformLayout();
            this.menuSend.ResumeLayout(false);
            this.pnlSet.ResumeLayout(false);
            this.pnlSet.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cbParity;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cbStopBit;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cbDataBit;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cbBaundrate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox gbSet2;
        private System.Windows.Forms.GroupBox gbStatus;
        private System.Windows.Forms.CheckBox chkRTS;
        private System.Windows.Forms.CheckBox chkBreak;
        private System.Windows.Forms.CheckBox chkDTR;
        private System.Windows.Forms.CheckBox chkCTS;
        private System.Windows.Forms.CheckBox chkRLSD;
        private System.Windows.Forms.CheckBox chkRing;
        private System.Windows.Forms.CheckBox chkDSR;
        private System.Windows.Forms.GroupBox gbReceive;
        private System.Windows.Forms.TextBox txtReceive;
        private System.Windows.Forms.GroupBox gbSend;
        private System.Windows.Forms.TextBox txtSend;
        private System.Windows.Forms.NumericUpDown numMutilSend;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ContextMenuStrip menuReceive;
        private System.Windows.Forms.ToolStripMenuItem mi清空;
        private System.Windows.Forms.ToolStripMenuItem miHEX编码;
        private System.Windows.Forms.ToolStripMenuItem mi字符串编码;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Panel pnlSet;
        private System.Windows.Forms.ContextMenuStrip menuSend;
        private System.Windows.Forms.ToolStripMenuItem mi清空2;
        private System.Windows.Forms.ToolStripMenuItem miHEX编码2;
        private System.Windows.Forms.ToolStripMenuItem miHex不换行;
        private System.Windows.Forms.ToolStripMenuItem miHex自动换行;
    }
}

