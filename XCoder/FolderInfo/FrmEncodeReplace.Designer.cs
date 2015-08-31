namespace XCoder.FolderInfo
{
    partial class FrmEncodeReplace
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
            this.txt_file_suffix_name = new System.Windows.Forms.TextBox();
            this.lbl_file_encode_name = new System.Windows.Forms.Label();
            this.cmb_file_encode_name = new System.Windows.Forms.ComboBox();
            this.btn_choice_file = new System.Windows.Forms.Button();
            this.txt_file_path = new System.Windows.Forms.TextBox();
            this.btn_replace = new System.Windows.Forms.Button();
            this.gv_data = new System.Windows.Forms.DataGridView();
            this.序号 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.编码 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.名称 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fbd_choice_folder = new System.Windows.Forms.FolderBrowserDialog();
            ((System.ComponentModel.ISupportInitialize)(this.gv_data)).BeginInit();
            this.SuspendLayout();
            // 
            // lbl_file_suffix_name
            // 
            this.lbl_file_suffix_name.AutoSize = true;
            this.lbl_file_suffix_name.Location = new System.Drawing.Point(12, 20);
            this.lbl_file_suffix_name.Name = "lbl_file_suffix_name";
            this.lbl_file_suffix_name.Size = new System.Drawing.Size(65, 12);
            this.lbl_file_suffix_name.TabIndex = 0;
            this.lbl_file_suffix_name.Text = "文件后缀名";
            // 
            // txt_file_suffix_name
            // 
            this.txt_file_suffix_name.Location = new System.Drawing.Point(83, 16);
            this.txt_file_suffix_name.Name = "txt_file_suffix_name";
            this.txt_file_suffix_name.Size = new System.Drawing.Size(85, 21);
            this.txt_file_suffix_name.TabIndex = 1;
            // 
            // lbl_file_encode_name
            // 
            this.lbl_file_encode_name.AutoSize = true;
            this.lbl_file_encode_name.Location = new System.Drawing.Point(187, 20);
            this.lbl_file_encode_name.Name = "lbl_file_encode_name";
            this.lbl_file_encode_name.Size = new System.Drawing.Size(65, 12);
            this.lbl_file_encode_name.TabIndex = 2;
            this.lbl_file_encode_name.Text = "文件编码名";
            // 
            // cmb_file_encode_name
            // 
            this.cmb_file_encode_name.FormattingEnabled = true;
            this.cmb_file_encode_name.Location = new System.Drawing.Point(258, 16);
            this.cmb_file_encode_name.Name = "cmb_file_encode_name";
            this.cmb_file_encode_name.Size = new System.Drawing.Size(84, 20);
            this.cmb_file_encode_name.TabIndex = 3;
            // 
            // btn_choice_file
            // 
            this.btn_choice_file.Location = new System.Drawing.Point(533, 15);
            this.btn_choice_file.Name = "btn_choice_file";
            this.btn_choice_file.Size = new System.Drawing.Size(31, 23);
            this.btn_choice_file.TabIndex = 4;
            this.btn_choice_file.Text = "...";
            this.btn_choice_file.UseVisualStyleBackColor = true;
            this.btn_choice_file.Click += new System.EventHandler(this.btn_choice_file_Click);
            // 
            // txt_file_path
            // 
            this.txt_file_path.Location = new System.Drawing.Point(370, 16);
            this.txt_file_path.Name = "txt_file_path";
            this.txt_file_path.Size = new System.Drawing.Size(157, 21);
            this.txt_file_path.TabIndex = 5;
            // 
            // btn_replace
            // 
            this.btn_replace.Location = new System.Drawing.Point(581, 15);
            this.btn_replace.Name = "btn_replace";
            this.btn_replace.Size = new System.Drawing.Size(97, 23);
            this.btn_replace.TabIndex = 6;
            this.btn_replace.Text = "批量替换编码";
            this.btn_replace.UseVisualStyleBackColor = true;
            this.btn_replace.Click += new System.EventHandler(this.btn_replace_Click);
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
            // FrmEncodeReplace
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(789, 681);
            this.Controls.Add(this.gv_data);
            this.Controls.Add(this.btn_replace);
            this.Controls.Add(this.txt_file_path);
            this.Controls.Add(this.btn_choice_file);
            this.Controls.Add(this.cmb_file_encode_name);
            this.Controls.Add(this.lbl_file_encode_name);
            this.Controls.Add(this.txt_file_suffix_name);
            this.Controls.Add(this.lbl_file_suffix_name);
            this.Name = "FrmEncodeReplace";
            this.Text = "文件编码名";
            this.Load += new System.EventHandler(this.FrmEncodeReplace_Load);
            ((System.ComponentModel.ISupportInitialize)(this.gv_data)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbl_file_suffix_name;
        private System.Windows.Forms.TextBox txt_file_suffix_name;
        private System.Windows.Forms.Label lbl_file_encode_name;
        private System.Windows.Forms.ComboBox cmb_file_encode_name;
        private System.Windows.Forms.Button btn_choice_file;
        private System.Windows.Forms.TextBox txt_file_path;
        private System.Windows.Forms.Button btn_replace;
        private System.Windows.Forms.DataGridView gv_data;
        private System.Windows.Forms.DataGridViewTextBoxColumn 序号;
        private System.Windows.Forms.DataGridViewTextBoxColumn 编码;
        private System.Windows.Forms.DataGridViewTextBoxColumn 名称;
        private System.Windows.Forms.FolderBrowserDialog fbd_choice_folder;
    }
}