namespace NewLife.Windows
{
    partial class SerialPortList
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

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.cbBaundrate = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbName = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mi数据位 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi停止位 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi校验 = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // cbBaundrate
            // 
            this.cbBaundrate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbBaundrate.FormattingEnabled = true;
            this.cbBaundrate.Location = new System.Drawing.Point(274, 3);
            this.cbBaundrate.Name = "cbBaundrate";
            this.cbBaundrate.Size = new System.Drawing.Size(62, 20);
            this.cbBaundrate.TabIndex = 7;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(226, 7);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 6;
            this.label2.Text = "波特率：";
            // 
            // cbName
            // 
            this.cbName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbName.FormattingEnabled = true;
            this.cbName.Location = new System.Drawing.Point(41, 3);
            this.cbName.Name = "cbName";
            this.cbName.Size = new System.Drawing.Size(162, 20);
            this.cbName.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 12);
            this.label3.TabIndex = 4;
            this.label3.Text = "端口：";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 300;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mi数据位,
            this.mi停止位,
            this.mi校验});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(153, 92);
            // 
            // mi数据位
            // 
            this.mi数据位.Name = "mi数据位";
            this.mi数据位.Size = new System.Drawing.Size(152, 22);
            this.mi数据位.Text = "数据位";
            // 
            // mi停止位
            // 
            this.mi停止位.Name = "mi停止位";
            this.mi停止位.Size = new System.Drawing.Size(152, 22);
            this.mi停止位.Text = "停止位";
            // 
            // mi校验
            // 
            this.mi校验.Name = "mi校验";
            this.mi校验.Size = new System.Drawing.Size(152, 22);
            this.mi校验.Text = "校验";
            // 
            // SerialPortList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ContextMenuStrip = this.contextMenuStrip1;
            this.Controls.Add(this.cbBaundrate);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cbName);
            this.Controls.Add(this.label3);
            this.Name = "SerialPortList";
            this.Size = new System.Drawing.Size(341, 29);
            this.Load += new System.EventHandler(this.SerialPortList_Load);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbBaundrate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem mi数据位;
        private System.Windows.Forms.ToolStripMenuItem mi停止位;
        private System.Windows.Forms.ToolStripMenuItem mi校验;

    }
}
