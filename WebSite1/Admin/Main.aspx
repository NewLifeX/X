<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Main.aspx.cs" Inherits="Pages_Main" %>

<%@ Import Namespace="System.Diagnostics" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="System.Reflection" %>
<%@ Import Namespace="NewLife.Reflection" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title></title>
    <style type="text/css">
        #info .name
        {
            text-align: right;
            width: 120px;
        }
        
        #info .value
        {
            text-align: left;
            width: 250px;
            color: Red;
        }
        a
        {
            text-decoration: none;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <%if (Request.QueryString == null || Request.QueryString.Count <= 0)
          {  %>服务器信息：
        <table border="0" id="info">
            <tr>
                <td class="name">
                    应用系统：
                </td>
                <td class="value">
                    <%= HttpRuntime.AppDomainAppVirtualPath%>
                </td>
                <td class="name">
                    目录：
                </td>
                <td class="value">
                    <%= HttpRuntime.AppDomainAppPath%>
                </td>
            </tr>
            <tr>
                <td class="name">
                    域名地址：
                </td>
                <td class="value" colspan="3">
                    <%= Request.ServerVariables["SERVER_NAME"]%>，
                    <%= Request.ServerVariables["LOCAl_ADDR"] + ":" + Request.ServerVariables["Server_Port"]%>
                </td>
            </tr>
            <tr>
                <td class="name">
                    计算机名：
                </td>
                <td class="value">
                    <%= Environment.MachineName%>
                </td>
                <td class="name">
                    用户名：
                </td>
                <td class="value">
                    <%= Environment.UserName%>
                </td>
            </tr>
            <tr>
                <td class="name">
                    应用程序域：
                </td>
                <td class="value" colspan="3">
                    <%= AppDomain.CurrentDomain.FriendlyName %>
                    <a href="?Act=Assembly" target="_blank" title="点击打开进程程序集列表">程序集列表</a>
                </td>
            </tr>
            <tr>
                <td class="name">
                    操作系统：
                </td>
                <td class="value">
                    <%= Environment.OSVersion%>
                </td>
                <td class="name">
                    Web服务器：
                </td>
                <td class="value">
                    <%= Request.ServerVariables["Server_SoftWare"]%><%if (HttpRuntime.UsingIntegratedPipeline)
                                                                      { %>
                    集成管道<%} %>
                </td>
            </tr>
            <tr>
                <td class="name">
                    系统版本：
                </td>
                <td class="value">
                    <%= Environment.Version%>
                </td>
                <td class="name">
                    .Net 版本：
                </td>
                <td class="value">
                    <%= Environment.Version%>
                </td>
            </tr>
            <tr>
                <td class="name">
                    当前时间：
                </td>
                <td class="value" title="这里使用了服务器默认的时间格式！">
                    <%= DateTime.Now%>
                </td>
                <td class="name">
                    开机时间：
                </td>
                <td class="value">
                    <%= new TimeSpan(Environment.TickCount)%>
                </td>
            </tr>
            <tr>
                <td class="name">
                    CPU 总数：
                </td>
                <td class="value" colspan="3">
                    <%= Environment.ProcessorCount%>
                    核心，
                    <%= Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")%>
                </td>
            </tr>
            <tr>
                <td class="name">
                    虚拟内存：
                </td>
                <td class="value">
                    <%= ((Double)Environment.WorkingSet / 1048576).ToString("n2") + "M"%>
                </td>
                <td class="name">
                    当前占用：
                </td>
                <td class="value">
                    <%= ((Double)GC.GetTotalMemory(false) / 1048576).ToString("n2") + "M"%>
                </td>
            </tr>
            <tr>
                <% Process process = Process.GetCurrentProcess(); %>
                <td class="name">
                    ASP.Net 内存：
                </td>
                <td class="value">
                    <%= ((Double)process.WorkingSet64 / 1048576).ToString("N2") + "M"%>
                    <a href="?Act=ProcessModules" target="_blank" title="点击打开进程模块列表">模块列表</a>
                </td>
                <td class="name">
                    ASP.Net CPU：
                </td>
                <td class="value">
                    <%= ((TimeSpan)process.TotalProcessorTime).TotalSeconds.ToString("N2")%>秒 启动于<%= process.StartTime%>
                </td>
            </tr>
            <tr>
                <td class="name">
                    Session数：
                </td>
                <td class="value">
                    <%= Session.Contents.Count%>，<%= Session.Timeout%>分钟
                </td>
                <td class="name">
                    SessionID：
                </td>
                <td class="value">
                    <%= Session.Contents.SessionID%>
                </td>
            </tr>
            <tr>
                <td class="name">
                    Cache数：
                </td>
                <td class="value">
                    <%= Cache.Count%>
                </td>
                <td class="name">
                    可用：
                </td>
                <td class="value">
                    <%= ((Double)Cache.EffectivePrivateBytesLimit / 1048576).ToString("n2")%>M，<%= Cache.EffectivePercentagePhysicalMemoryLimit%>%
                </td>
            </tr>
        </table>
        <br />
        <br />
        程序集版本信息：
        <asp:GridView ID="GridView1" runat="server" AllowSorting="True" AutoGenerateColumns="False"
            OnSorting="GridView1_Sorting">
            <Columns>
                <asp:BoundField DataField="Name" HeaderText="名称" SortExpression="Name" />
                <asp:BoundField DataField="Title" HeaderText="标题" SortExpression="Title" />
                <asp:BoundField DataField="FileVersion" HeaderText="文件版本" SortExpression="FileVersion" />
                <asp:BoundField DataField="Version" HeaderText="内部版本" SortExpression="Version" />
                <asp:BoundField DataField="Compile" DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" HeaderText="编译时间"
                    SortExpression="Compile" />
            </Columns>
        </asp:GridView>
        <%}
          if (Request["Act"] == "ProcessModules")
          {
              Boolean isAll = String.Equals("All", Request["Mode"], StringComparison.OrdinalIgnoreCase);

              Process process = Process.GetCurrentProcess();
              List<ProcessModule> list = new List<ProcessModule>();
              foreach (ProcessModule item in process.Modules)
              {
                  if (!isAll && item.FileVersionInfo.CompanyName == "Microsoft Corporation") continue;

                  list.Add(item);
              }
        %>进程模块(<%=process.ProcessName %>, PID=<%=process.Id %>)<% if (!isAll)
                                                                  { %>
        （<a href="?Act=ProcessModules&Mode=All">完整</a>，仅用户）：<%}
                                                                  else
                                                                  { %>（完整，<a href="?Act=ProcessModules&Mode=OnlyUser">仅用户</a>）：<%} %>
        <table border="0">
            <thead>
                <tr>
                    <th>
                        模块名称
                    </th>
                    <th>
                        公司名称
                    </th>
                    <th>
                        产品名称
                    </th>
                    <th>
                        描述
                    </th>
                    <th>
                        版本
                    </th>
                    <th>
                        大小
                    </th>
                    <th>
                        路径
                    </th>
                </tr>
            </thead>
            <% foreach (ProcessModule item in list)
               {
            %><tr>
                <td>
                    <%= item.ModuleName %>
                </td>
                <td>
                    <%= item.FileVersionInfo.CompanyName %>
                </td>
                <td>
                    <%= item.FileVersionInfo.ProductName %>
                </td>
                <td>
                    <%= item.FileVersionInfo.FileDescription %>
                </td>
                <td>
                    <%= item.FileVersionInfo.FileVersion %>
                </td>
                <td>
                    <%= item.ModuleMemorySize %>
                </td>
                <td>
                    <%= item.FileName %>
                </td>
            </tr>
            <%} %>
        </table>
        <%} %>
        <%
            if (Request["Act"] == "Assembly")
            {
        %>程序集列表(<%=AppDomain.CurrentDomain.FriendlyName %>)
        <table border="0">
            <thead>
                <tr>
                    <th>
                        名称
                    </th>
                    <th>
                        标题
                    </th>
                    <th>
                        文件版本
                    </th>
                    <th>
                        内部版本
                    </th>
                    <th>
                        编译时间
                    </th>
                    <th>
                        路径
                    </th>
                </tr>
            </thead>
            <% foreach (AssemblyX item in AssemblyX.GetAssemblies())
               {
            %><tr>
                <td>
                    <%= item.Name %>
                </td>
                <td>
                    <%= item.Title %>
                </td>
                <td>
                    <%= item.FileVersion%>
                </td>
                <td>
                    <%= item.Version %>
                </td>
                <td>
                    <%= item.Compile %>
                </td>
                <td>
                    <%
                        String location = String.Empty;
                        try
                        {
                            location = item.Asm.Location;
                        }
                        catch { }
                    %>
                    <%= location %>
                </td>
            </tr>
            <%} %>
        </table>
        <%} %>
    </div>
    </form>
</body>
</html>
