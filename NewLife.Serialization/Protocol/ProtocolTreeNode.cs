using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 协议树节点
    /// </summary>
    public class ProtocolTreeNode
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        public String Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        private Type _Type;
        /// <summary>类型</summary>
        public Type Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        private Int32 _Depth;
        /// <summary>深度</summary>
        public Int32 Depth
        {
            get { return _Depth; }
            set { _Depth = value; }
        }

        private ProtocolTreeNode _Parent;
        /// <summary>父亲节点</summary>
        public ProtocolTreeNode Parent
        {
            get { return _Parent; }
            set { _Parent = value; }
        }

        private ReadWriteContext _Context;
        /// <summary>上下文</summary>
        public ReadWriteContext Context
        {
            get { return _Context; }
            set { _Context = value; }
        }
        #endregion

        #region 扩展属性
        private List<ProtocolTreeNode> _Nodes;
        /// <summary>子节点</summary>
        public List<ProtocolTreeNode> Nodes
        {
            get
            {
                if (_Nodes == null) _Nodes = new List<ProtocolTreeNode>();
                return _Nodes;
            }
        }

        /// <summary>
        /// 路径
        /// </summary>
        public String Path
        {
            get
            {
                if (Parent == null)
                    return Name;
                else
                {
                    String str = Parent.Path;
                    if (String.IsNullOrEmpty(str)) return Name;
                    return str + @"." + Name;
                }
            }
        }

        /// <summary>
        /// 顶级节点
        /// </summary>
        public ProtocolTreeNode Top
        {
            get
            {
                return Parent == null ? this : Parent.Top;
            }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public ProtocolTreeNode(String name, Type type)
        {
            Name = name;
            Type = type;
        }
        #endregion

        #region 方法
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public ProtocolTreeNode Add(String name, Type type)
        {
            ProtocolTreeNode node = new ProtocolTreeNode(name, type);
            node.Depth = this.Depth + 1;
            node.Parent = this;
            Nodes.Add(node);
            return node;
        }
        #endregion

        #region 已重载
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Path;
        }
        #endregion
    }
}
