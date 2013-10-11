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
            this.gbSet = new System.Windows.Forms.GroupBox();
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
            this.gbSet3 = new System.Windows.Forms.GroupBox();
            this.cbEncoding = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.numMutilSend = new System.Windows.Forms.NumericUpDown();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnSendReceive = new System.Windows.Forms.Button();
            this.btnClearSend = new System.Windows.Forms.Button();
            this.chkMutilSend = new System.Windows.Forms.CheckBox();
            this.chkHEXShow = new System.Windows.Forms.CheckBox();
            this.chkHEXSend = new System.Windows.Forms.CheckBox();
            this.gbReceive = new System.Windows.Forms.GroupBox();
            this.txtReceive = new System.Windows.Forms.TextBox();
            this.gbSend = new System.Windows.Forms.GroupBox();
            this.btnMutilSend = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();
            this.txtSend = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.gbSet.SuspendLayout();
            this.gbSet2.SuspendLayout();
            this.gbStatus.SuspendLayout();
            this.gbSet3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMutilSend)).BeginInit();
            this.gbReceive.SuspendLayout();
            this.gbSend.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbSet
            // 
            this.gbSet.Controls.Add(this.cbParity);
            this.gbSet.Controls.Add(this.label5);
            this.gbSet.Controls.Add(this.cbStopBit);
            this.gbSet.Controls.Add(this.label4);
            this.gbSet.Controls.Add(this.cbDataBit);
            this.gbSet.Controls.Add(this.label3);
            this.gbSet.Controls.Add(this.cbBaundrate);
            this.gbSet.Controls.Add(this.label2);
            this.gbSet.Controls.Add(this.cbName);
            this.gbSet.Controls.Add(this.label1);
            this.gbSet.Location = new System.Drawing.Point(12, 12);
            this.gbSet.Name = "gbSet";
            this.gbSet.Size = new System.Drawing.Size(157, 156);
            this.gbSet.TabIndex = 0;
            this.gbSet.TabStop = false;
            this.gbSet.Text = "串口配置";
            // 
            // cbParity
            // 
            this.cbParity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbParity.FormattingEnabled = true;
            this.cbParity.Location = new System.Drawing.Point(71, 127);
            this.cbParity.Name = "cbParity";
            this.cbParity.Size = new System.Drawing.Size(80, 20);
            this.cbParity.TabIndex = 9;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(24, 131);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 12);
            this.label5.TabIndex = 8;
            this.label5.Text = "校验：";
            // 
            // cbStopBit
            // 
            this.cbStopBit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbStopBit.FormattingEnabled = true;
            this.cbStopBit.Location = new System.Drawing.Point(71, 101);
            this.cbStopBit.Name = "cbStopBit";
            this.cbStopBit.Size = new System.Drawing.Size(80, 20);
            this.cbStopBit.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 105);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 12);
            this.label4.TabIndex = 6;
            this.label4.Text = "停止位：";
            // 
            // cbDataBit
            // 
            this.cbDataBit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDataBit.FormattingEnabled = true;
            this.cbDataBit.Location = new System.Drawing.Point(71, 75);
            this.cbDataBit.Name = "cbDataBit";
            this.cbDataBit.Size = new System.Drawing.Size(80, 20);
            this.cbDataBit.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 79);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 4;
            this.label3.Text = "数据位：";
            // 
            // cbBaundrate
            // 
            this.cbBaundrate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbBaundrate.FormattingEnabled = true;
            this.cbBaundrate.Location = new System.Drawing.Point(71, 49);
            this.cbBaundrate.Name = "cbBaundrate";
            this.cbBaundrate.Size = new System.Drawing.Size(80, 20);
            this.cbBaundrate.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "波特率：";
            // 
            // cbName
            // 
            this.cbName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbName.FormattingEnabled = true;
            this.cbName.Location = new System.Drawing.Point(71, 23);
            this.cbName.Name = "cbName";
            this.cbName.Size = new System.Drawing.Size(80, 20);
            this.cbName.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 27);
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
            this.gbSet2.Location = new System.Drawing.Point(12, 212);
            this.gbSet2.Name = "gbSet2";
            this.gbSet2.Size = new System.Drawing.Size(157, 70);
            this.gbSet2.TabIndex = 1;
            this.gbSet2.TabStop = false;
            this.gbSet2.Text = "线路控制";
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
            this.gbStatus.Location = new System.Drawing.Point(12, 288);
            this.gbStatus.Name = "gbStatus";
            this.gbStatus.Size = new System.Drawing.Size(157, 72);
            this.gbStatus.TabIndex = 2;
            this.gbStatus.TabStop = false;
            this.gbStatus.Text = "线路状态（只读）";
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
            // gbSet3
            // 
            this.gbSet3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.gbSet3.Controls.Add(this.cbEncoding);
            this.gbSet3.Controls.Add(this.label6);
            this.gbSet3.Controls.Add(this.numMutilSend);
            this.gbSet3.Controls.Add(this.btnClear);
            this.gbSet3.Controls.Add(this.btnSendReceive);
            this.gbSet3.Controls.Add(this.btnClearSend);
            this.gbSet3.Controls.Add(this.chkMutilSend);
            this.gbSet3.Controls.Add(this.chkHEXShow);
            this.gbSet3.Controls.Add(this.chkHEXSend);
            this.gbSet3.Location = new System.Drawing.Point(12, 366);
            this.gbSet3.Name = "gbSet3";
            this.gbSet3.Size = new System.Drawing.Size(157, 145);
            this.gbSet3.TabIndex = 3;
            this.gbSet3.TabStop = false;
            this.gbSet3.Text = "辅助";
            // 
            // cbEncoding
            // 
            this.cbEncoding.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbEncoding.FormattingEnabled = true;
            this.cbEncoding.Location = new System.Drawing.Point(55, 16);
            this.cbEncoding.Name = "cbEncoding";
            this.cbEncoding.Size = new System.Drawing.Size(80, 20);
            this.cbEncoding.TabIndex = 16;
            this.cbEncoding.SelectedIndexChanged += new System.EventHandler(this.cbEncoding_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 20);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 12);
            this.label6.TabIndex = 15;
            this.label6.Text = "编码：";
            // 
            // numMutilSend
            // 
            this.numMutilSend.Location = new System.Drawing.Point(88, 62);
            this.numMutilSend.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numMutilSend.Name = "numMutilSend";
            this.numMutilSend.Size = new System.Drawing.Size(53, 21);
            this.numMutilSend.TabIndex = 14;
            this.numMutilSend.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numMutilSend.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(39, 115);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 23);
            this.btnClear.TabIndex = 11;
            this.btnClear.Text = "重新计数";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnSendReceive
            // 
            this.btnSendReceive.Location = new System.Drawing.Point(82, 86);
            this.btnSendReceive.Name = "btnSendReceive";
            this.btnSendReceive.Size = new System.Drawing.Size(75, 23);
            this.btnSendReceive.TabIndex = 9;
            this.btnSendReceive.Text = "清接收区";
            this.btnSendReceive.UseVisualStyleBackColor = true;
            this.btnSendReceive.Click += new System.EventHandler(this.btnSendReceive_Click);
            // 
            // btnClearSend
            // 
            this.btnClearSend.Location = new System.Drawing.Point(6, 86);
            this.btnClearSend.Name = "btnClearSend";
            this.btnClearSend.Size = new System.Drawing.Size(75, 23);
            this.btnClearSend.TabIndex = 7;
            this.btnClearSend.Text = "清发送区";
            this.btnClearSend.UseVisualStyleBackColor = true;
            this.btnClearSend.Click += new System.EventHandler(this.btnClearSend_Click);
            // 
            // chkMutilSend
            // 
            this.chkMutilSend.AutoSize = true;
            this.chkMutilSend.Location = new System.Drawing.Point(6, 64);
            this.chkMutilSend.Name = "chkMutilSend";
            this.chkMutilSend.Size = new System.Drawing.Size(72, 16);
            this.chkMutilSend.TabIndex = 3;
            this.chkMutilSend.Text = "连续发送";
            this.chkMutilSend.UseVisualStyleBackColor = true;
            // 
            // chkHEXShow
            // 
            this.chkHEXShow.AutoSize = true;
            this.chkHEXShow.Location = new System.Drawing.Point(76, 42);
            this.chkHEXShow.Name = "chkHEXShow";
            this.chkHEXShow.Size = new System.Drawing.Size(66, 16);
            this.chkHEXShow.TabIndex = 2;
            this.chkHEXShow.Text = "HEX显示";
            this.chkHEXShow.UseVisualStyleBackColor = true;
            // 
            // chkHEXSend
            // 
            this.chkHEXSend.AutoSize = true;
            this.chkHEXSend.Location = new System.Drawing.Point(6, 42);
            this.chkHEXSend.Name = "chkHEXSend";
            this.chkHEXSend.Size = new System.Drawing.Size(66, 16);
            this.chkHEXSend.TabIndex = 1;
            this.chkHEXSend.Text = "HEX发送";
            this.chkHEXSend.UseVisualStyleBackColor = true;
            // 
            // gbReceive
            // 
            this.gbReceive.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbReceive.Controls.Add(this.txtReceive);
            this.gbReceive.Location = new System.Drawing.Point(175, 12);
            this.gbReceive.Name = "gbReceive";
            this.gbReceive.Size = new System.Drawing.Size(561, 329);
            this.gbReceive.TabIndex = 4;
            this.gbReceive.TabStop = false;
            this.gbReceive.Text = "接收区：已接收0字节";
            // 
            // txtReceive
            // 
            this.txtReceive.BackColor = System.Drawing.Color.White;
            this.txtReceive.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtReceive.Location = new System.Drawing.Point(3, 17);
            this.txtReceive.Multiline = true;
            this.txtReceive.Name = "txtReceive";
            this.txtReceive.ReadOnly = true;
            this.txtReceive.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtReceive.Size = new System.Drawing.Size(555, 309);
            this.txtReceive.TabIndex = 0;
            // 
            // gbSend
            // 
            this.gbSend.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbSend.Controls.Add(this.btnMutilSend);
            this.gbSend.Controls.Add(this.btnSend);
            this.gbSend.Controls.Add(this.txtSend);
            this.gbSend.Location = new System.Drawing.Point(178, 347);
            this.gbSend.Name = "gbSend";
            this.gbSend.Size = new System.Drawing.Size(558, 164);
            this.gbSend.TabIndex = 5;
            this.gbSend.TabStop = false;
            this.gbSend.Text = "发送区：已发送0字节";
            // 
            // btnMutilSend
            // 
            this.btnMutilSend.Location = new System.Drawing.Point(390, 127);
            this.btnMutilSend.Name = "btnMutilSend";
            this.btnMutilSend.Size = new System.Drawing.Size(87, 29);
            this.btnMutilSend.TabIndex = 2;
            this.btnMutilSend.Text = "多项发送";
            this.btnMutilSend.UseVisualStyleBackColor = true;
            this.btnMutilSend.Visible = false;
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(64, 127);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(87, 29);
            this.btnSend.TabIndex = 1;
            this.btnSend.Text = "发送";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // txtSend
            // 
            this.txtSend.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSend.BackColor = System.Drawing.Color.White;
            this.txtSend.Location = new System.Drawing.Point(0, 20);
            this.txtSend.Multiline = true;
            this.txtSend.Name = "txtSend";
            this.txtSend.Size = new System.Drawing.Size(555, 101);
            this.txtSend.TabIndex = 0;
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(60, 177);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(87, 29);
            this.btnConnect.TabIndex = 3;
            this.btnConnect.Text = "打开串口";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 300;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(740, 516);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.gbSend);
            this.Controls.Add(this.gbReceive);
            this.Controls.Add(this.gbSet3);
            this.Controls.Add(this.gbStatus);
            this.Controls.Add(this.gbSet2);
            this.Controls.Add(this.gbSet);
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "串口调试工具";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmMain_FormClosing);
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.gbSet.ResumeLayout(false);
            this.gbSet.PerformLayout();
            this.gbSet2.ResumeLayout(false);
            this.gbSet2.PerformLayout();
            this.gbStatus.ResumeLayout(false);
            this.gbStatus.PerformLayout();
            this.gbSet3.ResumeLayout(false);
            this.gbSet3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMutilSend)).EndInit();
            this.gbReceive.ResumeLayout(false);
            this.gbReceive.PerformLayout();
            this.gbSend.ResumeLayout(false);
            this.gbSend.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbSet;
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
        private System.Windows.Forms.GroupBox gbSet3;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnSendReceive;
        private System.Windows.Forms.Button btnClearSend;
        private System.Windows.Forms.CheckBox chkMutilSend;
        private System.Windows.Forms.CheckBox chkHEXShow;
        private System.Windows.Forms.CheckBox chkHEXSend;
        private System.Windows.Forms.GroupBox gbReceive;
        private System.Windows.Forms.TextBox txtReceive;
        private System.Windows.Forms.GroupBox gbSend;
        private System.Windows.Forms.TextBox txtSend;
        private System.Windows.Forms.NumericUpDown numMutilSend;
        private System.Windows.Forms.Button btnMutilSend;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ComboBox cbEncoding;
        private System.Windows.Forms.Label label6;
    }
}

