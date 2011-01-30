using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Web.UI.WebControls;
using XCode.DataAccessLayer;
using NewLife.Web;

public partial class Admin_System_WebDb : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        lbResult.Text = null;

        if (!IsPostBack)
        {
            if (DAL.ConnStrs != null && DAL.ConnStrs.Count > 0)
            {
                foreach (String item in DAL.ConnStrs.Keys)
                {
                    ddlConn.Items.Add(new ListItem(item, item));
                }
                ddlConn_SelectedIndexChanged(null, null);
            }
        }
    }

    DAL GetDAL()
    {
        if (String.IsNullOrEmpty(ddlConn.SelectedValue)) return null;

        try
        {
            return DAL.Create(ddlConn.SelectedValue);
        }
        catch { return null; }
    }

    protected void ddlConn_SelectedIndexChanged(object sender, EventArgs e)
    {
        DAL dal = GetDAL();
        if (dal == null) return;

        try
        {
            txtConnStr.Text = dal.ConnStr;

            // 数据表
            ddlTable.Items.Clear();
            IList<XTable> tables = dal.Tables;
            if (tables != null && tables.Count > 0)
            {
                foreach (XTable item in tables)
                {
                    String des = String.IsNullOrEmpty(item.Description) ? item.Name : String.Format("{1}({0})", item.Name, item.Description);
                    ddlTable.Items.Add(new ListItem(des, item.Name));
                }

                ddlTable.Items.Insert(0, "--请选择--");
            }

            // 数据架构
            ddlSchema.Items.Clear();
            DataTable dt = dal.DB.GetSchema(null, null);
            if (dt != null)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    ddlSchema.Items.Add(dr[0].ToString());
                }

                ddlSchema.Items.Insert(0, "--请选择--");
            }
        }
        catch (Exception ex)
        {
            WebHelper.Alert(ex.ToString());
        }
    }

    protected void ddlTable_SelectedIndexChanged(object sender, EventArgs e)
    {
        DAL dal = GetDAL();
        if (dal == null) return;

        XTable table = dal.Tables.Find(delegate(XTable item) { return item.Name == ddlTable.SelectedValue; });
        if (table == null) return;

        gvTable.DataSource = table.Fields;
        gvTable.DataBind();

        txtSql.Text = String.Format("Select * From {0}", dal.Db.FormatKeyWord(table.Name));
    }

    protected void ddlSchema_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (String.IsNullOrEmpty(ddlConn.SelectedValue)) return;

        DAL dal = GetDAL();
        if (dal == null) return;

        String name = ddlSchema.SelectedValue;
        if (String.IsNullOrEmpty(name)) return;

        // 计算约束条件
        String[] pms = null;
        DataTable rdt = null;
        try
        {
            // SQLite不支持该集合
            rdt = dal.DB.GetSchema(DbMetaDataCollectionNames.Restrictions, null);
        }
        catch { }

        if (rdt != null)
        {
            DataRow[] drs = rdt.Select(String.Format("{0}='{1}'", DbMetaDataColumnNames.CollectionName, name));
            if (drs != null && drs.Length > 0)
            {
                pms = new String[drs.Length];
                for (int i = 0; i < drs.Length; i++)
                {
                    TextBox txt = Panel1.FindControl("txt" + drs[i][1].ToString()) as TextBox;
                    if (txt != null && !String.IsNullOrEmpty(txt.Text)) pms[i] = txt.Text;
                }
            }
        }

        DataTable dt = dal.DB.GetSchema(name, pms);
        if (dt != null)
        {
            gvTable.DataSource = dt;
            gvTable.DataBind();
        }
    }

    void CreateRestrictions()
    {
        if (String.IsNullOrEmpty(ddlConn.SelectedValue)) return;

        DAL dal = GetDAL();
        if (dal == null) return;

        String name = ddlSchema.SelectedValue;
        if (String.IsNullOrEmpty(name)) return;

        // 计算约束条件
        DataTable rdt = null;
        try
        {
            rdt = dal.DB.GetSchema(DbMetaDataCollectionNames.Restrictions, null);
        }
        catch { }

        if (rdt != null)
        {
            DataRow[] drs = rdt.Select(String.Format("{0}='{1}'", DbMetaDataColumnNames.CollectionName, name));
            if (drs != null && drs.Length > 0)
            {
                foreach (DataRow dr in drs)
                {
                    String rname = dr[1].ToString();

                    Literal lt = new Literal();
                    lt.Text = "&nbsp;";
                    Panel1.Controls.Add(lt);

                    Label lb = new Label();
                    lb.Text = dr[1].ToString() + "：";
                    Panel1.Controls.Add(lb);

                    TextBox txt = new TextBox();
                    txt.ID = "txt" + rname;
                    txt.AutoPostBack = true;
                    txt.TextChanged += new EventHandler(ddlSchema_SelectedIndexChanged);
                    Panel1.Controls.Add(txt);
                }
            }
        }
    }

    protected override void OnPreLoad(EventArgs e)
    {
        base.OnPreLoad(e);

        //ddlSchema_SelectedIndexChanged(this, e);
        CreateRestrictions();
    }

    protected void Button1_Click(object sender, EventArgs e)
    {
        String sql = txtSql.Text;
        if (String.IsNullOrEmpty(sql)) return;

        sql = sql.Trim();
        if (String.IsNullOrEmpty(sql)) return;

        DAL dal = GetDAL();
        if (dal == null) return;

        lbResult.Text = null;
        gvResult.DataSource = null;
        gvResult.DataBind();

        try
        {
            if (sql.StartsWith("select", StringComparison.OrdinalIgnoreCase))
            {
                Search(0);
            }
            else
            {
                // 执行
                Int32 count = dal.Execute(sql, "");
                lbResult.Text = String.Format("影响行数：{0}", count);
            }

            //WebHelper.Alert("完成！");
        }
        catch (Exception ex)
        {
            lbResult.Text = ex.Message;
        }
    }

    void Search(Int32 index)
    {
        String sql = txtSql.Text;
        if (String.IsNullOrEmpty(sql)) return;

        sql = sql.Trim();
        if (String.IsNullOrEmpty(sql)) return;

        DAL dal = GetDAL();
        if (dal == null) return;

        // 总行数
        Int32 count = dal.SelectCount(sql, "");
        DataPager1.TotalRowCount = count;
        lbResult.Text = String.Format("总记录数：{0}", count);

        // 只显示前1000行
        String key = txtKey.Text;
        if (String.IsNullOrEmpty(key)) key = "ID";

        sql = dal.PageSplit(sql, index * gvResult.PageSize, gvResult.PageSize, key);

        DataSet ds = dal.Select(sql, "");
        gvResult.DataSource = ds;
        gvResult.DataBind();
    }

    protected void DataPager2_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        Search(e.NewPageIndex);
    }
}