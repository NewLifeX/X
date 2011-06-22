using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity.Web;
using System.Threading;

public partial class CommonEntityInitTest : WebPageBase
{
    static bool isInited = false;
    protected override void OnPreLoad(EventArgs e)
    {
        if (isInited)
        {
            Thread.Sleep(5000);//模拟更长时间的系统初始化
        }
        else
        {
            isInited = true;
        }
        base.OnPreLoad(e);
    }
    protected void Page_Load(object sender, EventArgs e)
    {

    }
}