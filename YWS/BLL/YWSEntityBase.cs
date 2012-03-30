using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Web;
using XCode;
using NewLife.CommonEntity;

namespace NewLife.YWS.Entities
{
    /// <summary>YWS实体基类。增加写日志等功能。</summary>
    /// <typeparam name="TEntity"></typeparam>
    public class YWSEntityBase<TEntity> : Entity<TEntity> where TEntity : YWSEntityBase<TEntity>, new()
    {
        #region 对象操作
        /// <summary>已重载。调用Save时写日志，而调用Insert和Update时不写日志</summary>
        /// <returns></returns>
        public override int Save()
        {
            Int32 id = (Int32)this["ID"];
            Boolean isAdd = id == 0;

            Int32 ret = base.Save();

            id = (Int32)this["ID"];

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 5 && i < Meta.FieldNames.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.AppendFormat("{0}={1}", Meta.FieldNames[i], this[Meta.FieldNames[i]]);
            }

            if (isAdd)
                WriteLog("添加", sb.ToString());
            else
                WriteLog("修改", sb.ToString());

            return ret;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override int Delete()
        {

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Meta.FieldNames.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.AppendFormat("{0}={1}", Meta.FieldNames[i], this[Meta.FieldNames[i]]);
            }
            WriteLog("删除", sb.ToString());

            return base.Delete();
        }
        #endregion

        #region 日志
        ///// <summary>
        ///// 创建指定动作的日志实体。通过Http状态访问当前管理员对象，创建日志实体
        ///// </summary>
        ///// <param name="action"></param>
        ///// <returns></returns>
        //public static ILog CreateLog(String action)
        //{
        //    Admin admin = Admin.Current;
        //    if (admin == null) return null;

        //    return admin.CreateLog(typeof(TEntity), action);
        //}

        /// <summary>写日志</summary>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public static void WriteLog(String action, String remark)
        {
            //IAdministrator admin = Administrator.CurrentAdministrator;
            IAdministrator admin = ManageProvider.Provider.Current as IAdministrator;
            if (admin != null) admin.WriteLog(typeof(TEntity), action, remark);
        }
        #endregion
    }
}