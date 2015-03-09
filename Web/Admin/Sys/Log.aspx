<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Log.aspx.cs" Inherits="Pages_Log"
    Title="日志查看" MasterPageFile="~/Admin/ManagerPage.master" EnableEventValidation="false" %>

<%@ Import Namespace="NewLife.CommonEntity" %>
<asp:Content ID="Content1" ContentPlaceHolderID="C" runat="server">
    <div class="panel panel-default">
        <div class="panel-heading">日志列表</div>
        <div class="panel-body">
            <div class="form-inline">
                <div class="form-group">
                    <label for="<%=this.ddlCategory.ClientID %>" class="control-label">类别：</label>
                    <asp:DropDownList ID="ddlCategory" runat="server" AppendDataBoundItems="True"
                        DataSourceID="odsCategory" DataTextField="Category" CssClass="form-control" DataValueField="Category">
                        <asp:ListItem>全部</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <label class="control-label" for="<%=this.ddlAdmin.ClientID %>">管理员：</label>
                    <asp:DropDownList ID="ddlAdmin" runat="server" AppendDataBoundItems="True"
                        DataTextField="Name" DataValueField="ID" CssClass="form-control">
                        <asp:ListItem Value="0">全部</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <label class="control-label" for="<%=this.key.ClientID %>">关键字：</label>
                    <asp:TextBox ID="key" runat="server" CssClass="form-control"></asp:TextBox>
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
                <input name="BtnSearch" value="查询", class="btn btn-primary" />
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
