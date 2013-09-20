using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Threading;
using XCode.DataAccessLayer;
using XTemplate.Templating;

#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace XCoder
{
    /// <summary>代码生成引擎</summary>
    public class Engine
    {
        #region 属性
        public const String TemplatePath = "Template";

        private static Dictionary<String, String> _Templates;
        /// <summary>模版</summary>
        public static Dictionary<String, String> Templates { get { return _Templates ?? (_Templates = Source.GetTemplates()); } }

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
                    _FileTemplates = list.OrderBy(e => e).ToList();
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

        //private String _LastTableKey;
        //private List<IDataTable> _LastTables;
        private List<IDataTable> _Tables;
        /// <summary>所有表</summary>
        public List<IDataTable> Tables
        {
            get
            {
                //if (!Config.NeedFix) return _Tables;

                //// 不同的前缀、大小写选项，得到的表集合是不一样的。这里用字典来缓存
                //var key = String.Format("{0}_{1}_{2}_{3}_{4}", Config.AutoCutPrefix, Config.AutoCutTableName, Config.AutoFixWord, Config.Prefix, Config.UseID);
                ////return _cache.GetItem(key, k => FixTable(_Tables));
                //if (_LastTableKey != key)
                //{
                //    _LastTables = FixTable(_Tables);
                //    _LastTableKey = key;
                //}
                //return _LastTables;
                return _Tables;
            }
            set { _Tables = value; }
        }

        private static ITranslate _Translate;
        /// <summary>翻译接口</summary>
        static ITranslate Translate { get { return _Translate ?? (_Translate = new NnhyServiceTranslate()); } }
        #endregion

        #region 构造
        static Engine()
        {
            Template.BaseClassName = typeof(XCoderBase).FullName;
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

            var table = tables.Find(e => e.Name.EqualIgnoreCase(tableName) || e.TableName.EqualIgnoreCase(tableName));
            if (table == null) return null;

            return Render(table);
        }

        public String[] Render(IDataTable table)
        {
            var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            data["Config"] = Config;
            data["Tables"] = Tables;
            data["Table"] = table;

            #region 模版预处理
            // 声明模版引擎
            //Template tt = new Template();
            Template.Debug = Config.Debug;
            var templates = new Dictionary<string, string>();
            var tempName = Config.TemplateName;
            var tempKind = "";
            var p = tempName.IndexOf(']');
            if (p >= 0)
            {
                tempKind = tempName.Substring(0, p + 1);
                tempName = tempName.Substring(p + 1);
            }
            if (tempKind == "[内置]")
            {
                // 系统模版
                foreach (var item in Templates)
                {
                    var key = item.Key;
                    var name = key.Substring(0, key.IndexOf("."));
                    if (name != tempName) continue;

                    var content = item.Value;

                    // 添加文件头
                    if (Config.UseHeadTemplate && !String.IsNullOrEmpty(Config.HeadTemplate) && key.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        content = Config.HeadTemplate + content;

                    templates.Add(key.Substring(name.Length + 1), content);
                }
            }
            else
            {
                // 文件模版
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TemplatePath);
                dir = Path.Combine(dir, tempName);
                var ss = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
                if (ss != null && ss.Length > 0)
                {
                    foreach (var item in ss)
                    {
                        if (item.EndsWith("scc", StringComparison.OrdinalIgnoreCase)) continue;

                        var content = File.ReadAllText(item);

                        var name = item.Substring(dir.Length);
                        if (name.StartsWith(@"\")) name = name.Substring(1);

                        // 添加文件头
                        if (Config.UseHeadTemplate && !String.IsNullOrEmpty(Config.HeadTemplate) && name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                            content = Config.HeadTemplate + content;

                        templates.Add(name, content);
                    }
                }
            }
            if (templates.Count < 1) throw new Exception("没有可用模版！");

            var tt = Template.Create(templates);
            if (tempName.StartsWith("*")) tempName = tempName.Substring(1);
            tt.AssemblyName = tempName;
            #endregion

            #region 输出目录预处理
            var outpath = Config.OutputPath;
            // 使用正则替换处理
            var reg = new Regex(@"\$\((\w+)\)", RegexOptions.Compiled);
            outpath = reg.Replace(outpath, math =>
            {
                var key = math.Groups[1].Value;
                if (String.IsNullOrEmpty(key)) return null;

                var pix = PropertyInfoX.Create(typeof(IDataTable), key);
                if (pix != null)
                    return (String)pix.GetValue(table);
                else
                    return table.Properties[key];
            });
            #endregion

            #region 编译生成
            // 编译模版。这里至少要处理，只有经过了处理，才知道模版项是不是被包含的
            tt.Compile();

            var rs = new List<String>();
            foreach (var item in tt.Templates)
            {
                if (item.Included) continue;

                // 计算输出文件名
                var fileName = Path.GetFileName(item.Name);
                var fname = Config.UseCNFileName ? table.DisplayName : table.Name;
                fname = fname.Replace("/", "_").Replace("\\", "_").Replace("?", null);
                // 如果中文名无效，采用英文名
                if (String.IsNullOrEmpty(Path.GetFileNameWithoutExtension(fname)) || fname[0] == '.') fname = table.Name;
                fileName = fileName.Replace("类名", fname).Replace("中文名", fname).Replace("连接名", Config.EntityConnName);

                fileName = Path.Combine(outpath, fileName);

                // 如果不覆盖，并且目标文件已存在，则跳过
                if (!Config.Override && File.Exists(fileName)) continue;

                var content = tt.Render(item.Name, data);

                var dir = Path.GetDirectoryName(fileName);
                if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(fileName, content, Encoding.UTF8);

                rs.Add(content);
            }
            return rs.ToArray();
            #endregion
        }
        #endregion

        #region 修正表
        /// <summary>预先修正表名等各种东西，简化模版编写。</summary>
        public List<IDataTable> FixTable(List<IDataTable> tables)
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
            mr.FilterPrefixs = ("" + Config.Prefix).Split(',', ';');
            mr.UseID = Config.UseID;

            #region 修正数据
            foreach (var table in list)
            {
                table.Name = mr.GetName(table.TableName);

                if (String.IsNullOrEmpty(table.Description))
                    noCNDic.Add(table, table.Name);
                else
                    AddExistTranslate(existTrans, !string.IsNullOrEmpty(table.Name) ? table.Name : table.TableName, table.Description);

                // 字段
                foreach (var dc in table.Columns)
                {
                    dc.Name = mr.GetName(dc);

                    if (String.IsNullOrEmpty(dc.Description))
                        noCNDic.Add(dc, dc.Name);
                    else
                        AddExistTranslate(existTrans, !string.IsNullOrEmpty(dc.Name) ? dc.Name : dc.ColumnName, dc.Description);
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

            var words = new string[dic.Values.Count];
            dic.Values.CopyTo(words, 0);
            var rs = Translate.Translate(words);
            if (rs == null || rs.Length < 1) return;

            var ts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < words.Length && i < rs.Length; i++)
            {
                var key = words[i].Replace(" ", null);
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
            var existTrans = state as string[];
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
                    var f = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "XCoder");
                    f = Path.Combine(f, "SubmitedTranslations.dat");
                    if (File.Exists(f))
                        _ExistSubmitTrans = File.ReadAllLines(f);
                    else
                        _ExistSubmitTrans = new string[] { };
                }
                return _ExistSubmitTrans;
            }
            set
            {
                if (value != null && value.Length > 0)
                {
                    var f = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "XCoder");
                    if (!String.IsNullOrEmpty(f) && !Directory.Exists(f)) Directory.CreateDirectory(f);
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
            if (text.EqualIgnoreCase(tranText)) return;

            for (int i = 0; i < trans.Count - 1; i += 2)
            {
                if (trans[i].EqualIgnoreCase(text) && trans[i + 1].EqualIgnoreCase(tranText)) return;
            }

            var ests = ExistSubmitTrans;
            for (int i = 0; i < ests.Length - 1; i += 2)
            {
                if (ests[i].EqualIgnoreCase(text) && ests[i + 1].EqualIgnoreCase(tranText)) return;
            }

            trans.Add(text);
            trans.Add(tranText);
        }
        #endregion
    }
}