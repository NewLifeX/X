//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Reflection;
//using System.ComponentModel;
//using System.Web.UI.WebControls;
//using System.Web.UI;

//namespace XControl
//{
//    /// <summary>
//    /// 类型助手类
//    /// </summary>
//    internal class ViewHelper
//    {
//        /// <summary>
//        /// 取得所有属性列
//        /// </summary>
//        /// <param name="t"></param>
//        /// <returns></returns>
//        public static List<FieldItem> AllFields(Type t)
//        {
//            List<FieldItem> Fields = new List<FieldItem>();
//            PropertyInfo[] pis = t.GetProperties();
//            foreach (PropertyInfo pi in pis)
//            {
//                DescriptionAttribute[] Des = pi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
//                DataObjectFieldAttribute[] Dof = pi.GetCustomAttributes(typeof(DataObjectFieldAttribute), false) as DataObjectFieldAttribute[];
//                // 必须包含DataObjectFieldAttribute，DescriptionAttribute可以为空
//                if (Dof != null && Dof.Length > 0)
//                    if (Des != null && Des.Length > 0)
//                        Fields.Add(new FieldItem(pi, Dof[0], Des[0]));
//                    else
//                        Fields.Add(new FieldItem(pi, Dof[0]));
//            }
//            return Fields;
//        }

//        /// <summary>
//        /// 取得实体类型
//        /// </summary>
//        /// <returns></returns>
//        public static Type GetEntryType<T>(ISite Site) where T : DataBoundControl
//        {
//            if (Site == null || Site.Component == null || !(Site.Component is T)) return null;
//            T dbc = Site.Component as T;
//            if (dbc == null || dbc.Page == null) return null;
//            String datasourceid = dbc.DataSourceID;
//            if (String.IsNullOrEmpty(datasourceid)) return null;
//            // 找到数据绑定控件ObjectDataSource
//            //ObjectDataSource obj = dbc.Page.FindControl(datasourceid) as ObjectDataSource;
//            ObjectDataSource obj = Find(dbc.Page, datasourceid) as ObjectDataSource;
//            if (obj == null)
//            {
//                MsgBox<T>("无法找到名为 " + datasourceid + " 的ObjectDataSource！");
//                return null;
//            }
//            // 找到实体类型
//            String typeName = obj.DataObjectTypeName;
//            if (String.IsNullOrEmpty(typeName)) typeName = obj.TypeName;
//            if (String.IsNullOrEmpty(typeName))
//            {
//                MsgBox<T>("请先配置好" + datasourceid + "再绑定数据源！");
//                return null;
//            }
//            Type t = Type.GetType(typeName);
//            if (t == null)
//            {
//                t = System.Web.Compilation.BuildManager.GetType(typeName, false, true);
//                if (t == null)
//                {
//                    Assembly[] abs = AppDomain.CurrentDomain.GetAssemblies();
//                    foreach (Assembly ab in abs)
//                    {
//                        t = ab.GetType(typeName, false, true);
//                        if (t != null) break;
//                    }
//                    if (t == null)
//                    {
//                        MsgBox<T>("无法定位数据组件类：" + typeName + "，可能你需要编译一次数据组件类所在项目。");
//                        return null;
//                    }
//                }
//            }
//            return t;
//        }

//        public static void MsgBox<T>(String msg)
//        {
//            System.Windows.Forms.MessageBox.Show(msg, typeof(T).ToString() + "控件设计时出错！");
//        }

//        public static Control Find(Control control, String id)
//        {
//            if (control == null || String.IsNullOrEmpty(id)) return null;
//            if (control.ID == id) return control;
//            if (control.Controls == null || control.Controls.Count < 1) return null;
//            foreach (Control w in control.Controls)
//                if (w.ID == id) return w;
//            foreach (Control w in control.Controls)
//            {
//                Control webc = Find(w, id);
//                if (webc != null) return webc;
//            }
//            return null;
//        }

//    }

//    /// <summary>
//    /// 数据属性元数据以及特性
//    /// </summary>
//    internal class FieldItem
//    {
//        /// <summary>
//        /// 属性元数据
//        /// </summary>
//        public PropertyInfo Info;
//        /// <summary>
//        /// 属性说明
//        /// </summary>
//        public String Description;
//        /// <summary>
//        /// 数据字段特性
//        /// </summary>
//        public DataObjectFieldAttribute DataObjectField;
//        /// <summary>
//        /// 属性名
//        /// </summary>
//        public String Name
//        {
//            get
//            {
//                return Info.Name;
//            }
//        }

//        /// <summary>
//        /// 构造函数
//        /// </summary>
//        /// <param name="pi"></param>
//        /// <param name="dof"></param>
//        public FieldItem(PropertyInfo pi, DataObjectFieldAttribute dof)
//        {
//            Info = pi;
//            DataObjectField = dof;
//        }
//        /// <summary>
//        /// 构造函数
//        /// </summary>
//        /// <param name="pi"></param>
//        /// <param name="dof"></param>
//        /// <param name="bc"></param>
//        public FieldItem(PropertyInfo pi, DataObjectFieldAttribute dof, DescriptionAttribute bc)
//        {
//            Info = pi;
//            DataObjectField = dof;
//            Description = bc.Description;
//        }
//    }
//}