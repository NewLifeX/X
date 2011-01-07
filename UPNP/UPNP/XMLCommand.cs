using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.UPNP
{
    public class XMLCommand
    {
        /// <summary>
        /// 增加端口映射命令
        /// </summary>
        /// <param name="RemoteHost">远程主机</param>
        /// <param name="ExternalPort">外部端口</param>
        /// <param name="Protocol">TCP或UDP</param>
        /// <param name="InternalPort">内部端口</param>
        /// <param name="NewInternalClient">本地IP地址</param>
        /// <param name="NewEnabled">是否启用[0,1]</param>
        /// <param name="Description">端口映射的描述</param>
        /// <param name="Duration">映射的持续时间，用0表示不永久</param>
        /// <returns></returns>
        public static String Add(String RemoteHost, Int32 ExternalPort, String Protocol, Int32 InternalPort, String InternalClient, Int32 Enabled, String Description, int? Duration)
        {
            return Command(CommandEnumTXT.添加, new object[] { RemoteHost, ExternalPort, Protocol, InternalPort, InternalClient, Enabled, Description, Duration });
        }

        /// <summary>
        /// 删除端口映射命令
        /// </summary>
        /// <param name="RemoteHost">远程主机</param>
        /// <param name="ExternalPort">外部端口</param>
        /// <param name="Protocol">TCP或UDP</param>
        /// <returns></returns>
        public static String Del(String RemoteHost, Int32 ExternalPort, String Protocol)
        {

            return Command(CommandEnumTXT.删除, new object[] { RemoteHost, ExternalPort, Protocol });
        }

        /// <summary>
        /// 根据索引获得端口映射信息命令
        /// </summary>
        /// <param name="PortMappingIndex">索引</param>
        /// <returns></returns>
        //public static String GetMap(int? PortMappingIndex, String RemoteHost, int? ExternalPort, String Protocol, int? InternalPort, String InternalClient, int? Enabled, String Description, int? Duration)
        public static String GetMapByIndex(Int32 PortMappingIndex)
        {

            return Command(CommandEnumTXT.索引获取映射列表, new object[] { PortMappingIndex });

        }
        
        /// <summary>
        /// 获得端口映射信息命令
        /// </summary>
        /// <param name="RemoteHost">远程主机</param>
        /// <param name="ExternalPort">外部端口</param>
        /// <param name="Protocol">TCP/UDP</param>
        /// <returns></returns>
        public static String GetMapByPortAndProtocol(String RemoteHost, Int32 ExternalPort, String Protocol)
        {

            return Command(CommandEnumTXT.外部端口与协议获取映射列表, new object[] { RemoteHost, ExternalPort, Protocol });

        }

        /// <summary>
        /// 指令
        /// </summary>
        /// <param name="commandEnum"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private static String Command(CommandEnumTXT commandEnum, object[] parameter)
        {
            //如果IGD没有信息,触发Search
            if (UPNP.IGD == null)
                UPNP.SearchUPNP();
                //UPNP.Test_DownLoadAndSerializer();
            StringBuilder SB = new StringBuilder();
            String Xmlns = UPNP.IGD.device.deviceList[0].deviceList[0].serviceList[0].serviceType;
            String ControlURL = UPNP.IGD.device.deviceList[0].deviceList[0].serviceList[0].controlURL;
            String Host = UPNP.IGD.ServerHOST;
            String CommandStr = null;
            switch (commandEnum)
            {
                case CommandEnumTXT.添加:
                    CommandStr = CommandEnum.AddPortMapping.ToString();
                    SB.AppendLine("<u:" + CommandStr + " xmlns:u= \"" + Xmlns + "\">");
                    SB.AppendLine("<NewRemoteHost>" + parameter[0] + "</NewRemoteHost>");
                    SB.AppendLine("<NewExternalPort>" + parameter[1] + "</NewExternalPort>");
                    SB.AppendLine("<NewProtocol>" + parameter[2] + "</NewProtocol>");
                    SB.AppendLine("<NewInternalPort>" + parameter[3] + "</NewInternalPort>");
                    SB.AppendLine("<NewInternalClient>" + parameter[4] + "</NewInternalClient>");
                    SB.AppendLine("<NewEnabled>" + parameter[5] + "</NewEnabled>");
                    SB.AppendLine("<NewPortMappingDescription>" + parameter[6] + "</NewPortMappingDescription>");
                    SB.AppendLine("<NewLeaseDuration>" + parameter[7] + "</NewLeaseDuration>");
                    SB.AppendLine("</u:" + CommandStr + ">");
                    break;
                case CommandEnumTXT.删除:
                    CommandStr = CommandEnum.DeletePortMapping.ToString();
                    SB.AppendLine("<u:" + CommandStr + " xmlns:u= \"" + Xmlns + "\">");
                    SB.AppendLine("<NewRemoteHost>" + parameter[0] + "</NewRemoteHost>");
                    SB.AppendLine("<NewExternalPort>" + parameter[1] + "</NewExternalPort>");
                    SB.AppendLine("<NewProtocol>" + parameter[2] + "</NewProtocol>");
                    SB.AppendLine("</u:" + CommandStr + ">");
                    break;
                case CommandEnumTXT.索引获取映射列表:
                    CommandStr = CommandEnum.GetGenericPortMappingEntry.ToString();
                    SB.AppendLine("<u:" + CommandStr + " xmlns:u= \"" + Xmlns + "\">");
                    SB.AppendLine("<NewPortMappingIndex>" + parameter[0] + "</NewPortMappingIndex>");
                    SB.AppendLine("<NewRemoteHost></NewRemoteHost>");
                    SB.AppendLine("<NewExternalPort></NewExternalPort>");
                    SB.AppendLine("<NewProtocol></NewProtocol>");
                    SB.AppendLine("<NewInternalPort></NewInternalPort>");
                    SB.AppendLine("<NewInternalClient></NewInternalClient>");
                    SB.AppendLine("<NewEnabled></NewEnabled>");
                    SB.AppendLine("<NewPortMappingDescription></NewPortMappingDescription>");
                    SB.AppendLine("<NewLeaseDuration></NewLeaseDuration>");
                    SB.AppendLine("</u:" + CommandStr + ">");
                    break;
                case CommandEnumTXT.外部端口与协议获取映射列表:
                    CommandStr = CommandEnum.GetSpecificPortMappingEntry.ToString();
                    SB.AppendLine("<u:" + CommandStr + " xmlns:u= \"" + Xmlns + "\">");
                    SB.AppendLine("<NewRemoteHost>" + parameter[0] + "</NewRemoteHost>");
                    SB.AppendLine("<NewExternalPort>" + parameter[1] + "</NewExternalPort>");
                    SB.AppendLine("<NewProtocol>" + parameter[2] + "</NewProtocol>");
                    SB.AppendLine("<NewInternalPort></NewInternalPort>");
                    SB.AppendLine("<NewInternalClient></NewInternalClient>");
                    SB.AppendLine("<NewEnabled></NewEnabled>");
                    SB.AppendLine("<NewPortMappingDescription></NewPortMappingDescription>");
                    SB.AppendLine("<NewLeaseDuration></NewLeaseDuration>");
                    SB.AppendLine("</u:" + CommandStr + ">");
                    break;

            }

            return XMLFormat(SB.ToString(), Xmlns, CommandStr, ControlURL, Host);
        }

        public enum CommandEnumTXT
        {
            添加,
            删除,
            索引获取映射列表,
            外部端口与协议获取映射列表
        }

        public enum CommandEnum
        {
            AddPortMapping,
            DeletePortMapping,
            GetGenericPortMappingEntry,
            GetSpecificPortMappingEntry
        }

        public static String XMLFormat(String command, String Xmlns, String CommandStr, String ControlURL, String Host)
        {
            StringBuilder XMLStruct = new StringBuilder();
            XMLStruct.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            XMLStruct.AppendLine("<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            XMLStruct.AppendLine("<s:Body>");
            XMLStruct.Append(command);
            XMLStruct.AppendLine("</s:Body>");
            XMLStruct.AppendLine("</s:Envelope>");

            String XMLStr = XMLStruct.ToString();

            StringBuilder Head = new StringBuilder();
            Head.AppendLine("POST /ipc HTTP/1.1");
            Head.AppendLine("HOST: " + Host);
            Head.AppendLine("SOAPACTION: \"" + Xmlns + "#" + CommandStr + "\"");
            Head.AppendLine("CONTENT-TYPE: text/xml ; charset=\"utf-8\"");
            Head.AppendLine("Content-Length: " + XMLStr.Length);
            Head.AppendLine();
            Head.AppendLine();
            Head.Append(XMLStr);

            return Head.ToString();
        }

        /// <summary>
        /// UPNP请求头
        /// </summary>
        public static String UPNPSearch()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("M-SEARCH * HTTP/1.1");
            sb.AppendLine("HOST: 239.255.255.250:1900");
            sb.AppendLine("MAN: \"ssdp:discover\"");
            sb.AppendLine("MX: 3");
            sb.AppendLine("ST: UPnp:rootdevice");
            sb.AppendLine();
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
