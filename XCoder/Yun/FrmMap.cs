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

            // 动态调节宽度高度，兼容高DPI
            this.FixDpi();

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

            LoadConfig();

            var cfg = Setting;

            var cb = cbMap;
            cb.Items.Clear();
            cb.DisplayMember = "Name";
            foreach (var item in typeof(IMap).GetAllSubclasses(true))
            {
                //var name = item.GetDisplayName() ?? item.Name;
                //Maps[name] = item;
                //cb.Items.Add(name);
                cb.Items.Add(item);

                if (cfg.Map == item.Name) cb.SelectedItem = item;
            }
            //if (cb.Items.Count > 0) cb.SelectedText = cfg.Map;
        }
        #endregion

        #region 配置
        MapSetting Setting;

        void LoadConfig()
        {
            var cfg = Setting = MapSetting.Current;

            txtAddress.Text = cfg.Address;
            txtCity.Text = cfg.City;
            txtLocation.Text = cfg.Location;
            txtLocation2.Text = cfg.Location2;
            chkFormatAddress.Checked = cfg.FormatAddress;

            //cbMap.SelectedValue = cfg.Map;
            //cbMethod.SelectedValue = cfg.Method;
            //cbCoordtype.SelectedValue = cfg.Coordtype;
        }

        void SaveConfig()
        {
            var cfg = Setting = MapSetting.Current;

            cfg.Address = txtAddress.Text;
            cfg.City = txtCity.Text;
            cfg.Location = txtLocation.Text;
            cfg.Location2 = txtLocation2.Text;
            cfg.FormatAddress = chkFormatAddress.Checked;

            cfg.Map = (cbMap.SelectedItem as Type)?.Name;
            cfg.Method = (cbMethod.SelectedItem as MethodInfo)?.Name;
            cfg.Coordtype = cbCoordtype.SelectedItem as String;

            cfg.Save();
        }
        #endregion

        #region 收发数据
        private void btnInvoke_Click(Object sender, EventArgs e)
        {
            var type = cbMap.SelectedItem as Type;
            if (type == null) return;

            var method = cbMethod.SelectedItem as MethodInfo;
            if (method == null) return;

            SaveConfig();
            var cfg = Setting;

            var map = type.CreateInstance() as NewLife.Yun.Map;
            map.Log = XTrace.Log;
            map.CoordType = cfg.Coordtype;

            // 准备参数
            var addr = txtAddress.Text;
            var city = txtCity.Text;
            var point = new GeoPoint(txtLocation.Text);
            var point2 = new GeoPoint(txtLocation2.Text);

            var mps = method.GetParameters();

            Task.Factory.StartNew(async () =>
            {
                Object result = null;
                //var point = new GeoPoint(cfg.Location);
                //var point2 = new GeoPoint(cfg.Location2);
                try
                {
                    var im = map as IMap;
                    if (method.Name == nameof(im.GetGeocoderAsync) && mps.Length == 2)
                    {
                        result = await im.GetGeocoderAsync(addr,city);
                    }
                    else if (method.Name == nameof(im.GetGeocoderAsync) && mps.Length == 1)
                    {
                        result = await im.GetGeocoderAsync(point);
                    }
                    else if (method.Name == nameof(im.GetGeoAsync) && mps.Length == 3)
                    {
                        result = await im.GetGeoAsync(addr, city, cfg.FormatAddress);
                    }
                    else if (method.Name == nameof(im.GetGeoAsync) && mps.Length == 1)
                    {
                        result = await im.GetGeoAsync(point);
                    }
                    else if (method.Name == nameof(im.GetDistanceAsync))
                    {
                        result = await im.GetDistanceAsync(point, point2);
                    }
                    else if (map is BaiduMap bd && method.Name == nameof(bd.PlaceSearchAsync))
                    {
                        result = await bd.PlaceSearchAsync(addr, null, city, cfg.FormatAddress);
                    }
                    else if (map is AMap am && method.Name == nameof(am.GetAreaAsync))
                    {
                        result = (await am.GetAreaAsync(city))?.ToArray();
                    }
                    else
                    {
                        var ps = new Dictionary<String, Object>();
                        if (mps.Any(k => k.Name.EqualIgnoreCase("address"))) ps["address"] = addr;
                        if (mps.Any(k => k.Name.EqualIgnoreCase("city"))) ps["city"] = cfg.City;

                        var task = map.InvokeWithParams(method, ps) as Task;
                        await task;

                        result = task.GetValue("Result");
                    }
                }
                catch (Exception ex)
                {
                    ex = ex.GetTrue();
                    if (ex.GetType() == typeof(Exception))
                        XTrace.WriteLine(ex.Message);
                    else
                        XTrace.WriteException(ex);
                    return;
                }

                this.Invoke(() =>
                {
                    pgResult.SelectedObject = result;

                    if (result is GeoAddress geo)
                        txtLocation.Text = geo.Location + "";
                    else if (result is GeoPoint gp)
                        txtLocation.Text = gp + "";
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

            var cfg = Setting;

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
                if (cfg.Method == item.Name) cb.SelectedItem = item;
            }
            //if (cb.Items.Count > 0) cb.SelectedIndex = 0;

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

            //cb.SelectedIndex = 0;
            cb.SelectedItem = cfg.Coordtype;
        }
    }
}