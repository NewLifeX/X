using System;
using System.Dynamic;
using System.Xml.Linq;

namespace NewLife.Reflection
{
    /// <summary>动态Xml</summary>
    public class DynamicXml : DynamicObject
    {
        /// <summary>节点</summary>
        public XElement Node { get; set; }

        /// <summary>实例化</summary>
        public DynamicXml() { }

        /// <summary>实例化</summary>
        /// <param name="node"></param>
        public DynamicXml(XElement node)
        {
            Node = node;
        }

        /// <summary>实例化</summary>
        /// <param name="name"></param>
        public DynamicXml(String name)
        {
            Node = new XElement(name);
        }

        /// <summary>设置</summary>
        /// <param name="binder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TrySetMember(SetMemberBinder binder, Object value)
        {
            var setNode = Node.Element(binder.Name);
            if (setNode != null)
                setNode.SetValue(value);
            else
            {
                if (value.GetType() == typeof(DynamicXml))
                    Node.Add(new XElement(binder.Name));
                else
                    Node.Add(new XElement(binder.Name, value));
            }
            return true;
        }

        /// <summary>获取</summary>
        /// <param name="binder"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override Boolean TryGetMember(GetMemberBinder binder, out Object result)
        {
            result = null;
            var getNode = Node.Element(binder.Name);
            if (getNode == null) return false;

            result = new DynamicXml(getNode);
            return true;
        }

#if DEBUG
        // 2019-03-10合并为混合项目后一下编译不通过，注释掉
        ///// <summary>测试</summary>
        //public static void Test()
        //{
        //    dynamic xml = new DynamicXml("Test");
        //    xml.Name = "NewLife";
        //    xml.Sign = "学无先后达者为师！";
        //    xml.Detail = new DynamicXml();
        //    xml.Detail.Name = "新生命开发团队";
        //    xml.Detail.CreateTime = new DateTime(2002, 12, 31);

        //    var node = xml.Node as XElement;
        //    var str = node.ToString();
        //    Console.WriteLine(str);
        //}
#endif
    }
}