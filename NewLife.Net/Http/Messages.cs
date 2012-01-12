using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Globalization;
using System.IO;

namespace NewLife.Net.Http
{
    internal class Messages
    {
        private const String _dirListingDirFormat = "{0,38:dddd, MMMM dd, yyyy hh:mm tt}        &lt;dir&gt; <A href=\"{1}/\">{2}</A>\r\n";
        private const String _dirListingFileFormat = "{0,38:dddd, MMMM dd, yyyy hh:mm tt} {1,12:n0} <A href=\"{2}\">{3}</A>\r\n";
        private const String _dirListingFormat1 = "<html>\r\n    <head>\r\n    <title>{0}</title>\r\n";
        private const String _dirListingFormat2 = "    </head>\r\n    <body bgcolor=\"white\">\r\n\r\n    <h2> <i>{0}</i> </h2></span>\r\n\r\n            <hr width=100% size=1 color=silver>\r\n\r\n<PRE>\r\n";
        private const String _dirListingParentFormat = "<A href=\"{0}\">[To Parent Directory]</A>\r\n\r\n";
        private static String _dirListingTail = ("</PRE>\r\n            <hr width=100% size=1 color=silver>\r\n\r\n              <b>{0}:</b>&nbsp;{1} " + VersionString + "\r\n\r\n            </font>\r\n\r\n    </body>\r\n</html>\r\n");
        private const String _httpErrorFormat1 = "<html>\r\n    <head>\r\n        <title>{0}</title>\r\n";
        private static String _httpErrorFormat2 = ("    </head>\r\n    <body bgcolor=\"white\">\r\n\r\n            <span><h1>{0}<hr width=100% size=1 color=silver></h1>\r\n\r\n            <h2> <i>{1}</i> </h2></span>\r\n\r\n            <hr width=100% size=1 color=silver>\r\n\r\n            <b>{2}:</b>&nbsp;{3} " + VersionString + "\r\n\r\n            </font>\r\n\r\n    </body>\r\n</html>\r\n");
        private const String _httpStyle = "        <style>\r\n        \tbody {font-family:\"Verdana\";font-weight:normal;font-size: 8pt;color:black;} \r\n        \tp {font-family:\"Verdana\";font-weight:normal;color:black;margin-top: -5px}\r\n        \tb {font-family:\"Verdana\";font-weight:bold;color:black;margin-top: -5px}\r\n        \th1 { font-family:\"Verdana\";font-weight:normal;font-size:18pt;color:red }\r\n        \th2 { font-family:\"Verdana\";font-weight:normal;font-size:14pt;color:maroon }\r\n        \tpre {font-family:\"Lucida Console\";font-size: 8pt}\r\n        \t.marker {font-weight: bold; color: black;text-decoration: none;}\r\n        \t.version {color: gray;}\r\n        \t.error {margin-bottom: 10px;}\r\n        \t.expandable { text-decoration:underline; font-weight:bold; color:navy; cursor:hand; }\r\n        </style>\r\n";

        public static String Name;
        public static String VersionString;
        public static String VersionInfo = "版本信息";

        public static String FormatDirectoryListing(String dirPath, String parentPath, FileSystemInfo[] elements)
        {
            var builder = new StringBuilder();
            String str = "目录列表 -- " + dirPath;
            builder.AppendFormat("<html>\r\n    <head>\r\n    <title>{0}</title>\r\n", str);
            builder.Append("        <style>\r\n        \tbody {font-family:\"Verdana\";font-weight:normal;font-size: 8pt;color:black;} \r\n        \tp {font-family:\"Verdana\";font-weight:normal;color:black;margin-top: -5px}\r\n        \tb {font-family:\"Verdana\";font-weight:bold;color:black;margin-top: -5px}\r\n        \th1 { font-family:\"Verdana\";font-weight:normal;font-size:18pt;color:red }\r\n        \th2 { font-family:\"Verdana\";font-weight:normal;font-size:14pt;color:maroon }\r\n        \tpre {font-family:\"Lucida Console\";font-size: 8pt}\r\n        \t.marker {font-weight: bold; color: black;text-decoration: none;}\r\n        \t.version {color: gray;}\r\n        \t.error {margin-bottom: 10px;}\r\n        \t.expandable { text-decoration:underline; font-weight:bold; color:navy; cursor:hand; }\r\n        </style>\r\n");
            builder.AppendFormat("    </head>\r\n    <body bgcolor=\"white\">\r\n\r\n    <h2> <i>{0}</i> </h2></span>\r\n\r\n            <hr width=100% size=1 color=silver>\r\n\r\n<PRE>\r\n", str);
            if (parentPath != null)
            {
                if (!parentPath.EndsWith("/", StringComparison.Ordinal)) parentPath = parentPath + "/";
                builder.Append(String.Format(CultureInfo.InvariantCulture, "<A href=\"{0}\">[父目录]</A>\r\n\r\n", new object[] { parentPath }));
            }
            if (elements != null)
            {
                for (int i = 0; i < elements.Length; i++)
                {
                    if (elements[i] is FileInfo)
                    {
                        FileInfo info = (FileInfo)elements[i];
                        builder.AppendFormat("{0,38:dddd, MMMM dd, yyyy hh:mm tt} {1,12:n0} <A href=\"{2}\">{3}</A>\r\n", info.LastWriteTime, info.Length, info.Name, info.Name);
                    }
                    else if (elements[i] is DirectoryInfo)
                    {
                        DirectoryInfo info2 = (DirectoryInfo)elements[i];
                        builder.AppendFormat("{0,38:dddd, MMMM dd, yyyy hh:mm tt}        &lt;dir&gt; <A href=\"{1}/\">{2}</A>\r\n", info2.LastWriteTime, info2.Name, info2.Name);
                    }
                }
            }
            builder.AppendFormat(_dirListingTail, VersionInfo, Name);
            return builder.ToString();
        }

        public static String FormatErrorMessageBody(int statusCode, String appName)
        {
            String statusDescription = HttpWorkerRequest.GetStatusDescription(statusCode);
            String str2 = String.Format("{0}发生错误", appName);
            String str3 = String.Format("Http 错误 {0} - {1}", statusCode, statusDescription);
            return String.Format("<html>\r\n    <head>\r\n        <title>{0}</title>\r\n", statusDescription) + "        <style>\r\n        \tbody {font-family:\"Verdana\";font-weight:normal;font-size: 8pt;color:black;} \r\n        \tp {font-family:\"Verdana\";font-weight:normal;color:black;margin-top: -5px}\r\n        \tb {font-family:\"Verdana\";font-weight:bold;color:black;margin-top: -5px}\r\n        \th1 { font-family:\"Verdana\";font-weight:normal;font-size:18pt;color:red }\r\n        \th2 { font-family:\"Verdana\";font-weight:normal;font-size:14pt;color:maroon }\r\n        \tpre {font-family:\"Lucida Console\";font-size: 8pt}\r\n        \t.marker {font-weight: bold; color: black;text-decoration: none;}\r\n        \t.version {color: gray;}\r\n        \t.error {margin-bottom: 10px;}\r\n        \t.expandable { text-decoration:underline; font-weight:bold; color:navy; cursor:hand; }\r\n        </style>\r\n" + String.Format(_httpErrorFormat2, str2, str3, VersionInfo, Name);
        }
    }
}