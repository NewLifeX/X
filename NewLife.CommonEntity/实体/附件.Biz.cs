/*
 * XCoder v3.2.2010.1014
 * 作者：nnhy/NEWLIFE
 * 时间：2010-12-08 16:22:54
 * 版权：版权所有 (C) 新生命开发团队 2010
*/

using System;
using System.ComponentModel;
using System.IO;
using NewLife.Configuration;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 附件
    /// </summary>
    public class Attachment : Attachment<Attachment, Statistics> { }

    /// <summary>
    /// 附件
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TStatistics"></typeparam>
    public class Attachment<TEntity, TStatistics> : Attachment<TEntity>
        where TEntity : Attachment<TEntity>, new()
        where TStatistics : Statistics<TStatistics>, new()
    {
        #region 统计
        private TStatistics _Stat;
        /// <summary>统计</summary>
        public TStatistics Stat
        {
            get
            {
                if (_Stat == null && StatID > 0 && !Dirtys.ContainsKey("Stat"))
                {
                    _Stat = Statistics<TStatistics>.FindByID(StatID);
                    Dirtys.Add("Stat", true);
                }
                return _Stat;
            }
            set { _Stat = value; }
        }

        private static Object _incLock = new Object();
        /// <summary>
        /// 增加统计
        /// </summary>
        /// <param name="remark"></param>
        public void Increment(String remark)
        {
            if (Stat == null)
            {
                lock (_incLock)
                {
                    if (Stat == null)
                    {
                        TStatistics entity = new TStatistics();
                        entity.Save();

                        this.StatID = entity.ID;
                        this.Save();

                        if (Stat == null) Stat = entity;
                    }
                }
            }

            Stat.Increment(remark);
        }

        #endregion
    }

    /// <summary>
    /// 附件
    /// </summary>
    /// <remarks>
    /// 对于文件的存放，可以考虑同一个文件只存放一份，方法就是通过名称、大小、散列三个同时比较
    /// </remarks>
    public partial class Attachment<TEntity> : Entity<TEntity> where TEntity : Attachment<TEntity>, new()
    {
        #region 对象操作
        //基类Entity中包含三个对象操作：Insert、Update、Delete
        //你可以重载它们，以改变它们的行为
        //如：
        /*
        /// <summary>
        /// 已重载。把该对象插入到数据库。这里可以做数据插入前的检查
        /// </summary>
        /// <returns>影响的行数</returns>
        public override Int32 Insert()
        {
            return base.Insert();
        }
         * */
        #endregion

        #region 扩展属性
        //TODO: 本类与哪些类有关联，可以在这里放置一个属性，使用延迟加载的方式获取关联对象

        /*
        private Category _Category;
        /// <summary>该商品所对应的类别</summary>
        public Category Category
        {
            get
            {
                if (_Category == null && CategoryID > 0 && !Dirtys.ContainKey("Category"))
                {
                    _Category = Category.FindByKey(CategoryID);
                    Dirtys.Add("Category", true);
                }
                return _Category;
            }
            set { _Category = value; }
        }
         * */

        /// <summary>
        /// 完全文件路径
        /// </summary>
        public String FullFilePath
        {
            get
            {
                if (String.IsNullOrEmpty(FilePath)) return null;
                return Path.Combine(GetConfigPath(Category), FilePath);
            }
        }
        #endregion

        #region 扩展查询
        /// <summary>
        /// 根据主键查询一个附件实体对象用于表单编辑
        /// </summary>
        ///<param name="__ID">编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByKeyForEdit(Int32 __ID)
        {
            TEntity entity = Find(new String[] { _.ID }, new Object[] { __ID });
            if (entity == null)
            {
                entity = new TEntity();
            }
            return entity;
        }

        /// <summary>
        /// 根据编号查找
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            //return Find(_.ID, id);
            // 实体缓存
            //return Meta.Cache.Entities.Find(_.ID, id);
            // 单对象缓存
            return Meta.SingleCache[id];
        }
        #endregion

        #region 高级查询
        ///// <summary>
        ///// 查询满足条件的记录集，分页、排序
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="orderClause">排序，不带Order By</param>
        ///// <param name="startRowIndex">开始行，0开始</param>
        ///// <param name="maximumRows">最大返回行数</param>
        ///// <returns>实体集</returns>
        //[DataObjectMethod(DataObjectMethodType.Select, true)]
        //public static EntityList<Attachment> Search(String name, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        //{
        //    return FindAll(SearchWhere(name), orderClause, null, startRowIndex, maximumRows);
        //}

        ///// <summary>
        ///// 查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="orderClause">排序，不带Order By</param>
        ///// <param name="startRowIndex">开始行，0开始</param>
        ///// <param name="maximumRows">最大返回行数</param>
        ///// <returns>记录数</returns>
        //public static Int32 SearchCount(String name, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        //{
        //    return FindCount(SearchWhere(name), null, null, 0, 0);
        //}

        ///// <summary>
        ///// 构造搜索条件
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //private static String SearchWhere(String name)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append("1=1");

        //    if (!String.IsNullOrEmpty(name)) sb.AppendFormat(" And {0} like '%{1}%'", _.Name, name.Replace("'", "''"));

        //    if (sb.ToString() == "1=1")
        //        return null;
        //    else
        //        return sb.ToString();
        //}
        #endregion

        #region 扩展操作
        const String AttachmentPathKey = "NewLife.Attachment.Path";
        const String DefaultPath = @"..\Attachment\";

        /// <summary>
        /// 根据类别获取相应的存放路径设置，如果不存在，则返回顶级设置路径后加上类别作为目录名
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        static String GetConfigPath(String category)
        {
            String key = String.Empty;
            String config = String.Empty;

            if (String.IsNullOrEmpty(category))
            {
                key = AttachmentPathKey;
                config = Config.GetConfig<String>(key, DefaultPath);
            }
            else
            {
                key = String.Format("{0}_{1}", AttachmentPathKey, category);
                config = Config.GetConfig<String>(key);

                // 如果不存在，则返回顶级设置路径后加上类别作为目录名
                if (String.IsNullOrEmpty(config)) config = Path.Combine(GetConfigPath(null), category);
            }

            // 加上当前目录
            config = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config);
            // 重新计算目录，去掉..等字符
            config = new DirectoryInfo(config).FullName;
            return config;
        }

        const String AttachmentFormatKey = "NewLife.Attachment.Format";
        const String DefaultFormat = @"yyyy\MMdd";

        /// <summary>
        /// 取得时间格式化的路径
        /// </summary>
        /// <returns></returns>
        static String GetFormatPath()
        {
            String format = Config.GetConfig<String>(AttachmentFormatKey, DefaultFormat);

            return String.Format("{0:" + format + "}", DateTime.Now);
        }
        #endregion

        #region 业务
        /// <summary>
        /// 检查并设置文件存放名称，先尝试以原名存放，若有同名文件，则删除
        /// </summary>
        void GetFilePath()
        {
            if (String.IsNullOrEmpty(FileName)) throw new ArgumentNullException("FileName");

            String root = GetConfigPath(Category);
            String path = Path.Combine(root, GetFormatPath());

            String file = FileName;
            Int32 n = 2;
            while (File.Exists(Path.Combine(path, file)))
            {
                file = String.Format("{0}_{1}{2}", Path.GetFileNameWithoutExtension(FileName), n++, Path.GetExtension(FileName));
            }

            FilePath = file;
        }
        #endregion

        #region 上传
        /// <summary>
        /// 保存文件
        /// </summary>
        public void SaveFile()
        {

        }
        #endregion
    }
}