﻿using System.Reflection;
using NewLife;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Web;

namespace Zero.Desktop;

public partial class FrmMain : Form
{
    public FrmMain()
    {
        InitializeComponent();
    }

    private void FrmMain_Load(Object sender, EventArgs e)
    {
        var asm = AssemblyX.Create(Assembly.GetExecutingAssembly());
        Text = String.Format("{2} v{0} {1:HH:mm:ss}", asm.FileVersion, asm.Compile, Text);

        richTextBox1.UseWinFormControl();

        _timer = new TimerX(OnBindConn, null, 1_000, 3_000);
    }

    private TimerX _timer;
    private ApiClient _client;
    private String _lastConns;
    private void OnBindConn(Object state)
    {
        //var keys = DAL.ConnStrs.Keys;
        //var ks = keys.Join(",");
        //if (ks == _lastConns) return;
        //_lastConns = ks;

        //cbConns.DataSource = keys;
    }

    private void btnOpen_Click(Object sender, EventArgs e)
    {
        var server = txtServer.Text;
        if (server.IsNullOrEmpty()) return;

        var btn = sender as Button;
        var btn2 = btnOpenAsync;
        if (btn.Text == "打开")
        {
            var client = new ApiClient(server)
            {
                Log = XTrace.Log,
                EncoderLog = XTrace.Log,
                SocketLog = XTrace.Log
            };
            client.Open();

            var rs = client.Invoke<String[]>("api/all", null);
            cbApi.DataSource = rs;

            txtServer.Enabled = false;
            groupBox2.Enabled = true;
            btn.Text = "关闭";
            btn2.Text = "异步关闭";

            _client = client;
        }
        else
        {
            _client.Close(btn.Text);

            txtServer.Enabled = true;
            groupBox2.Enabled = false;
            btn.Text = "打开";
            btn2.Text = "异步打开";
        }
    }

    private async void btnAsyncOpen_Click(object sender, EventArgs e)
    {
        var server = txtServer.Text;
        if (server.IsNullOrEmpty()) return;

        var btn = btnOpen;
        var btn2 = sender as Button;
        if (btn2.Text == "异步打开")
        {
            var client = new ApiClient(server)
            {
                Log = XTrace.Log,
                EncoderLog = XTrace.Log,
                SocketLog = XTrace.Log
            };
            client.Open();

            var rs = await client.InvokeAsync<String[]>("api/all", null);
            cbApi.DataSource = rs;

            txtServer.Enabled = false;
            groupBox2.Enabled = true;
            btn.Text = "关闭";
            btn2.Text = "异步关闭";

            _client = client;
        }
        else
        {
            _client.Close(btn.Text);

            txtServer.Enabled = true;
            groupBox2.Enabled = false;
            btn.Text = "打开";
            btn2.Text = "异步打开";
        }
    }

    private void listBox1_SelectedIndexChanged(Object sender, EventArgs e)
    {
        //var table = listBox1.SelectedItem as IDataTable;
        //if (table == null) return;

        //var sql = $"select * from {table.TableName}";
        //var ds = _dal.Select(new SelectBuilder(sql), 0, 1000);

        //dataGridView1.DataSource = ds.Tables[0];
        //dataGridView1.Refresh();
    }

    private void btnCall_Click(object sender, EventArgs e)
    {
        var act = cbApi.Text.Substring(" ", "(");
        var args = txtArgument.Text.Trim().DecodeJson();
        var rs = _client.Invoke<String>(act, args);
    }

    private async void btnCallAsync_Click(object sender, EventArgs e)
    {
        var act = cbApi.Text.Substring(" ", "(");
        var args = txtArgument.Text.Trim().DecodeJson();
        var rs = await _client.InvokeAsync<String>(act, args);
    }

    private void btnDownloadPlugin_Click(object sender, EventArgs e)
    {
        var file = Setting.Current.PluginPath.CombinePath("System.Data.SQLite.dll");
        if (File.Exists(file)) File.Delete(file);

        // 下载时，内部存在同步调用异步，这里不应该卡死UI线程
        var linkName = "System.Data.SQLite.win-x64,System.Data.SQLite.win,System.Data.SQLite_net80,System.Data.SQLite_net70,System.Data.SQLite_net60,System.Data.SQLite_net50,System.Data.SQLite_netstandard21,System.Data.SQLite_netstandard20,System.Data.SQLite";
        //var type = PluginHelper.LoadPlugin("", "", "System.Data.SQLite", "");
        var type = PluginHelper.LoadPlugin("System.Data.SQLite.SQLiteFactory", null, "System.Data.SQLite.dll", linkName);
        XTrace.WriteLine("Type={0}", type);
    }
}