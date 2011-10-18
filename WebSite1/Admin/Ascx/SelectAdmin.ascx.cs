using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.YWS.Entities;
using NewLife.Web;

public partial class Admin_Ascx_SelectAdmin : System.Web.UI.UserControl
{
    #region 属性
    /// <summary>产品编号</summary>
    public Int32 Value
    {
        get
        {
            Int32 _Value = 0;
            if (String.IsNullOrEmpty(btn.Value) ||
                !Int32.TryParse(btn.Value, out _Value) ||
                _Value < 1) _Value = 0;
            if (_Value < 1)
            {
                _Value = WebHelper.RequestInt("AdminID");
                if (_Value < 1) _Value = WebHelper.RequestInt("UserID");
                btn.Value = _Value.ToString();
            }

            return _Value;
        }
        set
        {
            Int32 _Value = value;
            if (_Value < 1)
            {
                _Value = WebHelper.RequestInt("AdminID");
                if (_Value < 1) _Value = WebHelper.RequestInt("UserID");
            }
            btn.Value = _Value.ToString();
        }
    }

    private Admin _Admin;
    /// <summary>产品</summary>
    public Admin Admin
    {
        get
        {
            if (_Admin == null && Value > 0)
            {
                _Admin = Admin.Meta.Cache.Entities.Find(Admin._.ID, Value);
            }
            return _Admin;
        }
    }

    /// <summary>自动回发</summary>
    public Boolean AutoPostBack
    {
        get { return btn.AutoPostBack; }
        set { btn.AutoPostBack = value; }
    }

    public event EventHandler ValueChanged
    {
        add { btn.ValueChanged += value; }
        remove { btn.ValueChanged -= value; }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        ////if (IsPostBack) Value = Convert.ToInt32(frmAdminID.Value);
        //if (IsPostBack && !String.IsNullOrEmpty(frmAdminID.Value)) Value = Convert.ToInt32(frmAdminID.Value);

        btn.DataBind();
    }

    //protected override void OnPreRender(EventArgs e)
    //{
    //    base.OnPreRender(e);

    //    if (String.IsNullOrEmpty(frmAdminID.Value)) frmAdminID.DataBind();
    //}
}