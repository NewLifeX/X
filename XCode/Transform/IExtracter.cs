using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
using XCode.Configuration;

namespace XCode.Transform
{
    /// <summary>数据抽取接口</summary>
    public interface IExtracter
    {
        #region 属性
        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>实体工厂</summary>
        IEntityOperate Factory { get; set; }

        /// <summary>获取 或 设置 时间字段</summary>
        String FieldName { get; set; }

        /// <summary>附加条件</summary>
        String Where { get; set; }
        #endregion

        #region 方法
        /// <summary>初始化</summary>
        void Init();
        #endregion

        #region 抽取数据
        /// <summary>抽取一批数据</summary>
        /// <param name="set">设置</param>
        /// <returns></returns>
        IList<IEntity> Fetch(IExtractSetting set);
        #endregion

        #region 日志
        /// <summary>日志</summary>
        ILog Log { get; set; }
        #endregion
    }

    /// <summary>抽取器基类</summary>
    public abstract class ExtracterBase
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>实体工厂</summary>
        public IEntityOperate Factory { get; set; }

        /// <summary>获取 或 设置 时间字段</summary>
        public String FieldName { get; set; }

        /// <summary>附加条件</summary>
        public String Where { get; set; }

        /// <summary>时间字段</summary>
        public FieldItem Field { get; set; }

        /// <summary>排序</summary>
        public String OrderBy { get; set; }

        /// <summary>选择列</summary>
        public String Selects { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化时基抽取算法</summary>
        public ExtracterBase()
        {
            Name = GetType().Name.TrimEnd("Extracter");
        }
        #endregion

        #region 方法
        /// <summary>初始化</summary>
        public virtual void Init()
        {
            var fi = Field;
            // 自动找字段
            if (fi == null && !FieldName.IsNullOrEmpty()) fi = Field = Factory?.Table.FindByName(FieldName);

            if (fi == null) throw new ArgumentNullException(nameof(FieldName), "未指定用于顺序抽取数据的排序字段！");
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(format, args);
        }
        #endregion
    }
}