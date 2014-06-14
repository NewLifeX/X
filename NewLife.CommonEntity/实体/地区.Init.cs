using System.ComponentModel;
using System.IO;
using System.Reflection;
using NewLife.IO;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.CommonEntity
{
    /// <summary>地区</summary>
    public partial class Area<TEntity>
    {
        #region 数据
        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void InitData()
        {
            base.InitData();

            if (Meta.Count > 0) return;

            #region 新数据添加
            var tbname = Meta.TableName;
            var cnname = Meta.ConnName;

            // 异步初始化
            ThreadPoolX.QueueUserWorkItem(() =>
            {
                if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}地区数据……", typeof(TEntity).Name);

                // 异步初始化需要注意分表分库的可能
                Meta.ProcessWithSplit(cnname, tbname, () =>
                {
                    using (var sr = new StreamReader(FileSource.GetFileResource(Assembly.GetExecutingAssembly(), "AreaCode.txt")))
                    {
                        Import(sr);
                    }
                    return null;
                });

                if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}地区数据！", typeof(TEntity).Name);
            });
            #endregion
        }
        #endregion
    }
}
