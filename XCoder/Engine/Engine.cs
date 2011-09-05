using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using NewLife.Log;
using XCode.DataAccessLayer;
using XTemplate.Templating;

namespace XCoder
{
    /// <summary>
    /// 代码生成器类
    /// </summary>
    public class Engine
    {
        #region 属性
        public const String TemplatePath = "Template";

        public Engine(XConfig config)
        {
            Config = config;
        }

        private XConfig _Config;
        /// <summary>配置</summary>
        public XConfig Config
        {
            get { return _Config; }
            set { _Config = value; }
        }

        private List<IDataTable> _Tables;
        /// <summary>所有表</summary>
        public List<IDataTable> Tables
        {
            get { return _Tables; }
            //get
            //{
            //    if (_Tables == null)
            //    {
            //        try
            //        {
            //            _Tables = DAL.Create(Config.ConnName).Tables;
            //        }
            //        catch (Exception ex)
            //        {
            //            MessageBox.Show(ex.ToString());
            //        }
            //    }
            //    return _Tables;
            //}
            set { _Tables = FixTable(value); }
        }

        private String _OuputPath;
        /// <summary>输出路径</summary>
        public String OuputPath
        {
            get
            {
                if (_OuputPath == null)
                {
                    String str = Config.OutputPath;
                    if (!Directory.Exists(str)) Directory.CreateDirectory(str);

                    _OuputPath = str;
                    if (_OuputPath == null) _OuputPath = "";
                }
                return _OuputPath;
            }
            set
            {
                if (_OuputPath != null && !Directory.Exists(value)) Directory.CreateDirectory(value);
                _OuputPath = value;
            }
        }
        #endregion

        #region 辅助函数
        /// <summary>
        /// 处理前缀
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static String CutPrefix(String name)
        {
            String oldname = name;

            if (String.IsNullOrEmpty(name)) return null;

            //自动去掉前缀
            if (XConfig.Current.AutoCutPrefix && name.Contains("_"))
            {
                name = name.Substring(name.IndexOf("_") + 1);
            }

            if (String.IsNullOrEmpty(XConfig.Current.Prefix))
            {
                if (IsKeyWord(name)) return oldname;
                return name;
            }
            String[] ss = XConfig.Current.Prefix.Split(new Char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (String s in ss)
            {
                if (name.StartsWith(s))
                {
                    name = name.Substring(s.Length);
                }
                else if (name.EndsWith(s))
                {
                    name = name.Substring(0, name.Length - s.Length);
                }
            }

            if (IsKeyWord(name)) return oldname;

            return name;
        }

        private static CodeDomProvider[] _CGS;
        /// <summary>代码生成器</summary>
        public static CodeDomProvider[] CGS
        {
            get
            {
                if (_CGS == null)
                {
                    _CGS = new CodeDomProvider[] { new CSharpCodeProvider(), new VBCodeProvider() };
                }
                return _CGS;
            }
        }

        /// <summary>
        /// 检查是否为c#关键字
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static Boolean IsKeyWord(String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            foreach (CodeDomProvider item in CGS)
            {
                if (!item.IsValidIdentifier(name)) return true;
            }

            return false;
        }

        /// <summary>
        /// 自动处理大小写
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static String FixWord(String name)
        {
            Int32 count1 = 0;
            Int32 count2 = 0;
            foreach (Char item in name.ToCharArray())
            {
                if (item >= 'a' && item <= 'z')
                    count1++;
                else if (item >= 'A' && item <= 'Z')
                    count2++;
            }

            //没有或者只有一个小写字母的，需要修正
            //没有大写的，也要修正
            if (count1 <= 1 || count2 < 1)
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

            //自动匹配单词
            foreach (String item in Words.Keys)
            {
                if (name.Equals(item, StringComparison.OrdinalIgnoreCase))
                {
                    name = item;
                    break;
                }
            }

            return name;
        }

        /// <summary>
        /// 英文名转中文名
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static String ENameToCName(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            //foreach (String item in Words.Keys)
            //{
            //    if (name.Equals(item, StringComparison.OrdinalIgnoreCase)) return Words[item];
            //}
            //return null;
            String key = name.ToLower();
            if (LowerWords.ContainsKey(key))
                return LowerWords[key];
            else
                return null;
        }

        private static Dictionary<String, String> _Words;
        /// <summary>集合</summary>
        public static Dictionary<String, String> Words
        {
            get
            {
                if (_Words == null)
                {
                    _Words = new Dictionary<string, string>();

                    if (File.Exists("e2c.txt"))
                    {
                        String content = File.ReadAllText("e2c.txt");
                        if (!String.IsNullOrEmpty(content))
                        {
                            String[] ss = content.Split(new Char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            if (ss != null && ss.Length > 0)
                            {
                                foreach (String item in ss)
                                {
                                    String[] s = item.Split(new Char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (s != null && s.Length > 0)
                                    {
                                        String str = "";
                                        if (s.Length > 1) str = s[1];
                                        if (!_Words.ContainsKey(s[0])) _Words.Add(s[0], str);
                                    }
                                }
                            }
                        }
                    }
                }
                return _Words;
            }
        }

        private static SortedList<String, String> _LowerWords;
        /// <summary>集合</summary>
        public static SortedList<String, String> LowerWords
        {
            get
            {
                if (_LowerWords == null)
                {
                    _LowerWords = new SortedList<string, string>();

                    foreach (String item in Words.Keys)
                    {
                        if (!_LowerWords.ContainsKey(item.ToLower()))
                            _LowerWords.Add(item.ToLower(), Words[item]);
                        else if (String.IsNullOrEmpty(_LowerWords[item.ToLower()]))
                            _LowerWords[item.ToLower()] = Words[item];
                    }
                }
                return _LowerWords;
            }
        }

        public static void AddWord(String name, String cname)
        {
            String ename = CutPrefix(name);
            ename = FixWord(ename);
            if (LowerWords.ContainsKey(ename.ToLower())) return;
            LowerWords.Add(ename.ToLower(), cname);
            Words.Add(ename, cname);
            File.AppendAllText("e2c.txt", Environment.NewLine + ename + " " + cname, Encoding.UTF8);
        }
        #endregion

        #region 生成
        /// <summary>
        /// 生成代码，参数由Config传入
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public String[] Render(String tableName)
        {
            if (Tables == null || Tables.Count < 1) return null;

            IDataTable table = Tables.Find(delegate(IDataTable item) { return String.Equals(item.Name, tableName, StringComparison.OrdinalIgnoreCase); });
            if (tableName == null) return null;

            String path = Path.Combine(TemplatePath, Config.TemplateName);
            if (!Directory.Exists(path)) return null;

            String[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
            if (files == null || files.Length < 1) return null;

            Dictionary<String, Object> data = new Dictionary<string, object>();
            data["Config"] = Config;
            data["Tables"] = Tables;
            data["Table"] = table;

            // 声明模版引擎
            //Template tt = new Template();
            Template.Debug = Config.Debug;
            Dictionary<String, String> templates = new Dictionary<string, string>();
            foreach (String item in files)
            {
                if (item.EndsWith("scc", StringComparison.OrdinalIgnoreCase)) continue;

                String tempFile = item;
                if (!Path.IsPathRooted(tempFile) && !tempFile.StartsWith(TemplatePath, StringComparison.OrdinalIgnoreCase))
                    tempFile = Path.Combine(TemplatePath, tempFile);

                String content = File.ReadAllText(tempFile);

                // 添加文件头
                if (Config.UseHeadTemplate && !String.IsNullOrEmpty(Config.HeadTemplate) && item.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    content = Config.HeadTemplate + content;

                //tt.AddTemplateItem(item, content);

                templates.Add(item, content);
            }
            Template tt = Template.Create(templates);

            //tt.Process();

            //// 编译模版
            //tt.Compile();

            List<String> rs = new List<string>();
            foreach (TemplateItem item in tt.Templates)
            {
                if (item.Included) continue;

                String content = tt.Render(item.Name, data);

                // 计算输出文件名
                String fileName = Path.GetFileName(item.Name);
                String className = CutPrefix(table.Name);
                className = FixWord(className);
                String remark = table.Description;
                if (String.IsNullOrEmpty(remark)) remark = ENameToCName(className);
                if (Config.UseCNFileName && !String.IsNullOrEmpty(remark)) className = remark;
                fileName = fileName.Replace("类名", className).Replace("类说明", remark).Replace("连接名", Config.EntityConnName);

                fileName = Path.Combine(OuputPath, fileName);
                File.WriteAllText(fileName, content, Encoding.UTF8);

                rs.Add(content);
            }
            return rs.ToArray();
        }

        /// <summary>
        /// 预先修正表名等各种东西，简化模版编写。
        /// 因为与设置相关，所以，每次更改设置后，都应该调用一次该方法。
        /// </summary>
        List<IDataTable> FixTable(List<IDataTable> tables)
        {
            if (tables == null || tables.Count < 1) return tables;

            List<IDataTable> list = new List<IDataTable>();
            //foreach (IDataTable item in DAL.Create(Config.ConnName).Tables)
            //{
            //    list.Add(item.Clone() as IDataTable);
            //}
            foreach (IDataTable item in tables)
            {
                list.Add(item.Clone() as IDataTable);
            }
            //Tables = list;

            Dictionary<Object, String> noCNDic = new Dictionary<object, string>();

            #region 修正数据
            foreach (IDataTable table in list)
            {
                // 别名、类名
                String name = table.Name;
                if (IsKeyWord(name)) name = name + "1";
                if (Config.AutoCutPrefix) name = CutPrefix(name);
                if (Config.AutoFixWord) name = FixWord(name);
                table.Alias = name;

                // 描述
                if (Config.UseCNFileName)
                {
                    if (String.IsNullOrEmpty(table.Description)) table.Description = ENameToCName(table.Alias);

                    if (String.IsNullOrEmpty(table.Description)) noCNDic.Add(table, table.Alias);
                }

                // 字段
                foreach (IDataColumn dc in table.Columns)
                {
                    name = dc.Name;
                    if (Config.AutoCutPrefix)
                    {
                        String s = CutPrefix(name);
                        if (dc.Table.Columns.Exists(item => item.Name == s)) name = s;
                        String str = table.Alias;
                        if (!s.Equals(str, StringComparison.OrdinalIgnoreCase) &&
                            s.StartsWith(str, StringComparison.OrdinalIgnoreCase) &&
                            s.Length > str.Length && Char.IsLetter(s, str.Length))
                            s = s.Substring(str.Length);
                        if (dc.Table.Columns.Exists(item => item.Name == s)) name = s;
                    }
                    if (Config.AutoFixWord)
                    {
                        name = FixWord(name);
                    }

                    dc.Alias = name;

                    // 描述
                    if (Config.UseCNFileName)
                    {
                        if (String.IsNullOrEmpty(dc.Description)) dc.Description = Engine.ENameToCName(dc.Alias);

                        if (String.IsNullOrEmpty(dc.Description)) noCNDic.Add(dc, dc.Alias);
                    }
                }
            }
            #endregion

            #region 异步调用接口修正中文名
            if (Config.UseCNFileName && noCNDic.Count > 0)
            {
                ThreadPool.QueueUserWorkItem(TranslateWords, noCNDic);
            }
            #endregion

            return list;
        }

        void TranslateWords(Object state)
        {
            try
            {
                Dictionary<Object, String> dic = state as Dictionary<Object, String>;
                List<String> words = new List<string>();
                foreach (String item in dic.Values)
                {
                    if (Encoding.UTF8.GetByteCount(item) != item.Length) continue;

                    // 分词
                    String str = item;
                    List<String> ks = new List<string>();
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < str.Length; i++)
                    {
                        // 如果不是小写，作为边界，拆分
                        if (!(str[i] >= 'a' && str[i] <= 'z'))
                        {
                            if (sb.Length > 0)
                            {
                                ks.Add(sb.ToString());
                                sb.Remove(0, sb.Length);
                            }
                        }
                        sb.Append(str[i]);
                    }
                    if (sb.Length > 0)
                    {
                        ks.Add(sb.ToString());
                        sb.Remove(0, sb.Length);
                    }
                    str = String.Join(" ", ks.ToArray());

                    if (!String.IsNullOrEmpty(str) && !words.Contains(str)) words.Add(str);
                }

                ITranslate trs = new BingTranslate();
                String[] rs = trs.Translate(words.ToArray());
                if (rs == null || rs.Length < 1) return;

                Dictionary<String, String> ts = new Dictionary<string, string>();
                for (int i = 0; i < words.Count && i < rs.Length; i++)
                {
                    String key = words[i].Replace(" ", null);
                    if (!ts.ContainsKey(key) && !String.IsNullOrEmpty(rs[i]) && words[i] != rs[i] && key != rs[i].Replace(" ", null)) ts.Add(key, rs[i].Replace(" ", null));
                }

                foreach (KeyValuePair<Object, String> item in dic)
                {
                    if (!ts.ContainsKey(item.Value) || String.IsNullOrEmpty(ts[item.Value])) continue;

                    if (item.Key is IDataTable)
                        (item.Key as IDataTable).Description = ts[item.Value];
                    else if (item.Key is IDataColumn)
                        (item.Key as IDataColumn).Description = ts[item.Value];
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteLine(ex.ToString());
            }
        }
        #endregion

        #region 静态
        private static String _FileVersion;
        /// <summary>
        /// 文件版本
        /// </summary>
        public static String FileVersion
        {
            get
            {
                if (String.IsNullOrEmpty(_FileVersion))
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    AssemblyFileVersionAttribute av = Attribute.GetCustomAttribute(asm, typeof(AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;
                    if (av != null) _FileVersion = av.Version;
                    if (String.IsNullOrEmpty(_FileVersion)) _FileVersion = "1.0";
                }
                return _FileVersion;
            }
        }
        #endregion
    }
}