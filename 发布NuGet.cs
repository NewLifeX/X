using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using NewLife.Xml;
using NewLife.Log;

namespace NewLife.Reflection
{
    public class ScriptEngine
    {
        static void Main()
        {
            //PathHelper.BaseDirectory = @"C:\X\Src\NewLife.Core";
            XTrace.Debug = true;
            XTrace.UseConsole();

            // 找到名称
            var proj = ".".AsDirectory().GetAllFiles("*.csproj").First().Name;
            var name = Path.GetFileNameWithoutExtension(proj);
            Console.WriteLine("项目：{0}", proj);

            var spec = name + ".nuspec";
            if (!File.Exists(spec.GetFullPath())) "NuGet".Run("spec", 5000);

            // 部分项目加上前缀
            var name2 = name.EnsureStart("NewLife.");

            var cfg = Manifest.Load(spec.GetFullPath());

            // 修改配置文件
            cfg.Metadata.Id = name2;
            cfg.Metadata.LicenseUrl = "http://www.NewLifeX.com";
            cfg.Metadata.ProjectUrl = "http://www.NewLifeX.com/showtopic-51.aspx";
            cfg.Metadata.IconUrl = "http://www.NewLifeX.com/favicon.ico";
            cfg.Metadata.Copyright = "Copyright 2002-{0} 新生命开发团队 http://www.NewLifeX.com".F(DateTime.Now.Year);
            cfg.Metadata.Tags = "新生命团队 X组件 NewLife";
            cfg.Metadata.ReleaseNotes = "http://www.newlifex.com/showtopic-51.aspx";

            // 自动添加所有文件
            if (cfg.Files == null) cfg.Files = new List<ManifestFile>();
            if (cfg.Files.Count == 0)
            {
                AddFile(cfg, name, "dll");
                AddFile(cfg, name, "xml");
                AddFile(cfg, name, "pdb");
                AddFile(cfg, name, "exe");

                AddFile(cfg, name, "dll", false);
                AddFile(cfg, name, "xml", false);
                AddFile(cfg, name, "pdb", false);
                AddFile(cfg, name, "exe", false);
            }

            cfg.Save();

            //var pack = "pack {0} -IncludeReferencedProjects -Build -Prop Configuration={1} -Exclude **\\*.txt;**\\*.png;content\\*.xml";
            // *\\*.*干掉下级的所有文件
            var pack = "pack {0} -IncludeReferencedProjects -Exclude **\\*.txt;**\\*.png;*.jpg;*.xml;*\\*.*";
            Console.WriteLine("打包：{0}", proj);
            "cmd".Run("/c del *.nupkg /f/q");
            "NuGet".Run(pack.F(proj, "Release"), 30000);
            var fi = ".".AsDirectory().GetAllFiles("*.nupkg").FirstOrDefault();
            if (fi != null)
            {
                var nupkg = fi.Name;
                Console.WriteLine("发布：{0}", nupkg);
                "NuGet".Run("push {0}".F(nupkg), 30000);
            }
        }

        static void AddFile(Manifest cfg, String name, String ext, Boolean fx2 = true)
        {
            var mf = new ManifestFile();

            if (fx2)
            {
                mf.Source = @"..\..\Bin\{0}.{1}".F(name, ext);
                mf.Target = @"lib\net20\{0}.{1}".F(name, ext);
            }
            else
            {
                mf.Source = @"..\..\Bin4\{0}.{1}".F(name, ext);
                mf.Target = @"lib\net40\{0}.{1}".F(name, ext);
            }
            if (File.Exists(mf.Source.GetFullPath())) cfg.Files.Add(mf);
        }
    }

    [XmlType("package")]
    public class Manifest : XmlConfig<Manifest>
    {
        [XmlElement("metadata", IsNullable = false)]
        public ManifestMetadata Metadata { get; set; }

        [XmlArray("files")]
        public List<ManifestFile> Files { get; set; }

        public Manifest()
        {
            this.Metadata = new ManifestMetadata();
        }
    }

    [XmlType("file")]
    public class ManifestFile
    {
        [XmlAttribute("src")]
        public string Source { get; set; }

        [XmlAttribute("target")]
        public string Target { get; set; }

        [XmlAttribute("exclude")]
        public string Exclude { get; set; }
    }

    [XmlType("metadata")]
    public class ManifestMetadata
    {
        [XmlAttribute("minClientVersion")]
        public string MinClientVersionString { get; set; }

        [XmlElement("id")]
        public string Id { get; set; }

        [XmlElement("version")]
        public string Version { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("authors")]
        public string Authors { get; set; }

        [XmlElement("owners")]
        public string Owners { get; set; }

        [XmlElement("licenseUrl")]
        public string LicenseUrl { get; set; }

        [XmlElement("projectUrl")]
        public string ProjectUrl { get; set; }

        [XmlElement("iconUrl")]
        public string IconUrl { get; set; }

        [XmlElement("requireLicenseAcceptance")]
        public bool RequireLicenseAcceptance { get; set; }

        [XmlElement("developmentDependency")]
        public bool DevelopmentDependency { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("summary")]
        public string Summary { get; set; }

        [XmlElement("releaseNotes")]
        public string ReleaseNotes { get; set; }

        [XmlElement("copyright")]
        public string Copyright { get; set; }

        [XmlElement("language")]
        public string Language { get; set; }

        [XmlElement("tags")]
        public string Tags { get; set; }

        [XmlElement("dependencies")]
        public ManifestDependencySet DependencySets { get; set; }

        [XmlElement("frameworkAssemblies")]
        public List<ManifestFrameworkAssembly> FrameworkAssemblies { get; set; }

        [XmlElement("references")]
        public ManifestReferenceSet ReferenceSets { get; set; }
    }

    public class ManifestDependencySet
    {
        [XmlAttribute("targetFramework")]
        public string TargetFramework { get; set; }

        [XmlElement("dependency")]
        public List<ManifestDependency> Dependencies { get; set; }
    }

    [XmlType("dependency")]
    public class ManifestDependency
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("version")]
        public string Version { get; set; }
    }

    [XmlType("frameworkAssembly")]
    public class ManifestFrameworkAssembly
    {
        [XmlAttribute("assemblyName")]
        public string AssemblyName { get; set; }

        [XmlAttribute("targetFramework")]
        public string TargetFramework { get; set; }
    }

    public class ManifestReferenceSet
    {
        [XmlAttribute("targetFramework")]
        public string TargetFramework { get; set; }

        [XmlElement("reference")]
        public List<ManifestReference> References { get; set; }
    }

    [XmlType("reference")]
    public class ManifestReference
    {
        [XmlAttribute("file")]
        public string File { get; set; }
    }
}