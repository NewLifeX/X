<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Log.aspx.cs" Inherits="Pages_Log"
    Title="日志查看" MasterPageFile="~/Admin/Ace.master" EnableEventValidation="false" EnableViewState="False" %>

<%@ Import Namespace="NewLife.Web" %>
<%@ Import Namespace="NewLife.CommonEntity" %>
<asp:Content ID="Content1" ContentPlaceHolderID="C" runat="server">
    <div class="panel panel-default">
        <div class="panel-heading"><a href="<%= WebHelper.PageName %>">日志列表</a></div>
        <div class="panel-body">
            <div class="form-inline">
                <div class="form-group">
                    <label for="ddlCategory" class="control-label">类别：</label>
                    <select name="ddlCategory" id="ddlCategory" class="form-control" onchange="$(':submit').click();">
                        <option value="">全部</option>
                        <%foreach (ILog item in Log.FindAllCategory())
                          {
                        %><option value="<%= item.Category %>" <%if (WebHelper.Params["ddlCategory"] == item.Category)
                                                                 {%>
                            selected<%} %>><%= item.Category %></option>
                        <%
                          } %>
                    </select>
                </div>
                <div class="form-group">
                    <label class="control-label" for="ddlAdmin">管理员：</label>
                    <select name="ddlAdminID" id="ddlAdminID" class="form-control" onchange="$(':submit').click();">
                        <option value="">全部</option>
                        <%foreach (IAdministrator item in Administrator.FindAllWithCache())
                          {
                        %><option value="<%= item.ID %>" <%if (WebHelper.Params["ddlAdminID"] == item.ID + "")
                                                           {%>
                            selected<%} %>><%= item.FriendName %></option>
                        <%
                          } %>
                    </select>
                </div>
                <div class="form-group">
                    <label class="control-label" for="key">关键字：</label>
                    <input name="key" type="text" id="key" value="<%= Request["key"] %>" class="form-control" />
                </div>
                <div class="form-group">
                    <label for="dtStart" class="control-label">时间：</label>
                    <input name="dtStart" type="date" id="dtStart" value="<%= WebHelper.Params["dtStart"] %>" class="Wdate form-control" style="width: 86px;" onfocus="WdatePicker({autoPickDate:true,skin:'default',lang:'auto',readOnly:true})" />
                </div>
                <div class="form-group">
                    <label class="control-label" for="<%=this.dtEnd.ClientID %>">至</label>
                    <XCL:DateTimePicker ID="dtEnd" runat="server" LongTime="False" CssClass="form-control">
                    </XCL:DateTimePicker>
                </div>
                <input id="btnSearch" value="查询" type="submit" class="btn btn-primary" />
            </div>
        </div>
        <div class="table-responsive">
            <table class="table table-bordered table-hover table-striped table-condensed">
                <thead>
                    <tr>
                        <th style="width: 50px;"><a href="<%= grid.GetSortUrl("ID") %>">序号</a></th>
                        <th style="width: 120px;"><a href="<%= grid.GetSortUrl("Category") %>">类别</a></th>
                        <th style="width: 120px;"><a href="<%= grid.GetSortUrl("Action") %>">操作</a></th>
                        <th style="width: 120px;"><a href="<%= grid.GetSortUrl("UserID") %>">管理员</a></th>
                        <th style="width: 120px;"><a href="<%= grid.GetSortUrl("IP") %>">IP地址</a></th>
                        <th style="width: 140px;"><a href="<%= grid.GetSortUrl("OccurTime") %>">时间</a></th>
                        <th>详细信息</th>
                    </tr>
                </thead>
                <tbody>
                    <%foreach (Log entity in grid.DataSource)
                      {
                    %>
                    <tr>
                        <td><%= entity.ID %></td>
                        <td><%= entity.Category %></td>
                        <td><%= entity.Action %></td>
                        <td><%= entity.UserName %></td>
                        <td><%= entity.IP %></td>
                        <td><%= entity.OccurTime %></td>
                        <td><%= entity.Remark %></td>
                    </tr>
                    <%
                      } %>
                </tbody>
            </table>
        </div>
        <div class="panel-footer">
            <p>
                <%= grid.RenderPage() %>
            页大小
                <select id="PageSize" name="PageSize" onchange="$(':submit').click();">
                    <option value="10">10</option>
                    <option value="20">20</option>
                    <option value="30">30</option>
                    <option value="50">50</option>
                    <option value="100">100</option>
                </select>
                <script type="text/javascript">
                    $(function () {
                        $('#PageSize').val(<%= grid.PageSize %>);
                    });
                </script>
            </p>
        </div>
    </div>
</asp:Content>
