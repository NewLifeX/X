<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Main.aspx.cs" Inherits="Admin_Main"
    MasterPageFile="~/Admin/MasterPage.master" %>

<%@ Import Namespace="System.Diagnostics" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="System.Reflection" %>
<%@ Import Namespace="NewLife" %>
<%@ Import Namespace="NewLife.Reflection" %>
<asp:Content ID="Content1" ContentPlaceHolderID="H" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="C" runat="server">
    <%if (Request.QueryString == null || Request.QueryString.Count <= 0)
      {  %>
    <div class="row-filuid">
        <div class="widget-box">
            <div class="widget-title">
                <span class="icon"><i class="icon-th"></i></span>
                <h5>
                    服务器信息</h5>
            </div>
            <div class="widget-content nopadding">
                <table class="table table-bordered table-striped">
                    <tbody>
                        <tr>
                            <td>
                                应用系统：
                            </td>
                            <td>
                                <%= HttpRuntime.AppDomainAppVirtualPath%>&nbsp;<a href="?Act=Restart" onclick="return confirm('仅重启ASP.Net应用程序域，而不是操作系统！\n确认重启？')">重启应用系统</a>
                            </td>
                            <td>
                                目录：
                            </td>
                            <td>
                                <%= HttpRuntime.AppDomainAppPath%>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                域名地址：
                            </td>
                            <td>
                                <%= Request.ServerVariables["SERVER_NAME"]%>，
                                <%= Request.ServerVariables["LOCAl_ADDR"] + ":" + Request.ServerVariables["Server_Port"]%>
                                &nbsp;<=[<%= Request.ServerVariables["REMOTE_HOST"]%>]
                            </td>
                            <td>
                                计算机用户：
                            </td>
                            <td>
                                <%= Environment.UserName%>/<%= Environment.MachineName%>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                应用程序域：
                            </td>
                            <td>
                                <%= AppDomain.CurrentDomain.FriendlyName %>
                                <a href="?Act=Assembly" target="_blank" title="点击打开进程程序集列表">程序集列表</a>
                            </td>
                            <td>
                                .Net 版本：
                            </td>
                            <td>
                                <%= Environment.Version%>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                操作系统：
                            </td>
                            <td>
                                <%= Runtime.OSName %>
                            </td>
                            <td>
                                Web服务器：
                            </td>
                            <td>
                                <%= GetWebServerName()%>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                处理器：
                            </td>
                            <td>
                                <%= Environment.ProcessorCount%>
                                核心，
                                <%= Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")%>
                            </td>
                            <td>
                                时间：
                            </td>
                            <td title="这里使用了服务器默认的时间格式！后面是开机时间。">
                                <%= DateTime.Now%>，<%= new TimeSpan(Environment.TickCount)%>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                内存：
                            </td>
                            <td>
                                <% Process process = Process.GetCurrentProcess(); %>
                                工作集:<%= (process.WorkingSet64 / 1024).ToString("n0") + "KB"%>
                                提交:<%= (process.PrivateMemorySize64 / 1024).ToString("n0") + "KB"%>
                                GC:<%= (GC.GetTotalMemory(false) / 1024).ToString("n0") + "KB"%>
                                <a href="?Act=ProcessModules" target="_blank" title="点击打开进程模块列表">模块列表</a>
                            </td>
                            <td>
                                进程时间：
                            </td>
                            <td>
                                <%= process.TotalProcessorTime.TotalSeconds.ToString("N2")%>秒 启动于<%= process.StartTime.ToString("yyyy-MM-dd HH:mm:ss")%>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                Session：
                            </td>
                            <td>
                                <%= Session.Contents.Count%>个，<%= Session.Timeout%>分钟，SessionID：<%= Session.Contents.SessionID%>
                            </td>
                            <td>
                                Cache：
                            </td>
                            <td>
                                <%= Cache.Count%>个，可用：<%= (Cache.EffectivePrivateBytesLimit / 1024).ToString("n0")%>KB
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
        <div class="widget-box">
            <div class="widget-content nopadding">
                <asp:GridView ID="gv" runat="server" AllowSorting="True" AutoGenerateColumns="False"
                    CssClass="table table-bordered table-striped" BorderWidth="0px" CellPadding="0"
                    BorderStyle="None" GridLines="None">
                    <Columns>
                        <asp:BoundField DataField="Name" HeaderText="名称">
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:BoundField>
                        <asp:BoundField DataField="Title" HeaderText="标题" HeaderStyle-CssClass="widget-title"
                            HeaderStyle-Font-Size="Small" />
                        <asp:BoundField DataField="FileVersion" HeaderText="文件版本" HeaderStyle-CssClass="widget-title"
                            HeaderStyle-Font-Size="Small" />
                        <asp:BoundField DataField="Version" HeaderText="内部版本" HeaderStyle-CssClass="widget-title"
                            HeaderStyle-Font-Size="Small" />
                        <asp:BoundField DataField="Compile" DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" HeaderText="编译时间"
                            HeaderStyle-CssClass="widget-title" HeaderStyle-Font-Size="Small" />
                        <asp:TemplateField></asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </div>
    <%}%>
</asp:Content>
