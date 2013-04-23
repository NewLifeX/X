using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.Web;
using System.Text;

public partial class Control_LeftMenu : System.Web.UI.UserControl
{
    #region 属性
    private List<IMenu> _Menu;
    /// <summary>菜单</summary>
    public List<IMenu> Menu
    {
        get
        {
            if (_Menu == null)
            {
                Int32 id = WebHelper.RequestInt("ID");
                ICommonManageProvider cmp = CommonManageProvider.Provider;

                IMenu m = cmp.FindByMenuID(id);
                if (m == null)
                {
                    m = cmp.MenuRoot.Childs[0];

                }
            }
            return _Menu;
        }
        set { _Menu = value; }
    }

    private String _MenuDefault = "首页";
    //默认打开菜单
    public String MenuDefault
    {
        get { return _MenuDefault; }
        set { _MenuDefault = value; }
    }

    public class LeftMenu
    {
        private String _Name;
        /// <summary>名称</summary>
        public String Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        private String _Icon;
        /// <summary>图标</summary>
        public String Icon
        {
            get { return _Icon; }
            set { _Icon = value; }
        }

        private String _Url;
        /// <summary>地址</summary>
        public String Url
        {
            get { return _Url; }
            set { _Url = value; }
        }

        /// <summary>是否有子菜单</summary>
        public Boolean IsSubMenu { get { return SubMenus != null && SubMenus.Count > 0; } }

        private List<LeftMenu> _SubMenus;
        /// <summary>子菜单</summary>
        public List<LeftMenu> SubMenus
        {
            get
            {
                if (_SubMenus == null)
                    _SubMenus = new List<LeftMenu>();
                return _SubMenus;
            }
            set { _SubMenus = value; }
        }

        private static List<LeftMenu> _GetMenu;
        /// <summary>获取当前菜单</summary>
        public static List<LeftMenu> GetMenu
        {
            #region 1

            //get
            //{
            //    List<LeftMenu> leftmenu = new List<LeftMenu>();

            //    //设置默认菜单
            //    leftmenu.Add(ConvertToMenu(null, "首页", "Main.aspx", "icon-home"));

            //    //获取请求ID查询当前页面请求的根菜单
            //    Int32 MenuID = WebHelper.RequestInt("ID");
            //    //获取当前管理员用户
            //    ICommonManageProvider cmp = CommonManageProvider.Provider;

            //    List<IMenu> menus = new List<IMenu>();
            //    IMenu m = cmp.FindByMenuID(MenuID);

            //    //如无条件限制就以根菜单为主
            //    if (m == null)
            //        menus = cmp.MenuRoot.Childs as List<IMenu>;
            //    else
            //        menus = m.Childs as List<IMenu>;

            //    if (menus.Count > 0)
            //    {
            //        foreach (IMenu item in menus)
            //        {
            //            LeftMenu lm = ConvertToMenu(item, null, null, "icon-th-list");

            //            if (item.Childs.Count > 0)
            //            {
            //                foreach (IMenu child in item.Childs)
            //                {
            //                    //添加子菜单
            //                    lm.SubMenus.Add(ConvertToMenu(child, null, null, ""));
            //                }
            //            }
            //            leftmenu.Add(lm);
            //        }
            //    }
            //    return leftmenu;
            //}
            #endregion

            get
            {
                List<LeftMenu> leftmenu = new List<LeftMenu>();

                //设置默认菜单
                leftmenu.Add(ConvertToMenu(null, "首页", "../Main.aspx", "icon-home"));

                Int32 MenuID = WebHelper.RequestInt("ID");

                ICommonManageProvider cmp = CommonManageProvider.Provider;

                List<IMenu> menu = cmp.GetMySubMenus(MenuID) as List<IMenu>;

                foreach (IMenu item in menu)
                {
                    if (item.Childs.Count > 0)
                    {
                        LeftMenu lm = ConvertToMenu(item, null, null, "icon-th-list");

                        foreach (IMenu child in item.Childs)
                        {
                            if (!child.IsShow) continue;

                            lm.SubMenus.Add(ConvertToMenu(child, null, null, ""));
                        }

                        leftmenu.Add(lm);
                    }
                }

                return leftmenu;
            }
        }

        /// <summary>
        /// IMenu转换为LeftMenu的方法
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="Name"></param>
        /// <param name="Url"></param>
        /// <param name="Icon"></param>
        /// <returns></returns>
        public static LeftMenu ConvertToMenu(IMenu menu, String Name, String Url, String Icon)
        {
            LeftMenu Lm = new LeftMenu();
            if (menu != null)
            {
                Lm.Name = menu.Name;
                Lm.Url = menu.Url;
            }
            else
            {
                Lm.Name = Name;
                Lm.Url = Url;
            }
            Lm.Icon = Icon;
            return Lm;
        }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    String li = "<li class=\"{0}\">";
    String a = "<a address=\"{0}\">{1}</a>";
    String i = "<i class=\"icon {0}\"></i>";
    String span = "<span>{0}</span>";
    String numsub = "<span class=\"label\">{0}</span>";

    /// <summary>
    /// 菜单加载
    /// </summary>
    /// <returns></returns>
    public String MenuLoading()
    {
        String astr = "";
        String istr = "";
        String spanstr = "";
        String numstr = "";
        String listr = "";
        String content = "";
        Boolean IsOpen = false;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<ul>");
        foreach (LeftMenu item in LeftMenu.GetMenu)
        {
            istr = String.Format(i, item.Icon);
            spanstr = String.Format(span, item.Name);

            if (item.IsSubMenu)
            {
                numstr = String.Format(numsub, item.SubMenus.Count);
                content = FormatSub(item.SubMenus, out IsOpen);
                listr = String.Format(li, IsOpen ? "submenu open active" : "submenu");
                astr = String.Format(a, "#", istr + spanstr + numstr);
                //拼接li跟a标签
                content = listr + astr + content;
            }
            else
            {
                listr = item.Name == MenuDefault ? String.Format(li, "active") : "<li>";

                astr = String.Format(a, item.Url, istr + spanstr);
                //拼接li跟a标签
                content = listr + astr;
            }

            sb.AppendLine(content + "</li>");
        }
        sb.AppendLine("</ul>");

        return sb.ToString();
    }

    /// <summary>
    /// 格式化子菜单
    /// </summary>
    /// <param name="submenulist"></param>
    /// <param name="IsOpen"></param>
    /// <returns></returns>
    private string FormatSub(List<LeftMenu> submenulist, out Boolean IsOpen)
    {
        IsOpen = false;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("<ul>");

        foreach (LeftMenu item in submenulist)
        {
            if (item.Name == MenuDefault)
            {
                sb.AppendLine(String.Format(li, "active") + String.Format(a, item.Url, item.Name) + "</li>");
                IsOpen = true;
            }
            else
                sb.AppendLine("<li>" + String.Format(a, item.Url, item.Name) + "</li>");
        }

        sb.AppendLine("</ul>");

        return sb.ToString();
    }
}