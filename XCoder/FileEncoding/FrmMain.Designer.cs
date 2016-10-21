namespace XCoder.FileEncoding
{
    partial class FrmMain
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
            this.lbl_file_suffix_name = new System.Windows.Forms.Label();
            this.txtSuffix = new System.Windows.Forms.TextBox();
            this.lbl_file_encode_name = new System.Windows.Forms.Label();
            this.ddlEncodes = new System.Windows.Forms.ComboBox();
            this.btnChoice = new System.Windows.Forms.Button();
            this.txtPath = new System.Windows.Forms.TextBox();
            this.btnReplace = new System.Windows.Forms.Button();
            this.gv_data = new System.Windows.Forms.DataGridView();
            this.序号 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.编码 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.名称 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fbd_choice_folder = new System.Windows.Forms.FolderBrowserDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.btnFind = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.gv_data)).BeginInit();
            this.SuspendLayout();
            // 
            // lbl_file_suffix_name
            // 
            this.lbl_file_suffix_name.AutoSize = true;
            this.lbl_file_suffix_name.Location = new System.Drawing.Point(12, 20);
            this.lbl_file_suffix_name.Name = "lbl_file_suffix_name";
            this.lbl_file_suffix_name.Size = new System.Drawing.Size(29, 12);
            this.lbl_file_suffix_name.TabIndex = 0;
            this.lbl_file_suffix_name.Text = "后缀";
            // 
            // txtSuffix
            // 
            this.txtSuffix.Location = new System.Drawing.Point(47, 16);
            this.txtSuffix.Name = "txtSuffix";
            this.txtSuffix.Size = new System.Drawing.Size(85, 21);
            this.txtSuffix.TabIndex = 1;
            // 
            // lbl_file_encode_name
            // 
            this.lbl_file_encode_name.AutoSize = true;
            this.lbl_file_encode_name.Location = new System.Drawing.Point(480, 20);
            this.lbl_file_encode_name.Name = "lbl_file_encode_name";
            this.lbl_file_encode_name.Size = new System.Drawing.Size(53, 12);
            this.lbl_file_encode_name.TabIndex = 2;
            this.lbl_file_encode_name.Text = "目标编码";
            // 
            // ddlEncodes
            // 
            this.ddlEncodes.FormattingEnabled = true;
            this.ddlEncodes.Location = new System.Drawing.Point(539, 16);
            this.ddlEncodes.Name = "ddlEncodes";
            this.ddlEncodes.Size = new System.Drawing.Size(154, 20);
            this.ddlEncodes.TabIndex = 3;
            // 
            // btnChoice
            // 
            this.btnChoice.Location = new System.Drawing.Point(361, 15);
            this.btnChoice.Name = "btnChoice";
            this.btnChoice.Size = new System.Drawing.Size(31, 23);
            this.btnChoice.TabIndex = 4;
            this.btnChoice.Text = "...";
            this.btnChoice.UseVisualStyleBackColor = true;
            this.btnChoice.Click += new System.EventHandler(this.btn_choice_file_Click);
            // 
            // txtPath
            // 
            this.txtPath.Location = new System.Drawing.Point(198, 16);
            this.txtPath.Name = "txtPath";
            this.txtPath.Size = new System.Drawing.Size(157, 21);
            this.txtPath.TabIndex = 5;
            // 
            // btnReplace
            // 
            this.btnReplace.Location = new System.Drawing.Point(699, 15);
            this.btnReplace.Name = "btnReplace";
            this.btnReplace.Size = new System.Drawing.Size(66, 23);
            this.btnReplace.TabIndex = 6;
            this.btnReplace.Text = "批量转换";
            this.btnReplace.UseVisualStyleBackColor = true;
            this.btnReplace.Click += new System.EventHandler(this.btn_replace_Click);
            // 
            // gv_data
            // 
            this.gv_data.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gv_data.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gv_data.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.序号,
            this.编码,
            this.名称});
            this.gv_data.Location = new System.Drawing.Point(3, 57);
            this.gv_data.Name = "gv_data";
            this.gv_data.RowTemplate.Height = 23;
            this.gv_data.Size = new System.Drawing.Size(784, 623);
            this.gv_data.TabIndex = 7;
            // 
            // 序号
            // 
            this.序号.HeaderText = "序号";
            this.序号.Name = "序号";
            // 
            // 编码
            // 
            this.编码.HeaderText = "编码";
            this.编码.Name = "编码";
            // 
            // 名称
            // 
            this.名称.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.名称.HeaderText = "名称";
            this.名称.Name = "名称";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(152, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 12);
            this.label1.TabIndex = 8;
            this.label1.Text = "路径";
            // 
            // btnFind
            // 
            this.btnFind.Location = new System.Drawing.Point(398, 15);
            this.btnFind.Name = "btnFind";
            this.btnFind.Size = new System.Drawing.Size(61, 23);
            this.btnFind.TabIndex = 9;
            this.btnFind.Text = "查找";
            this.btnFind.UseVisualStyleBackColor = true;
            this.btnFind.Click += new System.EventHandler(this.btnFind_Click);
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(789, 681);
            this.Controls.Add(this.btnFind);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.gv_data);
            this.Controls.Add(this.btnReplace);
            this.Controls.Add(this.txtPath);
            this.Controls.Add(this.btnChoice);
            this.Controls.Add(this.ddlEncodes);
            this.Controls.Add(this.lbl_file_encode_name);
            this.Controls.Add(this.txtSuffix);
            this.Controls.Add(this.lbl_file_suffix_name);
            this.Name = "FrmMain";
            this.Text = "文件编码名";
            this.Load += new System.EventHandler(this.FrmEncodeReplace_Load);
            ((System.ComponentModel.ISupportInitialize)(this.gv_data)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbl_file_suffix_name;
        private System.Windows.Forms.TextBox txtSuffix;
        private System.Windows.Forms.Label lbl_file_encode_name;
        private System.Windows.Forms.ComboBox ddlEncodes;
        private System.Windows.Forms.Button btnChoice;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Button btnReplace;
        private System.Windows.Forms.DataGridView gv_data;
        private System.Windows.Forms.DataGridViewTextBoxColumn 序号;
        private System.Windows.Forms.DataGridViewTextBoxColumn 编码;
        private System.Windows.Forms.DataGridViewTextBoxColumn 名称;
        private System.Windows.Forms.FolderBrowserDialog fbd_choice_folder;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnFind;
    }
}