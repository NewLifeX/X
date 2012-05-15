using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
using XCode.Model;
using NewLife.Reflection;
#endif

namespace XCode.DataAccessLayer.Model
{
    /// <summary>模型名称解析器接口。解决名称大小写、去前缀、关键字等多个问题</summary>
    public interface INameResolver
    {
        /// <summary>获取别名。过滤特殊符号，过滤_之类的前缀。另外，避免一个表中的字段别名重名</summary>
        /// <param name="dc"></param>
        /// <returns></returns>
        String GetAlias(IDataColumn dc);

        /// <summary>获取别名。过滤特殊符号，过滤_之类的前缀。</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        String GetAlias(String name);

        /// <summary>去除前缀。默认去除第一个_前面部分，去除tbl和table前缀</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        String CutPrefix(String name);

        /// <summary>自动处理大小写</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        String FixWord(String name);

        /// <summary>是否关键字</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Boolean IsKeyWord(String name);

        /// <summary>获取显示名，如果描述不存在，则使用名称，否则使用描述前面部分，句号（中英文皆可）、换行分隔</summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        String GetDisplayName(String name, String description);
    }

    /// <summary>模型名称解析器。解决名称大小写、去前缀、关键字等多个问题</summary>
    public class NameResolver : INameResolver
    {
        #region 接口实现
        /// <summary>获取别名。过滤特殊符号，过滤_之类的前缀。另外，避免一个表中的字段别名重名</summary>
        /// <param name="dc"></param>
        /// <returns></returns>
        public virtual String GetAlias(IDataColumn dc)
        {
            var name = GetAlias(dc.Name);
            var df = dc as XField;
            if (df != null && dc.Table != null)
            {
                var lastname = name;
                var index = 0;
                var cs = dc.Table.Columns;
                for (int i = 0; i < cs.Count; i++)
                {
                    var item = cs[i] as XField;
                    if (item != dc && item.Name != dc.Name)
                    {
                        if (lastname.EqualIgnoreCase(item._Alias))
                        {
                            lastname = name + ++index;
                            // 从头开始
                            i = -1;
                        }
                    }
                }
                lastname = name;
            }
            return name;
        }

        /// <summary>获取别名。过滤特殊符号，过滤_之类的前缀。</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual String GetAlias(String name)
        {
            if (String.IsNullOrEmpty(name)) return name;

            name = name.Replace("$", null);
            name = name.Replace("(", null);
            name = name.Replace(")", null);
            name = name.Replace("（", null);
            name = name.Replace("）", null);
            name = name.Replace(" ", null);
            name = name.Replace("　", null);

            // 很多时候，这个别名就是表名
            return FixWord(CutPrefix(name));
        }

        /// <summary>去除前缀。默认去除第一个_前面部分，去除tbl和table前缀</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual String CutPrefix(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            // 自动去掉前缀
            Int32 n = name.IndexOf("_");
            // _后至少要有2个字母，并且后一个不能是_
            if (n >= 0 && n < name.Length - 2 && name[n + 1] != '_')
            {
                String str = name.Substring(n + 1);
                if (!IsKeyWord(str)) name = str;
            }

            String[] ss = new String[] { "tbl", "table" };
            foreach (String s in ss)
            {
                if (name.StartsWith(s))
                {
                    String str = name.Substring(s.Length);
                    if (!IsKeyWord(str)) name = str;
                }
                else if (name.EndsWith(s))
                {
                    String str = name.Substring(0, name.Length - s.Length);
                    if (!IsKeyWord(str)) name = str;
                }
            }

            return name;
        }

        /// <summary>自动处理大小写</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual String FixWord(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            if (name.Equals("ID", StringComparison.OrdinalIgnoreCase)) return "ID";

            if (name.Length <= 2) return name;

            Int32 lowerCount = 0;
            Int32 upperCount = 0;
            foreach (var item in name)
            {
                if (item >= 'a' && item <= 'z')
                    lowerCount++;
                else if (item >= 'A' && item <= 'Z')
                    upperCount++;
            }

            //没有或者只有一个小写字母的，需要修正
            //没有大写的，也要修正
            if (lowerCount <= 1 || upperCount < 1)
            {
                name = name.ToLower();
                Char c = name[0];
                if (c >= 'a' && c <= 'z') c = (Char)(c - 'a' + 'A');
                name = c + name.Substring(1);
            }

            //处理Is开头的，第三个字母要大写
            if (name.StartsWith("Is") && name.Length >= 3)
            {
                Char c = name[2];
                if (c >= 'a' && c <= 'z')
                {
                    c = (Char)(c - 'a' + 'A');
                    name = name.Substring(0, 2) + c + name.Substring(3);
                }
            }

            return name;
        }

        /// <summary>代码生成器</summary>
        private static CSharpCodeProvider _CG = new CSharpCodeProvider();

        /// <summary>是否关键字</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Boolean IsKeyWord(String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            // 特殊处理item
            if (String.Equals(name, "item", StringComparison.OrdinalIgnoreCase)) return true;

            // 只要有大写字母，就不是关键字
            if (name.Any(c => c >= 'A' && c <= 'Z')) return false;

            return !_CG.IsValidIdentifier(name);
        }

        /// <summary>获取显示名，如果描述不存在，则使用名称，否则使用描述前面部分，句号（中英文皆可）、换行分隔</summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual String GetDisplayName(String name, String description)
        {
            if (String.IsNullOrEmpty(description)) return name;

            String str = description.Trim();
            Int32 p = str.IndexOfAny(new Char[] { '.', '。', '\r', '\n' });
            // p=0表示符号在第一位，不考虑
            if (p > 0) str = str.Substring(0, p).Trim();

            return str;
        }
        #endregion

        #region 静态实例
        /// <summary>当前名称解析器</summary>
        public static INameResolver Current { get { return XCodeService.ResolveInstance<INameResolver>(); } }
        #endregion
    }
}