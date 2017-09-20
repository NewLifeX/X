using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;

namespace XCode.Transform
{
    /// <summary>数据抽取接口</summary>
    public interface IExtracter
    {
        #region 属性
        /// <summary>名称</summary>
        String Name { get; set; }

        ///// <summary>设置</summary>
        //IExtractSetting Setting { get; set; }

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
}