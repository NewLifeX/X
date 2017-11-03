using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Yun;

namespace XCoder.Yun
{
    [DisplayName("地图接口")]
    public partial class FrmMap : Form, IXForm
    {
        /// <summary>业务日志输出</summary>
        ILog BizLog;

        //IDictionary<String, Type> Maps = new Dictionary<String, Type>();
        //IDictionary<String, String> Methods = new Dictionary<String, String>();

        #region 窗体
        public FrmMap()
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
            var type = cbMap.SelectedItem as Type;
            if (type == null) return;

            var method = cbMethod.SelectedItem as MethodInfo;
            if (method == null) return;

            var map = type.CreateInstance() as NewLife.Yun.Map;
            map.Log = XTrace.Log;
            map.CoordType = cbCoordtype.SelectedItem + "";

            // 准备参数
            var addr = txtAddress.Text;
            var city = txtCity.Text;
            var point = new GeoPoint(txtLocation.Text);

            var mps = method.GetParameters();

            Task.Run(async () =>
            {
                Object result = null;
                try
                {
                    var im = map as IMap;
                    if (method.Name == nameof(im.GetGeocoderAsync) && mps.Length == 2)
                    {
                        result = await im.GetGeocoderAsync(addr, city);
                    }
                    else if (method.Name == nameof(im.GetGeocoderAsync) && mps.Length == 1)
                    {
                        result = await im.GetGeocoderAsync(point);
                    }
                    else if (method.Name == nameof(im.GetGeoAsync) && mps.Length == 3)
                    {
                        result = await im.GetGeoAsync(addr, city, true);
                    }
                    else if (method.Name == nameof(im.GetGeoAsync) && mps.Length == 1)
                    {
                        result = await im.GetGeoAsync(point);
                    }
                    else
                    {
                        var ps = new Dictionary<String, Object>();
                        if (mps.Any(k => k.Name.EqualIgnoreCase("address"))) ps["address"] = addr;
                        if (mps.Any(k => k.Name.EqualIgnoreCase("city"))) ps["city"] = city;

                        var task = map.InvokeWithParams(method, ps) as Task;
                        await task;

                        result = task.GetValue("Result");
                    }
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                    return;
                }

                this.Invoke(() =>
                {
                    pgResult.SelectedObject = result;
                });

                //XTrace.WriteLine(map.LastUrl);

                var js = new JsonParser(map.LastString).Decode();
                XTrace.WriteLine(js.ToJson(true));
            });
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

                //var name = item.Name;
                //Methods.Add(name, name);

                cb.Items.Add(item);
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