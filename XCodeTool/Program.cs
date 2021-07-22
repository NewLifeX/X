using System;
using System.Linq;
using System.Reflection;
using NewLife;
using NewLife.Reflection;

namespace XCodeTool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("NewLife.XCode 数据中间件工具，用于代码生成！");
                Console.WriteLine("可用命令：");
            }
            else
            {
                var act = args[0];
                var method = typeof(Program).GetMethod(act, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                //var method = typeof(Program).GetMethodsEx(act, 1).FirstOrDefault();
                if (method == null)
                {
                    Console.WriteLine($"找不到方法 {act}");
                    return;
                }

                method.Invoke(null, new Object[] { args });
            }
        }

        static void Show(String[] args)
        {
            Console.WriteLine("Show");
        }
    }
}
