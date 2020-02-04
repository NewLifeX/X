using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Threading;

namespace XCode.Membership
{
    /// <summary>地区。行政区划数据</summary>
    /// <remarks>
    /// 民政局 http://www.mca.gov.cn/article/sj/xzqh/2019/2019/201912251506.html
    /// 统计局 http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2018/index.html
    /// </remarks>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public partial class Area : Entity<Area>
    {
        #region 对象操作
        static Area()
        {
            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<UserModule>();
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();

            Meta.Factory.MasterTime = _.UpdateTime;

            var sc = Meta.SingleCache;
            sc.MaxEntity = 100000;
            if (sc.Expire < 20 * 60) sc.Expire = 20 * 60;
            sc.Using = true;

            // 实体缓存三级地区
            var ec = Meta.Cache;
            ec.Expire = 60 * 60;
            ec.FillListMethod = () => FindAll(_.ID >= 100000 & _.ID <= 999999, null, null, 0, 0);
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            //// 如果没有脏数据，则不需要进行任何处理
            //if (!HasDirty) return;

            FixLevel();

            if (Name.IsNullOrEmpty() || Name == FullName) FixName();

            if (FullName.IsNullOrEmpty()) FullName = Name;
        }

        /// <summary>初始化数据</summary>
        protected internal override void InitData()
        {
            if (Meta.Session.Count == 0)
            {
                if (XTrace.Debug) XTrace.WriteLine("开始初始化Area[地区]数据……");

                //todo 从网站加载数据

                if (XTrace.Debug) XTrace.WriteLine("完成初始化Area[地区]数据！");
            }

            // 预热数据
            ThreadPoolX.QueueUserWorkItem(() => ScanLoad());
        }
        #endregion

        #region 扩展属性
        /// <summary>顶级根。它的Childs就是各个省份</summary>
        [XmlIgnore, ScriptIgnore]
        public static Area Root { get; } = new Area();

        /// <summary>父级</summary>
        [XmlIgnore, ScriptIgnore]
        public Area Parent => Extends.Get(nameof(Parent), k => FindByID(ParentID) ?? Root);

        /// <summary>所有父级</summary>
        [XmlIgnore, ScriptIgnore]
        public IList<Area> AllParents => Extends.Get(nameof(AllParents), k =>
                                                       {
                                                           var list = new List<Area>();
                                                           var entity = Parent;
                                                           while (entity != null)
                                                           {
                                                               if (list.Contains(entity)) break;

                                                               list.Add(entity);

                                                               entity = entity.Parent;
                                                           }

                                                           // 倒序
                                                           list.Reverse();

                                                           return list;
                                                       });

        /// <summary>父级路径</summary>
        [XmlIgnore, ScriptIgnore]
        public String ParentPath
        {
            get
            {
                var list = AllParents;
                return list != null && list.Count > 0 ? list.Join("/", r => r.Name) : Parent?.Name;
            }
        }

        /// <summary>路径</summary>
        [XmlIgnore, ScriptIgnore]
        public String Path
        {
            get
            {
                var p = ParentPath;
                return p.IsNullOrEmpty() ? Name : (p + "/" + Name);
            }
        }

        /// <summary>下级网点</summary>
        [XmlIgnore, ScriptIgnore]
        public IList<Area> Childs => Extends.Get(nameof(Childs), k => FindAllByParentID(ID).Where(e => e.Enable).ToList());

        /// <summary>子孙级区域。支持省市区，不支持乡镇街道</summary>
        [XmlIgnore, ScriptIgnore]
        public IList<Area> AllChilds => Extends.Get(nameof(AllChilds), k =>
                                                      {
                                                          var list = new List<Area>();
                                                          foreach (var item in Childs)
                                                          {
                                                              list.Add(item);
                                                              if (item.ID % 100 == 0) list.AddRange(item.AllChilds);
                                                          }
                                                          return list;
                                                      });
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static Area FindByID(Int32 id)
        {
            //if (id == 0) return Root;
            if (id <= 0) return null;

            //// 实体缓存
            //var r = Meta.Cache.Find(e => e.ID == id);
            //if (r != null) return r;

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.ID == id);
        }

        /// <summary>根据ID列表数组查询，一般先后查街道、区县、城市、省份</summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static Area FindByIDs(params Int32[] ids)
        {
            foreach (var item in ids)
            {
                if (item > 0)
                {
                    var r = FindByID(item);
                    if (r != null) return r;
                }
            }

            return null;
        }

        /// <summary>在指定地区下根据名称查找</summary>
        /// <param name="parentId">父级</param>
        /// <param name="name">名称</param>
        /// <returns>实体列表</returns>
        public static Area FindByName(Int32 parentId, String name)
        {
            // 支持0级下查找省份
            var r = parentId == 0 ? Root : FindByID(parentId);
            if (r == null) return null;

            return r.Childs.Find(e => e.Name == name || e.FullName == name);
        }

        /// <summary>根据名称查询三级地区，可能有多个地区同名</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IList<Area> FindAllByName(String name) => Meta.Cache.Entities.FindAll(e => e.Name == name || e.FullName == name);

        /// <summary>根据名称列表数组查询，依次查省份、城市、区县、街道</summary>
        /// <param name="names">名称列表</param>
        /// <returns></returns>
        public static Area FindByNames(params String[] names)
        {
            var r = Root;
            foreach (var item in names)
            {
                if (!item.IsNullOrEmpty())
                {
                    var r2 = r.Childs.Find(e => e.Name == item || e.FullName == item);
                    if (r2 == null) return r;

                    r = r2;
                }
            }

            return r;
        }

        /// <summary>根据名称从高向低分级查找，广度搜索</summary>
        /// <param name="name">名称</param>
        /// <returns>实体列表</returns>
        public static Area FindByFullName(String name)
        {
            // 从高向低，分级搜索
            var q = new Queue<Area>();
            q.Enqueue(Root);

            while (q.Count > 0)
            {
                var r = q.Dequeue();
                if (r != null)
                {
                    // 子级进入队列
                    foreach (var item in r.Childs)
                    {
                        if (item.Name == name || item.FullName == name) return item;

                        q.Enqueue(item);
                    }
                }
            }

            return null;
        }

        /// <summary>根据父级查子级，专属缓存</summary>
        private static readonly DictionaryCache<Int32, IList<Area>> _pcache = new DictionaryCache<Int32, IList<Area>>
        {
            Expire = 20 * 60,
            Period = 10 * 60,
        };

        /// <summary>根据父级查找。三级地区使用实体缓存，四级地区使用专属缓存</summary>
        /// <param name="parentid">父级</param>
        /// <returns>实体列表</returns>
        public static IList<Area> FindAllByParentID(Int32 parentid)
        {
            if (parentid < 0 || parentid > 99_99_99) return new List<Area>();

            // 实体缓存
            var rs = Meta.Cache.FindAll(e => e.ParentID == parentid);
            // 有子节点，并且都是启用状态，则直接使用
            if (rs.Count > 0 && rs.Any(e => e.Enable)) return rs;

            if (_pcache.FindMethod == null) _pcache.FindMethod = k => FindAll(_.ParentID == k);

            return _pcache[parentid];
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="parentid">父级</param>
        /// <param name="level"></param>
        /// <param name="idstart"></param>
        /// <param name="idend"></param>
        /// <param name="enable"></param>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public static IList<Area> Search(Int32 parentid, Int32 level, Int32 idstart, Int32 idend, Boolean? enable, String key, DateTime start, DateTime end, PageParameter page)
        {
            var exp = _.UpdateTime.Between(start, end);

            if (parentid >= 0) exp &= _.ParentID == parentid;
            if (level > 0) exp &= _.Level == level;
            if (idstart > 0) exp &= _.ID >= idstart;
            if (idend > 0) exp &= _.ID <= idend;
            if (enable != null) exp &= _.Enable == enable;

            if (!key.IsNullOrEmpty())
            {
                var exp2 = _.Name.StartsWith(key) | _.FullName.StartsWith(key);
                if (key.ToLong() > 0) exp2 |= _.ID == key;

                exp &= exp2;
            }

            return FindAll(exp, page);
        }

        /// <summary>根据条件模糊搜索</summary>
        /// <param name="parentid"></param>
        /// <param name="key"></param>
        /// <param name="enable"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public static IList<Area> Search(Int32 parentid, String key, Boolean? enable, PageParameter page)
        {
            // 找到父节点所在位置，向后搜索子节点
            var r = parentid == 0 ? Root : FindByID(parentid);
            if (parentid >= 0 && r == null) return new List<Area>();

            if (r != null)
            {
                var start = (page.PageIndex - 1) * page.PageSize;
                var count = page.PageSize;

                if (key.IsNullOrEmpty()) return r.Childs.Where(e => e.Enable).Skip(start).Take(count).ToList();

                var list = r.AllChilds
                    .Where(e => e.Enable)
                    .Where(e => !e.Name.IsNullOrEmpty() && e.Name.Contains(key) || !e.FullName.IsNullOrEmpty() && e.FullName.Contains(key))
                    .Skip(start)
                    .Take(count)
                    .ToList();
                if (list.Count > 0 || r.ID > 0) return list;
            }

            var exp = new WhereExpression();

            if (parentid >= 0) exp &= _.ParentID == parentid;
            if (enable != null) exp &= _.Enable == enable;

            if (!key.IsNullOrEmpty())
            {
                var exp2 = _.Name == key | _.FullName == key;
                if (key.ToLong() > 0) exp2 |= _.ID == key;

                exp &= exp2;
            }

            return FindAll(exp, page);
        }
        #endregion

        #region 业务操作
        /// <summary>查找或创建地区</summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="parentid"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        public static Area Create(Int32 id, String name, Int32 parentid, String remark = null)
        {
            if (id <= 0) return null;

            // 一二三级地址是6位数字
            if (id < 100000) return null;
            if (id > 999999)
            {
                // 四级地址是9位数字
                if (parentid < 100000 || parentid > 999999) return null;
                if (id < 100000000) return null;
                if (id > 999999999) return null;
            }

            var r = FindByID(id) ?? Find(_.ID == id);
            if (r != null) return r;

            if (name.IsNullOrEmpty()) name = id + "";

            r = new Area
            {
                ID = id,
                Name = name,
                FullName = name,
                ParentID = parentid,
                Remark = remark
            };
            r.Insert();

            return r;
        }

        /// <summary>扫描预热数据</summary>
        public static Int32 ScanLoad()
        {
            //var ss = Meta.Session.Dal.Session;
            //var old = ss.ShowSQL;
            //ss.ShowSQL = false;

            var layer = 2;
            var count = 0;
            var list = Root.Childs.ToArray().ToList();
            for (var i = 0; i < layer; i++)
            {
                var bs = new List<Area>();
                foreach (var item in list)
                {
                    bs.AddRange(item.Childs);
                }

                list = bs;
                count += bs.Count;
            }
            //var list = Search(-1, 100000, 999999, null, null, DateTime.MinValue, DateTime.MinValue, null);
            //var count = list.Count;

            //ss.ShowSQL = old;

            return count;
        }

        /// <summary>扫描并修正级别为0的数据</summary>
        /// <returns></returns>
        public static Int32 ScanFixLevel()
        {
            var ss = Meta.Session.Dal.Session;
            var old = ss.ShowSQL;
            ss.ShowSQL = false;

            var count = 0;
            var success = 0;
            var p = 0;
            while (true)
            {
                var list = FindAll(_.Level == 0 | _.Level.IsNull(), _.ID.Asc(), null, p, 1000);
                if (list.Count == 0) break;

                foreach (var item in list)
                {
                    item.FixLevel();
                }

                p += list.Count;
                count += list.Count;

                success += list.Update();
            }

            ss.ShowSQL = old;

            XTrace.WriteLine("共扫描发现{0}个层级为0的地区，修正{1}个", count, success);

            return count;
        }

        /// <summary>从内容中分析得到地区。以民政部颁布的行政区划代码为准</summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static IEnumerable<Area> Parse(String html)
        {
            if (html.IsNullOrEmpty()) yield break;

            var p = 0;
            while (true)
            {
                var s = html.IndexOf("<tr ", p);
                if (s < 0) break;

                var e = html.IndexOf("</tr>", s);
                if (e < 0) break;

                // 分析数据
                var ss = html.Substring(s, e - s).Split("<td", "</td>");
                if (ss.Length > 4)
                {
                    var id = ss[3];
                    var p2 = id.LastIndexOf('>');
                    if (p2 >= 0) id = id.Substring(p2 + 1);

                    var name = ss[5];
                    var p3 = name.LastIndexOf('>');
                    if (p3 >= 0) name = name.Substring(p3 + 1);

                    if (!id.IsNullOrEmpty() && id.ToInt() > 10_00_00 && !name.IsNullOrEmpty())
                    {
                        var r = new Area
                        {
                            ID = id.ToInt(),
                            FullName = name,
                        };
                        yield return r;
                    }
                }

                p = e;
            }
        }

        /// <summary>从内容中分析得到地区并保存。以民政部颁布的行政区划代码为准</summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static Int32 ParseAndSave(String html)
        {
            var all = Parse(html).ToList();

            // 预备好所有三级数据
            var list = FindAll(_.ID > 10_00_00 & _.ID < 99_99_99);

            var rs = new List<Area>();
            foreach (var item in all)
            {
                // 查找是否已存在
                var r = list.Find(e => e.ID == item.ID);
                if (r == null && item.ID > 99_99_99) r = FindByID(item.ID);

                if (r == null)
                {
                    r = item;

                    // 找到它的上级
                    var pid = 0;
                    if (item.ID % 10000 == 0)
                        pid = 0;
                    else if (item.ID % 100 == 0)
                        pid = item.ID - (item.ID % 10000);
                    else if (item.ID <= 99_99_99)
                        pid = item.ID - (item.ID % 100);

                    // 部分区县由省直管，中间没有第二级
                    if (!list.Any(e => e.ID == pid) && !all.Any(e => e.ID == pid))
                    {
                        //pid = item.ID - (item.ID % 10000);
                        r.FixLevel();
                        for (var i = r.Level - 1; i >= 1; i--)
                        {
                            var str = item.ID.ToString();
                            str = str.Substring(0, 2 * i);
                            if (i < 3) str += new String('0', 6 - 2 * i);
                            var id = str.ToInt();
                            if (list.Any(e => e.ID == pid) || all.Any(e => e.ID == id))
                            {
                                pid = id;
                                break;
                            }
                        }
                    }

                    r.ParentID = pid;
                    r.Enable = true;
                }
                else
                {
                    r.FullName = item.FullName;
                }

                r.FixLevel();
                r.FixName();

                //item.Upsert();

                rs.Add(r);
            }

            return rs.Save(true);
        }

        /// <summary>抓取并保存数据</summary>
        /// <param name="url">民政局。http://www.mca.gov.cn/article/sj/xzqh/2019/2019/201912251506.html</param>
        /// <returns></returns>
        public static Int32 FetchAndSave(String url = null)
        {
            if (url.IsNullOrEmpty()) url = "http://www.mca.gov.cn/article/sj/xzqh/2019/2019/201912251506.html";

            var http = new HttpClient();
            var html = http.GetStringAsync(url).Result;
            if (html.IsNullOrEmpty()) return 0;

            return ParseAndSave(html);
        }
        #endregion

        #region 大区
        private static readonly Dictionary<String, String[]> _big = new Dictionary<String, String[]>
        {
            { "华北", new[]{ "北京", "天津", "河北", "山西", "内蒙古" } },
            { "东北", new[]{ "辽宁", "吉林", "黑龙江" } },
            { "华东", new[]{ "上海", "江苏", "浙江", "安徽", "福建", "江西", "山东" } },
            { "华中", new[]{ "河南", "湖北", "湖南" } },
            { "华南", new[]{ "广东", "广西", "海南" } },
            { "西南", new[]{ "重庆", "四川", "贵州", "云南", "西藏" } },
            { "西北", new[]{ "陕西", "甘肃", "青海", "宁夏", "新疆" } },
            { "港澳台", new[]{ "香港", "澳门", "台湾" } }
        };

        /// <summary>所属大区</summary>
        /// <returns></returns>
        public String GetBig()
        {
            foreach (var item in _big)
            {
                if (item.Value.Any(e => e == Name)) return item.Key;
            }

            return null;
        }
        #endregion

        #region 简写名称
        /// <summary>修正等级</summary>
        public void FixLevel()
        {
            // 计算父级编号和层级
            var id = ID;
            if (id % 10000 == 0)
                Level = 1;
            else if (id % 100 == 0)
                Level = 2;
            else if (id <= 99_99_99)
                Level = 3;
            else if (id <= 99_99_99_999)
                Level = 4;
        }

        private static readonly String[] minzu = new String[] { "汉族", "壮族", "满族", "回族", "苗族", "维吾尔族", "土家族", "彝族", "蒙古族", "藏族", "布依族", "侗族", "瑶族", "朝鲜族", "白族", "哈尼族", "哈萨克族", "黎族", "傣族", "畲族", "傈僳族", "仡佬族", "东乡族", "高山族", "拉祜族", "水族", "佤族", "纳西族", "羌族", "土族", "仫佬族", "锡伯族", "柯尔克孜族", "达斡尔族", "景颇族", "毛南族", "撒拉族", "布朗族", "塔吉克族", "阿昌族", "普米族", "鄂温克族", "怒族", "京族", "基诺族", "德昂族", "保安族", "俄罗斯族", "裕固族", "乌孜别克族", "门巴族", "鄂伦春族", "独龙族", "塔塔尔族", "赫哲族", "珞巴族" };

        /// <summary>修正名称</summary>
        public void FixName()
        {
            if (FullName.IsNullOrEmpty()) return;

            var name = FullName;

            if (ParentID == 0)
            {
                name = name.TrimEnd("省", "市", "自治区", "壮族", "回族", "维吾尔", "特别行政区");
            }
            else if (Level <= 3)
            {
                if (name.Length > 8) name = name.TrimEnd("经济技术开发区");
                if (name.Length > 6) name = name.TrimEnd("技术开发区", "经济开发区", "产业开发区", "旅游开发区");
                if (name.Length > 4) name = name.TrimEnd("开发区", "管理区", "风景区");
                if (name.Length > 3) name = name.TrimEnd("地区", "林区", "矿区", "新区");
                if (name.Length > 2) name = name.TrimName("县", "市", "区", "盟", "旗", "州");

                if (name.Length > 3) name = name.TrimEnd("自治", "联合", "各族");

                for (var i = 0; i < minzu.Length; i++)
                {
                    var item = minzu[i];
                    // 去掉结尾的民族名称
                    if (name.Length > item.Length + 1 && name.EndsWith(item))
                    {
                        name = name.TrimEnd(item);
                        i = -1;
                    }
                    else if (item.Length >= 3 && item.EndsWith("族"))
                    {
                        item = item.TrimEnd('族');
                        if (name.Length > item.Length + 1 && name.EndsWith(item))
                        {
                            name = name.TrimEnd(item);
                            i = -1;
                        }
                    }
                }

                // 数据错误导致多一个字
                if (name.Length > 2) name = name.TrimEnd("自");
            }
            else if (Level == 4)
            {
                if (name.Length > 3) name = name.TrimEnd("街道");
            }
            Name = name;
        }
        #endregion
    }

    static class FixHelper
    {
        public static String TrimName(this String name, params String[] ss)
        {
            foreach (var item in ss)
            {
                if (name.Length <= 2) break;

                name = name.TrimEnd(item);
            }

            return name;
        }
    }
}