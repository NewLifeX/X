using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Windows;
using NewLife.Yun;
using XCoder;

namespace XCoder.Map
{
    [DisplayName("地图接口")]
    public partial class FrmMain : Form
    {
        /// <summary>业务日志输出</summary>
        ILog BizLog;

        //IDictionary<String, Type> Maps = new Dictionary<String, Type>();
        //IDictionary<String, String> Methods = new Dictionary<String, String>();

        #region 窗体
        public FrmMain()
        {
            InitializeComponent();

            Icon = IcoHelper.GetIcon("地图");
        }

        private void FrmMain_Load(Object sender, EventArgs e)
        {
            var log = TextFileLog.Create(null, "Map_{0:yyyy_MM_dd}.log");
            BizLog = txtReceive.Combine(log);
            txtReceive.UseWinFormControl();

            txtReceive.SetDefaultStyle(12);

            // 加载保存的颜色
            UIConfig.Apply(txtReceive);

            var cb = cbMap;
            cb.Items.Clear();
            cb.DisplayMember = "Name";
            foreach (var item in typeof(IMap).GetAllSubclasses(true))
            {
                //var name = item.GetDisplayName() ?? item.Name;
                //Maps[name] = item;
                //cb.Items.Add(name);
                cb.Items.Add(item);
            }
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }
        #endregion

        #region 收发数据
        private void btnInvoke_Click(Object sender, EventArgs e)
        {
            var btn = sender as Button;

        }
        #endregion

        private void cbMap_SelectedIndexChanged(Object sender, EventArgs e)
        {
            //var name = cbMap.SelectedItem + "";
            //if (!Maps.TryGetValue(name, out var type)) return;
            var type = cbMap.SelectedItem as Type;
            if (type == null) return;

            var cb = cbMethod;
            cb.Items.Clear();
            //Methods.Clear();
            cb.DisplayMember = "Name";
            foreach (var item in type.GetMethods())
            {
                if (item.DeclaringType != type) continue;

                var name = item.Name;
                //Methods.Add(name, name);

                cb.Items.Add(name);
            }
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;

            cb = cbCoordtype;
            cb.Items.Clear();

            if (type.Name.Contains("Baidu"))
            {
                cb.Items.Add("bd09ll");
                cb.Items.Add("bd09mc");
            }
            //else if (name.Contains("高德"))
            //{

            //}

            cb.Items.Add("gcj02ll");
            cb.Items.Add("wgs84ll");
            cb.SelectedIndex = 0;
        }
    }
}