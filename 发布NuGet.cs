//assembly=DLL\NuGet.exe
//assembly=System.ComponentModel.DataAnnotations
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using NewLife.Log;
using NewLife.Xml;
using NuGet;

namespace NewLife.Reflection
{
    public class ScriptEngine
    {
        static void Main()
        {
            //PathHelper.BaseDirectory = @"E:\X\Src\NewLife.Cube";
            XTrace.Debug = true;
            XTrace.UseConsole();
			
			// 查找NuGet.exe
			var ng = "..\\DLL\\NuGet.exe".GetFullPath();
			
            //"cmd".Run("/c del *.nuspec /f/q");
			foreach(var item in ".".AsDirectory().GetAllFiles("*.nuspec"))
			{
				Console.WriteLine("删除 {0}", item);
				item.Delete();
            }
            // 找到名称
            var proj = ".".AsDirectory().FullName.EnsureEnd("\\");

            Console.WriteLine("proj项目：{0}", proj);
            string[] pathsplit = proj.Split("\\");

            var name = pathsplit[pathsplit.Count() - 1];
            Console.WriteLine("项目：{0}", name);
            proj = name + ".csproj";
            var spec = name + ".nuspec";
			var specFile = spec.GetFullPath();
            
            if (!File.Exists(specFile))
            {
				var tar = "..\\..\\Bin\\" + name + ".dll";
				tar = tar.GetFullPath();
                if (!File.Exists(tar))
                {
					tar = "..\\..\\Bin4\\" + name + ".exe";
					tar = tar.GetFullPath();
                }
                if (!File.Exists(tar))
                {
					tar = "..\\..\\XCoder\\" + name + ".exe";
					tar = tar.GetFullPath();
                }
                if (!File.Exists(tar))
                {
					Console.WriteLine("只能找项目文件了，总得做点啥不是");
					//编译当前工程
					"msbuild".Run(proj + " /t:Rebuild /p:Configuration=Release /p:VisualStudioVersion=15.0 /noconlog /nologo", 8000);
					//"NuGet".Run("spec -f -a " + name, 5000);
					return;
                }
				Console.WriteLine("目标 {0}", tar);
				ng.Run("spec -Force -a " + tar, 5000);
                /*var sCmd = new NuGet.CommandLine.SpecCommand();
                sCmd.Force = true;
                sCmd.AssemblyPath = tar;
                sCmd.ExecuteCommand();*/
				
                var spec2 = ".".AsDirectory().GetAllFiles(spec).First().Name;
                if (!spec.EqualIgnoreCase(spec2)) File.Move(spec2, spec);
            }

            // 部分项目加上前缀
            var name2 = name.EnsureStart("NewLife.");

			var ms = new MemoryStream(File.ReadAllBytes(specFile));
            var cfg = Manifest.ReadFrom(ms, false);
            // 修改配置文件
            cfg.Metadata.Id = name2;
            cfg.Metadata.LicenseUrl = "http://www.NewLifeX.com";
            cfg.Metadata.ProjectUrl = "https://github.com/NewLifeX";
            cfg.Metadata.IconUrl = "http://www.NewLifeX.com/favicon.ico";
            cfg.Metadata.Copyright = "Copyright 2002-{0} 新生命开发团队 http://www.NewLifeX.com".F(DateTime.Now.Year);
            cfg.Metadata.Tags = "新生命团队 X组件 NewLife " + name;
            cfg.Metadata.ReleaseNotes = "https://github.com/NewLifeX";
            //cfg.Metadata.Authors="新生命开发团队";
            //cfg.Metadata.Owners="新生命开发团队";

            /*var rep = new RepositoryMetadata();
            rep.Type = "git";
            rep.Url = "https://github.com/NewLifeX/X.git";
            cfg.Metadata.Repository = rep;*/
            
            // 清空依赖
            var dgs = cfg.Metadata.DependencySets;
            if (dgs != null) dgs.Clear();
            //var dgs = cfg.Metadata.DependencyGroups;
            //dps.RemoveAll(e => e.Id == "SampleDependency");

            //var fx = new NuGet.Frameworks.NuGet.Frameworks1094018.NuGetFramework(".NETFramework4.5");
            //var dg = new PackageDependencyGroup(fx, new PackageDependency[0]);
            //dgs.Add(dk);

            // 自动添加所有文件
            if (cfg.Files == null) cfg.Files = new List<ManifestFile>();
            cfg.Files.Clear();
            if (cfg.Files.Count == 0)
            {
                AddFile(cfg, name, "dll;xml;pdb;exe", @"..\..\Bin", @"lib\net45");
                AddFile(cfg, name, "dll;xml;pdb;exe", @"..\..\Bin2", @"lib\net20");
                AddFile(cfg, name, "dll;xml;pdb;exe", @"..\..\Bin4", @"lib\net40");
                AddFile(cfg, name, "dll;xml;pdb;exe", @"..\..\Bin\netstandard2.0", @"lib\netstandard2.0");

                if (name == "XCode") AddFile(cfg, null, "*.ps1", @"tools", @"tools");
            }

			ms = new MemoryStream();
			cfg.Save(ms);
			File.WriteAllBytes(specFile, ms.ToArray());

            //var pack = "pack {0} -IncludeReferencedProjects -Build -Prop Configuration={1} -Exclude **\\*.txt;**\\*.png;content\\*.xml";
            // *\\*.*干掉下级的所有文件
            var pack = "pack {0} -IncludeReferencedProjects -Exclude **\\*.txt;**\\*.png;*.jpg";
            Console.WriteLine("打包：{0}", proj);
            //"cmd".Run("/c del *.nupkg /f/q");
			foreach(var item in ".".AsDirectory().GetAllFiles("*.nupkg"))
			{
				Console.WriteLine("删除 {0}", item);
				item.Delete();
			}
            ng.Run(pack.F(proj), 30000);
            var fi = ".".AsDirectory().GetAllFiles("*.nupkg").FirstOrDefault();
            if (fi != null)
            {
                var nupkg = fi.Name;
                Console.WriteLine("发布：{0}", nupkg);
                ng.Run("push {0} {1} -Source https://www.nuget.org -Verbosity detailed".F(nupkg, File.ReadAllText("..\\..\\nuget.key")), 30000);
            }
        }

        //static ManifestDependency _md;
        static void AddFile(Manifest cfg, String name, String exts, String src, String target)
        {
			if (!name.IsNullOrEmpty()) exts = exts.Split(";").Select(e=>name + "." + e).Join(";");
            var fs = src.AsDirectory().GetAllFiles(exts).ToList();
            //XTrace.WriteLine("目录：{0} 文件：{1}", src.AsDirectory().FullName, fs.Count);
            if(fs.Count == 0) return;
            
            var dgs = cfg.Metadata.DependencySets;
            var dg = new ManifestDependencySet();
            switch(target.Substring(@"\"))
            {
                case "net20": dg.TargetFramework = ".NETFramework2.0"; break;
                case "net40": dg.TargetFramework = ".NETFramework4.0"; break;
                case "net45": dg.TargetFramework = ".NETFramework4.5"; break;
                case "netstandard2.0": dg.TargetFramework = ".NETStandard2.0"; break;
            }

            dg.Dependencies = new List<ManifestDependency>();

            /*if(_md == null)
            {
                var md = new ManifestDependency();
                md.Id = "NewLife.Core";
                md.Version = "7.0.6706.20578";
                md.Exclude = "Build,Analyzers";

                _md = md;
                dg.Dependencies.Add(md);
            }*/

            if(dgs !=null && !dg.TargetFramework.IsNullOrEmpty()) dgs.Add(dg);

            XTrace.WriteLine("目录：{0} 框架：{1} 文件：{2}", src, dg.TargetFramework, fs.Count);

			foreach(var item in fs)
			{
				var mf = new ManifestFile();
                mf.Source = item.FullName;
                mf.Target = @"{0}\{1}".F(target, item.Name);
				cfg.Files.Add(mf);
			}
        }
    }
}