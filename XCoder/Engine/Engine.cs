using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife.Reflection;
using NewLife.Threading;
using XCode.DataAccessLayer;
using XTemplate.Templating;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif
using XCode.DataAccessLayer.Model;
using NewLife.Model;

namespace XCoder
{
    /// <summary>代码生成器类</summary>
    public class Engine
    {
        #region 属性
        public const String TemplatePath = "Template";

        private static Dictionary<String, String> _Templates;
        /// <summary>模版</summary>
        public static Dictionary<String, String> Templates
        {
            get
            {
                if (_Templates == null) _Templates = FileSource.GetTemplates();
                return _Templates;
            }
        }

        private static List<String> _FileTemplates;
        /// <summary>文件模版</summary>
        public static List<String> FileTemplates
        {
            get
            {
                if (_FileTemplates == null)
                {
                    var list = new List<string>();

                    var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TemplatePath);
                    if (Directory.Exists(dir))
                    {
                        var ds = Directory.GetDirectories(dir);
                        if (ds != null && ds.Length > 0)
                        {
                            foreach (var item in ds)
                            {
                                var di = new DirectoryInfo(item);
                                list.Add(di.Name);
                            }
                        }
                    }
                    _FileTemplates = list;
                }
                return _FileTemplates;
            }
        }

        public Engine(XConfig config)
        {
            Config = config;
        }

        private XConfig _Config;
        /// <summary>配置</summary>
        public XConfig Config { get { return _Config; } set { _Config = value; } }

        private String _LastTableKey;
        private List<IDataTable> _LastTables;
        private List<IDataTable> _Tables;
        /// <summary>所有表</summary>
        public List<IDataTable> Tables
        {
            get
            {
                // 不同的前缀、大小写选项，得到的表集合是不一样的。这里用字典来缓存
                String key = String.Format("{0}_{1}_{2}_{3}_{4}", Config.AutoCutPrefix, Config.AutoCutTableName, Config.AutoFixWord, Config.Prefix, Config.UseID);
                //return _cache.GetItem(key, k => FixTable(_Tables));
                if (_LastTableKey != key)
                {
                    _LastTables = FixTable(_Tables);
                    _LastTableKey = key;
                }
                return _LastTables;
            }
            //set { _Tables = FixTable(value); }
            set { _Tables = value; }
        }

        private static ITranslate _Translate;
        /// <summary>翻译接口</summary>
        static ITranslate Translate
        {
            get { return _Translate ?? (_Translate = new NnhyServiceTranslate()); }
            //set { _Translate = value; }
        }
        #endregion

        #region 生成
        /// <summary>生成代码，参数由Config传入</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public String[] Render(String tableName)
        {
            var tables = Tables;
            if (tables == null || tables.Count < 1) return null;

            var table = tables.Find(e => e.Name.EqualIgnoreCase(tableName));
            if (tableName == null) return null;

            var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            data["Config"] = Config;
            data["Tables"] = tables;
            data["Table"] = table;

            // 声明模版引擎
            //Template tt = new Template();
            Template.Debug = Config.Debug;
            var templates = new Dictionary<string, string>();
            var tempName = Config.TemplateName;
            var tempKind = "";
            var p = tempName.IndexOf(']');
            if (p >= 0)
            {
                tempKind = tempName.Substring(0, p);
                tempName = tempName.Substring(p + 1);
            }
            if (tempKind == "[内置]")
            {
                // 系统模版
                foreach (var item in Templates)
                {
                    var key = item.Key;
                    String name = key.Substring(0, key.IndexOf("."));
                    if ("*" + name != tempName) continue;

                    String content = item.Value;

                    // 添加文件头
                    if (Config.UseHeadTemplate && !String.IsNullOrEmpty(Config.HeadTemplate) && key.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        content = Config.HeadTemplate + content;

                    templates.Add(key.Substring(name.Length + 1), content);
                }
            }
            else
            {
                // 文件模版
                String dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TemplatePath);
                dir = Path.Combine(dir, tempName);
                String[] ss = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
                if (ss != null && ss.Length > 0)
                {
                    foreach (String item in ss)
                    {
                        if (item.EndsWith("scc", StringComparison.OrdinalIgnoreCase)) continue;

                        String content = File.ReadAllText(item);

                        String name = item.Substring(dir.Length);
                        if (name.StartsWith(@"\")) name = name.Substring(1);

                        // 添加文件头
                        if (Config.UseHeadTemplate && !String.IsNullOrEmpty(Config.HeadTemplate) && name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                            content = Config.HeadTemplate + content;

                        templates.Add(name, content);
                    }
                }
            }
            if (templates.Count < 1) throw new Exception("没有可用模版！");

            Template tt = Template.Create(templates);
            if (tempName.StartsWith("*")) tempName = tempName.Substring(1);
            tt.AssemblyName = tempName;

            // 编译模版。这里至少要处理，只有经过了处理，才知道模版项是不是被包含的
            tt.Compile();

            List<String> rs = new List<string>();
            foreach (var item in tt.Templates)
            {
                if (item.Included) continue;

                String content = tt.Render(item.Name, data);

                // 计算输出文件名
                String fileName = Path.GetFileName(item.Name);
                var fname = Config.UseCNFileName ? table.DisplayName : table.Alias;
                fileName = fileName.Replace("类名", fname).Replace("中文名", fname).Replace("连接名", Config.EntityConnName);

                fileName = Path.Combine(Config.OutputPath, fileName);

                String dir = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
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

            var type = tables[0].GetType();
            var list = tables.Select(dt => (TypeX.CreateInstance(type) as IDataTable).CopyAllFrom(dt)).ToList();

            var noCNDic = new Dictionary<object, string>();
            var existTrans = new List<string>();

            var mr = ObjectContainer.Current.Resolve<IModelResolver>();
            mr.AutoCutPrefix = Config.AutoCutPrefix;
            mr.AutoCutTableName = Config.AutoCutTableName;
            mr.AutoFixWord = Config.AutoFixWord;
            mr.FilterPrefixs = Config.Prefix.Split(',', ';');
            mr.UseID = Config.UseID;

            #region 修正数据
            foreach (var table in list)
            {
                table.Alias = mr.GetAlias(table.Name);

                if (String.IsNullOrEmpty(table.Description))
                    noCNDic.Add(table, table.Alias);
                else
                    AddExistTranslate(existTrans, !string.IsNullOrEmpty(table.Alias) ? table.Alias : table.Name, table.Description);

                // 字段
                foreach (var dc in table.Columns)
                {
                    dc.Alias = mr.GetAlias(dc);

                    if (String.IsNullOrEmpty(dc.Description))
                        noCNDic.Add(dc, dc.Alias);
                    else
                        AddExistTranslate(existTrans, !string.IsNullOrEmpty(dc.Alias) ? dc.Alias : dc.Name, dc.Description);
                }

                //table.Fix();
            }

            ModelHelper.Connect(list);
            #endregion

            #region 异步调用接口修正中文名
            //if (Config.UseCNFileName && noCNDic.Count > 0)
            if (noCNDic.Count > 0)
            {
                ThreadPoolX.QueueUserWorkItem(TranslateWords, noCNDic);
            }
            #endregion

            #region 提交已翻译的项目
            if (existTrans.Count > 0)
            {
                ThreadPoolX.QueueUserWorkItem(SubmitTranslateNew, existTrans.ToArray());
            }
            #endregion

            return list;
        }

        void TranslateWords(Object state)
        {
            var dic = state as Dictionary<Object, String>;

            //ITranslate trs = new BingTranslate();
            string[] words = new string[dic.Values.Count];
            dic.Values.CopyTo(words, 0);
            String[] rs = Translate.Translate(words);
            if (rs == null || rs.Length < 1) return;

            var ts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < words.Length && i < rs.Length; i++)
            {
                String key = words[i].Replace(" ", null);
                if (!ts.ContainsKey(key) && !String.IsNullOrEmpty(rs[i]) && words[i] != rs[i] && key != rs[i].Replace(" ", null)) ts.Add(key, rs[i].Replace(" ", null));
            }

            foreach (var item in dic)
            {
                if (!ts.ContainsKey(item.Value) || String.IsNullOrEmpty(ts[item.Value])) continue;

                if (item.Key is IDataTable)
                    (item.Key as IDataTable).Description = ts[item.Value];
                else if (item.Key is IDataColumn)
                    (item.Key as IDataColumn).Description = ts[item.Value];
            }
        }

        void SubmitTranslateNew(object state)
        {
            string[] existTrans = state as string[];
            if (existTrans != null && existTrans.Length > 0)
            {
                //var serv = new NnhyServiceTranslate();
                Translate.TranslateNew("1", existTrans);
                var trans = new List<string>(ExistSubmitTrans);
                trans.AddRange(existTrans);
                ExistSubmitTrans = trans.ToArray();
            }
        }

        private static string[] _ExistSubmitTrans;
        private static string[] ExistSubmitTrans
        {
            get
            {
                if (_ExistSubmitTrans == null)
                {
                    string f = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "XCoder");
                    f = Path.Combine(f, "SubmitedTranslations.dat");
                    if (File.Exists(f))
                    {
                        _ExistSubmitTrans = File.ReadAllLines(f);
                    }
                    else
                    {
                        _ExistSubmitTrans = new string[] { };
                    }
                }
                return _ExistSubmitTrans;
            }
            set
            {
                if (value != null && value.Length > 0)
                {
                    string f = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "XCoder");
                    if (!Directory.Exists(f)) Directory.CreateDirectory(f);
                    f = Path.Combine(f, "SubmitedTranslations.dat");
                    File.WriteAllLines(f, value);
                    _ExistSubmitTrans = value;
                }
            }
        }

        void AddExistTranslate(List<string> trans, string text, string tranText)
        {
            if (text != null) text = text.Trim();
            if (tranText != null) tranText = tranText.Trim();
            if (string.IsNullOrEmpty(text)) return;
            if (string.IsNullOrEmpty(tranText)) return;
            if (text.Equals(tranText, StringComparison.OrdinalIgnoreCase)) return;

            for (int i = 0; i < trans.Count; i += 2)
            {
                if (trans[i].Equals(text, StringComparison.OrdinalIgnoreCase) &&
                    trans[i + 1].Equals(tranText, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            for (int i = 0; i < ExistSubmitTrans.Length; i += 2)
            {
                if (ExistSubmitTrans[i].Equals(text, StringComparison.OrdinalIgnoreCase) &&
                    ExistSubmitTrans[i + 1].Equals(tranText, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            trans.Add(text);
            trans.Add(tranText);
        }
        #endregion
    }
}