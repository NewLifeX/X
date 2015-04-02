using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Linq;
using System.IO;
using System.Xml;

namespace NewLife.Reflection
{
    public class ScriptEngine
    {
        static void Main()
        {
            //PathHelper.BaseDirectory = @"E:\X\Src\NewLife.Core";
            // 找到名称
            var proj = ".".AsDirectory().GetAllFiles("*.csproj").First().Name;
            var name = Path.GetFileNameWithoutExtension(proj);
            Console.WriteLine("项目：{0}", proj);

            var spec = name + ".nuspec";
            if (!File.Exists(spec.GetFullPath())) "NuGet".Run("spec", 5000);

            // 部分项目加上前缀
            name = name.EnsureStart("NewLife.");

            // 修改配置文件，区分FX2/FX4
            var doc = new XmlDocument();
            doc.Load(spec.GetFullPath());
            var root = doc.DocumentElement;

            // 处理.Net 2.0
            var node = root.SelectSingleNode("//id");
            var id = node.InnerText;
            node.InnerText = name + ".Fx20";

            node = root.SelectSingleNode("//title");
            var title = node.InnerText;
            //Console.WriteLine(title);
            node.InnerText = title.EnsureEnd(" For .Net 2.0");

            node = root.SelectSingleNode("//licenseUrl");
            node.InnerText = "http://www.NewLifeX.com";
            node = root.SelectSingleNode("//projectUrl");
            node.InnerText = "http://www.NewLifeX.com";
            node = root.SelectSingleNode("//iconUrl");
            node.InnerText = "http://www.NewLifeX.com/favicon.ico";
            node = root.SelectSingleNode("//copyright");
            node.InnerText = "Copyright 2002-{0} 新生命开发团队 http://www.NewLifeX.com".F(DateTime.Now.Year);
            node = root.SelectSingleNode("//tags");
            node.InnerText = "新生命团队 X组件 NewLife";

            node = root.SelectSingleNode("//releaseNotes");
            if (node != null) node.ParentNode.RemoveChild(node);

            doc.Save(spec);

            var pack = "pack {0} -IncludeReferencedProjects -Build -Prop Configuration={1} -Exclude *.txt;*.png";
            Console.WriteLine("打包：{0}", proj);
            "cmd".Run("/c del *.nupkg /f/q");
            "NuGet".Run(pack.F(proj, "Release"), 30000);
            var nupkg = ".".AsDirectory().GetAllFiles("*.nupkg").First().Name;
            Console.WriteLine("发布：{0}", nupkg);
            "NuGet".Run("push {0}".F(nupkg), 30000);

            // 处理.Net 4.0
            node = root.SelectSingleNode("//id");
            node.InnerText = name;

            node = root.SelectSingleNode("//title");
            node.InnerText = title.TrimEnd(" For .Net 2.0");

            doc.Save(spec);

            Console.WriteLine("打包：{0}", proj);
            "cmd".Run("/c del *.nupkg /f/q");
            "NuGet".Run(pack.F(proj, "Net4Release"), 30000);
            nupkg = ".".AsDirectory().GetAllFiles("*.nupkg").First().Name;
            Console.WriteLine("发布：{0}", nupkg);
            "NuGet".Run("push {0}".F(nupkg), 30000);
        }
    }
}