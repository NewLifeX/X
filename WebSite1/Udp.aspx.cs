using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net.Sockets;
using System.Text;
using System.Net;

public partial class Udp : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }
    protected void Button1_Click(object sender, EventArgs e)
    {
        UdpClient client = new UdpClient();
        String str = "I am 大石头!";
        Byte[] data = Encoding.UTF8.GetBytes(str);
        client.Send(data, data.Length, "", 7);
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        data = client.Receive(ref ep);
        str = Encoding.UTF8.GetString(data);
        Response.Write("来自" + ep.ToString() + "的数据：" + str);
    }
}