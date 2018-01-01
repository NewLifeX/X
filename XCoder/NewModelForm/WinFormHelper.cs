using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace XCoder
{
    public class WinFormHelper
    {
        #region 获取FormModel窗体，传入一个控件
        /// <summary>
        /// 获取控件的窗体，并设置标题
        /// </summary>
        /// <param name="cl">控件</param>
        /// <param name="titleText">标题文本</param>        
        public static BaseForm CreateForm(Control cl, String titleText = "")
        {
            var tf = new BaseForm();
            tf.Size = new Size(cl.Width + 15, cl.Size.Height + 40);
            tf.Controls.Add(cl);//将控件添加到窗体中
            tf.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            tf.Text = titleText;
            return tf;
        }
        #endregion

        #region 设置控件只能输入数字
        /// <summary>
        /// 设置控件只能输入数字
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void SetControlOnlyValue(Object sender, KeyPressEventArgs e)
        {
            e.Handled = !(Char.IsNumber(e.KeyChar) || e.KeyChar == (Char)8 || e.KeyChar == '.' || e.KeyChar == '-');
            if (!e.Handled)
                (sender as TextBox).Tag = (sender as TextBox).Text;//记录最后一次正确输入
        }
        /// <summary>
        /// 只能输入正数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void SetControlOnlyZS(Object sender, KeyPressEventArgs e)
        {
            e.Handled = !(Char.IsNumber(e.KeyChar) || e.KeyChar == (Char)8 || e.KeyChar == '.');
            if (!e.Handled)
                (sender as TextBox).Tag = (sender as TextBox).Text;//记录最后一次正确输入
        }
        #endregion
    }

    /// <summary>用于Combox显示绑定的对象</summary>
    public class BindComboxEnumType<T>
    {
        /// <summary>类型的名字</summary>
        public String Name { get; set; }

        /// <summary>类型</summary>
        public T Type { get; set; }

        private static readonly List<BindComboxEnumType<T>> bindTyps;

        static BindComboxEnumType()
        {
            bindTyps = new List<BindComboxEnumType<T>>();

            foreach (var name in Enum.GetNames(typeof(T)))
            {
                bindTyps.Add(new BindComboxEnumType<T>()
                {
                    Name = name,
                    Type = (T)Enum.Parse(typeof(T), name)
                });
            }
        }

        /// <summary>绑定的类型数据</summary>
        public static List<BindComboxEnumType<T>> BindTyps
        {
            get { return bindTyps; }
        }
    }

    /// <summary>暂时支持的数据类型,常规的</summary>
    public enum SupportDataType
    {
        Boolean = 3,
        DateTime = 6,
        Double = 8,
        Int32 = 9,
        Int64 = 10,
        SByte = 11,
        String = 12
    }

    /// <summary>模型设计所支持的数据类型,每一种类型的很多参数都固定了，所以写了这么个类</summary>
    public class PrimitiveType
    {
        /// <summary>原始类型名称</summary>
        public String Name { get; set; }
        /// <summary>类型长度</summary>
        public Int32 Length { get; set; }
        ///// <summary>字节数/summary>
        //public Int32 NumOfByte { get; set; }
        /// <summary>精度</summary>
        public Int32 Precision { get; set; }
        /// <summary>数据类型</summary>
        public String DataType { get; set; }

        public static List<PrimitiveType> TypeList;

        static PrimitiveType()
        {
            TypeList = new List<PrimitiveType>();
            TypeList.Add(new PrimitiveType() { Name = "bool", Length = 1, DataType = "System.Boolean", Precision = 1 });
            TypeList.Add(new PrimitiveType() { Name = "int", Length = 10, DataType = "System.Int32", Precision = 10 });
            TypeList.Add(new PrimitiveType() { Name = "double", Length = 22, DataType = "System.Double", Precision = 22 });
            TypeList.Add(new PrimitiveType() { Name = "nvarchar", Length = 20, DataType = "System.String", Precision = 20 });
            TypeList.Add(new PrimitiveType() { Name = "ntext", Length = 65535, DataType = "System.String", Precision = 65535 });
            TypeList.Add(new PrimitiveType() { Name = "datetime", Length = 20, DataType = "System.DateTime", Precision = 20 });
        }
    }
}