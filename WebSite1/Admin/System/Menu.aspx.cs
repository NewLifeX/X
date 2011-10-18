using System;
using System.Text;
using NewLife.Log;
using NewLife.Web;

using XCode;
using Menu = NewLife.CommonEntity.Menu;
using NewLife.CommonEntity;

public partial class Pages_Menu : PageBase
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // 添加按钮需要添加权限
            lbAdd.Visible = Acquire(PermissionFlags.Insert);
            // 最后一列是删除列，需要删除权限
            GridView1.Columns[GridView1.Columns.Count - 1].Visible = Acquire(PermissionFlags.Delete);
        }
    }

    protected void Button1_Click(object sender, EventArgs e)
    {
        try
        {
            Int32 n = Menu.ScanAndAdd("Admin");
            n += Menu.ScanAndAdd("MacDoc");
            n += Menu.ScanAndAdd("SCM");

            WebHelper.Alert("扫描完成，共添加菜单" + n + "个！");
        }
        catch (Exception ex)
        {
            WebHelper.Alert("出错！" + ex.Message);
        }
    }

    protected void Button2_Click(object sender, EventArgs e)
    {
        //Menu m = Menu.Find(Menu._.ParentID, 0);
        //if (m == null) return;
        EntityList<Menu> list = Menu.Root.Childs;

        String xml = list.ToXml();

        Response.Clear();
        Response.Buffer = true;
        Response.Charset = "utf8";
        Response.ContentEncoding = Encoding.UTF8;
        Response.AppendHeader("Content-Disposition", "attachment;filename=" + Server.UrlEncode("菜单.xml") + "");
        Response.ContentType = "xml/text";
        Response.Output.Write(xml);
        Response.Flush();
        Response.End();
    }

    protected void Button3_Click(object sender, EventArgs e)
    {
        if (!FileUpload1.HasFile) return;

        String xml = Encoding.UTF8.GetString(FileUpload1.FileBytes);

        try
        {
            //Menu m = Menu.FromXML(xml);
            //m.Import();
            EntityList<Menu> list = new EntityList<Menu>();
            list.FromXml(xml);

            if (list.Count > 0)
            {
                foreach (Menu item in list)
                {
                    item.Import();
                }
            }

            GridView1.DataBind();
        }
        catch (Exception ex)
        {
            WebHelper.Alert(ex.Message);

            XTrace.WriteLine(ex.ToString());
        }

    }

    protected void GridView1_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Up")
        {
            Menu entity = Menu.FindByID(Convert.ToInt32(e.CommandArgument));
            if (entity != null)
            {
                entity.Up();
                GridView1.DataBind();
            }
        }
        else if (e.CommandName == "Down")
        {
            Menu entity = Menu.FindByID(Convert.ToInt32(e.CommandArgument));
            if (entity != null)
            {
                entity.Down();
                GridView1.DataBind();
            }
        }
    }
}