using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using NewLife.Configuration;
using NewLife.Log;


public partial class Admin_System_WebLog : PageBase
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            String logPath = XTrace.LogPath;
            if (Directory.Exists(logPath))
            {
                String[] files = Directory.GetFiles(logPath, "*.log", SearchOption.TopDirectoryOnly);
                // 反序，让新的在线
                Array.Reverse(files);
                if (files != null && files.Length > 0)
                {
                    foreach (String item in files)
                    {
                        DropDownList1.Items.Add(Path.GetFileNameWithoutExtension(item));
                    }
                }
            }
        }
    }

    protected void Button1_Click(object sender, EventArgs e)
    {
        String name = DropDownList1.SelectedValue;
        Int32 n = 0;
        if (!Int32.TryParse(name.Replace("_", null), out n) || n <= 0) return;

        String fileName = Path.Combine(XTrace.LogPath, name + ".log");
        if (!File.Exists(fileName)) return;
        FileInfo fileinfo = new FileInfo(fileName);
        if (fileinfo.Length < 307200)
        {
            //txtLog.Text = File.ReadAllText(fileName);
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                StreamReader reader = new StreamReader(stream);
                txtLog.Text = reader.ReadToEnd();
            }
        }
        else
        {
            txtLog.Text = "文件内容过长，可下载后查看";
            DownLoadFile(fileName);
        }
    }

    //下载函数
    private void DownLoadFile(string fileName)
    {
        //string filePath = Server.MapPath(".") + "\\" + fileName;
        if (File.Exists(fileName))
        {
            FileInfo file = new FileInfo(fileName);
            Response.ContentEncoding = System.Text.Encoding.GetEncoding("UTF-8"); //解决中文乱码
            Response.AddHeader("Content-Disposition", "attachment; filename=" + Server.UrlEncode(file.Name)); //解决中文文件名乱码    
            Response.AddHeader("Content-length", file.Length.ToString());
            Response.ContentType = "appliction/octet-stream";
            Response.WriteFile(file.FullName);
            Response.End();
        }
    }
}