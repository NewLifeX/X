using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Configuration
{
    /// <summary>Json文件配置提供者</summary>
    /// <remarks>
    /// 支持从不同配置文件加载到不同配置模型
    /// </remarks>
    public class JsonConfigProvider : FileConfigProvider
    {
        /// <summary>初始化</summary>
        /// <param name="value"></param>
        public override void Init(String value)
        {
            // 加上默认后缀
            if (!value.IsNullOrEmpty() && Path.GetExtension(value).IsNullOrEmpty()) value += ".json";

            base.Init(value);
        }

        /// <summary>读取配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected override void OnRead(String fileName, IConfigSection section)
        {
            var txt = File.ReadAllText(fileName);
            var json = new JsonParser(txt);
            var src = json.Decode() as IDictionary<String, Object>;

            Map(src, section);
        }

        /// <summary>写入配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected override void OnWrite(String fileName, IConfigSection section)
        {
            var str = GetString(section);
            var old = "";
            if (File.Exists(fileName)) old = File.ReadAllText(fileName);

            if (str != old)
            {
                XTrace.WriteLine("保存配置 {0}", fileName);

                File.WriteAllText(fileName, str);
            }
        }

        /// <summary>获取字符串形式</summary>
        /// <param name="section">配置段</param>
        /// <returns></returns>
        public override String GetString(IConfigSection section = null)
        {
            if (section == null) section = Root;

            var rs = new Dictionary<String, Object>();
            Map(section, rs);

            var jw = new JsonWriter
            {
                IgnoreNullValues = false,
                IgnoreComment = false,
                Indented = true,
                SmartIndented = true,
            };

            jw.Write(rs);

            return jw.GetString();
        }

        #region 辅助
        /// <summary>字典映射到配置树</summary>
        /// <param name="src"></param>
        /// <param name="section"></param>
        protected virtual void Map(IDictionary<String, Object> src, IConfigSection section)
        {
            foreach (var item in src)
            {
                var name = item.Key;
                if (name[0] == '#') continue;

                var cfg = section.GetOrAddChild(name);
                var cname = "#" + name;
                if (src.TryGetValue(cname, out var comment) && comment != null) cfg.Comment = comment + "";

                // 支持字典
                if (item.Value is IDictionary<String, Object> dic)
                    Map(dic, cfg);
                else if (item.Value is IList<Object> list)
                {
                    // 数组处理
                    cfg.Childs = new List<IConfigSection>();
                    foreach (var elm in list)
                    {
                        if (elm is IDictionary<String, Object> dic2)
                        {
                            var cfg2 = new ConfigSection();
                            Map(dic2, cfg2);
                            cfg.Childs.Add(cfg2);
                        }
                    }
                }
                else
                    cfg.SetValue(item.Value);
            }
        }

        /// <summary>配置树映射到字典</summary>
        /// <param name="section"></param>
        /// <param name="dst"></param>
        protected virtual void Map(IConfigSection section, IDictionary<String, Object> dst)
        {
            foreach (var item in section.Childs)
            {
                // 注释
                if (!item.Comment.IsNullOrEmpty()) dst["#" + item.Key] = item.Comment;

                var cs = item.Childs;
                if (cs != null)
                {
                    // 数组
                    if (cs.Count == 0 || cs.Count > 0 && cs[0].Key == null || cs.Count >= 2 && cs[0].Key == cs[1].Key)
                    {
                        var list = new List<Object>();
                        foreach (var elm in cs)
                        {
                            var rs = new Dictionary<String, Object>();
                            Map(elm, rs);
                            list.Add(rs);
                        }
                        dst[item.Key] = list;
                    }
                    else
                    {
                        var rs = new Dictionary<String, Object>();
                        Map(item, rs);

                        dst[item.Key] = rs;
                    }
                }
                else
                {
                    dst[item.Key] = item.Value;
                }
            }
        }
        #endregion
    }
}