using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Web.UI;
using System.Web;
using System.Xml.XPath;
using System.Xml.Xsl;

[assembly: WebResource("NewLife.Core.Web.Menu.RS.tree.js", "text/javascript", PerformSubstitution = true)]
[assembly: WebResource("NewLife.Core.Web.Menu.RS.XSL.xsl", "text/xml", PerformSubstitution = true)]

[assembly: WebResource("NewLife.Core.Web.Menu.RS.book.gif", "image/gif")]
[assembly: WebResource("NewLife.Core.Web.Menu.RS.bookopen.gif", "image/gif")]
[assembly: WebResource("NewLife.Core.Web.Menu.RS.paper.gif", "image/gif")]


namespace NewLife
{
    /// <summary>
    /// 树菜单根
    /// </summary>
    [Serializable]
    public class TreeMenuRoot
    {
        private List<TreeMenuNode> _Nodes;
        /// <summary>节点</summary>
        public List<TreeMenuNode> Nodes
        {
            get { return _Nodes; }
            set { _Nodes = value; }
        }

        #region 资源
        /// <summary>
        /// 默认Xsl
        /// </summary>
        public static String xslFilePath = Page().ClientScript.GetWebResourceUrl(typeof(TreeMenuRoot), "NewLife.Core.Web.Menu.RS.XSL.xsl").Replace("&", "&amp;");

        /// <summary>
        /// 菜单js资源路径
        /// </summary>
        public static String jsFilePath = Page().ClientScript.GetWebResourceUrl(typeof(TreeMenuRoot), "NewLife.Core.Web.Menu.RS.tree.js").Replace("&", "&amp;");

        /// <summary>
        /// 关闭
        /// </summary>
        public static String ImagePath = Page().ClientScript.GetWebResourceUrl(typeof(TreeMenuRoot), "NewLife.Core.Web.Menu.RS.book.gif");

        /// <summary>
        /// 打开
        /// </summary>
        public static String ImageOpenPath = Page().ClientScript.GetWebResourceUrl(typeof(TreeMenuRoot), "NewLife.Core.Web.Menu.RS.bookopen.gif");

        /// <summary>
        /// 无子级
        /// </summary>
        public static String ImagePagePath = Page().ClientScript.GetWebResourceUrl(typeof(TreeMenuRoot), "NewLife.Core.Web.Menu.RS.paper.gif");


        #endregion

        /// <summary>
        /// 构造方法
        /// </summary>
        public TreeMenuRoot()
        {
        }

        #region 方法
        /// <summary>
        /// 用于获取资源URL
        /// </summary>
        /// <returns></returns>
        private static Page Page()
        {
            return HttpContext.Current.Handler as Page ?? new Page();
        }

        /// <summary>
        /// 获取菜单树
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rootNode"></param>
        /// <param name="getChildsfun"></param>
        /// <param name="convertToTreeMenu"></param>
        /// <returns></returns>
        private static List<TreeMenuNode> GetTreeMenu<T>(T rootNode, NewLife.Reflection.Func<T, List<T>> getChildsfun, NewLife.Reflection.Func<T, TreeMenuNode> convertToTreeMenu)
        {
            //Root root = new Root();
            List<TreeMenuNode> r = new List<TreeMenuNode>();

            List<T> list = getChildsfun(rootNode);

            if (list != null && list.Count > 0)
                foreach (T item in list)
                {
                    TreeMenuNode menu = convertToTreeMenu(item);
                    if (menu != null)
                    {
                        r.Add(menu);
                        menu.Childs = GetTreeMenu<T>(item, getChildsfun, convertToTreeMenu);

                        menu.ResetImage();
                    }
                }


            return r.Count > 0 ? r : null;
        }

        /// <summary>
        /// 通过根节点获取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rootNode"></param>
        /// <param name="getChildsfun"></param>
        /// <param name="convertToTreeMenu"></param>
        /// <returns></returns>
        public static TreeMenuRoot GetTreeMenuRoot<T>(T rootNode, NewLife.Reflection.Func<T, List<T>> getChildsfun, NewLife.Reflection.Func<T, TreeMenuNode> convertToTreeMenu)
        {
            TreeMenuRoot root = new TreeMenuRoot();
            root.Nodes = GetTreeMenu<T>(rootNode, getChildsfun, convertToTreeMenu);

            return root;
        }

        /// <summary>
        /// 通过节点列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodeList"></param>
        /// <param name="getChildsfun"></param>
        /// <param name="convertToTreeMenu"></param>
        /// <returns></returns>
        public static TreeMenuRoot GetTreeMenuRoot<T>(List<T> nodeList, NewLife.Reflection.Func<T, List<T>> getChildsfun, NewLife.Reflection.Func<T, TreeMenuNode> convertToTreeMenu)
        {
            TreeMenuRoot root = new TreeMenuRoot();
            root.Nodes = new List<TreeMenuNode>();
            if (nodeList != null && nodeList.Count > 0)
                foreach (T item in nodeList)
                {
                    TreeMenuNode menu = convertToTreeMenu(item);
                    if (menu != null)
                    {
                        root.Nodes.Add(menu);
                        menu.Childs = GetTreeMenu<T>(item, getChildsfun, convertToTreeMenu);

                        menu.ResetImage();
                    }
                }

            return root;
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <returns></returns>
        public XmlDocument Serialize()
        {
            return Serialize(xslFilePath);
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="xsl">xsl文件</param>
        /// <returns></returns>
        public XmlDocument Serialize(String xsl)
        {
            XmlDocument xml = new XmlDocument();

            try
            {
                MemoryStream stream = new MemoryStream();
                XmlSerializer xs = new XmlSerializer(this.GetType());
                xs.Serialize(stream, this);

                stream.Position = 0;

                xml.Load(stream);
            }
            catch { }

            if (!String.IsNullOrEmpty(xsl))
            {
                //插入xsl文件引用
                XmlProcessingInstruction xmlXsl = xml.CreateProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"" + xsl + "\"");
                xml.InsertAfter(xmlXsl, xml.ChildNodes[0]);
            }

            return xml;
        }

        /// <summary>
        /// 转换为Html
        /// 使用默认xsl
        /// </summary>
        /// <param name="isAddTreeJS"></param>
        /// <returns></returns>
        public String ToHtml(bool isAddTreeJS)
        {
            HttpContext hc = HttpContext.Current;

            if (hc == null)
                throw new Exception("获取HttpContext.Current失败，不能把资源地址转换！");

            String url = String.Format("{0}://{1}{2}", hc.Request.Url.Scheme, hc.Request.Url.Authority, xslFilePath);

            return ToHtml(url, true);
        }

        /// <summary>
        /// 转换为Html
        /// </summary>
        /// <param name="xsl"></param>
        /// <param name="isAddTreeJS"></param>
        /// <returns></returns>
        public String ToHtml(String xsl, bool isAddTreeJS)
        {
            using (TextWriter stringWriter = new StringWriter())
            {
                if (isAddTreeJS)
                    stringWriter.WriteLine("<script src=\"" + jsFilePath + "\" type=\"text/javascript\"></script>");

                using (TextReader stringReader = new StringReader(Serialize(null).InnerXml))
                {
                    using (XmlReader read = XmlReader.Create(stringReader))
                    {

                        XslCompiledTransform xsltransform = new XslCompiledTransform();

                        xsltransform.Load(xsl);

                        xsltransform.Transform(read, null, stringWriter);
                    }
                }
                return stringWriter.ToString();
            }
        }
        #endregion

    }


    /// <summary>
    /// Xml树状菜单节点
    /// </summary>
    [Serializable]
    public class TreeMenuNode
    {
        #region 菜单属性
        private String _ID;
        /// <summary>菜单项ID</summary>
        [XmlAttribute]
        public String ID
        {
            get { return _ID; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    _ID = Guid.NewGuid().ToString();
                _ID = String.Format("treeNode_{0}", value);
            }
        }

        private String _Title;
        /// <summary>菜单名</summary>
        public String Title
        {
            get { return _Title; }
            set { _Title = value; }
        }

        private String _Image;
        /// <summary>前缀图标 关闭状态</summary>
        public String Image
        {
            get { return _Image; }
            set { _Image = value; }
        }

        private String _ImageOpen;
        /// <summary>前缀图片 关闭状态</summary>
        public String ImageOpen
        {
            get { return _ImageOpen; }
            set { _ImageOpen = value; }
        }

        private String _Url;
        /// <summary>菜单连接</summary>
        public String Url
        {
            get { return _Url; }
            set { _Url = value; }
        }

        private List<TreeMenuNode> _Childs;
        /// <summary>子级</summary>
        public List<TreeMenuNode> Childs
        {
            get { return _Childs; }
            set { _Childs = value; }
        }
        #endregion

        /// <summary>
        /// 构造方法
        /// </summary>
        public TreeMenuNode()
        {
        }

        #region 扩展方法
        /// <summary>
        /// 设置默认图片
        /// </summary>
        /// <returns></returns>
        public TreeMenuNode ResetImage()
        {
            return ResetTreeNode(this);
        }

        /// <summary>
        /// 设置默认数据
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static TreeMenuNode ResetTreeNode(TreeMenuNode node)
        {
            if (node == null) return null;

            if (node.Childs != null && node.Childs.Count > 0)
            {
                if (String.IsNullOrEmpty(node.Image))
                    node.Image = TreeMenuRoot.ImagePath;
                if (String.IsNullOrEmpty(node.ImageOpen))
                    node.ImageOpen = TreeMenuRoot.ImageOpenPath;
            }
            else
            {
                if (String.IsNullOrEmpty(node.Image))
                    node.Image = TreeMenuRoot.ImagePagePath;
                if (String.IsNullOrEmpty(node.ImageOpen))
                    node.ImageOpen = TreeMenuRoot.ImagePagePath;
            }

            return node;
        }
        #endregion

    }
}
