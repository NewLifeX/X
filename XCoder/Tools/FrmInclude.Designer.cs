namespace XCoder.Tools
{
    partial class FrmInclude
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
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txt_Target = new System.Windows.Forms.TextBox();
            this.txt_Split = new System.Windows.Forms.TextBox();
            this.rtb_msg = new System.Windows.Forms.RichTextBox();
            this.btn_FilePath = new System.Windows.Forms.Button();
            this.txt_FilePath = new System.Windows.Forms.TextBox();
            this.btn_ReadFile = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("微软雅黑", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.ForeColor = System.Drawing.Color.Red;
            this.label2.Location = new System.Drawing.Point(20, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(181, 16);
            this.label2.TabIndex = 16;
            this.label2.Text = "选择.csproj项目文件,然后点击按钮执行";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(126, 41);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 16);
            this.label1.TabIndex = 15;
            this.label1.Text = "-->";
            // 
            // txt_Target
            // 
            this.txt_Target.Location = new System.Drawing.Point(174, 39);
            this.txt_Target.Name = "txt_Target";
            this.txt_Target.Size = new System.Drawing.Size(100, 21);
            this.txt_Target.TabIndex = 14;
            this.txt_Target.Text = ".cs";
            // 
            // txt_Split
            // 
            this.txt_Split.Location = new System.Drawing.Point(20, 39);
            this.txt_Split.Name = "txt_Split";
            this.txt_Split.Size = new System.Drawing.Size(100, 21);
            this.txt_Split.TabIndex = 13;
            this.txt_Split.Text = ".Biz.cs";
            // 
            // rtb_msg
            // 
            this.rtb_msg.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtb_msg.Location = new System.Drawing.Point(12, 80);
            this.rtb_msg.Name = "rtb_msg";
            this.rtb_msg.Size = new System.Drawing.Size(404, 167);
            this.rtb_msg.TabIndex = 12;
            this.rtb_msg.Text = "";
            // 
            // btn_FilePath
            // 
            this.btn_FilePath.Location = new System.Drawing.Point(280, 12);
            this.btn_FilePath.Name = "btn_FilePath";
            this.btn_FilePath.Size = new System.Drawing.Size(33, 23);
            this.btn_FilePath.TabIndex = 11;
            this.btn_FilePath.Text = "...";
            this.btn_FilePath.UseVisualStyleBackColor = true;
            this.btn_FilePath.Click += new System.EventHandler(this.btn_FilePath_Click);
            // 
            // txt_FilePath
            // 
            this.txt_FilePath.Location = new System.Drawing.Point(20, 12);
            this.txt_FilePath.Name = "txt_FilePath";
            this.txt_FilePath.Size = new System.Drawing.Size(254, 21);
            this.txt_FilePath.TabIndex = 10;
            // 
            // btn_ReadFile
            // 
            this.btn_ReadFile.Location = new System.Drawing.Point(336, 10);
            this.btn_ReadFile.Name = "btn_ReadFile";
            this.btn_ReadFile.Size = new System.Drawing.Size(75, 23);
            this.btn_ReadFile.TabIndex = 9;
            this.btn_ReadFile.Text = "依赖注入";
            this.btn_ReadFile.UseVisualStyleBackColor = true;
            this.btn_ReadFile.Click += new System.EventHandler(this.btn_ReadFile_Click);
            // 
            // FrmInclude
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(424, 250);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txt_Target);
            this.Controls.Add(this.txt_Split);
            this.Controls.Add(this.rtb_msg);
            this.Controls.Add(this.btn_FilePath);
            this.Controls.Add(this.txt_FilePath);
            this.Controls.Add(this.btn_ReadFile);
            this.Name = "FrmInclude";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "xcode  csproj项目文件包含更新";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txt_Target;
        private System.Windows.Forms.TextBox txt_Split;
        private System.Windows.Forms.RichTextBox rtb_msg;
        private System.Windows.Forms.Button btn_FilePath;
        private System.Windows.Forms.TextBox txt_FilePath;
        private System.Windows.Forms.Button btn_ReadFile;
    }
}