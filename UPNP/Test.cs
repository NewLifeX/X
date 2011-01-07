using System;
using NewLife.UPNP;

namespace UPNPTest
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine();
            Console.WriteLine("请输入相关命令:Exit,Add,Del,GetMap,GetMapAll");


            while (true)
            {
                String[] Command = Console.ReadLine().Split(new char[] { ' ' });
                Int32 Port;
                switch (Command[0])
                {
                    case "Exit":
                        return;
                    case "Add":
                        if (Command.Length != 4)
                        {
                            Console.WriteLine("Add Port UDP/TCP IP");
                        }
                        else
                            if (!Int32.TryParse(Command[1], out Port))
                                Console.WriteLine("请输入正确的端口号!");
                            else
                                if (UPNP.Add(null, Port, Command[2], Port, Command[3], 1, "whc", 0) == true)
                                    Console.WriteLine("映射成功!");
                                else
                                    Console.WriteLine("映射失败!");
                        break;
                    case "Del":
                        if (Command.Length != 3)
                        {
                            Console.WriteLine("Del Port UDP/TCP");
                        }
                        else
                            if (!Int32.TryParse(Command[1], out Port))
                                Console.WriteLine("请输入正确的端口号!");
                            else
                                if (UPNP.Del(null, Port, Command[2]) == true)
                                    Console.WriteLine("删除映射成功!");
                                else
                                    Console.WriteLine("删除映射失败!");
                        break;
                    case "GetMap":
                        if (Command.Length == 2)
                        {
                            if (!Int32.TryParse(Command[1], out Port))
                                Console.WriteLine("请输入正确的端口号!");
                            else
                            {
                                PortMappingEntry Document = UPNP.GetMapByIndex(Port);
                                if (Document == null)
                                    Console.WriteLine("没有找到相关映射!");
                                else
                                    Console.WriteLine(FormatPortMappingEntry(Document));
                            }
                        }
                        else if (Command.Length == 3)
                        {
                            if (!Int32.TryParse(Command[1], out Port))
                                Console.WriteLine("请输入正确的端口号!");
                            else
                            {
                                PortMappingEntry Document = UPNP.GetMapByPortAndProtocol(null, Port, Command[2]);
                                if (Document == null)
                                    Console.WriteLine("没有找到相关映射!");
                                else
                                    Console.WriteLine(FormatPortMappingEntry(Document));
                            }
                        }
                        else
                        {
                            Console.WriteLine("GetMap Index");
                            Console.WriteLine("GetMap Port UDP/TCP");
                        }
                        break;
                    case "GetMapAll":
                        Console.WriteLine("共有映射{0}个", UPNP.GetMapByIndexAll().Count);
                        foreach (PortMappingEntry Item in UPNP.GetMapByIndexAll())
                        {
                            Console.WriteLine(FormatPortMappingEntry(Item));
                        }
                        break;
                }
            }

            //Console.ReadKey();
        }

        static String FormatPortMappingEntry(PortMappingEntry item)
        {
            return "映射信息:" + item.NewInternalClient + ":" + item.NewInternalPort + "-" + item.NewExternalPort + " " + item.NewProtocol + " " + item.NewPortMappingDescription + " " + item.NewLeaseDuration;
        }


    }
}
