using System.Windows.Forms;

namespace XCoder
{
    public partial class FrmFix : Form
    {
        public FrmFix()
        {
            InitializeComponent();

            this.Icon = Source.GetIcon();
        }

        public static FrmFix Create(ModelConfig config)
        {
            var frm = new FrmFix();
            frm.Config = config;
            frm.LoadConfig();

            return frm;
        }

        private ModelConfig _Config;
        /// <summary>配置</summary>
        public ModelConfig Config { get { return _Config; } set { _Config = value; } }

        void LoadConfig()
        {
            cbNeedFix.Checked = Config.NeedFix;
            txtPrefix.Text = Config.Prefix;
            cbCutPrefix.Checked = Config.AutoCutPrefix;
            cbCutTableName.Checked = Config.AutoCutTableName;
            cbFixWord.Checked = Config.AutoFixWord;
            cbUseID.Checked = Config.UseID;
        }

        void SaveConfig()
        {
            Config.NeedFix = cbNeedFix.Checked;
            Config.Prefix = txtPrefix.Text;
            Config.AutoCutPrefix = cbCutPrefix.Checked;
            Config.AutoCutTableName = cbCutTableName.Checked;
            Config.AutoFixWord = cbFixWord.Checked;
            Config.UseID = cbUseID.Checked;
        }

        private void FrmFix_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
        }
    }
}
