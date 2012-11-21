<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Main.aspx.cs" Inherits="Pages_Main" %>

<%@ Import Namespace="System.Diagnostics" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="System.Reflection" %>
<%@ Import Namespace="NewLife" %>
<%@ Import Namespace="NewLife.Reflection" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title></title>
    <style type="text/css">
        body
        {
            margin-left: 0px;
            margin-top: 0px;
            margin-right: 0px;
            margin-bottom: 0px;
            font-size: 12px;
        }
        
        .tb
        {
            width: 100%;
            border-top: solid 1px #c6c6c6;
        }
        
        .tb td
        {
            font-size: 12px;
            line-height: 25px;
            padding: 0px 3px;
            border-bottom: solid 1px #dddddd;
        }
        
        .tb td.rightBorder
        {
            border-right: solid 1px #dddddd;
        }
        
        .tb td a
        {
            color: #0066aa;
            text-decoration: none;
        }
        .tb th
        {
            text-align: center;
            font-size: 12px;
            line-height: 27px;
            background-image: url(images/nbg.gif);
            color: #445055;
        }
        .tb th a
        {
            color: #445055;
            text-decoration: none;
        }
        .tb .btr td
        {
            background-color: #f9f9f9;
        }
        #info .name
        {
            text-align: right;
            width: 120px;
        }
        #info .value
        {
            text-align: left;
            color: Red;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <%if (Request.QueryString == null || Request.QueryString.Count <= 0)
      {  %>
    <table id="info" border="0" class="tb formtable" cellspacing="1" cellpadding="0"
        align="Center">
        <tr>
            <th colspan="4">
                服务器信息
            </th>
        </tr>
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
            <td class="value">
                <%= Request.ServerVariables["SERVER_NAME"]%>，
                <%= Request.ServerVariables["LOCAl_ADDR"] + ":" + Request.ServerVariables["Server_Port"]%>
            </td>
            <td class="name">
                计算机用户：
            </td>
            <td class="value">
                <%= Environment.UserName%>/<%= Environment.MachineName%>
            </td>
        </tr>
        <tr>
            <td class="name">
                应用程序域：
            </td>
            <td class="value">
                <%= AppDomain.CurrentDomain.FriendlyName %>
                <a href="?Act=Assembly" target="_blank" title="点击打开进程程序集列表">程序集列表</a>
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
                操作系统：
            </td>
            <td class="value">
                <%= Runtime.OSName %>
            </td>
            <td class="name">
                Web服务器：
            </td>
            <td class="value">
                <%= GetWebServerName()%>
            </td>
        </tr>
        <tr>
            <td class="name">
                处理器：
            </td>
            <td class="value">
                <%= Environment.ProcessorCount%>
                核心，
                <%= Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")%>
            </td>
            <td class="name">
                时间：
            </td>
            <td class="value" title="这里使用了服务器默认的时间格式！后面是开机时间。">
                <%= DateTime.Now%>，<%= new TimeSpan(Environment.TickCount)%>
            </td>
        </tr>
        <tr>
            <% Process process = Process.GetCurrentProcess(); %>
            <td class="name">
                内存：
            </td>
            <td class="value">
                工作集:<%= (process.WorkingSet64 / 1024).ToString("n0") + "KB"%>
                提交:<%= (process.PrivateMemorySize64 / 1024).ToString("n0") + "KB"%>
                GC:<%= (GC.GetTotalMemory(false) / 1024).ToString("n0") + "KB"%>
                <a href="?Act=ProcessModules" target="_blank" title="点击打开进程模块列表">模块列表</a>
            </td>
            <td class="name">
                进程时间：
            </td>
            <td class="value">
                <%= process.TotalProcessorTime.TotalSeconds.ToString("N2")%>秒 启动于<%= process.StartTime.ToString("yyyy-MM-dd HH:mm:ss")%>
            </td>
        </tr>
        <tr>
            <td class="name">
                Session：
            </td>
            <td class="value">
                <%= Session.Contents.Count%>个，<%= Session.Timeout%>分钟，SessionID：<%= Session.Contents.SessionID%>
            </td>
            <td class="name">
                Cache：
            </td>
            <td class="value">
                <%= Cache.Count%>个，可用：<%= (Cache.EffectivePrivateBytesLimit / 1024).ToString("n0")%>KB，<%= Cache.EffectivePercentagePhysicalMemoryLimit%>%
            </td>
        </tr>
    </table>
    <br />
    <br />
    <asp:GridView ID="gv" runat="server" AllowSorting="True" AutoGenerateColumns="False"
        OnSorting="gv_Sorting" CssClass="tb" BorderWidth="0px" CellPadding="0" 
        BorderStyle="None" GridLines="None">
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
    %>
    <table id="Table2" border="0" class="tb first" cellspacing="0" cellpadding="0" align="Center">
        <tr>
            <th colspan="7">
                进程模块(<%=process.ProcessName %>, PID=<%=process.Id %>)
                <% if (!isAll)
                   { %>
                （<a href="?Act=ProcessModules&Mode=All">完整</a>，仅用户）：<%}
                   else
                   { %>（完整，<a href="?Act=ProcessModules&Mode=OnlyUser">仅用户</a>）：<%} %>
            </th>
        </tr>
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
    %>
    <table id="Table1" border="0" class="tb first" cellspacing="0" cellpadding="0" align="Center">
        <tr>
            <th colspan="6">
                程序集列表(<%=AppDomain.CurrentDomain.FriendlyName %>)
            </th>
        </tr>
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
    </form>
</body>
</html>
