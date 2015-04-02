// 找到名称
var proj = ".".AsDirectory().GetAllFiles("*.csproj").First().Name;
var name = Path.GetFileNameWithoutExtension(proj);
Console.WriteLine("项目：{0}", proj);

//var spec = name + ".nuspec";
//if (!File.Exists(spec)) "NuGet".Run("spec");

Console.WriteLine("打包：{0}", proj);
"cmd".Run("/c del *.nupkg /f/q");
"NuGet".Run("pack {0} -Build -Prop Configuration=Release".F(proj), 30000);
var nupkg = ".".AsDirectory().GetAllFiles("*.nupkg").First().Name;
Console.WriteLine("发布：{0}", nupkg);
"NuGet".Run("push {0}".F(nupkg), 30000);

Console.WriteLine("打包：{0}", proj);
"cmd".Run("/c del *.nupkg /f/q");
"NuGet".Run("pack {0} -Build -Prop Configuration=Net4Release".F(proj), 30000);
nupkg = ".".AsDirectory().GetAllFiles("*.nupkg").First().Name;
Console.WriteLine("发布：{0}", nupkg);
"NuGet".Run("push {0}".F(nupkg), 30000);