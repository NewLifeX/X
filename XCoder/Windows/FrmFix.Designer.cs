namespace XCoder
{
    partial class FrmFix
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPrefix = new System.Windows.Forms.TextBox();
            this.cbCutPrefix = new System.Windows.Forms.CheckBox();
            this.cbFixWord = new System.Windows.Forms.CheckBox();
            this.cbCutTableName = new System.Windows.Forms.CheckBox();
            this.cbUseID = new System.Windows.Forms.CheckBox();
            this.cbNeedFix = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtPrefix);
            this.groupBox1.Controls.Add(this.cbCutPrefix);
            this.groupBox1.Controls.Add(this.cbFixWord);
            this.groupBox1.Controls.Add(this.cbCutTableName);
            this.groupBox1.Controls.Add(this.cbUseID);
            this.groupBox1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox1.ForeColor = System.Drawing.Color.Blue;
            this.groupBox1.Location = new System.Drawing.Point(9, 34);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(315, 97);
            this.groupBox1.TabIndex = 58;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "格式化（设置对数据库或导入的模型进行处理）";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(68, 17);
            this.label2.TabIndex = 35;
            this.label2.Text = "删除前缀：";
            // 
            // txtPrefix
            // 
            this.txtPrefix.Location = new System.Drawing.Point(66, 21);
            this.txtPrefix.Name = "txtPrefix";
            this.txtPrefix.Size = new System.Drawing.Size(243, 23);
            this.txtPrefix.TabIndex = 36;
            // 
            // cbCutPrefix
            // 
            this.cbCutPrefix.AutoSize = true;
            this.cbCutPrefix.Location = new System.Drawing.Point(6, 53);
            this.cbCutPrefix.Name = "cbCutPrefix";
            this.cbCutPrefix.Size = new System.Drawing.Size(140, 21);
            this.cbCutPrefix.TabIndex = 37;
            this.cbCutPrefix.Text = "去除前缀（以_为准）";
            this.cbCutPrefix.UseVisualStyleBackColor = true;
            // 
            // cbFixWord
            // 
            this.cbFixWord.AutoSize = true;
            this.cbFixWord.Location = new System.Drawing.Point(146, 53);
            this.cbFixWord.Name = "cbFixWord";
            this.cbFixWord.Size = new System.Drawing.Size(111, 21);
            this.cbFixWord.TabIndex = 38;
            this.cbFixWord.Text = "更正名称大小写";
            this.cbFixWord.UseVisualStyleBackColor = true;
            // 
            // cbCutTableName
            // 
            this.cbCutTableName.AutoSize = true;
            this.cbCutTableName.Location = new System.Drawing.Point(6, 75);
            this.cbCutTableName.Name = "cbCutTableName";
            this.cbCutTableName.Size = new System.Drawing.Size(135, 21);
            this.cbCutTableName.TabIndex = 52;
            this.cbCutTableName.Text = "去除字段前面的表名";
            this.cbCutTableName.UseVisualStyleBackColor = true;
            // 
            // cbUseID
            // 
            this.cbUseID.AutoSize = true;
            this.cbUseID.Location = new System.Drawing.Point(146, 75);
            this.cbUseID.Name = "cbUseID";
            this.cbUseID.Size = new System.Drawing.Size(89, 21);
            this.cbUseID.TabIndex = 51;
            this.cbUseID.Text = "强制使用ID";
            this.cbUseID.UseVisualStyleBackColor = true;
            // 
            // cbNeedFix
            // 
            this.cbNeedFix.AutoSize = true;
            this.cbNeedFix.Checked = true;
            this.cbNeedFix.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbNeedFix.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbNeedFix.ForeColor = System.Drawing.Color.Blue;
            this.cbNeedFix.Location = new System.Drawing.Point(12, 12);
            this.cbNeedFix.Name = "cbNeedFix";
            this.cbNeedFix.Size = new System.Drawing.Size(207, 21);
            this.cbNeedFix.TabIndex = 57;
            this.cbNeedFix.Text = "自动格式化（在导入模型前设置）";
            this.cbNeedFix.UseVisualStyleBackColor = true;
            // 
            // FrmFix
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(331, 140);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.cbNeedFix);
            this.Name = "FrmFix";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "自动格式化设置";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmFix_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtPrefix;
        private System.Windows.Forms.CheckBox cbCutPrefix;
        private System.Windows.Forms.CheckBox cbFixWord;
        private System.Windows.Forms.CheckBox cbCutTableName;
        private System.Windows.Forms.CheckBox cbUseID;
        private System.Windows.Forms.CheckBox cbNeedFix;
    }
}