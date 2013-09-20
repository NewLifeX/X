namespace XICO
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
            this.picSrc = new System.Windows.Forms.PictureBox();
            this.picDes = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.fontDialog1 = new System.Windows.Forms.FontDialog();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.txt = new System.Windows.Forms.TextBox();
            this.lbFont = new System.Windows.Forms.Label();
            this.btnWater = new System.Windows.Forms.Button();
            this.btnMakeICO = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.numX = new System.Windows.Forms.NumericUpDown();
            this.numY = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.chk16 = new System.Windows.Forms.CheckBox();
            this.chk32 = new System.Windows.Forms.CheckBox();
            this.chk64 = new System.Windows.Forms.CheckBox();
            this.chk128 = new System.Windows.Forms.CheckBox();
            this.chk256 = new System.Windows.Forms.CheckBox();
            this.sfd = new System.Windows.Forms.SaveFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.picSrc)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numY)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // picSrc
            // 
            this.picSrc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.picSrc.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(224)))), ((int)(((byte)(192)))));
            this.picSrc.Cursor = System.Windows.Forms.Cursors.Default;
            this.picSrc.Location = new System.Drawing.Point(12, 206);
            this.picSrc.Name = "picSrc";
            this.picSrc.Size = new System.Drawing.Size(256, 256);
            this.picSrc.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picSrc.TabIndex = 0;
            this.picSrc.TabStop = false;
            this.picSrc.DragDrop += new System.Windows.Forms.DragEventHandler(this.picSrc_DragDrop);
            this.picSrc.DragEnter += new System.Windows.Forms.DragEventHandler(this.picSrc_DragEnter);
            // 
            // picDes
            // 
            this.picDes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.picDes.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.picDes.Location = new System.Drawing.Point(274, 206);
            this.picDes.Name = "picDes";
            this.picDes.Size = new System.Drawing.Size(256, 256);
            this.picDes.TabIndex = 1;
            this.picDes.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "文本：";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 98);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "字体：";
            // 
            // txt
            // 
            this.txt.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txt.Location = new System.Drawing.Point(55, 22);
            this.txt.Name = "txt";
            this.txt.Size = new System.Drawing.Size(106, 21);
            this.txt.TabIndex = 5;
            this.txt.Text = "码神";
            this.txt.TextChanged += new System.EventHandler(this.txt_TextChanged);
            // 
            // lbFont
            // 
            this.lbFont.AutoSize = true;
            this.lbFont.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbFont.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.lbFont.Location = new System.Drawing.Point(55, 95);
            this.lbFont.Name = "lbFont";
            this.lbFont.Size = new System.Drawing.Size(80, 17);
            this.lbFont.TabIndex = 6;
            this.lbFont.Text = "点击更改字体";
            this.lbFont.Click += new System.EventHandler(this.label3_Click);
            // 
            // btnWater
            // 
            this.btnWater.Location = new System.Drawing.Point(210, 21);
            this.btnWater.Name = "btnWater";
            this.btnWater.Size = new System.Drawing.Size(94, 47);
            this.btnWater.TabIndex = 7;
            this.btnWater.Text = "加水印";
            this.btnWater.UseVisualStyleBackColor = true;
            this.btnWater.Click += new System.EventHandler(this.btnWater_Click);
            // 
            // btnMakeICO
            // 
            this.btnMakeICO.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnMakeICO.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnMakeICO.Location = new System.Drawing.Point(437, 16);
            this.btnMakeICO.Name = "btnMakeICO";
            this.btnMakeICO.Size = new System.Drawing.Size(73, 47);
            this.btnMakeICO.TabIndex = 8;
            this.btnMakeICO.Text = "存图标";
            this.btnMakeICO.UseVisualStyleBackColor = true;
            this.btnMakeICO.Click += new System.EventHandler(this.btnMakeICO_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 74);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 12);
            this.label3.TabIndex = 9;
            this.label3.Text = "颜色：";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.label4.Location = new System.Drawing.Point(55, 74);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(83, 12);
            this.label4.TabIndex = 10;
            this.label4.Text = "点击更改颜色";
            this.label4.Click += new System.EventHandler(this.label4_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 50);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 12);
            this.label5.TabIndex = 11;
            this.label5.Text = "位置：";
            // 
            // numX
            // 
            this.numX.Location = new System.Drawing.Point(76, 46);
            this.numX.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numX.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            -2147483648});
            this.numX.Name = "numX";
            this.numX.Size = new System.Drawing.Size(44, 21);
            this.numX.TabIndex = 12;
            this.numX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numX.Value = new decimal(new int[] {
            25,
            0,
            0,
            -2147483648});
            this.numX.ValueChanged += new System.EventHandler(this.numX_ValueChanged);
            // 
            // numY
            // 
            this.numY.Location = new System.Drawing.Point(144, 46);
            this.numY.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numY.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            -2147483648});
            this.numY.Name = "numY";
            this.numY.Size = new System.Drawing.Size(44, 21);
            this.numY.TabIndex = 13;
            this.numY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numY.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numY.ValueChanged += new System.EventHandler(this.numX_ValueChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(55, 50);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(17, 12);
            this.label6.TabIndex = 14;
            this.label6.Text = "X:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(126, 50);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(17, 12);
            this.label7.TabIndex = 15;
            this.label7.Text = "Y:";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(310, 20);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(94, 47);
            this.btnSave.TabIndex = 16;
            this.btnSave.Text = "保存结果";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnSave);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.txt);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.lbFont);
            this.groupBox1.Controls.Add(this.numY);
            this.groupBox1.Controls.Add(this.btnWater);
            this.groupBox1.Controls.Add(this.numX);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Location = new System.Drawing.Point(12, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(516, 123);
            this.groupBox1.TabIndex = 17;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "水印";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.chk256);
            this.groupBox2.Controls.Add(this.chk128);
            this.groupBox2.Controls.Add(this.chk64);
            this.groupBox2.Controls.Add(this.chk32);
            this.groupBox2.Controls.Add(this.chk16);
            this.groupBox2.Controls.Add(this.btnMakeICO);
            this.groupBox2.Location = new System.Drawing.Point(12, 131);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(516, 69);
            this.groupBox2.TabIndex = 18;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "图标";
            // 
            // chk16
            // 
            this.chk16.AutoSize = true;
            this.chk16.Checked = true;
            this.chk16.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk16.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.chk16.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.chk16.Location = new System.Drawing.Point(6, 31);
            this.chk16.Name = "chk16";
            this.chk16.Size = new System.Drawing.Size(77, 26);
            this.chk16.TabIndex = 9;
            this.chk16.Text = "16*16";
            this.chk16.UseVisualStyleBackColor = true;
            // 
            // chk32
            // 
            this.chk32.AutoSize = true;
            this.chk32.Checked = true;
            this.chk32.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk32.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.chk32.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.chk32.Location = new System.Drawing.Point(83, 31);
            this.chk32.Name = "chk32";
            this.chk32.Size = new System.Drawing.Size(77, 26);
            this.chk32.TabIndex = 10;
            this.chk32.Text = "32*32";
            this.chk32.UseVisualStyleBackColor = true;
            // 
            // chk64
            // 
            this.chk64.AutoSize = true;
            this.chk64.Checked = true;
            this.chk64.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk64.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.chk64.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.chk64.Location = new System.Drawing.Point(160, 31);
            this.chk64.Name = "chk64";
            this.chk64.Size = new System.Drawing.Size(77, 26);
            this.chk64.TabIndex = 11;
            this.chk64.Text = "64*64";
            this.chk64.UseVisualStyleBackColor = true;
            // 
            // chk128
            // 
            this.chk128.AutoSize = true;
            this.chk128.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.chk128.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.chk128.Location = new System.Drawing.Point(237, 31);
            this.chk128.Name = "chk128";
            this.chk128.Size = new System.Drawing.Size(97, 26);
            this.chk128.TabIndex = 12;
            this.chk128.Text = "128*128";
            this.chk128.UseVisualStyleBackColor = true;
            // 
            // chk256
            // 
            this.chk256.AutoSize = true;
            this.chk256.Checked = true;
            this.chk256.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk256.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.chk256.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.chk256.Location = new System.Drawing.Point(334, 31);
            this.chk256.Name = "chk256";
            this.chk256.Size = new System.Drawing.Size(97, 26);
            this.chk256.TabIndex = 13;
            this.chk256.Text = "256*256";
            this.chk256.UseVisualStyleBackColor = true;
            // 
            // sfd
            // 
            this.sfd.Filter = "PNG图片(*.png)|*.png|ICO图标(*.ico)|*.ico|所有文件(*.*)|*.*";
            // 
            // FrmMain
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(540, 472);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.picDes);
            this.Controls.Add(this.picSrc);
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ICO图标水印工具";
            this.Shown += new System.EventHandler(this.FrmMain_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.picSrc)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numY)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox picSrc;
        private System.Windows.Forms.PictureBox picDes;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.FontDialog fontDialog1;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.TextBox txt;
        private System.Windows.Forms.Label lbFont;
        private System.Windows.Forms.Button btnWater;
        private System.Windows.Forms.Button btnMakeICO;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown numX;
        private System.Windows.Forms.NumericUpDown numY;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox chk256;
        private System.Windows.Forms.CheckBox chk128;
        private System.Windows.Forms.CheckBox chk64;
        private System.Windows.Forms.CheckBox chk32;
        private System.Windows.Forms.CheckBox chk16;
        private System.Windows.Forms.SaveFileDialog sfd;
    }
}

