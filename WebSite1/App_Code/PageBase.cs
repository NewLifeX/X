using System;
using System.Web.UI.WebControls;
using NewLife.CommonEntity.Web;
using NewLife.Web;
using NewLife.YWS.Entities;
using Menu = NewLife.CommonEntity.Menu;
using System.Text;
using NewLife.Reflection;
using NewLife.Log;
using System.Web;
using System.Web.UI;

/// <summary>
/// 页面基类
/// </summary>
/// <typeparam name="TAdminEntity"></typeparam>
/// <typeparam name="TMenuEntity"></typeparam>
public abstract class PageBase : WebPageBase<Admin, Menu>
{
    /// <summary>
    /// 是否管理员
    /// </summary>
    public Boolean IsAdmin { get { return Current != null && Current.RoleName == "管理员"; } }

    /// <summary>
    /// 校验权限
    /// </summary>
    /// <returns></returns>
    public override Boolean CheckPermission()
    {
        if (base.CheckPermission()) return true;

        //Response.Redirect("../../Admin/Login.aspx");
        WebHelper.AlertAndEnd("无权访问！");
        return false;
    }

    public override bool CheckLogin()
    {
        if (Admin.Current == null)
        {
            // 不可能出现的情况，记录Cookie
            StringBuilder sb = new StringBuilder();
            foreach (String item in Request.Cookies)
            {
                if (sb.Length > 0) sb.Append(",");

                HttpCookie cookie = Request.Cookies[item];
                sb.AppendFormat("{0}={1}", item, cookie.Value);
            }
            XTrace.WriteLine("自动登录失败，这是不可能的错误！" + sb.ToString());

            Response.Redirect("../../Admin/Login.aspx");
        }

        return base.CheckLogin();
    }

    #region 导出
    //protected override void OnInit(EventArgs e)
    //{
    //    base.OnInit(e);

    //    //Button btn = ControlHelper.FindControl<Button>(Page, "btnExport");
    //    Button btn = FindControl("btnExport") as Button;
    //    if (btn != null)
    //    {
    //        btn.Visible = IsAdmin;
    //        btn.Click += new EventHandler(btn_Click);
    //    }
    //}
    protected override void OnPreLoad(EventArgs e)
    {
        base.OnPreLoad(e);

        //Button btn = ControlHelper.FindControl<Button>(Page, "btnExport");
        //Button btn = FindControl("btnExport") as Button;
        Button btn = FindControl("btnExport") as Button;
        if (btn != null)
        {
            btn.Visible = IsAdmin;
            btn.Click += new EventHandler(btn_Click);
        }
    }

    protected Boolean isExport = false;
    void btn_Click(object sender, EventArgs e)
    {
        isExport = true;

        OnExportExcel();
    }

    protected virtual void OnExportExcel()
    {

    }

    public override void VerifyRenderingInServerForm(System.Web.UI.Control control)
    {
        if (!isExport) base.VerifyRenderingInServerForm(control);
    }

    void ExportExcel()
    {
        GridView gv = ControlHelper.FindControl<GridView>(Page, null);
        if (gv != null)
        {
            Int32 n = gv.Columns.Count - 1;
            //if (gv.Columns[n].HeaderText == "删除") gv.Columns.RemoveAt(n);
            if (gv.Columns[n].HeaderText == "删除") gv.Columns[n].Visible = false;
            n = gv.Columns.Count - 1;
            //if (gv.Columns[n].HeaderText == "编辑") gv.Columns.RemoveAt(n);
            if (gv.Columns[n].HeaderText == "编辑") gv.Columns[n].Visible = false;
            for (int i = gv.Columns.Count - 1; i >= 0; i--)
            {
                //if (gv.Columns[i] is XControl.LinkBoxField) gv.Columns.RemoveAt(i);
                if (gv.Columns[i] is XControl.LinkBoxField || gv.Columns[i] is HyperLinkField)
                    gv.Columns[i].Visible = false;
            }
            String name = MyMenu.Remark;
            if (String.IsNullOrEmpty(name)) name = MyMenu.Name;
            WebHelper.ExportExcel(gv, name + ".xls", 10000, Encoding.UTF8);
            return;
        }
    }
    #endregion

    protected override void OnPreRender(EventArgs e)
    {
        if (isExport) ExportExcel();

        base.OnPreRender(e);

        WriteReloadForm();

        WriteEntterKeyPress();

        WriteToolTip();
    }

    protected virtual void WriteReloadForm()
    {
        // 在页面回发后，如果reload页面，会提示重新发送啥啥啥的。找到搜索按钮，改变页面重刷为点击按钮
        if (IsPostBack)
        {
            Button btn = ControlHelper.FindControl<Button>(Page, "btnSearch");
            //if (btn == null) btn = ControlHelper.FindControl<Button>(Page, null);
            if (btn != null)
            {
                String js = "function reloadForm(){";
                js += "document.getElementById('" + btn.ClientID + "').click();}";
                ClientScript.RegisterStartupScript(GetType(), "reloadForm", js, true);
            }
            else
            {
                String js = "function reloadForm(){/*可以通过把查询按钮改名为btnSearch来避免重发数据的提示！*/location.reload();}";
                ClientScript.RegisterStartupScript(GetType(), "reloadForm", js, true);
            }
        }
        else
        {
            String js = "function reloadForm(){location.reload();}";
            ClientScript.RegisterStartupScript(GetType(), "reloadForm", js, true);
        }
    }

    /// <summary>
    /// 在关键字输入框按下回车时，调用查询
    /// </summary>
    protected virtual void WriteEntterKeyPress()
    {
        FieldInfoX fix = FieldInfoX.Create(this.GetType(), "txtKey");
        if (fix == null) return;
        TextBox box = fix.GetValue(this) as TextBox;
        if (box == null) return;

        fix = FieldInfoX.Create(this.GetType(), "btnSearch");
        if (fix == null) return;
        Button btn = fix.GetValue(this) as Button;
        if (btn == null) return;

        String js = "if((event.which || event.keyCode)==13){document.getElementById('" + btn.ClientID + "').click(); return false;} return true;";
        box.Attributes["onkeypress"] = js;
    }

    private String _ToolTip;
    /// <summary>提示</summary>
    public String ToolTip
    {
        get { return _ToolTip; }
        set { _ToolTip = value; }
    }

    void WriteToolTip()
    {
        if (String.IsNullOrEmpty(ToolTip)) return;

        Literal lt = Master.FindControl("Navigation") as Literal;
        if (lt != null) lt.Text += "&nbsp;&nbsp;" + ToolTip;
    }

    public override Control FindControl(string id)
    {
        // 首先采用快速反射找字段，这样子可以避免破坏控件树结构
        FieldInfoX fix = FieldInfoX.Create(this.GetType(), id);
        if (fix != null) return fix.GetValue(this) as Control;

        return base.FindControl(id);
    }
}