using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewLife.Threading;
using XCode.DataAccessLayer;

namespace XCoder
{
    public partial class FrmQuery : Form
    {
        #region 属性
        private DAL _Dal;
        /// <summary>数据层</summary>
        public DAL Dal { get { return _Dal; } set { _Dal = value; } }
        #endregion

        #region 初始化界面
        public FrmQuery()
        {
            InitializeComponent();

            this.Icon = Source.GetIcon();
        }

        public static FrmQuery Create(DAL db)
        {
            if (db == null) throw new ArgumentNullException("db");

            FrmQuery frm = new FrmQuery();
            frm.Dal = db;

            return frm;
        }

        private void FrmQuery_Load(object sender, EventArgs e)
        {
        }
        #endregion

        private void btnQuery_Click(object sender, EventArgs e)
        {
            var sql = txtSQL.Text;
            if (sql.IsNullOrWhiteSpace()) return;

            Task.Factory.StartNew(() =>
            {
                var sw = new Stopwatch();
                sw.Start();

                String msg = null;
                DataTable dt = null;
                try
                {
                    DataSet ds = Dal.Session.Query(sql);
                    if (ds != null && ds.Tables != null && ds.Tables.Count > 0) dt = ds.Tables[0];

                    msg = "查询完成！";
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                }
                finally
                {
                    sw.Stop();

                    msg += String.Format(" 耗时{0}", sw.Elapsed);
                }

                this.Invoke(() => lbStatus.Text = msg);
                if (dt != null) this.Invoke(() => gv.DataSource = dt);
            }).LogException();
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            var sql = txtSQL.Text;
            if (sql.IsNullOrWhiteSpace()) return;

            Task.Factory.StartNew(() =>
            {
                var sw = new Stopwatch();
                sw.Start();

                String msg = null;
                try
                {
                    Int32 n = Dal.Session.Execute(sql);

                    msg = String.Format("执行完成！共影响{0}行！", n);
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                }
                finally
                {
                    sw.Stop();

                    msg += String.Format(" 耗时{0:HH:mm:ss.zzz}", sw.Elapsed);
                }

                this.Invoke(() => lbStatus.Text = msg);
            }).LogException();
        }
    }
}
