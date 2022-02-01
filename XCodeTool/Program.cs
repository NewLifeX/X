using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NewLife;
using NewLife.Log;
using XCode.Code;

namespace XCodeTool
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.UseConsole();

            //if (args.Length == 0)
            {
                Console.WriteLine("NewLife.XCode v{0}", Assembly.GetExecutingAssembly().GetName().Version);
                Console.WriteLine("Usage: xcode model.xml");
                Console.WriteLine();
                //Console.WriteLine("commands:");
                //Console.WriteLine("\tentity\t\tGenerate entity class");
                //Console.WriteLine("\tmodel\t\tGenerate model class");
                //Console.WriteLine("\tinterface\tGenerate interface");
                //Console.WriteLine();
                //Console.WriteLine("options:");
                //Console.WriteLine("\t-output <PATH>\t\t输出目录");
                //Console.WriteLine("\t-baseClass <NAME>\t\t基类。可能包含基类和接口，其中{name}替换为Table.Name");
                //Console.WriteLine("\t-classNameTemplate <NAME>\t类名模板。其中{name}替换为Table.Name，如{name}Model/I{name}Dto等");
                //Console.WriteLine("\t-modelNameForCopy <NAME>\t用于生成拷贝函数的模型类。例如{name}或I{name}");
            }
            if (args.Length > 0)
            {
                var file = args.LastOrDefault();
                if (!Path.IsPathRooted(file))
                {
                    var file2 = Environment.CurrentDirectory.CombinePath(file);
                    if (File.Exists(file2)) file = file2;
                }
                if (!File.Exists(file))
                {
                    Console.WriteLine("文件不存在：{0}", file);
                    return;
                }

                Build(file);
            }
        }

        static void Build(String modelFile)
        {
            Console.WriteLine("正在处理：{0}", modelFile);

            EntityBuilder.Debug = true;

            // 设置当前工作目录
            PathHelper.BasePath = Path.GetDirectoryName(modelFile);

            // 设置如何格式化字段名，默认去掉下划线并转驼峰命名
            //ModelResolver.Current = new ModelResolver { TrimUnderline = false, Camel = false };

            // 加载模型文件，得到数据表
            var option = new BuilderOption();
            var tables = ClassBuilder.LoadModels(modelFile, option, out var atts);
            EntityBuilder.FixModelFile(modelFile, option, atts, tables);

            // 简易模型类名称，如{name}Model。指定后将生成简易模型类和接口，可用于数据传输
            var modelClass = atts["ModelClass"];
            var modelInterface = atts["ModelInterface"];

            // 生成实体类
            if (!modelInterface.IsNullOrEmpty())
            {
                option.BaseClass = modelInterface;
                option.ModelNameForCopy = modelInterface;
            }
            else if (!modelClass.IsNullOrEmpty())
            {
                option.ModelNameForCopy = modelClass;
            }
            EntityBuilder.BuildTables(tables, option, chineseFileName: true);

            // 生成简易模型类
            if (!modelClass.IsNullOrEmpty())
            {
                option.Output = @"..\Models\";
                option.ClassNameTemplate = modelClass;
                if (!modelInterface.IsNullOrEmpty()) option.ModelNameForCopy = modelInterface;
                ClassBuilder.BuildModels(tables, option);
            }

            // 生成简易接口
            if (!modelInterface.IsNullOrEmpty())
            {
                option.BaseClass = null;
                option.ClassNameTemplate = null;
                option.Output = @"..\Interfaces\";
                ClassBuilder.BuildInterfaces(tables, option);
            }
        }
    }
}