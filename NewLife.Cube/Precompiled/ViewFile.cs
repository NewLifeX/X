using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;

namespace NewLife.Cube.Precompiled
{
    /// <summary>视图文件</summary>
    public class ViewFile : VirtualFile
    {
        private string path;

        /// <summary>实例化视图文件</summary>
        /// <param name="virtualPath"></param>
        public ViewFile(string virtualPath)
            : base(virtualPath)
        {
            path = virtualPath;
        }

        /// <summary>打开视图文件</summary>
        /// <returns></returns>
        public override Stream Open()
        {
            if (string.IsNullOrEmpty(path))
                return new MemoryStream();

            //string content = Falafel.Providers.Pages.GetByVirtualPath(path);
            //if (string.IsNullOrEmpty(content))
            return new MemoryStream();

            //return new MemoryStream(ASCIIEncoding.UTF8.GetBytes(content));
        }
    }
}