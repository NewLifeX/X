using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Web.UI.WebControls;
using XCode.DataAccessLayer;
using NewLife.Web;
using NewLife.Reflection;
using System.Reflection;
using System.ComponentModel;
using System.Web.UI;
using System.Collections;
using XCode;

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
            DataTable dt = dal.Session.GetSchema(null, null);
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
            rdt = dal.Session.GetSchema(DbMetaDataCollectionNames.Restrictions, null);
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

        DataTable dt = dal.Session.GetSchema(name, pms);
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
            rdt = dal.Session.GetSchema(DbMetaDataCollectionNames.Restrictions, null);
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
            //lbResult.Text = ex.Message;
            WebHelper.Alert(ex.ToString());
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

        //String tableName = ddlTable.SelectedValue;
        //String fsql = String.Format("Select * From {0}", dal.Db.FormatKeyWord(tableName));
        //if (sql.ToLower().StartsWith(fsql.ToLower()))
        //{
        //    IEntityOperate factory = dal.CreateOperate(tableName);
        //    if (factory != null)
        //    {
        //        gvResult.DataSource = factory.FindAll(sql.Substring(fsql.Length), null, null, index * gvResult.PageSize, gvResult.PageSize);
        //        gvResult.DataBind();
        //        return;
        //    }
        //}

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

    protected void gvTable_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.Header)
        {
            GridView gv = sender as GridView;
            Type type = null;

            if (gv.DataSource is DataSet || gv.DataSource is DataTable)
            {
                String sql = txtSql.Text;
                if (String.IsNullOrEmpty(sql)) return;

                sql = sql.Trim();
                if (String.IsNullOrEmpty(sql)) return;

                DAL dal = GetDAL();
                if (dal == null) return;

                String tableName = ddlTable.SelectedValue;
                String fsql = String.Format("Select * From {0}", dal.Db.FormatKeyWord(tableName));
                if (!sql.ToLower().StartsWith(fsql.ToLower())) return;

                XTable table = dal.Tables.Find(delegate(XTable item) { return item.Name == ddlTable.SelectedValue; });
                if (table == null) return;

                // 更新表头
                foreach (TableCell item in e.Row.Cells)
                {
                    String name = item.Text;
                    if (String.IsNullOrEmpty(name)) continue;

                    XField field = table.Fields.Find(delegate(XField elm) { return elm.Name == name; });
                    if (field == null) continue;

                    if (!String.IsNullOrEmpty(field.Description)) item.Text = field.Description;
                }
            }
            else
            {
                IEnumerable ie = gv.DataSource as IEnumerable;
                if (ie == null) return;
                IEnumerator iet = ie.GetEnumerator();
                if (!iet.MoveNext()) return;

                type = iet.Current.GetType();

                // 更新表头
                foreach (TableCell item in e.Row.Cells)
                {
                    String name = item.Text;
                    if (String.IsNullOrEmpty(name)) continue;
                    PropertyInfo pi = type.GetProperty(name);
                    if (pi == null) continue;

                    DescriptionAttribute att = AttributeX.GetCustomAttribute<DescriptionAttribute>(pi, false);
                    if (att == null) continue;

                    if (!String.IsNullOrEmpty(att.Description)) item.Text = att.Description;
                }
            }
        }
    }
}