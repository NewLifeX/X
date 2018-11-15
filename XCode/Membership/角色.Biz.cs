using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;

namespace XCode.Membership
{
    /// <summary>操作权限</summary>
    [Flags]
    [Description("操作权限")]
    public enum PermissionFlags
    {
        /// <summary>无权限</summary>
        [Description("无权限")]
        None = 0,

        /// <summary>查看权限</summary>
        [Description("查看")]
        Detail = 1,

        /// <summary>添加权限</summary>
        [Description("添加")]
        Insert = 2,

        /// <summary>修改权限</summary>
        [Description("修改")]
        Update = 4,

        /// <summary>删除权限</summary>
        [Description("删除")]
        Delete = 8,

        /// <summary>所有权限</summary>
        [Description("所有")]
        All = 0xFF,
    }

    /// <summary>角色</summary>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Role : Role<Role> { }

    /// <summary>角色</summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract partial class Role<TEntity> : LogEntity<TEntity>
          where TEntity : Role<TEntity>, new()
    {
        #region 对象操作
        static Role()
        {
            // 用于引发基类的静态构造函数
            var entity = new TEntity();

            Meta.Modules.Add<UserModule>();
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();
        }

        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal protected override void InitData()
        {
            base.InitData();

            if (Meta.Count > 0)
            {
                // 必须有至少一个可用的系统角色
                //var list = Meta.Cache.Entities.ToList();
                // InitData中用缓存将会导致二次调用InitData，从而有一定几率死锁
                var list = FindAll().ToList();
                if (list.Count > 0 && !list.Any(e => e.IsSystem))
                {
                    // 如果没有，让第一个角色作为系统角色
                    var role = list[0];
                    role.IsSystem = true;

                    XTrace.WriteLine("必须有至少一个可用的系统角色，修改{0}为系统角色！", role.Name);

                    role.Save();
                }
            }
            else
            {
                if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}角色数据……", typeof(TEntity).Name);

                Add("管理员", true, "默认拥有全部最高权限，由系统工程师使用，安装配置整个系统");
                Add("高级用户", false, "业务管理人员，可以管理业务模块，可以分配授权用户等级");
                Add("普通用户", false, "普通业务人员，可以使用系统常规业务模块功能");
                Add("游客", false, "新注册用户默认属于游客组");

                if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}角色数据！", typeof(TEntity).Name);
            }

            //CheckRole();
            // 当前处于事务之中，下面使用Menu会触发异步检查架构，SQLite单线程机制可能会造成死锁
            ThreadPoolX.QueueUserWorkItem(CheckRole);
        }

        /// <summary>初始化时执行必要的权限检查，以防万一管理员无法操作</summary>
        static void CheckRole()
        {
            // InitData中用缓存将会导致二次调用InitData，从而有一定几率死锁
            var list = FindAll();

            // 如果某些菜单已经被删除，但是角色权限表仍然存在，则删除
            var eopMenu = ManageProvider.GetFactory<IMenu>();
            var menus = eopMenu.FindAll().Cast<IMenu>().ToList();
            var ids = menus.Select(e => (Int32)e["ID"]).ToArray();
            foreach (var role in list)
            {
                if (!role.CheckValid(ids)) XTrace.WriteLine("删除[{0}]中的无效资源权限！", role);
            }

            // 所有角色都有权进入管理平台，否则无法使用后台
            var menu = menus.FirstOrDefault(e => e.Name == "Admin");
            if (menu != null)
            {
                foreach (var role in list)
                {
                    role.Set(menu.ID, PermissionFlags.Detail);
                }
            }
            list.Save();

            // 系统角色
            var sys = list.Where(e => e.IsSystem).OrderBy(e => e.ID).FirstOrDefault();
            if (sys == null) return;

            // 如果没有任何角色拥有权限管理的权限，那是很悲催的事情
            var count = 0;
            foreach (var item in menus)
            {
                //if (item.Visible && !list.Any(e => e.Has(item.ID, PermissionFlags.Detail)))
                if (!list.Any(e => e.Has(item.ID, PermissionFlags.Detail)))
                {
                    count++;
                    sys.Set(item.ID, PermissionFlags.All);

                    XTrace.WriteLine("没有任何角色拥有菜单[{0}]的权限", item.Name);
                }
            }
            if (count > 0)
            {
                XTrace.WriteLine("共有{0}个菜单，没有任何角色拥有权限，准备授权第一系统角色[{1}]拥有其完全管理权！", count, sys);
                sys.Save();

                // 更新缓存
                Meta.Cache.Clear("CheckRole");
            }
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否新数据</param>
        public override void Valid(Boolean isNew)
        {
            if (String.IsNullOrEmpty(Name)) throw new ArgumentNullException(__.Name, _.Name.DisplayName + "不能为空！");

            base.Valid(isNew);

            SavePermission();
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override Int32 Delete()
        {
            var entity = this;
            var name = entity.Name;
            if (String.IsNullOrEmpty(name))
            {
                entity = FindByID(ID);
                if (entity != null) name = entity.Name;
            }

            if (Meta.Count <= 1 && FindCount() <= 1)
            {
                var msg = String.Format("至少保留一个角色[{0}]禁止删除！", name);
                WriteLog("删除", msg);

                throw new XException(msg);
            }

            if (entity.IsSystem)
            {
                var msg = String.Format("系统角色[{0}]禁止删除！", name);
                WriteLog("删除", msg);

                throw new XException(msg);
            }

            return base.Delete();
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override Int32 Save()
        {
            // 先处理一次，否则可能因为别的字段没有修改而没有脏数据
            SavePermission();

            return base.Save();
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override Int32 Update()
        {
            // 先处理一次，否则可能因为别的字段没有修改而没有脏数据
            SavePermission();

            return base.Update();
        }

        /// <summary>加载权限字典</summary>
        protected override void OnLoad()
        {
            base.OnLoad();

            // 构造权限字典
            LoadPermission();
        }

        /// <summary>如果Permission被修改，则重新加载</summary>
        /// <param name="fieldName"></param>
        protected override void OnPropertyChanged(String fieldName)
        {
            base.OnPropertyChanged(fieldName);

            if (fieldName == __.Permission) LoadPermission();
        }
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找角色</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0 || Meta.Cache.Entities == null || Meta.Cache.Entities.Count < 1) return null;

            return Meta.Cache.Entities.ToArray().FirstOrDefault(e => e.ID == id);
        }

        /// <summary>根据编号查找角色</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IRole IRole.FindByID(Int32 id) { return FindByID(id); }

        /// <summary>根据名称查找角色</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindByName(String name)
        {
            if (String.IsNullOrEmpty(name) || Meta.Cache.Entities == null || Meta.Cache.Entities.Count < 1) return null;

            return Meta.Cache.Find(e => e.Name.EqualIgnoreCase(name));
        }
        #endregion

        #region 扩展权限
        /// <summary>本角色权限集合</summary>
        [XmlIgnore, ScriptIgnore]
        public IDictionary<Int32, PermissionFlags> Permissions { get; } = new Dictionary<Int32, PermissionFlags>();

        /// <summary>是否拥有指定资源的指定权限</summary>
        /// <param name="resid"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public Boolean Has(Int32 resid, PermissionFlags flag = PermissionFlags.None)
        {
            var pf = PermissionFlags.None;
            if (!Permissions.TryGetValue(resid, out pf)) return false;
            if (flag == PermissionFlags.None) return true;

            return pf.Has(flag);
        }

        void Remove(Int32 resid)
        {
            if (Permissions.ContainsKey(resid)) Permissions.Remove(resid);
        }

        /// <summary>获取权限</summary>
        /// <param name="resid"></param>
        /// <returns></returns>
        public PermissionFlags Get(Int32 resid)
        {
            if (!Permissions.TryGetValue(resid, out var pf)) return PermissionFlags.None;

            return pf;
        }

        /// <summary>设置该角色拥有指定资源的指定权限</summary>
        /// <param name="resid"></param>
        /// <param name="flag"></param>
        public void Set(Int32 resid, PermissionFlags flag = PermissionFlags.All)
        {
            var pf = PermissionFlags.None;
            if (!Permissions.TryGetValue(resid, out pf))
            {
                if (flag != PermissionFlags.None)
                    Permissions.Add(resid, flag);
            }
            else
            {
                Permissions[resid] = pf | flag;
            }
        }

        /// <summary>重置该角色指定的权限</summary>
        /// <param name="resid"></param>
        /// <param name="flag"></param>
        public void Reset(Int32 resid, PermissionFlags flag)
        {
            var pf = PermissionFlags.None;
            if (Permissions.TryGetValue(resid, out pf))
            {
                Permissions[resid] = pf & ~flag;
            }
        }

        /// <summary>检查是否有无效权限项，有则删除</summary>
        /// <param name="resids"></param>
        internal Boolean CheckValid(Int32[] resids)
        {
            if (resids == null || resids.Length == 0) return true;

            var ps = Permissions;
            var count = ps.Count;

            var list = new List<Int32>();
            foreach (var item in ps)
            {
                if (!resids.Contains(item.Key)) list.Add(item.Key);
            }
            // 删除无效项
            foreach (var item in list)
            {
                ps.Remove(item);
            }

            return count == ps.Count;
        }

        void LoadPermission()
        {
            Permissions.Clear();
            if (String.IsNullOrEmpty(Permission)) return;

            var dic = Permission.SplitAsDictionary("#", ",");
            foreach (var item in dic)
            {
                var resid = item.Key.ToInt();
                Permissions[resid] = (PermissionFlags)item.Value.ToInt();
            }
        }

        void SavePermission()
        {
            // 不能这样子直接清空，因为可能没有任何改变，而这么做会两次改变脏数据，让系统以为有改变
            //Permission = null;
            if (Permissions.Count <= 0)
            {
                //Permission = null;
                SetItem(__.Permission, null);
                return;
            }

            var sb = Pool.StringBuilder.Get();
            // 根据资源按照从小到大排序一下
            foreach (var item in Permissions.OrderBy(e => e.Key))
            {
                //// 跳过None
                //if (item.Value == PermissionFlags.None) continue;
                // 不要跳过None，因为None表示只读

                if (sb.Length > 0) sb.Append(",");
                sb.AppendFormat("{0}#{1}", item.Key, (Int32)item.Value);
            }
            SetItem(__.Permission, sb.Put(true));
        }

        /// <summary>当前角色拥有的资源</summary>
        [XmlIgnore, ScriptIgnore]
        public Int32[] Resources { get { return Permissions.Keys.ToArray(); } }
        #endregion

        #region 业务
        /// <summary>根据名称查找角色，若不存在则创建</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IRole GetOrAdd(String name)
        {
            if (name.IsNullOrEmpty()) return null;

            return Add(name, false);
        }

        /// <summary>根据名称查找角色，若不存在则创建</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IRole IRole.GetOrAdd(String name)
        {
            if (name.IsNullOrEmpty()) return null;

            return Add(name, false);
        }

        /// <summary>添加角色，如果存在，则直接返回，否则创建</summary>
        /// <param name="name"></param>
        /// <param name="issys"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        public static TEntity Add(String name, Boolean issys, String remark = null)
        {
            //var entity = FindByName(name);
            var entity = Find(__.Name, name);
            if (entity != null) return entity;

            entity = new TEntity
            {
                Name = name,
                IsSystem = issys,
                Remark = remark
            };
            entity.Save();

            return entity;
        }
        #endregion
    }

    public partial interface IRole
    {
        /// <summary>本角色权限集合</summary>
        IDictionary<Int32, PermissionFlags> Permissions { get; }

        /// <summary>是否拥有指定资源的指定权限</summary>
        /// <param name="resid"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        Boolean Has(Int32 resid, PermissionFlags flag = PermissionFlags.None);

        /// <summary>获取权限</summary>
        /// <param name="resid"></param>
        /// <returns></returns>
        PermissionFlags Get(Int32 resid);

        /// <summary>设置该角色拥有指定资源的指定权限</summary>
        /// <param name="resid"></param>
        /// <param name="flag"></param>
        void Set(Int32 resid, PermissionFlags flag = PermissionFlags.Detail);

        /// <summary>重置该角色指定的权限</summary>
        /// <param name="resid"></param>
        /// <param name="flag"></param>
        void Reset(Int32 resid, PermissionFlags flag);

        /// <summary>当前角色拥有的资源</summary>
        Int32[] Resources { get; }

        /// <summary>根据编号查找角色</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IRole FindByID(Int32 id);

        /// <summary>根据名称查找角色，若不存在则创建</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IRole GetOrAdd(String name);

        /// <summary>保存</summary>
        /// <returns></returns>
        Int32 Save();
    }
}