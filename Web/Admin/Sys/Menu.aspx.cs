using System;
using System.Text;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;

public partial class Pages_Menu : MyEntityList
{
    ICommonManageProvider Provider { get { return CommonManageProvider.Provider; } }

    /// <summary>实体类型</summary>
    public override Type EntityType { get { return Provider.MenuType; } set { base.EntityType = value; } }

    protected void Page_Load(object sender, EventArgs e) { }

    protected void Button1_Click(object sender, EventArgs e)
    {
        try
        {
            Int32 n = (Int32)MethodInfoX.Create(EntityType, "ScanAndAdd").Invoke(null);

            WebHelper.Alert("扫描完成，共添加菜单" + n + "个！");
        }
        catch (Exception ex)
        {
            WebHelper.Alert("出错！" + ex.Message);
        }
    }

    protected void Button2_Click(object sender, EventArgs e)
    {
        String xml = MethodInfoX.Create(EntityType, "Export").Invoke(null, Provider.MenuRoot.Childs) as String;

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
            MethodInfoX.Create(EntityType, "Import").Invoke(null, xml);

            gv.DataBind();
        }
        catch (Exception ex)
        {
            WebHelper.Alert(ex.Message);

            XTrace.WriteLine(ex.ToString());
        }

    }

    protected void gv_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Up")
        {
            IMenu entity = Provider.FindByMenuID(Convert.ToInt32(e.CommandArgument));
            if (entity != null)
            {
                entity.Up();
                gv.DataBind();
            }
        }
        else if (e.CommandName == "Down")
        {
            IMenu entity = Provider.FindByMenuID(Convert.ToInt32(e.CommandArgument));
            if (entity != null)
            {
                entity.Down();
                gv.DataBind();
            }
        }
    }

    public Boolean IsFirst(Object dataItem)
    {
        IMenu menu = dataItem as IMenu;
        if (menu == null) return true;
        IMenu parent = menu.Parent ?? Provider.MenuRoot;
        return menu.ID == parent.Childs[0].ID;
    }

    public Boolean IsLast(Object dataItem)
    {
        IMenu menu = dataItem as IMenu;
        if (menu == null) return true;
        IMenu parent = menu.Parent ?? Provider.MenuRoot;
        return menu.ID == parent.Childs[parent.Childs.Count - 1].ID;
    }
}