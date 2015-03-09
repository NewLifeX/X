<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Log.aspx.cs" Inherits="Pages_Log"
    Title="日志查看" MasterPageFile="~/Admin/ManagerPage.master" EnableEventValidation="false" EnableViewState="False" %>

<%@ Import Namespace="NewLife.CommonEntity" %>
<asp:Content ID="Content1" ContentPlaceHolderID="C" runat="server">
    <div class="panel panel-default">
        <div class="panel-heading"><a href="?">日志列表</a></div>
        <div class="panel-body">
            <div class="form-inline">
                <div class="form-group">
                    <label for="ddlCategory" class="control-label">类别：</label>
                    <select name="ddlCategory" id="ddlCategory" class="form-control">
                        <option value="全部">全部</option>
                        <%foreach (ILog item in Log.FindAllCategory())
                          {
                        %><option value="<%= item.Category %>"><%= item.Category %></option>
                        <%
                          } %>
                    </select>
                </div>
                <div class="form-group">
                    <label class="control-label" for="ddlAdmin">管理员：</label>
                    <select name="ddlAdmin" id="ddlAdmin" class="form-control">
                        <option value="全部">全部</option>
                        <%foreach (IAdministrator item in Administrator.FindAllWithCache())
                          {
                        %><option value="<%= item.ID %>"><%= item.FriendName %></option>
                        <%
                          } %>
                    </select>
                </div>
                <div class="form-group">
                    <label class="control-label" for="key">关键字：</label>
                    <input name="key" type="text" id="key" class="form-control" />
                </div>
                <div class="form-group">
                    <label for="<%=this.StartDate.ClientID %>" class="control-label">时间：</label>
                    <XCL:DateTimePicker ID="StartDate" runat="server" LongTime="False" CssClass="form-control">
                    </XCL:DateTimePicker>
                </div>
                <div class="form-group">
                    <label class="control-label" for="<%=this.EndDate.ClientID %>">至</label>
                    <XCL:DateTimePicker ID="EndDate" runat="server" LongTime="False" CssClass="form-control">
                    </XCL:DateTimePicker>
                </div>
                <input name="BtnSearch" value="查询" type="submit" class="btn btn-primary" />
            </div>
        </div>
        <div class="table-responsive">
            <table class="table table-bordered table-hover table-striped table-condensed">
                <thead>
                    <tr>
                        <%= grid.RenderHeader(false, "ID:序号","Category:类别","Action:操作","UserID:管理员","IP:IP地址","OccurTime:时间","详细信息") %>
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
            <p><%= grid.RenderPage() %></p>
        </div>
    </div>
    <asp:ObjectDataSource ID="odsCategory" runat="server" OldValuesParameterFormatString="original_{0}"
        SelectMethod="FindAllCategory" TypeName=""></asp:ObjectDataSource>
    <XCL:GridViewExtender ID="gvExt" runat="server">
    </XCL:GridViewExtender>
</asp:Content>
