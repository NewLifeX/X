namespace Zero.Desktop
{
    partial class FrmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupBox1 = new GroupBox();
            btnOpenAsync = new Button();
            txtServer = new TextBox();
            btnOpen = new Button();
            label1 = new Label();
            groupBox2 = new GroupBox();
            textBox2 = new TextBox();
            label3 = new Label();
            btnCall = new Button();
            cbApi = new ComboBox();
            label2 = new Label();
            groupBox3 = new GroupBox();
            richTextBox1 = new RichTextBox();
            btnCallAsync = new Button();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(btnOpenAsync);
            groupBox1.Controls.Add(txtServer);
            groupBox1.Controls.Add(btnOpen);
            groupBox1.Controls.Add(label1);
            groupBox1.Location = new Point(11, 10);
            groupBox1.Margin = new Padding(3, 2, 3, 2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(3, 2, 3, 2);
            groupBox1.Size = new Size(1134, 75);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "数据库连接";
            // 
            // btnOpenAsync
            // 
            btnOpenAsync.Location = new Point(630, 19);
            btnOpenAsync.Margin = new Padding(3, 2, 3, 2);
            btnOpenAsync.Name = "btnOpenAsync";
            btnOpenAsync.Size = new Size(106, 45);
            btnOpenAsync.TabIndex = 4;
            btnOpenAsync.Text = "异步打开";
            btnOpenAsync.UseVisualStyleBackColor = true;
            btnOpenAsync.Click += btnAsyncOpen_Click;
            // 
            // txtServer
            // 
            txtServer.Location = new Point(96, 28);
            txtServer.Name = "txtServer";
            txtServer.Size = new Size(346, 26);
            txtServer.TabIndex = 3;
            txtServer.Text = "tcp://127.0.0.1:5500";
            // 
            // btnOpen
            // 
            btnOpen.Location = new Point(489, 19);
            btnOpen.Margin = new Padding(3, 2, 3, 2);
            btnOpen.Name = "btnOpen";
            btnOpen.Size = new Size(106, 45);
            btnOpen.TabIndex = 2;
            btnOpen.Text = "打开";
            btnOpen.UseVisualStyleBackColor = true;
            btnOpen.Click += btnOpen_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(19, 31);
            label1.Name = "label1";
            label1.Size = new Size(60, 20);
            label1.TabIndex = 1;
            label1.Text = "连接：";
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox2.Controls.Add(btnCallAsync);
            groupBox2.Controls.Add(textBox2);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(btnCall);
            groupBox2.Controls.Add(cbApi);
            groupBox2.Controls.Add(label2);
            groupBox2.Enabled = false;
            groupBox2.Location = new Point(11, 90);
            groupBox2.Margin = new Padding(3, 2, 3, 2);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(3, 2, 3, 2);
            groupBox2.Size = new Size(1134, 202);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "内容区";
            // 
            // textBox2
            // 
            textBox2.Location = new Point(96, 79);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(346, 26);
            textBox2.TabIndex = 4;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(19, 82);
            label3.Name = "label3";
            label3.Size = new Size(69, 20);
            label3.TabIndex = 3;
            label3.Text = "参数1：";
            // 
            // btnCall
            // 
            btnCall.Location = new Point(489, 67);
            btnCall.Name = "btnCall";
            btnCall.Size = new Size(106, 45);
            btnCall.TabIndex = 2;
            btnCall.Text = "调用";
            btnCall.UseVisualStyleBackColor = true;
            btnCall.Click += btnCall_Click;
            // 
            // cbApi
            // 
            cbApi.FormattingEnabled = true;
            cbApi.Location = new Point(96, 35);
            cbApi.Name = "cbApi";
            cbApi.Size = new Size(346, 28);
            cbApi.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(19, 38);
            label2.Name = "label2";
            label2.Size = new Size(60, 20);
            label2.TabIndex = 0;
            label2.Text = "接口：";
            // 
            // groupBox3
            // 
            groupBox3.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox3.Controls.Add(richTextBox1);
            groupBox3.Location = new Point(14, 296);
            groupBox3.Margin = new Padding(3, 2, 3, 2);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new Padding(3, 2, 3, 2);
            groupBox3.Size = new Size(1128, 400);
            groupBox3.TabIndex = 2;
            groupBox3.TabStop = false;
            groupBox3.Text = "日志";
            // 
            // richTextBox1
            // 
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Location = new Point(3, 21);
            richTextBox1.Margin = new Padding(3, 2, 3, 2);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(1122, 377);
            richTextBox1.TabIndex = 0;
            richTextBox1.Text = "";
            // 
            // btnCallAsync
            // 
            btnCallAsync.Location = new Point(630, 67);
            btnCallAsync.Name = "btnCallAsync";
            btnCallAsync.Size = new Size(106, 45);
            btnCallAsync.TabIndex = 5;
            btnCallAsync.Text = "异步调用";
            btnCallAsync.UseVisualStyleBackColor = true;
            btnCallAsync.Click += btnCallAsync_Click;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(10F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1155, 707);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Margin = new Padding(3, 2, 3, 2);
            Name = "FrmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "零代客户端";
            Load += FrmMain_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private Label label1;
        private Button btnOpen;
        private GroupBox groupBox3;
        private RichTextBox richTextBox1;
        private TextBox txtServer;
        private Button btnCall;
        private ComboBox cbApi;
        private Label label2;
        private TextBox textBox2;
        private Label label3;
        private Button btnOpenAsync;
        private Button btnCallAsync;
    }
}