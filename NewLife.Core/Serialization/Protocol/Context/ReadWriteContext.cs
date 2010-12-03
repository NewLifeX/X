using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 读写上下文
    /// </summary>
    public abstract class ReadWriteContext
    {
        #region 属性
        private ProtocolFormatter _Formatter;
        /// <summary>序列化器</summary>
        public ProtocolFormatter Formatter
        {
            get { return _Formatter; }
            set { _Formatter = value; }
        }

        private FormatterConfig _Config;
        /// <summary>设置</summary>
        public FormatterConfig Config
        {
            get { return _Config; }
            set { _Config = value; }
        }

        private ProtocolTreeNode _Node;
        /// <summary>树形节点</summary>
        public ProtocolTreeNode Node
        {
            get { return _Node; }
            set { _Node = value; }
        }

        private Object _Data;
        /// <summary>读写的对象</summary>
        public Object Data
        {
            get { return _Data; }
            set { _Data = value; }
        }

        private Type _Type;
        /// <summary>类型</summary>
        public Type Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        private List<Object> _Objects;
        /// <summary>对象集合。在序列化中，一个对象的多次引用只序列化一份，其它的使用引用计数</summary>
        public List<Object> Objects
        {
            get { return _Objects ?? (_Objects = new List<Object>()); }
            set { _Objects = value; }
        }
        #endregion

        #region 扩展
        /// <summary>
        /// 上一级上下文，依赖于Node节点目录树实现
        /// </summary>
        public ReadWriteContext Parent
        {
            get
            {
                if (Node == null || Node.Parent == null) return null;

                return Node.Parent.Context;
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 创建当前对象的新实例
        /// </summary>
        /// <returns></returns>
        internal protected abstract ReadWriteContext Create();

        /// <summary>
        /// 使用新参数克隆当前对象
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual ReadWriteContext Clone(Object data, Type type, MemberInfo member)
        {
            ReadWriteContext context = Create();
            context.Formatter = Formatter;
            context.Data = data;
            context.Type = type;
            context.Objects = Objects;

            if (member != null)
            {
                context.Config = Config.CloneAndMerge(member);

                if (member is FieldInfo)
                    context.Node = Node.Add(member.Name, (member as FieldInfo).FieldType);
                else if (member is PropertyInfo)
                    context.Node = Node.Add(member.Name, (member as PropertyInfo).PropertyType);
                else if (member is Type)
                    context.Node = Node.Add(member.Name, member as Type);
            }
            else
            {
                context.Config = Config;
                context.Node = Node.Add(null, type);
            }
            context.Node.Context = context;

            return context;
        }

        /// <summary>
        /// 使用新参数克隆当前对象
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <param name="member"></param>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        public ReadWriteContext Clone(Object data, Type type, MemberInfo member, String nodeName)
        {
            ReadWriteContext context = Clone(data, type, member);
            context.Node.Name = nodeName;
            return context;
        }
        #endregion

        #region 自定义序列化
        /// <summary>
        /// 获取自定义序列化接口，向上递归
        /// </summary>
        /// <returns></returns>
        public IProtocolSerializable GetCustomInterface()
        {
            if (Data != null && Data is IProtocolSerializable) return Data as IProtocolSerializable;

            if (Parent == null) return null;

            return Parent.GetCustomInterface();
        }
        #endregion

        #region 重载
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Node != null)
                return Node.ToString();
            else
                return base.ToString();
        }
        #endregion
    }
}
