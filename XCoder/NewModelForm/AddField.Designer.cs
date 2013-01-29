namespace XCoder
{
    partial class AddField
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
            this.gbInfo = new System.Windows.Forms.GroupBox();
            this.btnCancle = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.txtDataType = new System.Windows.Forms.TextBox();
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.txtPrecision = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.ckbNullable = new System.Windows.Forms.CheckBox();
            this.txtNumOfByte = new System.Windows.Forms.TextBox();
            this.txtLength = new System.Windows.Forms.TextBox();
            this.txtDefault = new System.Windows.Forms.TextBox();
            this.ckbPrimarykey = new System.Windows.Forms.CheckBox();
            this.ckbIdentity = new System.Windows.Forms.CheckBox();
            this.combRawType = new System.Windows.Forms.ComboBox();
            this.txtName = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.gbInfo.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbInfo
            // 
            this.gbInfo.Controls.Add(this.btnCancle);
            this.gbInfo.Controls.Add(this.btnSave);
            this.gbInfo.Controls.Add(this.txtDataType);
            this.gbInfo.Controls.Add(this.txtDescription);
            this.gbInfo.Controls.Add(this.txtPrecision);
            this.gbInfo.Controls.Add(this.label2);
            this.gbInfo.Controls.Add(this.ckbNullable);
            this.gbInfo.Controls.Add(this.txtNumOfByte);
            this.gbInfo.Controls.Add(this.txtLength);
            this.gbInfo.Controls.Add(this.txtDefault);
            this.gbInfo.Controls.Add(this.ckbPrimarykey);
            this.gbInfo.Controls.Add(this.ckbIdentity);
            this.gbInfo.Controls.Add(this.combRawType);
            this.gbInfo.Controls.Add(this.txtName);
            this.gbInfo.Controls.Add(this.label13);
            this.gbInfo.Controls.Add(this.label12);
            this.gbInfo.Controls.Add(this.label10);
            this.gbInfo.Controls.Add(this.label8);
            this.gbInfo.Controls.Add(this.label7);
            this.gbInfo.Controls.Add(this.label6);
            this.gbInfo.Controls.Add(this.label3);
            this.gbInfo.Controls.Add(this.label1);
            this.gbInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbInfo.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.gbInfo.Location = new System.Drawing.Point(0, 0);
            this.gbInfo.Name = "gbInfo";
            this.gbInfo.Size = new System.Drawing.Size(468, 267);
            this.gbInfo.TabIndex = 0;
            this.gbInfo.TabStop = false;
            // 
            // btnCancle
            // 
            this.btnCancle.Location = new System.Drawing.Point(368, 228);
            this.btnCancle.Name = "btnCancle";
            this.btnCancle.Size = new System.Drawing.Size(88, 29);
            this.btnCancle.TabIndex = 28;
            this.btnCancle.Text = "取消";
            this.btnCancle.UseVisualStyleBackColor = true;
            this.btnCancle.Click += new System.EventHandler(this.btnCancle_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(277, 228);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(85, 29);
            this.btnSave.TabIndex = 27;
            this.btnSave.Text = "保存";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // txtDataType
            // 
            this.txtDataType.Enabled = false;
            this.txtDataType.Location = new System.Drawing.Point(101, 116);
            this.txtDataType.Name = "txtDataType";
            this.txtDataType.Size = new System.Drawing.Size(155, 29);
            this.txtDataType.TabIndex = 26;
            // 
            // txtDescription
            // 
            this.txtDescription.Location = new System.Drawing.Point(326, 153);
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.Size = new System.Drawing.Size(130, 29);
            this.txtDescription.TabIndex = 25;
            // 
            // txtPrecision
            // 
            this.txtPrecision.Enabled = false;
            this.txtPrecision.Location = new System.Drawing.Point(326, 114);
            this.txtPrecision.Name = "txtPrecision";
            this.txtPrecision.Size = new System.Drawing.Size(130, 29);
            this.txtPrecision.TabIndex = 23;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(24, 120);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 21);
            this.label2.TabIndex = 1;
            this.label2.Text = "数据类型";
            // 
            // ckbNullable
            // 
            this.ckbNullable.AutoSize = true;
            this.ckbNullable.Location = new System.Drawing.Point(253, 197);
            this.ckbNullable.Name = "ckbNullable";
            this.ckbNullable.Size = new System.Drawing.Size(109, 25);
            this.ckbNullable.TabIndex = 21;
            this.ckbNullable.Text = "是否允许空";
            this.ckbNullable.UseVisualStyleBackColor = true;
            // 
            // txtNumOfByte
            // 
            this.txtNumOfByte.Enabled = false;
            this.txtNumOfByte.Location = new System.Drawing.Point(326, 75);
            this.txtNumOfByte.Name = "txtNumOfByte";
            this.txtNumOfByte.Size = new System.Drawing.Size(130, 29);
            this.txtNumOfByte.TabIndex = 20;
            // 
            // txtLength
            // 
            this.txtLength.Enabled = false;
            this.txtLength.Location = new System.Drawing.Point(326, 36);
            this.txtLength.Name = "txtLength";
            this.txtLength.Size = new System.Drawing.Size(130, 29);
            this.txtLength.TabIndex = 19;
            this.txtLength.TextChanged += new System.EventHandler(this.txtLength_TextChanged);
            this.txtLength.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtLength_KeyPress);
            // 
            // txtDefault
            // 
            this.txtDefault.Location = new System.Drawing.Point(101, 153);
            this.txtDefault.Name = "txtDefault";
            this.txtDefault.Size = new System.Drawing.Size(155, 29);
            this.txtDefault.TabIndex = 18;
            this.txtDefault.Text = "0";
            // 
            // ckbPrimarykey
            // 
            this.ckbPrimarykey.AutoSize = true;
            this.ckbPrimarykey.Location = new System.Drawing.Point(143, 197);
            this.ckbPrimarykey.Name = "ckbPrimarykey";
            this.ckbPrimarykey.Size = new System.Drawing.Size(93, 25);
            this.ckbPrimarykey.TabIndex = 17;
            this.ckbPrimarykey.Text = "是否主键";
            this.ckbPrimarykey.UseVisualStyleBackColor = true;
            // 
            // ckbIdentity
            // 
            this.ckbIdentity.AutoSize = true;
            this.ckbIdentity.Location = new System.Drawing.Point(28, 197);
            this.ckbIdentity.Name = "ckbIdentity";
            this.ckbIdentity.Size = new System.Drawing.Size(109, 25);
            this.ckbIdentity.TabIndex = 16;
            this.ckbIdentity.Text = "是否标识列";
            this.ckbIdentity.UseVisualStyleBackColor = true;
            // 
            // combRawType
            // 
            this.combRawType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.combRawType.FormattingEnabled = true;
            this.combRawType.Location = new System.Drawing.Point(101, 78);
            this.combRawType.Name = "combRawType";
            this.combRawType.Size = new System.Drawing.Size(155, 29);
            this.combRawType.TabIndex = 15;
            this.combRawType.SelectedIndexChanged += new System.EventHandler(this.combRawType_SelectedIndexChanged);
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(101, 36);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(155, 29);
            this.txtName.TabIndex = 13;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(281, 156);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(42, 21);
            this.label13.TabIndex = 12;
            this.label13.Text = "说明";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(40, 156);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(58, 21);
            this.label12.TabIndex = 11;
            this.label12.Text = "默认值";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(302, 70);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(0, 21);
            this.label10.TabIndex = 9;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(281, 117);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(42, 21);
            this.label8.TabIndex = 7;
            this.label8.Text = "精度";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(265, 78);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(58, 21);
            this.label7.TabIndex = 6;
            this.label7.Text = "字节数";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(281, 39);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(42, 21);
            this.label6.TabIndex = 5;
            this.label6.Text = "长度";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(24, 81);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(74, 21);
            this.label3.TabIndex = 2;
            this.label3.Text = "原始类型";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 21);
            this.label1.TabIndex = 0;
            this.label1.Text = "字段名称";
            // 
            // AddField
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gbInfo);
            this.Name = "AddField";
            this.Size = new System.Drawing.Size(468, 267);
            this.gbInfo.ResumeLayout(false);
            this.gbInfo.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbInfo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.ComboBox combRawType;
        private System.Windows.Forms.CheckBox ckbIdentity;
        private System.Windows.Forms.CheckBox ckbPrimarykey;
        private System.Windows.Forms.TextBox txtDefault;
        private System.Windows.Forms.TextBox txtNumOfByte;
        private System.Windows.Forms.TextBox txtLength;
        private System.Windows.Forms.CheckBox ckbNullable;
        private System.Windows.Forms.TextBox txtDescription;
        private System.Windows.Forms.TextBox txtPrecision;
        private System.Windows.Forms.TextBox txtDataType;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancle;
    }
}
